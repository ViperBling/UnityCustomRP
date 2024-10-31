using UnityEngine;

namespace CustomRP
{
    [System.Serializable]
    public class CustomRenderPipelineSettings
    {
        public CameraBufferSettings m_CameraBufferSettings = new()
        {
            m_AllowHDR = true,
            m_RenderScale = 1f,
        };
        
        public bool m_UseSRPBatch = true;
        
        public enum ColorLUTResolution
        {
            _16 = 16,
            _32 = 32,
            _64 = 64
        }
        public ColorLUTResolution m_ColorLUTResolution = ColorLUTResolution._32;

        public Shader m_CameraRendererShader;
        public Shader m_CameraDebuggerShader;
    }
}