using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace CustomRP
{
    public partial class CustomRenderPipeline : RenderPipeline
    {
        private readonly CameraRenderer m_Renderer;
        private readonly CustomRenderPipelineSettings m_Settings;
        private readonly RenderGraph m_RenderGraph = new RenderGraph("CustomRP Render Graph");
        
        public CustomRenderPipeline(CustomRenderPipelineSettings settings)
        {
            this.m_Settings = settings;
            GraphicsSettings.useScriptableRenderPipelineBatching = settings.m_UseSRPBatch;
            GraphicsSettings.lightsUseLinearIntensity = true;
            InitializeForEditor();
            m_Renderer = new CameraRenderer(settings.m_CameraRendererShader, settings.m_CameraDebuggerShader);
        }
    
        protected override void Render(ScriptableRenderContext context, Camera[] cameras) {}
    
        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            for (int i = 0; i < cameras.Count; i++)
            {
                m_Renderer.Render(m_RenderGraph, context, cameras[i], m_Settings);
            }
            m_RenderGraph.EndFrame();
        }
        
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DisposeForEditor();
            m_Renderer.Dispose();
            m_RenderGraph.Cleanup();
        }
    }

}
