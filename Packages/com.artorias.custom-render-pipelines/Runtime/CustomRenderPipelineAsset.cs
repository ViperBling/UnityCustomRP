using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset 
{
    [SerializeField]
    bool m_UseDynamicBatching = true, m_UseGPUInstancing = true, m_UseSRPBatcher = true;
    
    protected override RenderPipeline CreatePipeline () 
    {
        return new CustomRenderPipeline(m_UseDynamicBatching, m_UseGPUInstancing, m_UseSRPBatcher);
    }
}
