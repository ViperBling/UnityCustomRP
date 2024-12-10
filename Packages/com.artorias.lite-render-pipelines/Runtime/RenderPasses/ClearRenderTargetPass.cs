using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

using LiteRP.FrameData;

namespace LiteRP
{
    public partial class LiteRGRecorder
    {
        private static readonly ProfilingSampler s_ClearRTPassSampler = new ProfilingSampler("LiteRP.ClearRT");
        
        internal class ClearRTPassData
        {
            internal RTClearFlags clearFlags;
            internal Color clearColor;
        }

        private void AddClearRTPass(RenderGraph rg, CameraData cameraData)
        {
            using var rgBuilder = rg.AddRasterRenderPass<ClearRTPassData>(s_ClearRTPassSampler.name, out var passData, s_ClearRTPassSampler);
            
            passData.clearFlags = cameraData.GetClearFlags();
            passData.clearColor = cameraData.GetClearColor();
            
            if (m_BackBufferColorHandle.IsValid())
            {
                rgBuilder.SetRenderAttachment(m_BackBufferColorHandle, 0, AccessFlags.Write);
            }
            if (m_BackBufferDepthHandle.IsValid())
            {
                rgBuilder.SetRenderAttachmentDepth(m_BackBufferDepthHandle, AccessFlags.Write);
            }
            
            rgBuilder.AllowPassCulling(false);
            
            rgBuilder.SetRenderFunc<ClearRTPassData>((clearRTPassData, rgContext) =>
            {
                rgContext.cmd.ClearRenderTarget(clearRTPassData.clearFlags, clearRTPassData.clearColor, 1, 0);
            });
        }
    }
}