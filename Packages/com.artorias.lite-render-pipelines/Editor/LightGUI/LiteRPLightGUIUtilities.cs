using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace LiteRP.Editor
{
    using CED = CoreEditorDrawer<SerializedLiteRPLightProperties>;

    internal static class LiteRPLightGUIUtilities
    {
        enum Expandable
        {
            General = 1 << 0,
            Shape = 1 << 1,
            Emission = 1 << 2,
            Rendering = 1 << 3,
            Shadows = 1 << 4,
            LightCookie = 1 << 5
        }
        
        internal static class Styles
        {
            public static readonly GUIContent generalHeader = EditorGUIUtility.TrTextContent("General");
            
            public static readonly GUIContent type = EditorGUIUtility.TrTextContent("type", "Specifies the current type of light. Possible types are Directional, Spot, Point, and Area lights.");

            public static readonly GUIContent areaLightShapeContent = EditorGUIUtility.TrTextContent("Shape", "Specifies the shape of the Area light. Possible types are Rectangle and Disc.");
            public static readonly GUIContent[] lightTypeTitles = { EditorGUIUtility.TrTextContent("Spot"), EditorGUIUtility.TrTextContent("Directional"), EditorGUIUtility.TrTextContent("Point"), EditorGUIUtility.TrTextContent("Area (baked only)") };
            public static readonly int[] lightTypeValues = { (int)LightType.Spot, (int)LightType.Directional, (int)LightType.Point, (int)LightType.Rectangle };

            public static readonly GUIContent[] areaLightShapeTitles = { EditorGUIUtility.TrTextContent("Rectangle"), EditorGUIUtility.TrTextContent("Disc") };
            public static readonly int[] areaLightShapeValues = { (int)LightType.Rectangle, (int)LightType.Disc };

            public static readonly GUIContent spotAngle = EditorGUIUtility.TrTextContent("Spot Angle", "Controls the angle in degrees at the base of a Spot light's cone.");

            public static readonly GUIContent bakingWarning = EditorGUIUtility.TrTextContent("Light mode is currently overridden to Realtime mode. Enable Baked Global Illumination to use Mixed or Baked light modes.");
            public static readonly GUIContent disabledLightWarning = EditorGUIUtility.TrTextContent("Lighting has been disabled in at least one Scene view. Any changes applied to lights in the Scene will not be updated in these views until Lighting has been enabled again.");
            public static readonly GUIContent sunSourceWarning = EditorGUIUtility.TrTextContent("This light is set as the current Sun Source, which requires a directional light. Go to the Lighting Window's Environment settings to edit the Sun Source.");
            public static readonly GUIContent cullingMask = EditorGUIUtility.TrTextContent("Culling Mask", "Specifies which lights are culled per camera. To control exclude certain lights affecting certain objects, use Rendering Layers instead, which is supported across all rendering paths.");
            public static readonly GUIContent cullingMaskWarning = EditorGUIUtility.TrTextContent("Culling Mask should be used to control which lights are culled per camera. If you want to exclude certain lights from affecting certain objects, use Rendering Layers on the Light, and Rendering Layer Mask on the Mesh Renderer.");

            public static readonly GUIContent shadowRealtimeSettings = EditorGUIUtility.TrTextContent("Realtime Shadows", "Settings for realtime direct shadows.");
            public static readonly GUIContent shadowStrength = EditorGUIUtility.TrTextContent("Strength", "Controls how dark the shadows cast by the light will be.");
            public static readonly GUIContent shadowNearPlane = EditorGUIUtility.TrTextContent("Near Plane", "Controls the value for the near clip plane when rendering shadows. Currently clamped to 0.1 units or 1% of the lights range property, whichever is lower.");
            public static readonly GUIContent shadowNormalBias = EditorGUIUtility.TrTextContent("Normal", "Determines the bias this Light applies along the normal of surfaces it illuminates. This is ignored for point lights.");
            public static readonly GUIContent shadowDepthBias = EditorGUIUtility.TrTextContent("Depth", "Determines the bias at which shadows are pushed away from the shadow-casting Game Object along the line from the Light.");
            public static readonly GUIContent shadowInfo = EditorGUIUtility.TrTextContent("Unity might reduce the Light's shadow resolution to ensure that shadow maps fit in the shadow atlas. Consider this when selecting the the size of the shadow atlas, the shadow resolution of Lights, the number of Lights in your scene and whether you use soft shadows.");
            

            public static GUIContent softShadowQuality = EditorGUIUtility.TrTextContent("Soft Shadows Quality", "Controls the filtering quality of soft shadows. Higher quality has lower performance.");

            // Bias (default or custom)
            public static GUIContent shadowBias = EditorGUIUtility.TrTextContent("Bias", "Select if the Bias should use the settings from the Pipeline Asset or Custom settings.");
            public static int[] optionDefaultValues = { 0, 1 };
            public static GUIContent[] displayedDefaultOptions =
            {
                EditorGUIUtility.TrTextContent("Custom"),
                EditorGUIUtility.TrTextContent("Use settings from Render Pipeline Asset")
            };

            public static readonly GUIContent customShadowLayers = EditorGUIUtility.TrTextContent("Custom Shadow Layers", "When enabled, you can use the Layer property below to specify the layers for shadows seperately to lighting. When disabled, the Light Layer property in the General section specifies the layers for both lighting and for shadows.");
            public static readonly GUIContent shadowLayer = EditorGUIUtility.TrTextContent("Layer", "Specifies the light layer to use for shadows.");

            public static readonly GUIContent lightCookieSize = EditorGUIUtility.TrTextContent("Cookie Size", "Controls the size of the cookie mask currently assigned to the light.");
            public static readonly GUIContent lightCookieOffset = EditorGUIUtility.TrTextContent("Cookie Offset", "Controls the offset of the cookie mask currently assigned to the light.");
            /// <summary>Title with "Rendering Layer"</summary>
            public static readonly GUIContent renderingLayers = EditorGUIUtility.TrTextContent("Rendering Layers", "Select the Rendering Layers that the Light affects. This Light affects objects where at least one Rendering Layer value matches.");
            public static readonly GUIContent renderingLayersDisabled = EditorGUIUtility.TrTextContent("Rendering Layers", "Rendering Layers are disabled because they have a small GPU performance cost. To enable this setting, go to the active Universal Render Pipeline Asset, and enable Lighting -> Use Rendering Layers.");
        }
        
        static readonly ExpandedState<Expandable, Light> k_ExpandedState = new(~-1, "LiteRP");
        public static readonly CED.IDrawer Inspector = CED.Group(CED.Conditional((_, __) => 
                {
                    if (SceneView.lastActiveSceneView == null) return false;
                    
                    var sceneLighting = SceneView.lastActiveSceneView.sceneLighting;
                    return !sceneLighting;
                },
                (_, __) => EditorGUILayout.HelpBox(Styles.disabledLightWarning.text, MessageType.Warning)),
            CED.FoldoutGroup(LightUI.Styles.generalHeader, Expandable.General, k_ExpandedState, DrawGeneralContent),
            CED.Conditional(
                (serializedLight, editor) => !serializedLight.settings.lightType.hasMultipleDifferentValues && serializedLight.settings.light.type == LightType.Spot,
                CED.FoldoutGroup(LightUI.Styles.shapeHeader, Expandable.Shape, k_ExpandedState, DrawSpotShapeContent)),
            CED.Conditional(
                (serializedLight, editor) =>
                {
                    if (serializedLight.settings.lightType.hasMultipleDifferentValues) return false;
                    
                    var lightType = serializedLight.settings.light.type;
                    return lightType == LightType.Rectangle || lightType == LightType.Disc;
                },
                CED.FoldoutGroup(LightUI.Styles.shapeHeader, Expandable.Shape, k_ExpandedState, DrawAreaShapeContent)),
            CED.FoldoutGroup(LightUI.Styles.emissionHeader, Expandable.Emission, k_ExpandedState,
                CED.Group(LightUI.DrawColor, DrawEmissionContent)),
            CED.FoldoutGroup(LightUI.Styles.renderingHeader, Expandable.Rendering, k_ExpandedState, DrawRenderingContent),
            CED.Conditional(
                (serializedLight, editor) => !serializedLight.settings.lightType.hasMultipleDifferentValues && serializedLight.settings.light.type == LightType.Directional,
                CED.FoldoutGroup(LightUI.Styles.shadowHeader, Expandable.Shadows, k_ExpandedState, DrawShadowsContent))
            
        );
        
        static Func<int> s_SetGizmosDirty = SetGizmosDirty();
        static Func<int> SetGizmosDirty()
        {
            var type = Type.GetType("UnityEditor.AnnotationUtility,UnityEditor");
            var method = type.GetMethod("SetGizmosDirty", BindingFlags.Static | BindingFlags.NonPublic);
            var lambda = Expression.Lambda<Func<int>>(Expression.Call(method));
            return lambda.Compile();
        }

        static void DrawGeneralContent(SerializedLiteRPLightProperties serializedLight, UnityEditor.Editor owner)
        {
            int selectedLightType = serializedLight.settings.lightType.intValue;
            if (!Styles.lightTypeValues.Contains(serializedLight.settings.lightType.intValue))
            {
                if (serializedLight.settings.lightType.intValue == (int)LightType.Disc)
                {
                    selectedLightType = (int)LightType.Rectangle;
                }
            }
            
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, Styles.type, serializedLight.settings.lightType);
            EditorGUI.BeginChangeCheck();
            int type;
            if (Styles.lightTypeValues.Contains(selectedLightType))
            {
                type = EditorGUI.IntPopup(rect, Styles.type, selectedLightType, Styles.lightTypeTitles, Styles.lightTypeValues);
            }
            else
            {
                string currentTitle = ((LightType)selectedLightType).ToString();
                GUIContent[] titles = Styles.lightTypeTitles.Append(EditorGUIUtility.TrTextContent(currentTitle)).ToArray();
                int[] values = Styles.lightTypeValues.Append(selectedLightType).ToArray();
                type = EditorGUI.IntPopup(rect, Styles.type, selectedLightType, titles, values);
            }
            if (EditorGUI.EndChangeCheck())
            {
                s_SetGizmosDirty();
                serializedLight.settings.lightType.intValue = type;
            }
            EditorGUI.EndProperty();
            
            if (!Styles.lightTypeValues.Contains(type))
            {
                EditorGUILayout.HelpBox(
                    "This light type is not supported in the current active render pipeline. Change the light type or the active Render Pipeline to use this light.",
                    MessageType.Info
                );
            }

            Light light = serializedLight.settings.light;
            var lightType = light.type;
            if (LightType.Directional != lightType && light == RenderSettings.sun)
            {
                EditorGUILayout.HelpBox(Styles.sunSourceWarning.text, MessageType.Warning);
            }

            if (!serializedLight.settings.lightType.hasMultipleDifferentValues)
            {
                using (new EditorGUI.DisabledScope(serializedLight.settings.isAreaLightType))
                    serializedLight.settings.DrawLightmapping();

                if (serializedLight.settings.isAreaLightType && serializedLight.settings.lightmapping.intValue != (int)LightmapBakeType.Baked)
                {
                    serializedLight.settings.lightmapping.intValue = (int)LightmapBakeType.Baked;
                    serializedLight.Apply();
                }
            }
        }
        
        static void DrawSpotShapeContent(SerializedLiteRPLightProperties serializedLight, UnityEditor.Editor owner)
        {
            serializedLight.settings.DrawInnerAndOuterSpotAngle();
        }
        
        static void DrawAreaShapeContent(SerializedLiteRPLightProperties serializedLight, UnityEditor.Editor owner)
        {
            int selectedShape = serializedLight.settings.isAreaLightType ? serializedLight.settings.lightType.intValue : 0;

            // Handle all lights that are not in the default set
            if (!Styles.lightTypeValues.Contains(serializedLight.settings.lightType.intValue))
            {
                if (serializedLight.settings.lightType.intValue == (int)LightType.Disc)
                {
                    selectedShape = (int)LightType.Disc;
                }
            }

            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, Styles.areaLightShapeContent, serializedLight.settings.lightType);
            EditorGUI.BeginChangeCheck();
            int shape = EditorGUI.IntPopup(rect, Styles.areaLightShapeContent, selectedShape, Styles.areaLightShapeTitles, Styles.areaLightShapeValues);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(serializedLight.settings.light, "Adjust Light Shape");
                serializedLight.settings.lightType.intValue = shape;
            }
            EditorGUI.EndProperty();

            using (new EditorGUI.IndentLevelScope())
                serializedLight.settings.DrawArea();
        }
        
        static void DrawEmissionContent(SerializedLiteRPLightProperties serializedLight, UnityEditor.Editor owner)
        {
            serializedLight.settings.DrawIntensity();
            serializedLight.settings.DrawBounceIntensity();

            if (!serializedLight.settings.lightType.hasMultipleDifferentValues)
            {
                var lightType = serializedLight.settings.light.type;
                if (lightType != LightType.Directional)
                    serializedLight.settings.DrawRange();
            }
            //暂时不实现
            //DrawLightCookieContent(serializedLight, owner);
        }
        static void DrawLightCookieContent(SerializedLiteRPLightProperties serializedLight, UnityEditor.Editor owner)
        {
            var settings = serializedLight.settings;
            if (settings.lightType.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("Cannot multi edit light cookies from different light types.", MessageType.Info);
                return;
            }

            settings.DrawCookie();
        }
        
        static void DrawRenderingContent(SerializedLiteRPLightProperties serializedLight, UnityEditor.Editor owner)
        {
            serializedLight.settings.DrawRenderMode();
            EditorGUILayout.PropertyField(serializedLight.settings.cullingMask, Styles.cullingMask);
            if (serializedLight.settings.cullingMask.intValue != -1)
            {
                EditorGUILayout.HelpBox(Styles.cullingMaskWarning.text, MessageType.Info);
            }
        }
        
        static void DrawShadowsContent(SerializedLiteRPLightProperties serializedLight, UnityEditor.Editor owner)
        {
            if (serializedLight.settings.lightType.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("Cannot multi edit shadows from different light types.", MessageType.Info);
                return;
            }

            serializedLight.settings.DrawShadowsType();

            if (serializedLight.settings.shadowsType.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("Cannot multi edit different shadow types", MessageType.Info);
                return;
            }

            if (serializedLight.settings.light.shadows == LightShadows.None)
                return;

            var lightType = serializedLight.settings.light.type;
            if (lightType == LightType.Directional && !serializedLight.settings.isCompletelyBaked)
            {
                EditorGUILayout.LabelField(Styles.shadowRealtimeSettings, EditorStyles.boldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.Slider(serializedLight.settings.shadowsStrength, 0f, 1f, Styles.shadowStrength);

                    // Bias
                    DrawAdditionalShadowData(serializedLight, owner);

                    // this min bound should match the calculation in SharedLightData::GetNearPlaneMinBound()
                    float nearPlaneMinBound = Mathf.Min(0.01f * serializedLight.settings.range.floatValue, 0.1f);
                    EditorGUILayout.Slider(serializedLight.settings.shadowsNearPlane, nearPlaneMinBound, 10.0f, Styles.shadowNearPlane);
                    // Soft Shadow Quality
                    if (serializedLight.settings.light.shadows == LightShadows.Soft)
                        EditorGUILayout.PropertyField(serializedLight.softShadowQualityProp, Styles.softShadowQuality);
                }
            }

            if (!UnityEditor.Lightmapping.bakedGI && !serializedLight.settings.lightmapping.hasMultipleDifferentValues && serializedLight.settings.isBakedOrMixed)
                EditorGUILayout.HelpBox(Styles.bakingWarning.text, MessageType.Warning);
        }
        
        static void DrawAdditionalShadowData(SerializedLiteRPLightProperties serializedLight, UnityEditor.Editor editor)
        {
            // 0: Custom bias - 1: Bias values defined in Pipeline settings
            int selectedUseAdditionalData = serializedLight.m_AdditionalData.usePipelineSettings ? 1 : 0;
            Rect r = EditorGUILayout.GetControlRect(true);
            EditorGUI.BeginProperty(r, Styles.shadowBias, serializedLight.useAdditionalDataProp);
            {
                using (var checkScope = new EditorGUI.ChangeCheckScope())
                {
                    selectedUseAdditionalData = EditorGUI.IntPopup(r, Styles.shadowBias, selectedUseAdditionalData, Styles.displayedDefaultOptions, Styles.optionDefaultValues);
                    if (checkScope.changed)
                    {
                        Undo.RecordObjects(serializedLight.m_AdditionalDataArray, "Modified light additional data");
                        foreach (var additionData in serializedLight.m_AdditionalDataArray)
                            additionData.usePipelineSettings = selectedUseAdditionalData != 0;

                        serializedLight.Apply();
                        (editor as LiteRPLightEditor)?.ReconstructReferenceToAdditionalDataSO();
                    }
                }
            }
            EditorGUI.EndProperty();

            if (!serializedLight.useAdditionalDataProp.hasMultipleDifferentValues)
            {
                if (selectedUseAdditionalData != 1) // Custom Bias
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        using (var checkScope = new EditorGUI.ChangeCheckScope())
                        {
                            EditorGUILayout.Slider(serializedLight.settings.shadowsBias, 0f, 10f, Styles.shadowDepthBias);
                            EditorGUILayout.Slider(serializedLight.settings.shadowsNormalBias, 0f, 10f, Styles.shadowNormalBias);
                            if (checkScope.changed)
                                serializedLight.Apply();
                        }
                    }
                }
            }
        }
    }
}