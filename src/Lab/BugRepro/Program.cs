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

            var fgc = false;
            var input = window.CreateInput();

            while (true)
            {
                window.DoEvents();

                if (window.IsClosing)
                    break;

                if (!fgc)
                {
                    GC.Collect();
                    fgc = true;
                }

                window.DoUpdate();
                window.DoRender();
            }

            window.DoEvents();
            window.Reset();
        }
    }
}
