using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace LiteRP
{
    public class LiteRenderPipeline : RenderPipeline
    {
        private readonly static ShaderTagId s_UnlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
        private RenderGraph m_RenderGraph = null;
        private LiteRGRecorder m_LiteRGRecorder = null;
        private ContextContainer m_ContextContainer = null;

        public LiteRenderPipeline()
        {
            InitializeRenderGraph();
        }

        protected override void Dispose(bool bDispose)
        {
            CleanupRenderGraph();
            base.Dispose(bDispose);
        }

        // Older version
        protected override void Render(ScriptableRenderContext context, Camera[] cameras) { }

        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            BeginContextRendering(context, cameras);
            
            for (int i = 0; i < cameras.Count; i++)
            {
                RenderCamera(context, cameras[i]);
            }
            // RG结束当前帧
            m_RenderGraph.EndFrame();
            
            EndContextRendering(context, cameras);
        }
        
        private void RenderCamera(ScriptableRenderContext context, Camera camera)
        {
            BeginCameraRendering(context, camera);
            
            // 准备FrameData
            if (!PrepareFrameData(context, camera)) return;
            
            // 获取相机剔除参数，并进行剔除
            ScriptableCullingParameters cullingParameters;
            if (!camera.TryGetCullingParameters(out cullingParameters)) return;
            CullingResults cullingResults = context.Cull(ref cullingParameters);
            
            // 为相机创建CommandBuffer
            CommandBuffer cmdBuffer = CommandBufferPool.Get(camera.name);
            // 设置相机属性
            context.SetupCameraProperties(camera);
            
            RecordAndExecuteRenderGraph(context, camera, cmdBuffer);
            
            // 提交渲染命令
            context.ExecuteCommandBuffer(cmdBuffer);
            // 释放CommandBuffer
            cmdBuffer.Clear();
            
            CommandBufferPool.Release(cmdBuffer);
            
            // 提交渲染上下文
            context.Submit();
            
            EndCameraRendering(context, camera);
        }

        private void InitializeRenderGraph()
        {
            m_RenderGraph = new RenderGraph("LiteRPRenderGraph");
            m_LiteRGRecorder = new LiteRGRecorder();
            m_ContextContainer = new ContextContainer();
        }
        
        private void CleanupRenderGraph()
        {
            m_ContextContainer?.Dispose();
            m_ContextContainer = null;
            m_LiteRGRecorder = null;
            m_RenderGraph?.Cleanup();
            m_RenderGraph = null;
        }
        
        private bool PrepareFrameData(ScriptableRenderContext context, Camera camera)
        {
            return true;
        }
        
        private void RecordAndExecuteRenderGraph(ScriptableRenderContext context, Camera camera, CommandBuffer cmdBuffer)
        {
            RenderGraphParameters rgParams = new RenderGraphParameters()
            {
                executionName = camera.name,
                commandBuffer = cmdBuffer,
                scriptableRenderContext = context,
                currentFrameIndex = Time.frameCount
            };
            
            m_RenderGraph.BeginRecording(rgParams);
            
            // 开启RenderGraph的记录线
            m_LiteRGRecorder.RecordRenderGraph(m_RenderGraph, m_ContextContainer);
            
            m_RenderGraph.EndRecordingAndExecute();
        }
    }
}