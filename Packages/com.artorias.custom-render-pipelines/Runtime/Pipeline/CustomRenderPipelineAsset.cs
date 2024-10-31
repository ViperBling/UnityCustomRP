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

        [SerializeField, Tooltip("Move to CRPSettings."), HideInInspector]
        CameraBufferSettings m_CameraBufferSettings = new()
        {
            m_AllowHDR = true,
            m_RenderScale = 1.0f
        };
        
        [SerializeField, Tooltip("Move to CRPSettings."), HideInInspector]
        bool m_UseSRPBatch = true;
        
        public enum ColorLUTResolution { _16 = 16, _32 = 32, _64 = 64 };
        [SerializeField, Tooltip("Move to CRPSettings."), HideInInspector]
        ColorLUTResolution m_ColorLUTResolution = ColorLUTResolution._32;
        
        [SerializeField, Tooltip("Move to CRPSettings."), HideInInspector]
        Shader m_CameraRendererShader = default;

        /// <summary>
        /// Resolve warning: You must either inherit from RenderPipelineAsset<TRenderPipeline> or override pipelineType property.
        /// </summary>
        public override Type pipelineType => typeof(CustomRenderPipeline);
        
        /// <summary>
        /// Resolve warning: The property renderPipelineShaderTag has not been overridden. At build time, any shader variants that use any RenderPipeline tag will be stripped.
        /// </summary>
        public override string renderPipelineShaderTag => string.Empty;

        protected override RenderPipeline CreatePipeline()
        {
            if ((m_RPSettings == null || m_RPSettings.m_CameraRendererShader == null) && m_CameraRendererShader != null)
            {
                m_RPSettings = new CustomRenderPipelineSettings()
                {
                    m_CameraBufferSettings = m_CameraBufferSettings,
                    m_UseSRPBatch = m_UseSRPBatch,
                    m_ColorLUTResolution = (CustomRenderPipelineSettings.ColorLUTResolution)m_ColorLUTResolution,
                    m_CameraRendererShader = m_CameraRendererShader
                };
            }
            
            m_CameraRendererShader = null;
            
            return new CustomRenderPipeline(m_RPSettings);
        }
    }
}
