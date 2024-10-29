using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP
{
    [CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
    public partial class CustomRenderPipelineAsset : RenderPipelineAsset
    {
        [SerializeField]
        CustomRenderPipelineSettings m_RPSettings;

        [SerializeField, Tooltip("Move to Settings."), HideInInspector]
        CameraBufferSettings m_CameraBuffer = new()
        {
            m_AllowHDR = true,
            m_RenderScale = 1.0f
        };

        [SerializeField, Tooltip("Move to Settings."), HideInInspector]
        bool m_UseSRPBatch = true;

        public enum ColorLUTResolution { _16 = 16, _32 = 32, _64 = 64 };
        [SerializeField, Tooltip("Move to Settings."), HideInInspector]
        ColorLUTResolution m_ColorLUTResolution = ColorLUTResolution._32;
    
        [SerializeField, Tooltip("Move to Settings."), HideInInspector]
        Shader m_CameraRendererShader = default;

        public override Type pipelineType => typeof(CustomRenderPipeline);

        protected override RenderPipeline CreatePipeline()
        {
            if ((m_RPSettings == null || m_RPSettings.m_CameraRendererShader == null) && m_CameraRendererShader != null)
            {
                m_RPSettings = new CustomRenderPipelineSettings()
                {
                    m_CameraBuffer = m_CameraBuffer,
                    m_UseSRPBatch = m_UseSRPBatch,
                    m_ColorLUTResolution = (CustomRenderPipelineSettings.ColorLUTResolution)m_ColorLUTResolution,
                    m_CameraRendererShader = m_CameraRendererShader
                };
            }

            if (m_CameraRendererShader != null)
            {
                m_CameraRendererShader = null;
            }

            return new CustomRenderPipeline(m_RPSettings);
        }
    }
}
