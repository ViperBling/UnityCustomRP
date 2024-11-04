using UnityEngine.Rendering.RenderGraphModule;

namespace CustomRP
{
    public readonly ref struct LightResources
    {
        public readonly BufferHandle m_DirectLightDataBuffer;
        public readonly BufferHandle m_OtherLightDataBuffer;
        
        public LightResources(BufferHandle directLightDataBuffer, BufferHandle otherLightDataBuffer)
        {
            m_DirectLightDataBuffer = directLightDataBuffer;
            m_OtherLightDataBuffer = otherLightDataBuffer;
        }
    }
}