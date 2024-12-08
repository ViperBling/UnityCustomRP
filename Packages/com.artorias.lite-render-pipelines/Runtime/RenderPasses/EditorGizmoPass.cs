using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

using LiteRP.FrameData;

namespace LiteRP
{
    public partial class LiteRGRecorder
    {
        private static readonly ProfilingSampler s_EditorGizmoPassSampler = new ProfilingSampler("LiteRP.EditorGizmoPass");
        internal class EditorGizmoPassData
        {
            internal RendererListHandle gizmoRendererListHandle;
        }

        private void AddEditorGizmoPass(RenderGraph rg, CameraData cameraData, GizmoSubset gizmoSubset)
        {
#if UNITY_EDITOR
            if (!Handles.ShouldRenderGizmos() || cameraData.m_Camera.sceneViewFilterMode == Camera.SceneViewFilterMode.ShowFiltered) return;

            bool preOrPostGizmo = gizmoSubset == GizmoSubset.PreImageEffects;
            var passName = preOrPostGizmo ? "_Pre" : "_Post";
            
            using var rgBuilder = rg.AddRasterRenderPass<EditorGizmoPassData>(s_EditorGizmoPassSampler.name + passName, out var passData, s_EditorGizmoPassSampler);
            
            if (m_BackBufferColorHandle.IsValid())
            {
                rgBuilder.SetRenderAttachment(m_BackBufferColorHandle, 0, AccessFlags.Write);
            }
            if (m_BackBufferDepthHandle.IsValid())
            {
                rgBuilder.SetRenderAttachmentDepth(m_BackBufferDepthHandle, AccessFlags.Read);
            }

            passData.gizmoRendererListHandle = rg.CreateGizmoRendererList(cameraData.m_Camera, gizmoSubset);
            rgBuilder.UseRendererList(passData.gizmoRendererListHandle);
            rgBuilder.AllowPassCulling(false);
            
            rgBuilder.SetRenderFunc<EditorGizmoPassData>((gizmoPassData, rgContext) =>
            {
                rgContext.cmd.DrawRendererList(gizmoPassData.gizmoRendererListHandle);
            });
#endif
        }
    }
}