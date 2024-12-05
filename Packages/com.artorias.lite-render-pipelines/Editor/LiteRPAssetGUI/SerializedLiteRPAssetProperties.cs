using UnityEditor;
using UnityEngine;

namespace LiteRP.Editor
{
    internal static class LiteRPAssetProperties
    {
        public static readonly string UseSRPBatcher = "m_UseSRPBatcher";
        public static readonly string UseGPUDriven = "m_GPUDrivenMode";
        public static readonly string UseSmallMeshScreenPercentage = "m_SmallMeshScreenPercentage";
        public static readonly string UseGPUDrivenEnableOCInCams = "m_GPUDrivenEnableOcclusionCullingInCams";
        
        public static readonly string AntiAliasing = "m_AntiAliasing";
    }

    internal class SerializedLiteRPAssetProperties
    {
        public LiteRPAsset rpAsset { get; }
        public SerializedObject serializedObject { get; }
        
        // Pipeline Settings
        public SerializedProperty srpBatcher { get; }
        public SerializedProperty gpuDrivenMode { get; }
        public SerializedProperty smallMeshScreenPercentage { get; }
        public SerializedProperty gpuDrivenEnableOCInCams { get; }
        
        // Quality Settings
        public SerializedProperty antiAliasing { get; }

        public SerializedLiteRPAssetProperties(SerializedObject serializedObject)
        {
            rpAsset = serializedObject.targetObject as LiteRPAsset;
            this.serializedObject = serializedObject;
            
            // Pipeline Settings
            srpBatcher = serializedObject.FindProperty(LiteRPAssetProperties.UseSRPBatcher);
            gpuDrivenMode = serializedObject.FindProperty(LiteRPAssetProperties.UseGPUDriven);
            smallMeshScreenPercentage = serializedObject.FindProperty(LiteRPAssetProperties.UseSmallMeshScreenPercentage);
            gpuDrivenEnableOCInCams = serializedObject.FindProperty(LiteRPAssetProperties.UseGPUDrivenEnableOCInCams);
            
            // Quality Settings
            antiAliasing = serializedObject.FindProperty(LiteRPAssetProperties.AntiAliasing);
        }

        public void Update()
        {
            serializedObject.Update();
        }

        public void Apply()
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}