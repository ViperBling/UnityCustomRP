using UnityEngine;
using UnityEngine.Rendering;

namespace LiteRP
{
    public class CameraData : ContextItem
    {
        public Camera m_Camera;
        public CullingResults m_CullingResults;

        public override void Reset()
        {
            m_Camera = null;
            m_CullingResults = default;
        }

        public RTClearFlags GetClearFlags()
        {
            CameraClearFlags clearFlags = m_Camera.clearFlags;
            switch (clearFlags)
            {
                case CameraClearFlags.Depth:
                    return RTClearFlags.DepthStencil;
                case CameraClearFlags.Nothing:
                    return RTClearFlags.None;
                default:
                    return RTClearFlags.All;
            }
        }
        
        public Color GetClearColor()
        {
            return CoreUtils.ConvertSRGBToActiveColorSpace(m_Camera.backgroundColor);
        }
    }
}