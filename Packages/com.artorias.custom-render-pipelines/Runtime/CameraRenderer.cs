using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace CustomRP
{
    public class CameraRenderer
    {
        public const float m_RenderScaleMin = 0.1f, m_RenderScaleMax = 2f;
        static readonly CameraSettings m_DefaultCameraSettings = new();
 
        private readonly Material m_Material;

        public CameraRenderer(Shader shader, Shader cameraDebugShader)
        {
            m_Material = CoreUtils.CreateEngineMaterial(shader);
            CameraDebugger.Initialize(cameraDebugShader);
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);
            CameraDebugger.Cleanup();
        }

        public void Render(RenderGraph rg, ScriptableRenderContext srpContext, Camera camera, CustomRenderPipelineSettings settings)
        {
            CameraBufferSettings bufferSettings = settings.m_CameraBufferSettings;
            
            ProfilingSampler cameraSampler;
            CameraSettings cameraSettings;

            if (camera.TryGetComponent(out CustomRenderPipelineCamera crpCamera))
            {
                cameraSampler = crpCamera.m_Sampler;
                cameraSettings = crpCamera.m_CameraSettings;
            }
            else
            {
                cameraSampler = ProfilingSampler.Get(camera.cameraType);
                cameraSettings = m_DefaultCameraSettings;
            }
            
            bool isCopyColorTexture, isCopyDepthTexture;
            if (camera.cameraType == CameraType.Reflection)
            {
                isCopyColorTexture = bufferSettings.m_IsCopyColorReflection;
                isCopyDepthTexture = bufferSettings.m_IsCopyDepthReflection;
            }
            else
            {
                isCopyColorTexture = bufferSettings.m_IsCopyColor && cameraSettings.m_IsCopyColor;
                isCopyDepthTexture = bufferSettings.m_IsCopyDepth && cameraSettings.m_IsCopyDepth;
            }
            
            float renderScale = cameraSettings.GetRenderScale(bufferSettings.m_RenderScale);
            bool useScaleRendering = renderScale < 0.99f || renderScale > 1.01f;

#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
                useScaleRendering = false;
            }
#endif
            
            if (!camera.TryGetCullingParameters(out ScriptableCullingParameters spCullingParams))
            {
                return;
            }
            // spCullingParams.shadowDistance = Mathf.Min()
            CullingResults cullingResults = srpContext.Cull(ref spCullingParams);

            bufferSettings.m_AllowHDR &= camera.allowHDR;
            Vector2Int bufferSize = default;
            if (useScaleRendering)
            {
                renderScale = Mathf.Clamp(renderScale, m_RenderScaleMin, m_RenderScaleMax);
                bufferSize.x = (int)(camera.pixelWidth * renderScale);
                bufferSize.y = (int)(camera.pixelHeight * renderScale);
            }
            else
            {
                bufferSize.x = camera.pixelWidth;
                bufferSize.y = camera.pixelHeight;
            }
            
            // Record the render graph
            var renderGraphParams = new RenderGraphParameters()
            {
                commandBuffer = CommandBufferPool.Get(),
                currentFrameIndex = Time.frameCount,
                executionName = cameraSampler.name,
                rendererListCulling = true,
                scriptableRenderContext = srpContext
            };
            rg.BeginRecording(renderGraphParams);
            using (new RenderGraphProfilingScope(rg, cameraSampler))
            {
                CameraRendererTextures renderTextures = SetupPass.Record(
                    rg, isCopyColorTexture, isCopyDepthTexture,
                    bufferSettings.m_AllowHDR, bufferSize, camera);
                
                SkyboxPass.Record(rg, camera, renderTextures);

                var copier = new CameraRendererCopier(m_Material, camera, cameraSettings.m_FinalBlendMode);
                CopyAttachmentsPass.Record(rg, isCopyColorTexture, isCopyDepthTexture, copier, renderTextures);
                
                FinalPass.Record(rg, copier, renderTextures);
            }
            rg.EndRecordingAndExecute();
            
            srpContext.ExecuteCommandBuffer(renderGraphParams.commandBuffer);
            srpContext.Submit();
            
            CommandBufferPool.Release(renderGraphParams.commandBuffer);
        }
    }
}