using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

using LiteRP.FrameData;

namespace LiteRP
{
    partial class LiteRGRecorder : IRenderGraphRecorder, IDisposable
    {
        private static readonly ShaderTagId[] s_ShaderTagIDs = new ShaderTagId[]
        {
            new ShaderTagId("SRPDefaultUnlit"),
        };

        private RenderingPath m_RenderingPath = RenderingPath.Forward;
        
        private TextureHandle m_BackBufferColorHandle = TextureHandle.nullHandle;
        private RTHandle m_BackBufferColorRTHandle = null;
        
        private TextureHandle m_BackBufferDepthHandle = TextureHandle.nullHandle;
        private RTHandle m_BackBufferDepthRTHandle = null;

        internal LiteRGRecorder(RenderingPath renderingPath)
        {
            m_RenderingPath = renderingPath;
            InitializeMainLightShadowPass();
        }
        
        public void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            CameraData camData = frameData.Get<CameraData>();
            LightData lightData = frameData.Get<LightData>();
            ShadowData shadowData = frameData.Get<ShadowData>();
            
            AddSetupLightsPass(renderGraph, camData, lightData);
            CreateRenderGraphCameraRTs(renderGraph, camData);
            AddInitRenderGraphFramePass(renderGraph);
            AddSetupCameraPropertiesPass(renderGraph, camData);
            
            if (NeedMainLightShadowPass(camData, lightData, shadowData))
            {
                AddMainLightShadowmapPass(renderGraph, camData, lightData, shadowData);
                AddSetupCameraPropertiesPass(renderGraph, camData);
            }
            
            CameraClearFlags clearFlags = camData.m_Camera.clearFlags;
            if (!renderGraph.nativeRenderPassesEnabled && clearFlags != CameraClearFlags.Nothing)
            {
                AddClearRTPass(renderGraph, camData);
            }

            switch (m_RenderingPath)
            {
                case RenderingPath.Forward:
                {
                    AddOpaqueObjectPass(renderGraph, camData);
                    if (clearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null)
                    {
                        AddSkyboxPass(renderGraph, camData);
                    }
            
                    AddTransparentObjectPass(renderGraph, camData);
                    break;
                }
                case RenderingPath.Deferred:
                {
                    
                    break;
                }
                default: break;
            }
            
#if UNITY_EDITOR
            AddEditorGizmoPass(renderGraph, camData, GizmoSubset.PreImageEffects);
            AddEditorGizmoPass(renderGraph, camData, GizmoSubset.PostImageEffects);
#endif
        }

        private void CreateRenderGraphCameraRTs(RenderGraph renderGraph, CameraData camData)
        {
            var cameraTargetTexture = camData.m_Camera.targetTexture;
            bool isBuildInTexture = cameraTargetTexture == null;
            bool isCameraOffscreenDepth = !isBuildInTexture && cameraTargetTexture.format == RenderTextureFormat.Depth;

            RenderTargetIdentifier targetColorID = isBuildInTexture ? BuiltinRenderTextureType.CameraTarget : new RenderTargetIdentifier(cameraTargetTexture);
            if (m_BackBufferColorRTHandle == null)
            {
                m_BackBufferColorRTHandle = RTHandles.Alloc(targetColorID, "BackBuffer_Color");
            }
            else if (m_BackBufferColorRTHandle.nameID != targetColorID)
            {
                RTHandleStaticHelpers.SetRTHandleUserManagedWrapper(ref m_BackBufferColorRTHandle, targetColorID);
            }
            
            RenderTargetIdentifier targetDepthID = isBuildInTexture ? BuiltinRenderTextureType.Depth : new RenderTargetIdentifier(cameraTargetTexture);
            if (m_BackBufferDepthRTHandle == null)
            {
                m_BackBufferDepthRTHandle = RTHandles.Alloc(targetDepthID, "BackBuffer_Depth");
            }
            else if (m_BackBufferDepthRTHandle.nameID != targetDepthID)
            {
                RTHandleStaticHelpers.SetRTHandleUserManagedWrapper(ref m_BackBufferDepthRTHandle, targetDepthID);
            }

            Color clearColor = camData.GetClearColor();
            RTClearFlags clearFlags = camData.GetClearFlags();

            bool clearOnFirstUse = !renderGraph.nativeRenderPassesEnabled;
            bool discardColorOnLastUse = !renderGraph.nativeRenderPassesEnabled;
            bool discardDepthOnLastUse = !isCameraOffscreenDepth;
            
            ImportResourceParams importBackBufferColorParams = new ImportResourceParams();
            importBackBufferColorParams.clearOnFirstUse = clearOnFirstUse;
            importBackBufferColorParams.clearColor = clearColor;
            importBackBufferColorParams.discardOnLastUse = discardColorOnLastUse;
            
            ImportResourceParams importBackBufferDepthParams = new ImportResourceParams();
            importBackBufferColorParams.clearOnFirstUse = clearOnFirstUse;
            importBackBufferColorParams.clearColor = clearColor;
            importBackBufferColorParams.discardOnLastUse = discardDepthOnLastUse;
            
#if UNITY_EDITOR
            if (camData.m_Camera.cameraType == CameraType.SceneView)
            {
                importBackBufferDepthParams.discardOnLastUse = false;
            }
#endif
            
            bool colorRT_sSRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            RenderTargetInfo importInfoColor = new RenderTargetInfo();
            RenderTargetInfo importInfoDepth = new RenderTargetInfo();

            if (isBuildInTexture)
            {
                importInfoColor.width = Screen.width;
                importInfoColor.height = Screen.height;
                importInfoColor.volumeDepth = 1;
                importInfoColor.msaaSamples = 1;
            }
            else
            {
                importInfoColor.width = cameraTargetTexture.width;
                importInfoColor.height = cameraTargetTexture.height;
                importInfoColor.volumeDepth = cameraTargetTexture.volumeDepth;
                importInfoColor.msaaSamples = cameraTargetTexture.antiAliasing;
            }
            importInfoColor.bindMS = false;
            importInfoColor.format = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Default, colorRT_sSRGB);
            
            importInfoDepth = importInfoColor;
            importInfoDepth.format = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil);

            m_BackBufferColorHandle = renderGraph.ImportTexture(m_BackBufferColorRTHandle, importInfoColor, importBackBufferColorParams);
            m_BackBufferDepthHandle = renderGraph.ImportTexture(m_BackBufferDepthRTHandle, importInfoDepth, importBackBufferDepthParams);
        }
        
        public void Dispose()
        {
            ReleaseMainLightShadowPass();
            
            RTHandles.Release(m_BackBufferColorRTHandle);
            RTHandles.Release(m_BackBufferDepthRTHandle);
            GC.SuppressFinalize(this);
        }
    }
}