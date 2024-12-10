using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

using LiteRPSurfaceType = LiteRP.Editor.LiteRPShaderGUI.SurfaceType;
using LiteRPBlendMode = LiteRP.Editor.LiteRPShaderGUI.BlendMode;
using LiteRPRenderFace = LiteRP.Editor.LiteRPShaderGUI.RenderFace;
using LiteRPZWriteControl = LiteRP.Editor.LiteRPShaderGUI.ZWriteControl;
using LiteRPZTestMode = LiteRP.Editor.LiteRPShaderGUI.ZTestMode;
using LiteRPQueueControl = LiteRP.Editor.LiteRPShaderGUI.QueueControl;

namespace LiteRP.Editor
{
    public class LiteRPShaderGUIUtilities
    {
        internal class Styles
        {
            /// <summary>
            /// The names for options available in the SurfaceType enum.
            /// </summary>
            public static readonly string[] surfaceTypeNames = Enum.GetNames(typeof(LiteRPSurfaceType));

            /// <summary>
            /// The names for options available in the BlendMode enum.
            /// </summary>
            public static readonly string[] blendModeNames = Enum.GetNames(typeof(LiteRPBlendMode));

            /// <summary>
            /// The names for options available in the RenderFace enum.
            /// </summary>
            public static readonly string[] renderFaceNames = Enum.GetNames(typeof(LiteRPRenderFace));

            /// <summary>
            /// The names for options available in the ZWriteControl enum.
            /// </summary>
            public static readonly string[] zWriteNames = Enum.GetNames(typeof(LiteRPZWriteControl));

            /// <summary>
            /// The names for options available in the QueueControl enum.
            /// </summary>
            public static readonly string[] queueControlNames = Enum.GetNames(typeof(LiteRPQueueControl));

            /// <summary>
            /// The values for options available in the ZTestMode enum.
            /// </summary>
            // Skipping the first entry for ztest (ZTestMode.Disabled is not a valid value)
            public static readonly int[] zTestValues = ((int[])Enum.GetValues(typeof(LiteRPZTestMode))).Skip(1).ToArray();

            /// <summary>
            /// The names for options available in the ZTestMode enum.
            /// </summary>
            // Skipping the first entry for ztest (ZTestMode.Disabled is not a valid value)
            public static readonly string[] zTestNames = Enum.GetNames(typeof(LiteRPZTestMode)).Skip(1).ToArray();

            // Categories
            /// <summary>
            /// The text and tooltip for the surface options GUI.
            /// </summary>
            public static readonly GUIContent surfaceOptions = EditorGUIUtility.TrTextContent("Surface Options", "Controls how URP Renders the material on screen.");

            /// <summary>
            /// The text and tooltip for the surface inputs GUI.
            /// </summary>
            public static readonly GUIContent surfaceInputs = EditorGUIUtility.TrTextContent("Surface Inputs",
                "These settings describe the look and feel of the surface itself.");

            /// <summary>
            /// The text and tooltip for the advanced options GUI.
            /// </summary>
            public static readonly GUIContent advancedLabel = EditorGUIUtility.TrTextContent("Advanced Options",
                "These settings affect behind-the-scenes rendering and underlying calculations.");

            /// <summary>
            /// The text and tooltip for the Surface Type GUI.
            /// </summary>
            public static readonly GUIContent surfaceType = EditorGUIUtility.TrTextContent("Surface Type",
                "Select a surface type for your texture. Choose between Opaque or Transparent.");

            /// <summary>
            /// The text and tooltip for the blending mode GUI.
            /// </summary>
            public static readonly GUIContent blendingMode = EditorGUIUtility.TrTextContent("Blending Mode",
                "Controls how the color of the Transparent surface blends with the Material color in the background.");

            /// <summary>
            /// The text and tooltip for the preserve specular lighting GUI.
            /// </summary>
            public static readonly GUIContent preserveSpecularText = EditorGUIUtility.TrTextContent("Preserve Specular Lighting",
                "Preserves specular lighting intensity and size by not applying transparent alpha to the specular light contribution.");

            /// <summary>
            /// The text and tooltip for the render face GUI.
            /// </summary>
            public static readonly GUIContent cullingText = EditorGUIUtility.TrTextContent("Render Face",
                "Specifies which faces to cull from your geometry. Front culls front faces. Back culls back faces. Both means that both sides are rendered.");

            /// <summary>
            /// The text and tooltip for the depth write GUI.
            /// </summary>
            public static readonly GUIContent zWriteText = EditorGUIUtility.TrTextContent("Depth Write",
                "Controls whether the shader writes depth.  Auto will write only when the shader is opaque.");

            /// <summary>
            /// The text and tooltip for the depth test GUI.
            /// </summary>
            public static readonly GUIContent zTestText = EditorGUIUtility.TrTextContent("Depth Test",
                "Specifies the depth test mode.  The default is LEqual.");

            /// <summary>
            /// The text and tooltip for the alpha clipping GUI.
            /// </summary>
            public static readonly GUIContent alphaClipText = EditorGUIUtility.TrTextContent("Alpha Clipping",
                "Makes your Material act like a Cutout shader. Use this to create a transparent effect with hard edges between opaque and transparent areas. Avoid using when Alpha is constant for the entire material as enabling in this case could introduce visual artifacts and will add an unnecessary performance cost when used with MSAA (due to AlphaToMask).");

            /// <summary>
            /// The text and tooltip for the alpha clipping threshold GUI.
            /// </summary>
            public static readonly GUIContent alphaClipThresholdText = EditorGUIUtility.TrTextContent("Threshold",
                "Sets where the Alpha Clipping starts. The higher the value is, the brighter the  effect is when clipping starts.");

            /// <summary>
            /// The text and tooltip for the cast shadows GUI.
            /// </summary>
            public static readonly GUIContent castShadowText = EditorGUIUtility.TrTextContent("Cast Shadows",
                "When enabled, this GameObject will cast shadows onto any geometry that can receive them.");

            /// <summary>
            /// The text and tooltip for the receive shadows GUI.
            /// </summary>
            public static readonly GUIContent receiveShadowText = EditorGUIUtility.TrTextContent("Receive Shadows",
                "When enabled, other GameObjects can cast shadows onto this GameObject.");

            /// <summary>
            /// The text and tooltip for the base map GUI.
            /// </summary>
            public static readonly GUIContent baseMap = EditorGUIUtility.TrTextContent("Base Map",
                "Specifies the base Material and/or Color of the surface. If you’ve selected Transparent or Alpha Clipping under Surface Options, your Material uses the Texture’s alpha channel or color.");

            /// <summary>
            /// The text and tooltip for the emission map GUI.
            /// </summary>
            public static readonly GUIContent emissionMap = EditorGUIUtility.TrTextContent("Emission Map",
                "Determines the color and intensity of light that the surface of the material emits.");

            /// <summary>
            /// The text and tooltip for the normal map GUI.
            /// </summary>
            public static readonly GUIContent normalMapText =
                EditorGUIUtility.TrTextContent("Normal Map", "Designates a Normal Map to create the illusion of bumps and dents on this Material's surface.");

            /// <summary>
            /// The text and tooltip for the bump scale not supported GUI.
            /// </summary>
            public static readonly GUIContent bumpScaleNotSupported =
                EditorGUIUtility.TrTextContent("Bump scale is not supported on mobile platforms");

            /// <summary>
            /// The text and tooltip for the normals fix now GUI.
            /// </summary>
            public static readonly GUIContent fixNormalNow = EditorGUIUtility.TrTextContent("Fix now",
                "Converts the assigned texture to be a normal map format.");

            /// <summary>
            /// The text and tooltip for the sorting priority GUI.
            /// </summary>
            public static readonly GUIContent queueSlider = EditorGUIUtility.TrTextContent("Sorting Priority",
                "Determines the chronological rendering order for a Material. Materials with lower value are rendered first.");

            /// <summary>
            /// The text and tooltip for the queue control GUI.
            /// </summary>
            public static readonly GUIContent queueControl = EditorGUIUtility.TrTextContent("Queue Control",
                "Controls whether render queue is automatically set based on material surface type, or explicitly set by the user.");

            /// <summary>
            /// The text and tooltip for the help reference GUI.
            /// </summary>
            public static readonly GUIContent documentationIcon = EditorGUIUtility.TrIconContent("_Help", $"Open Reference for URP Shaders.");
        }
        
        // Base Properties
        internal static void DrawBaseProperties(MaterialEditor matEditor, MaterialProperty baseMapProperty, MaterialProperty baseColorProperty)
        {
            if (baseMapProperty != null && baseColorProperty != null)
            {
                matEditor.TexturePropertySingleLine(Styles.baseMap, baseMapProperty, baseColorProperty);
            }
        }
        
        // Normal Map Properties
        internal static void DrawNormalMapProperties(MaterialEditor matEditor, MaterialProperty normalMap, MaterialProperty bumpScale)
        {
            if (bumpScale != null)
            {
                matEditor.TexturePropertySingleLine(Styles.normalMapText, normalMap, normalMap.textureValue != null ? bumpScale : null);
                if (bumpScale.floatValue != 1 && UnityEditorInternal.InternalEditorUtility.IsMobilePlatform(EditorUserBuildSettings.activeBuildTarget))
                {
                    if (matEditor.HelpBoxWithButton(Styles.bumpScaleNotSupported, Styles.fixNormalNow)) bumpScale.floatValue = 1;
                }
            }
            else
            {
                matEditor.TexturePropertySingleLine(Styles.normalMapText, normalMap);
            }
        }
        
        // Emission Map Properties
        internal static void DrawEmissionProperties(MaterialEditor materialEditor, MaterialProperty emissionMapProperty, MaterialProperty emissionColorProperty, bool keyword)
        {
            var emissive = true;

            if (!keyword)
            {
                if ((emissionMapProperty == null) || (emissionColorProperty == null))
                    return;
                using (new EditorGUI.IndentLevelScope(2))
                {
                    materialEditor.TexturePropertyWithHDRColor(Styles.emissionMap, emissionMapProperty, emissionColorProperty, false);
                }
            }
            else
            {
                emissive = materialEditor.EmissionEnabledProperty();
                using (new EditorGUI.DisabledScope(!emissive))
                {
                    if ((emissionMapProperty == null) || (emissionColorProperty == null))
                        return;
                    using (new EditorGUI.IndentLevelScope(2))
                    {
                        materialEditor.TexturePropertyWithHDRColor(Styles.emissionMap, emissionMapProperty, emissionColorProperty, false);
                    }
                }
            }
            // If texture was assigned and color was black set color to white
            if (emissionMapProperty != null && emissionColorProperty != null)
            {
                var hadEmissionTexture = emissionMapProperty?.textureValue != null;
                var brightness = emissionColorProperty.colorValue.maxColorComponent;
                if (emissionMapProperty.textureValue != null && !hadEmissionTexture && brightness <= 0f)
                {
                    emissionColorProperty.colorValue = Color.white;
                }
            }

            if (emissive)
            {
                // Change the GI emission flag and fix it up with emissive as black if necessary.
                materialEditor.LightmapEmissionFlagsProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel, true);
            }
        }
        
        internal static void DrawTileOffset(MaterialEditor materialEditor, MaterialProperty textureProperty)
        {
            if (textureProperty != null)
            {
                materialEditor.TextureScaleOffsetProperty(textureProperty);
            }
        }

        internal static void DrawFloatToggleProperty(GUIContent styles, MaterialProperty matProp, int indentLevel = 0, bool isDisabled = false)
        {
            if (matProp == null) return;
            
            EditorGUI.BeginDisabledGroup(isDisabled);
            EditorGUI.indentLevel = indentLevel;
            EditorGUI.BeginChangeCheck();
            MaterialEditor.BeginProperty(matProp);
            bool newValue = EditorGUILayout.Toggle(styles, (int)matProp.floatValue == 1);
            if (EditorGUI.EndChangeCheck())
            {
                matProp.floatValue = newValue ? 1 : 0;
            }
            MaterialEditor.EndProperty();
            EditorGUI.indentLevel -= indentLevel;
            EditorGUI.EndDisabledGroup();
        }
        
        public static Rect TextureColorProperties(MaterialEditor materialEditor, GUIContent label, MaterialProperty textureProp, MaterialProperty colorProp, bool hdr = false)
        {
            MaterialEditor.BeginProperty(textureProp);
            if (colorProp != null)
                MaterialEditor.BeginProperty(colorProp);

            Rect rect = EditorGUILayout.GetControlRect();
            EditorGUI.showMixedValue = textureProp.hasMixedValue;
            materialEditor.TexturePropertyMiniThumbnail(rect, textureProp, label.text, label.tooltip);
            EditorGUI.showMixedValue = false;

            if (colorProp != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = colorProp.hasMixedValue;
                int indentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                Rect rectAfterLabel = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y,
                    EditorGUIUtility.fieldWidth, EditorGUIUtility.singleLineHeight);
                var col = EditorGUI.ColorField(rectAfterLabel, GUIContent.none, colorProp.colorValue, true,
                    false, hdr);
                EditorGUI.indentLevel = indentLevel;
                if (EditorGUI.EndChangeCheck())
                {
                    materialEditor.RegisterPropertyChangeUndo(colorProp.displayName);
                    colorProp.colorValue = col;
                }
                EditorGUI.showMixedValue = false;
            }

            if (colorProp != null)
                MaterialEditor.EndProperty();
            MaterialEditor.EndProperty();

            return rect;
        }
    }
}