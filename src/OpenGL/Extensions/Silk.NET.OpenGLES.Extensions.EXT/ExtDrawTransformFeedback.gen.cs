// This file is part of Silk.NET.
// 
// You may modify and distribute Silk.NET under the terms
// of the MIT license. See the LICENSE file for details.
using System;
using System.Runtime.InteropServices;
using System.Text;
using Silk.NET.OpenGLES;
using Silk.NET.Core.Loader;
using Silk.NET.Core.Native;
using Silk.NET.Core.Attributes;
using Ultz.SuperInvoke;

namespace Silk.NET.OpenGLES.Extensions.EXT
{
    [Extension("EXT_draw_transform_feedback")]
    public abstract unsafe partial class ExtDrawTransformFeedback : NativeExtension<GL>
    {
        /// <inheritdoc />
        [NativeApi(EntryPoint = "glDrawTransformFeedbackEXT")]
        public abstract void DrawTransformFeedback([Flow(FlowDirection.In)] EXT mode, [Flow(FlowDirection.In)] uint id);

        /// <inheritdoc />
        [NativeApi(EntryPoint = "glDrawTransformFeedbackInstancedEXT")]
        public abstract void DrawTransformFeedbackInstanced([Flow(FlowDirection.In)] EXT mode, [Flow(FlowDirection.In)] uint id, [Flow(FlowDirection.In)] uint instancecount);

        /// <inheritdoc />
        [NativeApi(EntryPoint = "glDrawTransformFeedbackEXT")]
        public abstract void DrawTransformFeedback([Flow(FlowDirection.In)] PrimitiveType mode, [Flow(FlowDirection.In)] uint id);

        /// <inheritdoc />
        [NativeApi(EntryPoint = "glDrawTransformFeedbackInstancedEXT")]
        public abstract void DrawTransformFeedbackInstanced([Flow(FlowDirection.In)] PrimitiveType mode, [Flow(FlowDirection.In)] uint id, [Flow(FlowDirection.In)] uint instancecount);

        public ExtDrawTransformFeedback(ref NativeApiContext ctx)
            : base(ref ctx)
        {
        }
    }
}
