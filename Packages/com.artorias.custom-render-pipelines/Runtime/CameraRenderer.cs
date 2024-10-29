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

        public void Render(RenderGraph renderGraph, ScriptableRenderContext context, Camera camera, CustomRenderPipelineSettings settings)
        {
            CameraBufferSettings bufferSettings = settings.m_CameraBuffer;
            
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
            
            bool useColorTexture, useDepthTexture;
            if (camera.cameraType == CameraType.Reflection)
            {
                useColorTexture = bufferSettings.m_CopyColorReflection;
                useDepthTexture = bufferSettings.m_CopyDepthReflection;
            }
            else
            {
                useColorTexture = bufferSettings.m_CopyColor && cameraSettings.m_CopyColor;
                useDepthTexture = bufferSettings.m_CopyDepth && cameraSettings.m_CopyDepth;
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
            CullingResults cullingResults = context.Cull(ref spCullingParams);

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
                scriptableRenderContext = context
            };
            renderGraph.BeginRecording(renderGraphParams);

            using (new RenderGraphProfilingScope(renderGraph, cameraSampler))
            {
                
            }
            renderGraph.EndRecordingAndExecute();
            
            context.ExecuteCommandBuffer(renderGraphParams.commandBuffer);
            context.Submit();
            
            CommandBufferPool.Release(renderGraphParams.commandBuffer);
        }
    }
}