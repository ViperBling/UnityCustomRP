using UnityEngine;
using UnityEngine.Rendering;

namespace LiteRP.FrameData
{
    public class CameraData : ContextItem
    {
        public Camera m_Camera;
        public CullingResults m_CullingResults;
        
        public float m_MaxShadowDistance;
        public bool m_PostProcessEnabled;

        public override void Reset()
        {
            m_Camera = null;
            m_CullingResults = default;
            m_MaxShadowDistance = 0.0f;
            m_PostProcessEnabled = false;
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

        public float GetCameraAspectRatio()
        {
            return m_Camera.aspect;
        }
    }
}