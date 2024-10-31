using System.Diagnostics;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace CustomRP
{
    public class GizmosPass
    {
#if UNITY_EDITOR
        static readonly ProfilingSampler m_ProfileSampler = new("CustomRP.GizmoPass");

        private CameraRendererCopier m_Copier;
        private TextureHandle m_DepthAttachment;

        void Render(RenderGraphContext rgContext)
        {
            CommandBuffer cmdBuffer = rgContext.cmd;
            ScriptableRenderContext srpContext = rgContext.renderContext;
            m_Copier.CopyByDrawing(cmdBuffer, m_DepthAttachment, BuiltinRenderTextureType.CameraTarget, true);
            srpContext.ExecuteCommandBuffer(cmdBuffer);
            cmdBuffer.Clear();
            
            srpContext.DrawGizmos(m_Copier.m_Camera, GizmoSubset.PreImageEffects);
            srpContext.DrawGizmos(m_Copier.m_Camera, GizmoSubset.PostImageEffects);
        }
#endif

        [Conditional("UNITY_EDITOR")]
        public static void Record(RenderGraph rg, CameraRendererCopier copier, in CameraRendererTextures renderTextures)
        {
#if UNITY_EDITOR
            if (Handles.ShouldRenderGizmos())
            {
                using RenderGraphBuilder rgBuilder =
                    rg.AddRenderPass(m_ProfileSampler.name, out GizmosPass pass, m_ProfileSampler);
                pass.m_Copier = copier;
                pass.m_DepthAttachment = rgBuilder.ReadTexture(renderTextures.m_DepthAttachment);
                
                rgBuilder.SetRenderFunc<GizmosPass>(static (pass, rgContext) => pass.Render(rgContext));
            }
#endif
        }
    }
}