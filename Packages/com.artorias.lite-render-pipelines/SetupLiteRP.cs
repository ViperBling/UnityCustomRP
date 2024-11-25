using System;
using UnityEngine;
using UnityEngine.Rendering;

public class SetupLiteRP : MonoBehaviour
{
    public RenderPipelineAsset m_CurrentPipelineAsset;
    
    private void OnEnable()
    {
        GraphicsSettings.defaultRenderPipeline = m_CurrentPipelineAsset;
    }

    private void OnValidate()
    {
        GraphicsSettings.defaultRenderPipeline = m_CurrentPipelineAsset;
    }
}
