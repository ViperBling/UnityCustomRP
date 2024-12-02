using UnityEngine;
using UnityEngine.Rendering;

namespace LiteRP
{
    public static class LiteRPRenderGraphUtils
    {
        public static bool IsNativeRenderPassSupport()
        {
            return SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D12 &&
                   SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES3 &&
                   SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLCore;
        }
    }
}