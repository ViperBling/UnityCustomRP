using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace LiteRP
{
    public partial class LiteRGRecorder
    {
        private static readonly ProfilingSampler s_SetupCameraPropsSampler = new ProfilingSampler("LiteRP.SetupCameraProps");
        
        internal class SetupCameraPropsData
        {
            internal CameraData cameraData;
        }

        private void AddSetupCameraPropsPass(RenderGraph rg, CameraData cameraData)
        {
            using var rgBuilder = rg.AddRasterRenderPass<SetupCameraPropsData>(s_SetupCameraPropsSampler.name, out var passData, s_SetupCameraPropsSampler);
            
            passData.cameraData = cameraData;
            
            rgBuilder.AllowPassCulling(false);
            rgBuilder.AllowGlobalStateModification(true);
            
            rgBuilder.SetRenderFunc<SetupCameraPropsData>((setupCamPassData, rgContext) =>
            {
                rgContext.cmd.SetupCameraProperties(setupCamPassData.cameraData.m_Camera);
            });
        }
    }
}