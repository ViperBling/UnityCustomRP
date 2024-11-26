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
    }
}