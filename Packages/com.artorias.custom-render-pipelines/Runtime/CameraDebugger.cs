using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace CustomRP
{
    public static class CameraDebugger
    {
        private const string m_Name = "Forward+";
        
        static readonly int m_OpacityID = Shader.PropertyToID("_DebugOpacity");
        private static Material m_Material;
        private static bool m_ShowTiles;
        private static float m_Opacity = 0.5f;
        public static bool m_IsActive => m_ShowTiles && m_Opacity > 0f;

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public static void Initialize(Shader shader)
        {
            
        }
        
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public static void Cleanup()
        {
            
        }
        
        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        public static void Render(RenderGraphContext context)
        {
            
        }
    }
}