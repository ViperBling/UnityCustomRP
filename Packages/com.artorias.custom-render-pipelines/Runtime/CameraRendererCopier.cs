using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP
{
    public readonly struct CameraRendererCopier
    {
        static readonly int m_SourceTextureID = Shader.PropertyToID("_SourceTexture");
        static readonly int m_SrcBlendID = Shader.PropertyToID("_CameraSrcBlend");
        static readonly int m_DstBlendID = Shader.PropertyToID("_CameraDstBlend");

        private static readonly Rect m_FullViewRect = new(0.0f, 0.0f, 1.0f, 1.0f);
        
        static readonly bool m_CopyTextureSupported = SystemInfo.copyTextureSupport > CopyTextureSupport.None;
        public static bool m_RequiresRTResetAfterCopy => !m_CopyTextureSupported;

        public readonly Camera m_Camera => m_InnerCamera;
        private readonly Camera m_InnerCamera;

        private readonly Material m_CameraCopyMat;
        readonly CameraSettings.FinalBlendMode m_FinalBlendMode;
        
        public CameraRendererCopier(Material material, Camera camera, CameraSettings.FinalBlendMode finalBlendMode)
        {
            m_CameraCopyMat = material;
            m_InnerCamera = camera;
            m_FinalBlendMode = finalBlendMode;
        }

        public readonly void Copy(CommandBuffer cmdBuffer, RenderTargetIdentifier rtFrom, RenderTargetIdentifier rtTo, bool isDepth)
        {
            if (m_CopyTextureSupported)
            {
                cmdBuffer.CopyTexture(rtFrom, rtTo);
            }
            else
            {
                CopyByDrawing(cmdBuffer, rtFrom, rtTo, isDepth);
            }
        }

        public readonly void CopyByDrawing(CommandBuffer cmdBuffer, RenderTargetIdentifier rtFrom, RenderTargetIdentifier rtTo, bool isDepth)
        {
            cmdBuffer.SetGlobalTexture(m_SourceTextureID, rtFrom);
            cmdBuffer.SetRenderTarget(rtTo, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmdBuffer.SetViewport(m_InnerCamera.pixelRect);
            cmdBuffer.DrawProcedural(Matrix4x4.identity, m_CameraCopyMat, isDepth ? 1 : 0, MeshTopology.Triangles, 3);
        }

        public readonly void CopyToCameraTarget(CommandBuffer cmdBuffer, RenderTargetIdentifier rtFrom)
        {
            cmdBuffer.SetGlobalFloat(m_SrcBlendID, (float)m_FinalBlendMode.m_Source);
            cmdBuffer.SetGlobalFloat(m_DstBlendID, (float)m_FinalBlendMode.m_Destination);
            cmdBuffer.SetGlobalTexture(m_SourceTextureID, rtFrom);
            cmdBuffer.SetRenderTarget(
                BuiltinRenderTextureType.CameraTarget,
                m_FinalBlendMode.m_Destination == BlendMode.Zero && m_InnerCamera.rect == m_FullViewRect ? 
                    RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load,
                RenderBufferStoreAction.Store);
            cmdBuffer.SetViewport(m_InnerCamera.pixelRect);
            
            cmdBuffer.DrawProcedural(Matrix4x4.identity, m_CameraCopyMat, 0, MeshTopology.Triangles, 3);
            cmdBuffer.SetGlobalFloat(m_SrcBlendID, 1f);
            cmdBuffer.SetGlobalFloat(m_DstBlendID, 0f);
        }
    }
}