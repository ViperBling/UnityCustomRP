using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LiteRP
{
    public class LiteRenderPipeline : RenderPipeline
    {
        private readonly static ShaderTagId s_UnlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
        
        // Older version
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            
        }

        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            BeginContextRendering(context, cameras);
            
            for (int i = 0; i < cameras.Count; i++)
            {
                RenderCamera(context, cameras[i]);
            }
            
            EndContextRendering(context, cameras);
        }
        
        private void RenderCamera(ScriptableRenderContext context, Camera camera)
        {
            BeginCameraRendering(context, camera);
            
            // 获取相机剔除参数，并进行剔除
            ScriptableCullingParameters cullingParameters;
            if (!camera.TryGetCullingParameters(out cullingParameters)) return;
            CullingResults cullingResults = context.Cull(ref cullingParameters);
            
            // 为相机创建CommandBuffer
            CommandBuffer cmdBuffer = CommandBufferPool.Get(camera.name);
            // 设置相机属性
            context.SetupCameraProperties(camera);
            
            bool clearSkybox = camera.clearFlags == CameraClearFlags.Skybox;
            bool clearDepth = camera.clearFlags != CameraClearFlags.Nothing;
            bool clearColor = camera.clearFlags == CameraClearFlags.Color;
            // 清理渲染目标
            cmdBuffer.ClearRenderTarget(clearDepth, clearColor, CoreUtils.ConvertSRGBToActiveColorSpace(camera.backgroundColor));
            
            // 绘制天空盒
            if (clearSkybox)
            {
                var skyboxRendererList = context.CreateSkyboxRendererList(camera);
                cmdBuffer.DrawRendererList(skyboxRendererList);
            }
            
            // 指定渲染排序设置，SortSettings
            var sortSettings = new SortingSettings(camera);
            // 不透明物体
            sortSettings.criteria = SortingCriteria.CommonOpaque;
            // 指定渲染状态设置，DrawSettings
            var drawSettings = new DrawingSettings(s_UnlitShaderTagId, sortSettings);
            // 指定渲染过滤设置，FilterSettings
            var filterSettings = new FilteringSettings(RenderQueueRange.opaque);
            // 创建渲染指令列表
            var rendererListParams = new RendererListParams(cullingResults, drawSettings, filterSettings);
            var rendererList = context.CreateRendererList(ref rendererListParams);
            // 绘制
            cmdBuffer.DrawRendererList(rendererList);
            
            // 半透
            sortSettings.criteria = SortingCriteria.CommonTransparent;
            filterSettings.renderQueueRange = RenderQueueRange.transparent;
            rendererListParams = new RendererListParams(cullingResults, drawSettings, filterSettings);
            rendererList = context.CreateRendererList(ref rendererListParams);
            cmdBuffer.DrawRendererList(rendererList);
            
            // 提交渲染命令
            context.ExecuteCommandBuffer(cmdBuffer);
            // 释放CommandBuffer
            cmdBuffer.Clear();
            
            CommandBufferPool.Release(cmdBuffer);
            
            // 提交渲染上下文
            context.Submit();
            
            EndCameraRendering(context, camera);
        }
    }
}