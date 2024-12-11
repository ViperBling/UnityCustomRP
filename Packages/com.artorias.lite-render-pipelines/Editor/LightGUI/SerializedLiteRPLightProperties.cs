using UnityEditor;
using UnityEditor.Rendering;

using LiteRP.AdditionalData;

namespace LiteRP.Editor
{
    public class SerializedLiteRPLightProperties : ISerializedLight
    {
        public LightEditor.Settings settings { get; }
        public SerializedObject serializedObject { get; }
        public SerializedObject serializedAdditionalDataObject { get; }
        public SerializedProperty intensity { get; }
        
        public AdditionalLightData[] m_AdditionalDataArray { get; private set; }
        public AdditionalLightData m_AdditionalData => m_AdditionalDataArray[0];
        
        // LiteRP Properties
        public SerializedProperty useAdditionalDataProp { get; }                // 灯光是否使用LiteRP Asset文件中定义的Shadow bias Settings
        public SerializedProperty additionalLightShadowResTierProp { get; }
        public SerializedProperty softShadowQualityProp { get; }
        public SerializedProperty lightCookieSizeProp { get; }
        public SerializedProperty lightCookieOffsetProp { get; }
        
        public SerializedProperty renderingLayers { get; }
        public SerializedProperty customShadowLayers { get; }
        public SerializedProperty shadowRenderingLayers { get; }

        public SerializedLiteRPLightProperties(SerializedObject serializedObject, LightEditor.Settings lightSettings)
        {
            this.settings = lightSettings;
            settings.OnEnable();

            this.serializedObject = serializedObject;

            m_AdditionalDataArray = CoreEditorUtils.GetAdditionalData<AdditionalLightData>(serializedObject.targetObjects);
            serializedAdditionalDataObject = new SerializedObject(m_AdditionalDataArray);

            intensity = serializedObject.FindProperty("m_Intensity");

            useAdditionalDataProp = serializedAdditionalDataObject.FindProperty("m_UsePipelineSettings");
            additionalLightShadowResTierProp = serializedAdditionalDataObject.FindProperty("m_AdditionalLightsShadowResolutionTier");
            softShadowQualityProp = serializedAdditionalDataObject.FindProperty("m_SoftShadowQuality");
            lightCookieSizeProp = serializedAdditionalDataObject.FindProperty("m_LightCookieSize");
            lightCookieOffsetProp = serializedAdditionalDataObject.FindProperty("m_LightCookieOffset");
            
            renderingLayers = serializedAdditionalDataObject.FindProperty("m_RenderingLayers");
            customShadowLayers = serializedAdditionalDataObject.FindProperty("m_CustomShadowLayers");
            shadowRenderingLayers = serializedAdditionalDataObject.FindProperty("m_ShadowRenderingLayers");
            
            settings.ApplyModifiedProperties();
        }
        
        public void Update()
        {
            serializedObject.Update();
            serializedAdditionalDataObject.Update();
            settings.Update();
        }

        public void Apply()
        {
            serializedObject.ApplyModifiedProperties();
            serializedAdditionalDataObject.ApplyModifiedProperties();
            settings.ApplyModifiedProperties();
        }
    }
}