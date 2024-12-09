using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

using LiteRP.FrameData;

namespace LiteRP
{
    public partial class LiteRGRecorder
    {
        private static readonly ProfilingSampler s_SetupCameraPropsPassSampler = new ProfilingSampler("LiteRP.SetupCameraProps");
        
        internal class SetupCameraPropsData
        {
            internal CameraData cameraData;
        }

        private void AddSetupCameraPropsPass(RenderGraph rg, CameraData cameraData)
        {
            using var rgBuilder = rg.AddRasterRenderPass<SetupCameraPropsData>(s_SetupCameraPropsPassSampler.name, out var passData, s_SetupCameraPropsPassSampler);
            
            passData.cameraData = cameraData;
            
            rgBuilder.AllowPassCulling(false);
            rgBuilder.AllowGlobalStateModification(true);
            
            rgBuilder.SetRenderFunc<SetupCameraPropsData>((setupCamPassData, rgContext) =>
            {
                rgContext.cmd.SetupCameraProperties(setupCamPassData.cameraData.m_Camera);
                
#if UNITY_EDITOR
                float time = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
#else
                    float time = Time.time;
#endif
                float deltaTime = Time.deltaTime;
                float smoothDeltaTime = Time.smoothDeltaTime;

                bool yFlip = !SystemInfo.graphicsUVStartsAtTop;
                // 重置Shader时间变量，因为他们会在SetupCameraPropertiesPass中被覆盖，如果我们不设置，阴影和主渲染可能会不匹配
                SetShaderTimeValues(rgContext.cmd, time, deltaTime, smoothDeltaTime);
                SetPerCameraShaderVariables(rgContext.cmd, passData.cameraData, yFlip);
            });
        }

        internal void SetPerCameraShaderVariables(RasterCommandBuffer cmdBuffer, CameraData cameraData, bool isTargetFlipped = false)
        {
            Camera camera = cameraData.m_Camera;
            
            float cameraWidth = (float)camera.pixelWidth;
            float cameraHeight = (float)camera.pixelHeight;

            float near = camera.nearClipPlane;
            float far = camera.farClipPlane;
            float invNear = Mathf.Approximately(near, 0.0f) ? 0.0f : 1.0f / near;
            float invFar = Mathf.Approximately(far, 0.0f) ? 0.0f : 1.0f / far;
            float isOrthographic = camera.orthographic ? 1.0f : 0.0f;

            // From http://www.humus.name/temp/Linearize%20depth.txt
            // But as depth component textures on OpenGL always return in 0..1 range (as in D3D), we have to use
            // the same constants for both D3D and OpenGL here.
            // OpenGL would be this:
            // zc0 = (1.0 - far / near) / 2.0;
            // zc1 = (1.0 + far / near) / 2.0;
            // D3D is this:
            float zc0 = 1.0f - far * invNear;
            float zc1 = far * invNear;

            Vector4 zBufferParams = new Vector4(zc0, zc1, zc0 * invFar, zc1 * invFar);

            if (SystemInfo.usesReversedZBuffer)
            {
                zBufferParams.y += zBufferParams.x;
                zBufferParams.x = -zBufferParams.x;
                zBufferParams.w += zBufferParams.z;
                zBufferParams.z = -zBufferParams.z;
            }

            // Projection flip sign logic is very deep in GfxDevice::SetInvertProjectionMatrix
            // This setup is tailored especially for overlay camera game view
            // For other scenarios this will be overwritten correctly by SetupCameraProperties
            float projectionFlipSign = isTargetFlipped ? -1.0f : 1.0f;
            Vector4 projectionParams = new Vector4(projectionFlipSign, near, far, 1.0f * invFar);
            cmdBuffer.SetGlobalVector(ShaderPropertyID.projectionParams, projectionParams);

            float aspectRatio = cameraData.GetCameraAspectRatio();

            Vector4 orthoParams = new Vector4(camera.orthographicSize * aspectRatio, camera.orthographicSize, 0.0f, isOrthographic);

            // Camera and Screen variables as described in https://docs.unity3d.com/Manual/SL-UnityShaderVariables.html
            cmdBuffer.SetGlobalVector(ShaderPropertyID.worldSpaceCameraPos, camera.transform.position);
            cmdBuffer.SetGlobalVector(ShaderPropertyID.screenParams, new Vector4(cameraWidth, cameraHeight, 1.0f + 1.0f / cameraWidth, 1.0f + 1.0f / cameraHeight));
            cmdBuffer.SetGlobalVector(ShaderPropertyID.zBufferParams, zBufferParams);
            cmdBuffer.SetGlobalVector(ShaderPropertyID.orthoParams, orthoParams);
        }
    }
}