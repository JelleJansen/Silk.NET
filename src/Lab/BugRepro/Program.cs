using System;
using Silk.NET.Input;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Common;

namespace BugRepro
{
    class Program
    {
        public static unsafe void Main()
        {
            using var window = Window.Create(WindowOptions.DefaultVulkan);

            window.Initialize();

            var input = window.CreateInput();

            while (true)
            {
                window.DoEvents();

                if (window.IsClosing)
                    break;

                window.DoUpdate();
                window.DoRender();
            }

            window.DoEvents();
            window.Reset();
        }
    }
}
