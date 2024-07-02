using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline
{
    private CameraRenderer m_Renderer = new CameraRenderer();
    
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        
    }
    
    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        for (int i = 0; i < cameras.Count; i++)
        {
            m_Renderer.Render(context, cameras[i]);
        }
    }
}
