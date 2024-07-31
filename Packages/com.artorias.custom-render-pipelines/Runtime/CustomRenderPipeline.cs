using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline
{
    private CameraRenderer m_Renderer = new CameraRenderer();
    private bool m_UseDynamicBatching;
    private bool m_UseGPUInstancing;
    
    public CustomRenderPipeline(bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher)
    {
        this.m_UseDynamicBatching = useDynamicBatching;
        this.m_UseGPUInstancing = useGPUInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
    }
    
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        
    }
    
    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        for (int i = 0; i < cameras.Count; i++)
        {
            m_Renderer.Render(context, cameras[i], m_UseDynamicBatching, m_UseGPUInstancing);
        }
    }
}
