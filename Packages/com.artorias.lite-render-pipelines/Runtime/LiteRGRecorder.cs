using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace LiteRP
{
    partial class LiteRGRecorder : IRenderGraphRecorder, IDisposable
    {
        private TextureHandle m_BackBufferColorHandle = TextureHandle.nullHandle;
        private RTHandle m_BackBufferColorRTHandle = null;
        
        public void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            CameraData camData = frameData.Get<CameraData>();
            CreateRenderGraphCameraRTs(renderGraph, camData);
            
            AddSetupCameraPropsPass(renderGraph, camData);
            AddGeometryPass(renderGraph, camData);
        }

        private void CreateRenderGraphCameraRTs(RenderGraph renderGraph, CameraData camData)
        {
            RenderTargetIdentifier targetColorID = BuiltinRenderTextureType.CameraTarget;
            if (m_BackBufferColorRTHandle == null)
            {
                m_BackBufferColorRTHandle = RTHandles.Alloc(targetColorID, "BackBuffer_Color");
            }
            
            Color cameraBackgroundColor = CoreUtils.ConvertSRGBToActiveColorSpace(camData.m_Camera.backgroundColor);

            ImportResourceParams importBackBufferColorParams = new ImportResourceParams();
            importBackBufferColorParams.clearOnFirstUse = true;
            importBackBufferColorParams.clearColor = cameraBackgroundColor;
            importBackBufferColorParams.discardOnLastUse = false;
            
            bool colorRTIsSRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            RenderTargetInfo importInfoColor = new RenderTargetInfo();
            importInfoColor.width = Screen.width;
            importInfoColor.height = Screen.height;
            importInfoColor.volumeDepth = 1;
            importInfoColor.msaaSamples = 1;
            importInfoColor.format = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Default, colorRTIsSRGB);

            m_BackBufferColorHandle = renderGraph.ImportTexture(m_BackBufferColorRTHandle, importInfoColor, importBackBufferColorParams);
        }
        
        public void Dispose()
        {
            RTHandles.Release(m_BackBufferColorRTHandle);
            GC.SuppressFinalize(this);
        }
    }
}