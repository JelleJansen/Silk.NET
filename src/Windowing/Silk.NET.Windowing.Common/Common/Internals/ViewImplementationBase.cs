﻿// This file is part of Silk.NET.
// 
// You may modify and distribute Silk.NET under the terms
// of the MIT license. See the LICENSE file for details.

using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using Silk.NET.Core.Contexts;

// No immediate urgency to document these as they're not intended for public consumption.
#pragma warning disable 1591

namespace Silk.NET.Windowing.Common.Internals
{
    /// <summary>
    /// Abstracts away common view functions to ease implementation of the windowing API.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class ViewImplementationBase : IView
    {
        private const int InitialInvocationRental = 2;
        
        // Cache the options for when the window is closed
        private ViewOptions _optionsCache;
        
        // Game loop fields
        private readonly Stopwatch _renderStopwatch = new Stopwatch();
        private readonly Stopwatch _updateStopwatch = new Stopwatch();
        private readonly Stopwatch _lifetimeStopwatch = new Stopwatch();
        private double _renderPeriod;
        private double _updatePeriod;

        // Invocations
        private readonly ArrayPool<object> _returnArrayPool = ArrayPool<object>.Create();
        private PendingInvocation[] _pendingInvocations;
        private int _rented;

        // Ensure we keep SwapInterval up-to-date
        private bool _swapIntervalChanged = true;

        /// <summary>
        /// Creates a base view with the given options.
        /// </summary>
        /// <param name="opts">The options, used to configure the view.</param>
        protected ViewImplementationBase(ViewOptions opts)
        {
            _optionsCache = opts;
            _renderPeriod = 1 / opts.FramesPerSecond;
            _updatePeriod = 1 / opts.UpdatesPerSecond;
        }
        
        // Property bases - these have extra functionality baked into their getters and setters
        protected abstract Size CoreSize { get; }
        protected abstract IntPtr CoreHandle { get; }
        
        // Function bases - again extra functionality on top
        protected abstract void CoreInitialize(ViewOptions opts);
        protected abstract void CoreReset();

        // Other APIs implemented abstractly
        public abstract IGLContext? GLContext { get; }
        public abstract IVkSurface? VkSurface { get; }
        public abstract bool IsClosing { get; }
        public abstract VideoMode VideoMode { get; }
        public abstract bool IsEventDriven { get; set; }
        public abstract void DoEvents();
        public abstract void ContinueEvents();
        public abstract void Dispose();
        public abstract Point PointToClient(Point point);
        public abstract Point PointToScreen(Point point);
        public abstract void Close();
        protected abstract void RegisterCallbacks();
        protected abstract void UnregisterCallbacks();
        
        // Events
        public abstract event Action<Size> Resize;
        public abstract event Action Closing;
        public abstract event Action<bool> FocusChanged;
        public event Action Load;
        public event Action<double> Update;
        public event Action<double> Render;
        
        // Lifetime controls
        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            CoreInitialize(_optionsCache);
            RegisterCallbacks();
            EnsureArrayIsReady(-1);
            _renderStopwatch.Start();
            _updateStopwatch.Start();
            _lifetimeStopwatch.Start();
            IsInitialized = true;
            Load?.Invoke();
        }
        public void Reset()
        {
            _renderStopwatch.Reset();
            _updateStopwatch.Reset();
            _lifetimeStopwatch.Reset();
            CoreReset();
            UnregisterCallbacks();
            IsInitialized = false;
        }

        // Game loop controls
        public double FramesPerSecond { get => 1 / _renderPeriod; set => _renderPeriod = 1 / value; }
        public double UpdatesPerSecond { get => 1 / _updatePeriod; set => _updatePeriod = 1 / value; }
        public bool ShouldSwapAutomatically => _optionsCache.ShouldSwapAutomatically /* TODO set? */;
        
        // Cache controls for derived classes
        protected VideoMode CachedVideoMode
        {
            get => _optionsCache.VideoMode;
            set => _optionsCache.VideoMode = value;
        }

        // Game loop implementation
        public void DoRender()
        {
            DoInvokes();
            var delta = _renderStopwatch.Elapsed.TotalSeconds;
            if ((delta >= _renderPeriod) || VSync)
            {
                if (!(GLContext is null) && !GLContext.IsCurrent)
                {
                    GLContext.MakeCurrent();
                }
                
                if (_swapIntervalChanged)
                {
                    GLContext?.SwapInterval(VSync ? 1 : 0);
                    _swapIntervalChanged = false;
                }
                
                Render?.Invoke(_renderStopwatch.Elapsed.TotalSeconds);
                if (ShouldSwapAutomatically)
                {
                    GLContext?.SwapBuffers();
                }
                
                _renderStopwatch.Restart();
            }
        }

        public void DoUpdate()
        {
            var delta = _updateStopwatch.Elapsed.TotalSeconds;
            if (delta >= _updatePeriod)
            {
                Update?.Invoke(_updateStopwatch.Elapsed.TotalSeconds);
                _updateStopwatch.Restart();
            }
        }
        
        // Misc properties
        protected bool IsInitialized { get; private set; }
        public Size Size => IsInitialized ? CoreSize : default;
        public IntPtr Handle => IsInitialized ? CoreHandle : IntPtr.Zero;
        public GraphicsAPI API => _optionsCache.API;
        public double Time => _lifetimeStopwatch.Elapsed.TotalSeconds;
        public int? PreferredDepthBufferBits => _optionsCache.PreferredDepthBufferBits;

        public bool VSync
        {
            get => _optionsCache.VSync;
            set
            {
                _swapIntervalChanged = true;
                _optionsCache.VSync = value;
            }
        }

        // Invoke system
        public object Invoke(Delegate d, params object[] args)
        {
            var rentalIndex = Interlocked.Increment(ref _rented) - 1;
            EnsureArrayIsReady(rentalIndex);
            ref var x = ref _pendingInvocations[rentalIndex];
            x.Delegate = d;
            x.Data = args;
            x.ResetEvent.Reset();
            x.IsComplete = false;
            x.ResetEvent.Wait();
            var ret = x.Data[0];
            _returnArrayPool.Return(x.Data);
            x.Delegate = null;
            x.Data = null;
            return ret;
        }

        public void DoInvokes()
        {
            var completed = 0;
            for (var i = 0; i < _rented + completed && i < _pendingInvocations.Length; i++)
            {
                ref var invocation = ref _pendingInvocations[i];
                if (invocation.IsComplete)
                {
                    completed++;
                }
                else
                {
                    var ret = _returnArrayPool.Rent(1);
                    ret[0] = invocation.Delegate.DynamicInvoke(invocation.Data);
                    invocation.Data = ret;
                    invocation.IsComplete = true;
                    invocation.ResetEvent.Set();
                    Interlocked.Decrement(ref _rented);
                }
            }
        }

        private void EnsureArrayIsReady(int rentalIndex)
        {
            _pendingInvocations ??= new PendingInvocation[InitialInvocationRental];

            if (rentalIndex == -1)
            {
                return;
            }

            var finalSize = _pendingInvocations.Length;
            while (rentalIndex + 1 > finalSize)
            {
                finalSize *= 2;
            }

            if (finalSize == _pendingInvocations.Length)
            {
                return;
            }

            var na = new PendingInvocation[finalSize];
            var og = Interlocked.Exchange(ref _pendingInvocations, na);
            og.CopyTo(na, 0);
            for (var i = 0; i < na.Length; i++)
            {
                na[i].ResetEvent ??= new ManualResetEventSlim();
            }
        }

        private struct PendingInvocation
        {
            public bool IsComplete { get; set; }
            public Delegate Delegate { get; set; }
            public object[] Data { get; set; }
            public ManualResetEventSlim ResetEvent { get; set; }
        }
    }
}
