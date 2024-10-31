using UnityEngine.Rendering.RenderGraphModule;

namespace CustomRP
{
    public readonly ref struct CameraRendererTextures
    {
        public readonly TextureHandle m_ColorAttachment, m_DepthAttachment;
        public readonly TextureHandle m_ColorCopyAttachment, m_DepthCopyAttachment;
        
        public CameraRendererTextures(
            TextureHandle colorAttachment, TextureHandle depthAttachment,
            TextureHandle colorCopyAttachment, TextureHandle depthCopyAttachment)
        {
            m_ColorAttachment = colorAttachment;
            m_DepthAttachment = depthAttachment;
            m_ColorCopyAttachment = colorCopyAttachment;
            m_DepthCopyAttachment = depthCopyAttachment;
        }
    }
}