using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Camera))]
    [SupportedOnRenderPipeline(typeof(CustomRenderPipelineAsset))]
    public class CustomCameraEditor : UnityEditor.Editor
    {
        
    }
}