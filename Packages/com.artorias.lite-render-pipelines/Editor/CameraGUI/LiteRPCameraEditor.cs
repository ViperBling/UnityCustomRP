using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// 修复Camera.GetCommandBuffer Warning
namespace LiteRP.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Camera))]
    [SupportedOnRenderPipeline(typeof(LiteRPAsset))]
    public class CustomCameraEditor : UnityEditor.Editor
    {
        private SerializedLiteRPCameraProperties serializedCameraProperties { get; set; }
        
        CameraEditor.Settings m_Settings;
        protected CameraEditor.Settings settings => m_Settings ??= new CameraEditor.Settings(serializedObject);

        public void OnEnable()
        {
            settings.OnEnable();
            serializedCameraProperties = new SerializedLiteRPCameraProperties(serializedObject, settings);
            Undo.undoRedoPerformed += ReconstructReferenceToAdditionalDataSO;
        }
        
        public void OnDisable()
        {
            Undo.undoRedoPerformed -= ReconstructReferenceToAdditionalDataSO;
        }

        void ReconstructReferenceToAdditionalDataSO()
        {
            OnDisable();
            OnEnable();
        }

        public override void OnInspectorGUI()
        {
            serializedCameraProperties.Update();
            LiteRPCameraGUIUtilities.Inspector.Draw(serializedCameraProperties, this);
            serializedCameraProperties.Apply();
        }
    }
}