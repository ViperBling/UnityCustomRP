using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// 修复Camera.GetCommandBuffer Warning
namespace LiteRP
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Camera))]
    [SupportedOnRenderPipeline(typeof(LiteRPAsset))]
    public class CustomCameraEditor : UnityEditor.Editor
    {
        
    }
}