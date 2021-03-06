// This file is part of Silk.NET.
// 
// You may modify and distribute Silk.NET under the terms
// of the MIT license. See the LICENSE file for details.
using System;
using System.Runtime.InteropServices;
using System.Text;
using Silk.NET.OpenGL;
using Silk.NET.Core.Loader;
using Silk.NET.Core.Native;
using Silk.NET.Core.Attributes;
using Ultz.SuperInvoke;

#pragma warning disable 1591

namespace Silk.NET.OpenGL.Extensions.ARB
{
    [Extension("ARB_shader_storage_buffer_object")]
    public abstract unsafe partial class ArbShaderStorageBufferObject : NativeExtension<GL>
    {
        public const string ExtensionName = "ARB_shader_storage_buffer_object";
        /// <summary>
        /// To be added.
        /// </summary>
        /// <param name="program">
        /// To be added.
        /// </param>
        /// <param name="storageBlockIndex">
        /// To be added.
        /// </param>
        /// <param name="storageBlockBinding">
        /// To be added.
        /// </param>
        [NativeApi(EntryPoint = "glShaderStorageBlockBinding")]
        public abstract void ShaderStorageBlockBinding([Flow(FlowDirection.In)] uint program, [Flow(FlowDirection.In)] uint storageBlockIndex, [Flow(FlowDirection.In)] uint storageBlockBinding);

        public ArbShaderStorageBufferObject(ref NativeApiContext ctx)
            : base(ref ctx)
        {
        }
    }
}

