﻿// This file is part of Silk.NET.
// 
// You may modify and distribute Silk.NET under the terms
// of the MIT license. See the LICENSE file for details.

using System;

namespace Silk.NET.Core.Native
{
    public enum PinMode
    {
        Persist,
        UntilNextCall
    }
    
    [AttributeUsage(AttributeTargets.Parameter)]
    public class PinObjectAttribute : Attribute
    {
        public PinObjectAttribute(PinMode mode = PinMode.Persist)
        {
            Mode = mode;
        }

        public PinMode Mode { get; }
    }
}