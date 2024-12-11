using System;
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
            QualitySettings = 1 << 2,
            ShadowSettings = 1 << 3
        }

        internal static class Styles
        {
            public static GUIContent renderPipelineSettingsText = EditorGUIUtility.TrTextContent("RenderPipelineSettings", "Settings for the render pipeline.");
            public static GUIContent qualitySettingsText = EditorGUIUtility.TrTextContent("QualitySettings", "Settings for the quality.");
            public static GUIContent shadowSettingsText = EditorGUIUtility.TrTextContent("ShadowSettings", "Settings for the shadow.");
            
            // RenderPipelineSettings
            public static GUIContent srpBatcherText = EditorGUIUtility.TrTextContent("SRP Batcher", "Enable the SRP Batcher.");
            public static GUIContent gpuDrivenModeText = EditorGUIUtility.TrTextContent("GPU Driven Mode", "Enable the GPU Driven Mode.");
            public static GUIContent smallMeshScreenPercentageText = EditorGUIUtility.TrTextContent("Small Mesh Screen Percentage", "The percentage of the screen that small meshes cover.");
            public static GUIContent gpuDrivenEnableOCInCamsText = EditorGUIUtility.TrTextContent("GPU Occulsion Culling In Cameras", "Enable the GPU Occulsion Culling In Cameras.");
            
            // Quality Settings
            public static GUIContent antiAliasingText = EditorGUIUtility.TrTextContent("Anti Aliasing", "The anti aliasing mode.");
            public static GUIContent msaaQualityText = EditorGUIUtility.TrTextContent("MSAA Quality", "The MSAA quality setting for the pipeline.");
            
            // ShadowSettings
            public static GUIContent mainLightShadowEnabledText = EditorGUIUtility.TrTextContent("Main Light Shadow", "Enable the main light shadow.");
            public static GUIContent mainLightShadowmapResolutionText = EditorGUIUtility.TrTextContent("Main Light ShadowMap Resolution", "The resolution of the main light shadowmap.");
            public static GUIContent mainLightShadowDistanceText = EditorGUIUtility.TrTextContent("Main Light Shadow Distance", "The distance of the main light shadow.");
            public static GUIContent shadowWorkingUnitText = EditorGUIUtility.TrTextContent("Working Unit", "The unit in which Unity measures the shadow cascade distances. The exception is Max Distance, which will still be in meters.");
            public static GUIContent mainLightShadowCascadesText = EditorGUIUtility.TrTextContent("Cascade Count", "The number of cascades used in the main light shadow.");
            public static GUIContent mainLightShadowDepthBiasText = EditorGUIUtility.TrTextContent("Depth Bias", "Controls the distance at which the shadows will be pushed away from the light. Useful for avoiding false self-shadowing artifacts.");
            public static GUIContent mainLightShadowNormalBiasText = EditorGUIUtility.TrTextContent("Normal Bias", "Controls distance at which the shadow casting surfaces will be shrunk along the surface normal. Useful for avoiding false self-shadowing artifacts.");
            public static GUIContent supportsSoftShadowsText = EditorGUIUtility.TrTextContent("Soft Shadows", "If enabled pipeline will perform shadow filtering. Otherwise all lights that cast shadows will fallback to perform a single shadow sample.");
            public static GUIContent softShadowsQualityText = EditorGUIUtility.TrTextContent("Quality", "Default shadow quality setting for Lights.");
            public static GUIContent[] softShadowsQualityAssetOptions =
            {
                EditorGUIUtility.TrTextContent(nameof(SoftShadowQuality.Low)),
                EditorGUIUtility.TrTextContent(nameof(SoftShadowQuality.Medium)),
                EditorGUIUtility.TrTextContent(nameof(SoftShadowQuality.High))
            };
            public static int[] SoftShadowsQualityAssetValues =  { (int)SoftShadowQuality.Low, (int)SoftShadowQuality.Medium, (int)SoftShadowQuality.High };
            
            // Error Message
            public static GUIContent brgShaderStrippingErrorMessage =
                EditorGUIUtility.TrTextContent("\"BatchRendererGroup Variants\" setting must be \"Keep All\". To fix, modify Graphics settings and set \"BatchRendererGroup Variants\" to \"Keep All\".");
            public static GUIContent staticBatchingInfoMessage =
                EditorGUIUtility.TrTextContent("Static Batching is not recommended when using GPU draw submission modes, performance may improve if Static Batching is disabled in Player Settings.");
        }

        static readonly ExpandedState<Expandable, LiteRPAsset> k_ExpandedState = new(Expandable.RenderPipelineSettings, "LiteRP");
        public static readonly CED.IDrawer Inspector = CED.Group(
            CED.FoldoutGroup(Styles.renderPipelineSettingsText, Expandable.RenderPipelineSettings, k_ExpandedState, DrawRenderPipelineSettings),
            CED.FoldoutGroup(Styles.qualitySettingsText, Expandable.QualitySettings, k_ExpandedState, DrawQualitySettings),
            CED.FoldoutGroup(Styles.shadowSettingsText, Expandable.ShadowSettings, k_ExpandedState, DrawShadowSettings));
        
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
            }
        }

        static void DrawQualitySettings(SerializedLiteRPAssetProperties serialized, UnityEditor.Editor ownerEditor)
        {
            if (ownerEditor is LiteRPAssetEditor liteRPAssetEditor)
            {
                // EditorGUILayout.PropertyField(serialized.antiAliasing, Styles.antiAliasingText);
                EditorGUILayout.PropertyField(serialized.msaaaQuality, Styles.msaaQualityText);
                // EditorGUILayout.Space();
            }
        }

        static void DrawShadowSettings(SerializedLiteRPAssetProperties serialized, UnityEditor.Editor ownerEditor)
        {
            if (ownerEditor is LiteRPAssetEditor liteRPAssetEditor)
            {
                EditorGUILayout.Space();
                
                bool disableGroup = false;
                disableGroup |= !SystemInfo.supportsShadows;

                EditorGUI.BeginDisabledGroup(disableGroup);
                EditorGUILayout.PropertyField(serialized.mainLightShadowEnabled, Styles.mainLightShadowEnabledText);
                if (serialized.mainLightShadowEnabled.boolValue)
                {
                    EditorGUILayout.PropertyField(serialized.mainLightShadowmapResolution, Styles.mainLightShadowmapResolutionText);
                    EditorGUILayout.PropertyField(serialized.mainLightShadowDistance, Styles.mainLightShadowDistanceText);
                    EditorUtils.Unit unit = EditorUtils.Unit.Metric;
                    int cascadeCount = serialized.mainLightShadowCascadesCount.intValue;
                    if (cascadeCount != 0)
                    {
                        EditorGUI.BeginChangeCheck();
                        unit = (EditorUtils.Unit)EditorGUILayout.EnumPopup(Styles.shadowWorkingUnitText, serialized.state.value);
                        if (EditorGUI.EndChangeCheck())
                        {
                            serialized.state.value = unit;
                        }
                    }

                    EditorGUILayout.IntSlider(serialized.mainLightShadowCascadesCount, LiteRPAsset.k_ShadowCascadeMinCount, LiteRPAsset.k_ShadowCascadeMaxCount, Styles.mainLightShadowCascadesText);
                    EditorGUI.indentLevel++;

                    bool useMetric = unit == EditorUtils.Unit.Metric;
                    float baseMetric = serialized.mainLightShadowDistance.floatValue;
                    int cascadeSplitCount = cascadeCount - 1;

                    DrawCascadeSliders(serialized, cascadeSplitCount, useMetric, baseMetric);

                    EditorGUI.indentLevel--;
                    DrawCascades(serialized, cascadeCount, useMetric, baseMetric);
                    
                    serialized.mainLightShadowDepthBias.floatValue = EditorGUILayout.Slider(Styles.mainLightShadowDepthBiasText, serialized.mainLightShadowDepthBias.floatValue, 0.0f, LiteRPAsset.k_MaxShadowBias);
                    serialized.mainLightShadowNormalBias.floatValue = EditorGUILayout.Slider(Styles.mainLightShadowNormalBiasText, serialized.mainLightShadowNormalBias.floatValue, 0.0f, LiteRPAsset.k_MaxShadowBias);
                    // Soft Shadows
                    EditorGUILayout.PropertyField(serialized.supportsSoftShadows, Styles.supportsSoftShadowsText);
                    if (serialized.supportsSoftShadows.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        DrawShadowsSoftShadowQuality(serialized, ownerEditor);
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        static void DrawCascadeSliders(SerializedLiteRPAssetProperties serialized, int splitCount, bool useMetric, float baseMetric)
        {
            Vector4 shadowCascadeSplit = Vector4.one;
            if (splitCount == 3)
                shadowCascadeSplit = new Vector4(serialized.mainLightShadowCascade4Split.vector3Value.x, serialized.mainLightShadowCascade4Split.vector3Value.y, serialized.mainLightShadowCascade4Split.vector3Value.z, 1);
            else if (splitCount == 2)
                shadowCascadeSplit = new Vector4(serialized.mainLightShadowCascade3Split.vector2Value.x, serialized.mainLightShadowCascade3Split.vector2Value.y, 1, 0);
            else if (splitCount == 1)
                shadowCascadeSplit = new Vector4(serialized.mainLightShadowCascade2Split.floatValue, 1, 0, 0);

            float splitBias = 0.001f;
            float invBaseMetric = baseMetric == 0 ? 0 : 1f / baseMetric;

            // Ensure correct split order
            shadowCascadeSplit[0] = Mathf.Clamp(shadowCascadeSplit[0], 0f, shadowCascadeSplit[1] - splitBias);
            shadowCascadeSplit[1] = Mathf.Clamp(shadowCascadeSplit[1], shadowCascadeSplit[0] + splitBias, shadowCascadeSplit[2] - splitBias);
            shadowCascadeSplit[2] = Mathf.Clamp(shadowCascadeSplit[2], shadowCascadeSplit[1] + splitBias, shadowCascadeSplit[3] - splitBias);


            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < splitCount; ++i)
            {
                float value = shadowCascadeSplit[i];

                float minimum = i == 0 ? 0 : shadowCascadeSplit[i - 1] + splitBias;
                float maximum = i == splitCount - 1 ? 1 : shadowCascadeSplit[i + 1] - splitBias;

                if (useMetric)
                {
                    float valueMetric = value * baseMetric;
                    valueMetric = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent($"Split {i + 1}", "The distance where this cascade ends and the next one starts."), valueMetric, 0f, baseMetric, null);

                    shadowCascadeSplit[i] = Mathf.Clamp(valueMetric * invBaseMetric, minimum, maximum);
                }
                else
                {
                    float valueProcentage = value * 100f;
                    valueProcentage = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent($"Split {i + 1}", "The distance where this cascade ends and the next one starts."), valueProcentage, 0f, 100f, null);

                    shadowCascadeSplit[i] = Mathf.Clamp(valueProcentage * 0.01f, minimum, maximum);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                switch (splitCount)
                {
                    case 3:
                        serialized.mainLightShadowCascade4Split.vector3Value = shadowCascadeSplit;
                        break;
                    case 2:
                        serialized.mainLightShadowCascade3Split.vector2Value = shadowCascadeSplit;
                        break;
                    case 1:
                        serialized.mainLightShadowCascade2Split.floatValue = shadowCascadeSplit.x;
                        break;
                }
            }

            var borderValue = serialized.mainLightShadowCascadeBorder.floatValue;

            EditorGUI.BeginChangeCheck();
            if (useMetric)
            {
                var lastCascadeSplitSize = splitCount == 0 ? baseMetric : (1.0f - shadowCascadeSplit[splitCount - 1]) * baseMetric;
                var invLastCascadeSplitSize = lastCascadeSplitSize == 0 ? 0 : 1f / lastCascadeSplitSize;
                float valueMetric = borderValue * lastCascadeSplitSize;
                valueMetric = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent("Last Border", "The distance of the last cascade."), valueMetric, 0f, lastCascadeSplitSize, null);

                borderValue = valueMetric * invLastCascadeSplitSize;
            }
            else
            {
                float valueProcentage = borderValue * 100f;
                valueProcentage = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent("Last Border", "The distance of the last cascade."), valueProcentage, 0f, 100f, null);

                borderValue = valueProcentage * 0.01f;
            }

            if (EditorGUI.EndChangeCheck())
            {
                serialized.mainLightShadowCascadeBorder.floatValue = borderValue;
            }
        }

        static void DrawCascades(SerializedLiteRPAssetProperties serialized, int cascadeCount, bool useMetric, float baseMetric)
        {
            var cascades = new ShadowCascadeGUI.Cascade[cascadeCount];

            Vector3 shadowCascadeSplit = Vector3.zero;
            if (cascadeCount == 4)
                shadowCascadeSplit = serialized.mainLightShadowCascade4Split.vector3Value;
            else if (cascadeCount == 3)
                shadowCascadeSplit = serialized.mainLightShadowCascade3Split.vector2Value;
            else if (cascadeCount == 2)
                shadowCascadeSplit.x = serialized.mainLightShadowCascade2Split.floatValue;
            else
                shadowCascadeSplit.x = serialized.mainLightShadowCascade2Split.floatValue;

            float lastCascadePartitionSplit = 0;
            for (int i = 0; i < cascadeCount - 1; ++i)
            {
                cascades[i] = new ShadowCascadeGUI.Cascade()
                {
                    size = i == 0 ? shadowCascadeSplit[i] : shadowCascadeSplit[i] - lastCascadePartitionSplit, // Calculate the size of cascade
                    borderSize = 0,
                    cascadeHandleState = ShadowCascadeGUI.HandleState.Enabled,
                    borderHandleState = ShadowCascadeGUI.HandleState.Hidden,
                };
                lastCascadePartitionSplit = shadowCascadeSplit[i];
            }

            // Last cascade is special
            var lastCascade = cascadeCount - 1;
            cascades[lastCascade] = new ShadowCascadeGUI.Cascade()
            {
                size = lastCascade == 0 ? 1.0f : 1 - shadowCascadeSplit[lastCascade - 1], // Calculate the size of cascade
                borderSize = serialized.mainLightShadowCascadeBorder.floatValue,
                cascadeHandleState = ShadowCascadeGUI.HandleState.Hidden,
                borderHandleState = ShadowCascadeGUI.HandleState.Enabled,
            };

            EditorGUI.BeginChangeCheck();
            ShadowCascadeGUI.DrawCascades(ref cascades, useMetric, baseMetric);
            if (EditorGUI.EndChangeCheck())
            {
                if (cascadeCount == 4)
                    serialized.mainLightShadowCascade4Split.vector3Value = new Vector3(
                        cascades[0].size,
                        cascades[0].size + cascades[1].size,
                        cascades[0].size + cascades[1].size + cascades[2].size
                    );
                else if (cascadeCount == 3)
                    serialized.mainLightShadowCascade3Split.vector2Value = new Vector2(
                        cascades[0].size,
                        cascades[0].size + cascades[1].size
                    );
                else if (cascadeCount == 2)
                    serialized.mainLightShadowCascade2Split.floatValue = cascades[0].size;

                serialized.mainLightShadowCascadeBorder.floatValue = cascades[lastCascade].borderSize;
            }
        }
        
        static void DrawShadowsSoftShadowQuality(SerializedLiteRPAssetProperties serialized, UnityEditor.Editor ownerEditor)
        {
            int selectedAssetSoftShadowQuality = serialized.softShadowQuality.intValue;
            Rect r = EditorGUILayout.GetControlRect(true);
            EditorGUI.BeginProperty(r, Styles.softShadowsQualityText, serialized.softShadowQuality);
            {
                using (var checkScope = new EditorGUI.ChangeCheckScope())
                {
                    selectedAssetSoftShadowQuality = EditorGUI.IntPopup(r, Styles.softShadowsQualityText, selectedAssetSoftShadowQuality, Styles.softShadowsQualityAssetOptions, Styles.SoftShadowsQualityAssetValues);
                    if (checkScope.changed)
                    {
                        serialized.softShadowQuality.intValue = Math.Clamp(selectedAssetSoftShadowQuality, (int)SoftShadowQuality.Low, (int)SoftShadowQuality.High);
                    }
                }
            }
            EditorGUI.EndProperty();
        }
    }
}