using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace LiteRP.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Light))]
    [SupportedOnRenderPipeline(typeof(LiteRPAsset))]
    public class LiteRPLightEditor : LightEditor
    {
        SerializedLiteRPLightProperties m_SerializedLiteRPLightProperties { get; set; }

        protected override void OnEnable()
        {
            m_SerializedLiteRPLightProperties = new SerializedLiteRPLightProperties(serializedObject, settings);
            Undo.undoRedoPerformed += ReconstructReferenceToAdditionalDataSO;
        }

        protected void OnDisable()
        {
            Undo.undoRedoPerformed -= ReconstructReferenceToAdditionalDataSO;
        }

        internal void ReconstructReferenceToAdditionalDataSO()
        {
            OnDisable();
            OnEnable();
        }
        
        public override void OnInspectorGUI()
        {
            m_SerializedLiteRPLightProperties.Update();
            LiteRPLightGUIUtilities.Inspector.Draw(m_SerializedLiteRPLightProperties, this);
            m_SerializedLiteRPLightProperties.Apply();
        }
        
        protected override void OnSceneGUI()
        {
            if (!(GraphicsSettings.currentRenderPipeline is LiteRPAsset))
                return;

            if (!(target is Light light) || light == null)
                return;

            switch (light.type)
            {
                case LightType.Spot:
                    using (new Handles.DrawingScope(Matrix4x4.TRS(light.transform.position, light.transform.rotation, Vector3.one)))
                    {
                        CoreLightEditorUtilities.DrawSpotLightGizmo(light);
                    }
                    break;

                case LightType.Point:
                    using (new Handles.DrawingScope(Matrix4x4.TRS(light.transform.position, Quaternion.identity, Vector3.one)))
                    {
                        CoreLightEditorUtilities.DrawPointLightGizmo(light);
                    }
                    break;

                case LightType.Rectangle:
                    using (new Handles.DrawingScope(Matrix4x4.TRS(light.transform.position, light.transform.rotation, Vector3.one)))
                    {
                        CoreLightEditorUtilities.DrawRectangleLightGizmo(light);
                    }
                    break;

                case LightType.Disc:
                    using (new Handles.DrawingScope(Matrix4x4.TRS(light.transform.position, light.transform.rotation, Vector3.one)))
                    {
                        CoreLightEditorUtilities.DrawDiscLightGizmo(light);
                    }
                    break;

                case LightType.Directional:
                    using (new Handles.DrawingScope(Matrix4x4.TRS(light.transform.position, light.transform.rotation, Vector3.one)))
                    {
                        CoreLightEditorUtilities.DrawDirectionalLightGizmo(light);
                    }
                    break;

                default:
                    base.OnSceneGUI();
                    break;
            }
        }
    }
}