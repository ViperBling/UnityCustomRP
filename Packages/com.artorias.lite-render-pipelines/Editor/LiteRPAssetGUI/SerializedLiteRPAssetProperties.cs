using UnityEditor;
using UnityEditor.Rendering;

namespace LiteRP.Editor
{
    internal static class LiteRPAssetProperties
    {
        public static readonly string RenderingPath = "m_RenderingPath";
        public static readonly string UseSRPBatcher = "m_UseSRPBatcher";
        public static readonly string UseGPUDriven = "m_GPUDrivenMode";
        public static readonly string UseSmallMeshScreenPercentage = "m_SmallMeshScreenPercentage";
        public static readonly string UseGPUDrivenEnableOCInCams = "m_GPUDrivenEnableOcclusionCullingInCams";
        
        public static readonly string AntiAliasing = "m_AntiAliasing";
        public static readonly string MSAAAQuality = "m_MSAA";
        
        // Shadow Settings
        public static readonly string MainLightShadowEnabled = "m_MainLightShadowEnable";
        public static readonly string MainLightShadowmapResolution = "m_MainLightShadowmapResolution";
        public static readonly string MainLightShadowDistance = "m_MainLightShadowDistance";
        public static readonly string MainLightShadowCascadesCount = "m_MainLightShadowCascadesCount";
        public static readonly string MainLightShadowCascades2Split = "m_MainLightCascade2Split";
        public static readonly string MainLightShadowCascades3Split = "m_MainLightCascade3Split";
        public static readonly string MainLightShadowCascades4Split = "m_MainLightCascade4Split";
        public static readonly string MainLightShadowCascadesBorder = "m_MainLightCascadeBorder";
        public static readonly string MainLightShadowDepthBias = "m_MainLightShadowDepthBias";
        public static readonly string MainLightShadowNormalBias = "m_MainLightShadowNormalBias";
        
        public static readonly string SupportsSoftShadows = "m_SoftShadowSupported";
        public static readonly string SoftShadowQuality = "m_SoftShadowQuality";
        
        // Other Settings
        public static readonly string VolumeFrameworkUpdateMode = "m_VolumeFrameworkUpdateMode";
        public static readonly string VolumeProfile = "m_VolumeProfile";
    }

    internal class SerializedLiteRPAssetProperties
    {
        public LiteRPAsset rpAsset { get; }
        public SerializedObject serializedObject { get; }
        
        // Pipeline Settings
        public SerializedProperty renderingPath { get; }
        public SerializedProperty srpBatcher { get; }
        public SerializedProperty gpuDrivenMode { get; }
        public SerializedProperty smallMeshScreenPercentage { get; }
        public SerializedProperty gpuDrivenEnableOCInCams { get; }
        
        // Quality Settings
        public SerializedProperty antiAliasing { get; }
        public SerializedProperty msaaaQuality { get; }
        
        // Shadow Settings
        // Shadow Settings
        public SerializedProperty mainLightShadowEnabled { get; }
        public SerializedProperty mainLightShadowmapResolution { get; }
        public SerializedProperty mainLightShadowDistance { get; }
        public SerializedProperty mainLightShadowCascadesCount { get; }
        public SerializedProperty mainLightShadowCascade2Split { get; }
        public SerializedProperty mainLightShadowCascade3Split { get; }
        public SerializedProperty mainLightShadowCascade4Split { get; }
        public SerializedProperty mainLightShadowCascadeBorder { get; }
        public SerializedProperty mainLightShadowDepthBias { get; }
        public SerializedProperty mainLightShadowNormalBias { get; }
        
        public SerializedProperty supportsSoftShadows { get; }
        public SerializedProperty softShadowQuality { get; }
        
        // Other Settings
        public EditorPrefBoolFlags<EditorUtils.Unit> state;
        
        public SerializedProperty volumeFrameworkUpdateModeProp { get; }
        public SerializedProperty volumeProfileProp { get; }

        public SerializedLiteRPAssetProperties(SerializedObject serializedObject)
        {
            rpAsset = serializedObject.targetObject as LiteRPAsset;
            this.serializedObject = serializedObject;
            
            // Pipeline Settings
            renderingPath = serializedObject.FindProperty(LiteRPAssetProperties.RenderingPath);
            srpBatcher = serializedObject.FindProperty(LiteRPAssetProperties.UseSRPBatcher);
            gpuDrivenMode = serializedObject.FindProperty(LiteRPAssetProperties.UseGPUDriven);
            smallMeshScreenPercentage = serializedObject.FindProperty(LiteRPAssetProperties.UseSmallMeshScreenPercentage);
            gpuDrivenEnableOCInCams = serializedObject.FindProperty(LiteRPAssetProperties.UseGPUDrivenEnableOCInCams);
            
            // Quality Settings
            antiAliasing = serializedObject.FindProperty(LiteRPAssetProperties.AntiAliasing);
            msaaaQuality = serializedObject.FindProperty(LiteRPAssetProperties.MSAAAQuality);
            
            // Shadow Settings
            mainLightShadowEnabled = serializedObject.FindProperty(LiteRPAssetProperties.MainLightShadowEnabled);
            mainLightShadowmapResolution = serializedObject.FindProperty(LiteRPAssetProperties.MainLightShadowmapResolution);
            mainLightShadowDistance = serializedObject.FindProperty(LiteRPAssetProperties.MainLightShadowDistance);
            mainLightShadowCascadesCount = serializedObject.FindProperty(LiteRPAssetProperties.MainLightShadowCascadesCount);
            mainLightShadowCascade2Split = serializedObject.FindProperty(LiteRPAssetProperties.MainLightShadowCascades2Split);
            mainLightShadowCascade3Split = serializedObject.FindProperty(LiteRPAssetProperties.MainLightShadowCascades3Split);
            mainLightShadowCascade4Split = serializedObject.FindProperty(LiteRPAssetProperties.MainLightShadowCascades4Split);
            mainLightShadowCascadeBorder = serializedObject.FindProperty(LiteRPAssetProperties.MainLightShadowCascadesBorder);
            mainLightShadowDepthBias = serializedObject.FindProperty(LiteRPAssetProperties.MainLightShadowDepthBias);
            mainLightShadowNormalBias = serializedObject.FindProperty(LiteRPAssetProperties.MainLightShadowNormalBias);
            
            supportsSoftShadows = serializedObject.FindProperty(LiteRPAssetProperties.SupportsSoftShadows);
            softShadowQuality = serializedObject.FindProperty(LiteRPAssetProperties.SoftShadowQuality);
            
            // Other Settings
            string Key = "ShadowSettings_Unit:UI_State";
            state = new EditorPrefBoolFlags<EditorUtils.Unit>(Key);
            
            volumeFrameworkUpdateModeProp = serializedObject.FindProperty(LiteRPAssetProperties.VolumeFrameworkUpdateMode);
            volumeProfileProp = serializedObject.FindProperty(LiteRPAssetProperties.VolumeProfile);
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