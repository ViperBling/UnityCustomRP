using System;

namespace CustomRP
{
    partial class CustomRenderPipelineAsset
    {
#if UNITY_EDITOR
        
        static string[] m_RenderingLayerNames;
        
        static CustomRenderPipelineAsset()
        {
            m_RenderingLayerNames = new string[31];
            for (int i = 0; i < m_RenderingLayerNames.Length; i++)
            {
                m_RenderingLayerNames[i] = "Layer " + (i + 1);
            }
        }
        
        [Obsolete]
        public override string[] renderingLayerMaskNames => m_RenderingLayerNames;
        
#endif
    }
}