using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace LiteRP.Editor
{
    using CED = CoreEditorDrawer<SerializedLiteRPAssetProperties>;
    
    internal static class LiteRPAssetGUIUtilities
    {
        enum Expandable
        {
            RenderPipelineSettings = 1 << 1,    
        }

        internal static class Styles
        {
            public static GUIContent renderPipelineSettingsText = EditorGUIUtility.TrTextContent("RenderPipelineSettings", "Settings for the render pipeline.");
            public static GUIContent srpBatcherText = EditorGUIUtility.TrTextContent("SRP Batcher", "Enable the SRP Batcher.");
            public static GUIContent gpuDrivenModeText = EditorGUIUtility.TrTextContent("GPU Driven Mode", "Enable the GPU Driven Mode.");
            public static GUIContent smallMeshScreenPercentageText = EditorGUIUtility.TrTextContent("Small Mesh Screen Percentage", "The percentage of the screen that small meshes cover.");
            public static GUIContent gpuDrivenEnableOCInCamsText = EditorGUIUtility.TrTextContent("GPU Occulsion Culling In Cameras", "Enable the GPU Occulsion Culling In Cameras.");
            
            // Quality Settings
            public static GUIContent antiAliasingText = EditorGUIUtility.TrTextContent("Anti Aliasing", "The Anti Aliasing quality.");
            
            // Error Message
            public static GUIContent brgShaderStrippingErrorMessage =
                EditorGUIUtility.TrTextContent("\"BatchRendererGroup Variants\" setting must be \"Keep All\". To fix, modify Graphics settings and set \"BatchRendererGroup Variants\" to \"Keep All\".");
            public static GUIContent staticBatchingInfoMessage =
                EditorGUIUtility.TrTextContent("Static Batching is not recommended when using GPU draw submission modes, performance may improve if Static Batching is disabled in Player Settings.");
        }

        static readonly ExpandedState<Expandable, LiteRPAsset> k_ExpandedState = new(Expandable.RenderPipelineSettings, "LiteRP");
        public static readonly CED.IDrawer Inspector = CED.Group(
            CED.FoldoutGroup(Styles.renderPipelineSettingsText, Expandable.RenderPipelineSettings, k_ExpandedState, DrawRenderPipelineSettings));
        
        static void DrawRenderPipelineSettings(SerializedLiteRPAssetProperties properties, UnityEditor.Editor ownerEditor)
        {
            if (ownerEditor is LiteRPAssetEditor liteRPAssetEditor)
            {
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(properties.srpBatcher, Styles.srpBatcherText);
                EditorGUILayout.PropertyField(properties.gpuDrivenMode, Styles.gpuDrivenModeText);

                var brgStrippingError = EditorGraphicsSettings.batchRendererGroupShaderStrippingMode != BatchRendererGroupStrippingMode.KeepAll;
                var staticBatchingWaring = PlayerSettings.GetStaticBatchingForPlatform(EditorUserBuildSettings.activeBuildTarget);

                if ((GPUResidentDrawerMode)properties.gpuDrivenMode.intValue != GPUResidentDrawerMode.Disabled)
                {
                    ++EditorGUI.indentLevel;
                    properties.smallMeshScreenPercentage.floatValue = Mathf.Clamp(EditorGUILayout.FloatField(Styles.smallMeshScreenPercentageText, properties.smallMeshScreenPercentage.floatValue), 0.0f, 20.0f);
                    EditorGUILayout.PropertyField(properties.gpuDrivenEnableOCInCams, Styles.gpuDrivenEnableOCInCamsText);
                    --EditorGUI.indentLevel;

                    if (brgStrippingError)
                    {
                        EditorGUILayout.HelpBox(Styles.brgShaderStrippingErrorMessage.text, MessageType.Warning, true);
                    }
                    if (staticBatchingWaring)
                    {
                        EditorGUILayout.HelpBox(Styles.staticBatchingInfoMessage.text, MessageType.Info, true);
                    }
                }
                EditorGUILayout.PropertyField(properties.antiAliasing, Styles.antiAliasingText);
            }
        }
    }
}