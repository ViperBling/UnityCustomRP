using System;
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
    internal static class LiteRPShaderProperty
    {
        public static readonly string SurfaceType = "_Surface";
        public static readonly string BlendMode = "_Blend";
        public static readonly string AlphaClip = "_AlphaClip";
        public static readonly string AlphaToMask = "_AlphaToMask";
        public static readonly string SrcBlend = "_SrcBlend";
        public static readonly string DstBlend = "_DstBlend";
        public static readonly string SrcBlendAlpha = "_SrcBlendAlpha";
        public static readonly string DstBlendAlpha = "_DstBlendAlpha";
        public static readonly string BlendModePreserveSpecular = "_BlendModePreserveSpecular";
        public static readonly string ZWrite = "_ZWrite";
        public static readonly string CullMode = "_Cull";
        public static readonly string CastShadows = "_CastShadows";
        public static readonly string ReceiveShadows = "_ReceiveShadows";
        public static readonly string QueueOffset = "_QueueOffset";
        public static readonly string Cutoff = "_Cutoff";
        public static readonly string BaseMap = "_BaseMap";
        public static readonly string BaseColor = "_BaseColor";
        
        // for lit shader
        public static readonly string WorkflowMode = "_WorkflowMode";
        public static readonly string Metallic = "_Metallic";
        public static readonly string MetallicGlossMap = "_MetallicGlossMap";
        public static readonly string SpecColor = "_SpecColor";
        public static readonly string SpecGlossMap = "_SpecGlossMap";
        public static readonly string Smoothness = "_Smoothness";
        public static readonly string SmoothnessTextureChannel = "_SmoothnessTextureChannel";
        public static readonly string NormalMap = "_BumpMap";
        public static readonly string NormalScale = "_BumpScale";
        public static readonly string ParallaxMap = "_ParallaxMap";
        public static readonly string Parallax = "_Parallax";
        public static readonly string OcclusionStrength = "_OcclusionStrength";
        public static readonly string OcclusionMap = "_OcclusionMap";
        
        public static readonly string ClearCoat = "_ClearCoat";
        public static readonly string ClearCoatMap = "_ClearCoatMap";
        public static readonly string ClearCoatMask = "_ClearCoatMask";
        public static readonly string ClearCoatSmoothness = "_ClearCoatSmoothness";
        
        // for lit Advanced Props
        public static readonly string SpecularHighlights = "_SpecularHighlights";
        public static readonly string EnvironmentReflections = "_EnvironmentReflections";
        public static readonly string OptimizedBRDF = "_OptimizedBRDF";
            
        // for ShaderGraph shaders only
        public static readonly string ZTest = "_ZTest";
        public static readonly string ZWriteControl = "_ZWriteControl";
        public static readonly string QueueControl = "_QueueControl";
        public static readonly string AddPrecomputedVelocity = "_AddPrecomputedVelocity";
            
        // Global Illumination requires some properties to be named specifically:
        public static readonly string EmissionMap = "_EmissionMap";
        public static readonly string EmissionColor = "_EmissionColor";
        
        // for Particles shaders
        public static readonly string ColorMode = "_ColorMode";
        public static readonly string FlipbookMode = "_FlipbookBlending";
        public static readonly string SoftParticlesEnabled = "_SoftParticlesEnabled";
        public static readonly string CameraFadingEnabled = "_CameraFadingEnabled";
        public static readonly string DistortionEnabled = "_DistortionEnabled";
        public static readonly string SoftParticlesNearFadeDistance = "_SoftParticlesNearFadeDistance";
        public static readonly string SoftParticlesFarFadeDistance = "_SoftParticlesFarFadeDistance";
        public static readonly string SoftParticleFadeParams = "_SoftParticleFadeParams";
        public static readonly string CameraNearFadeDistance = "_CameraNearFadeDistance";
        public static readonly string CameraFarFadeDistance = "_CameraFarFadeDistance";
        public static readonly string CameraFadeParams = "_CameraFadeParams";
        public static readonly string DistortionBlend = "_DistortionBlend";
        public static readonly string DistortionStrengthScaled = "_DistortionStrengthScaled";
        public static readonly string DistortionStrength = "_DistortionStrength";
    }
    
    internal static class LiteRPShaderUtilities
    {
        internal static event Action<Material> ShadowCasterPassEnabledChanged;

        internal static bool b_IsShaderGraph = false;
        
        public static void SetMaterialKeywords(Material material, Action<Material> shadingModelFunc = null, Action<Material> shaderFunc = null)
        {
            UpdateMaterialSurfaceOptions(material, automaticRenderQueue: true);

            // Setup double sided GI based on Cull state
            if (material.HasProperty(LiteRPShaderProperty.CullMode))
            {
                material.doubleSidedGI = (LiteRPRenderFace)material.GetFloat(LiteRPShaderProperty.CullMode) != LiteRPRenderFace.Front;
            }

            // Emission
            if (material.HasProperty(LiteRPShaderProperty.EmissionColor))
            {
                MaterialEditor.FixupEmissiveFlag(material);
            }

            bool shouldEmissionBeEnabled = (material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) == 0;

            CoreUtils.SetKeyword(material, ShaderKeywordStrings.Emission, shouldEmissionBeEnabled);

            // Normal Map
            if (material.HasProperty(LiteRPShaderProperty.NormalMap))
            {
                CoreUtils.SetKeyword(material, ShaderKeywordStrings.NormalMap, material.GetTexture(LiteRPShaderProperty.NormalMap));
            }
            
            // Shader specific keyword functions
            shadingModelFunc?.Invoke(material);
            shaderFunc?.Invoke(material);
        }

        public static void SetupMaterialBlendMode(Material material)
        {
            SetupMaterialBlendModeInternal(material, out int renderQueue);

            // apply automatic render queue
            if (renderQueue != material.renderQueue)
            {
                material.renderQueue = renderQueue;
            }
        }

        public static bool GetAutomaticQueueControlSetting(Material material)
        {
            // If a Shader Graph material doesn't yet have the queue control property,
            // we should not engage automatic behavior until the shader gets reimported.
            bool automaticQueueControl = !b_IsShaderGraph;
            if (material.HasProperty(LiteRPShaderProperty.QueueControl))
            {
                var queueControl = material.GetFloat(LiteRPShaderProperty.QueueControl);
                if (queueControl < 0.0f)
                {
                    // The property was added with a negative value, indicating it needs to be validated for this material
                    UpdateMaterialRenderQueueControl(material);
                }
                automaticQueueControl = material.GetFloat(LiteRPShaderProperty.QueueControl) == (float)LiteRPQueueControl.Auto;
            }
            return automaticQueueControl;
        }
        
        internal static void SetupMaterialBlendModeInternal(Material material, out int automaticRenderQueue)
        {
            bool alphaClip = false;
            if (material.HasProperty(LiteRPShaderProperty.AlphaClip))
            {
                alphaClip = material.GetFloat(LiteRPShaderProperty.AlphaClip) >= 0.5f;
            }
            CoreUtils.SetKeyword(material, ShaderKeywordStrings.AlphaTestOn, alphaClip);

            int renderQueue = material.shader.renderQueue;
            material.SetOverrideTag("RenderType", "");
            if (material.HasProperty(LiteRPShaderProperty.SurfaceType))
            {
                LiteRPSurfaceType surfaceType = (LiteRPSurfaceType)material.GetFloat(LiteRPShaderProperty.SurfaceType);
                bool zWrite = false;
                CoreUtils.SetKeyword(material, ShaderKeywordStrings.SurfaceTypeTransparent, surfaceType == LiteRPSurfaceType.Transparent);
                bool alphaToMask = false;
                
                if (surfaceType == LiteRPSurfaceType.Opaque)
                {
                    if (alphaClip)
                    {
                        renderQueue = (int)RenderQueue.AlphaTest;
                        material.SetOverrideTag("RenderType", "TransparentCutout");
                        alphaToMask = true;
                    }
                    else
                    {
                        renderQueue = (int)RenderQueue.Geometry;
                        material.SetOverrideTag("RenderType", "Opaque");
                    }

                    SetMaterialSrcDstBlendProps(material, BlendMode.One, BlendMode.Zero);
                    zWrite = true;
                    material.DisableKeyword(ShaderKeywordStrings.AlphaPreMultiplyOn);
                    material.DisableKeyword(ShaderKeywordStrings.AlphaModulateOn);
                }
                else
                {
                    LiteRPBlendMode blendMode = (LiteRPBlendMode)material.GetFloat(LiteRPShaderProperty.BlendMode);

                    var srcBlendColor = BlendMode.One;
                    var dstBlendColor = BlendMode.OneMinusSrcAlpha;
                    var srcBlendAlpha = BlendMode.One;
                    var dstBlendAlpha = BlendMode.OneMinusSrcAlpha;
                    
                    // 半透设置
                    // ref: https://docs.unity3d.com/Manual/SL-Blend.html
                    switch (blendMode)
                    {
                        // srcRGB * srcAlpha + dstRGB * (1 - srcAlpha)
                        // preserve spec: 
                        // srcRGB * (<in shader> ? 1 : srcAlpha) + dstRGB * (1 - srcAlpha)
                        case LiteRPBlendMode.Alpha:
                        {
                            srcBlendColor = BlendMode.SrcAlpha;
                            dstBlendColor = BlendMode.OneMinusSrcAlpha;
                            srcBlendAlpha = BlendMode.One;
                            dstBlendAlpha = BlendMode.OneMinusSrcAlpha;
                            break;
                        }
                        // srcRGB < srcAlpha, (alpha multiplied in asset)
                        // srcRGB * 1 + dstRGB * (1 - srcAlpha)
                        case LiteRPBlendMode.Premultiply:
                        {
                            srcBlendColor = BlendMode.One;
                            dstBlendColor = BlendMode.OneMinusSrcAlpha;
                            srcBlendAlpha = BlendMode.One;
                            dstBlendAlpha = BlendMode.OneMinusSrcAlpha;
                            break;
                        }
                        // srcRGB * srcAlpha + dstRGB * 1, (alpha controls amount of addition)
                        // preserve spec:
                        // srcRGB * (<in shader> ? 1 : srcAlpha) + dstRGB * (1 - srcAlpha)
                        case LiteRPBlendMode.Additive:
                        {
                            srcBlendColor = BlendMode.SrcAlpha;
                            dstBlendColor = BlendMode.One;
                            srcBlendAlpha = BlendMode.One;
                            dstBlendAlpha = BlendMode.One;
                            break;
                        }
                        // srcRGB * 0 + dstRGB * srcRGB
                        // in shader alpha controls amount of multiplication, lerp(1, srcRGB, srcAlpha)
                        // Multiply affects color only, keep existing alpha.
                        case LiteRPBlendMode.Multiply:
                        {
                            srcBlendColor = BlendMode.DstColor;
                            dstBlendColor = BlendMode.Zero;
                            srcBlendAlpha = BlendMode.Zero;
                            dstBlendAlpha = BlendMode.One;
                            break;
                        }
                    }
                    
                    // Lift alpha multiply from ROP to shader by setting pre-multiplied _SrcBlend mode.
                    // The intent is to do different blending for diffuse and specular in shader.
                    // ref: http://advances.realtimerendering.com/other/2016/naughty_dog/NaughtyDog_TechArt_Final.pdf
                    bool preserveSpecular = material.HasProperty(LiteRPShaderProperty.BlendModePreserveSpecular) &&
                                            material.GetFloat(LiteRPShaderProperty.BlendModePreserveSpecular) > 0.0f &&
                                            blendMode != LiteRPBlendMode.Alpha;

                    if (preserveSpecular)
                    {
                        srcBlendColor = BlendMode.One;
                    }
                    
                    SetMaterialSrcDstBlendProps(material, srcBlendColor, dstBlendColor, srcBlendAlpha, dstBlendAlpha);
                    
                    CoreUtils.SetKeyword(material, ShaderKeywordStrings.AlphaPreMultiplyOn, preserveSpecular);
                    CoreUtils.SetKeyword(material, ShaderKeywordStrings.AlphaModulateOn, blendMode == LiteRPBlendMode.Multiply);
                    
                    material.SetOverrideTag("RenderType", "Transparent");
                    zWrite = false;
                    renderQueue = (int)RenderQueue.Transparent;
                }

                if (material.HasProperty(LiteRPShaderProperty.AlphaToMask))
                {
                    material.SetFloat(LiteRPShaderProperty.AlphaToMask, alphaToMask ? 1.0f : 0.0f);
                }
                SetMaterialZWriteProperty(material, zWrite);
                material.SetShaderPassEnabled("DepthOnly", zWrite);
            }
            else
            {
                material.SetShaderPassEnabled("DepthOnly", true);
            }
            
            // 处理Shader中改变的RenderQueue
            if (material.HasProperty(LiteRPShaderProperty.QueueOffset))
            {
                renderQueue += (int)material.GetFloat(LiteRPShaderProperty.QueueOffset);
            }
            
            automaticRenderQueue = renderQueue;
        }

        private static void UpdateMaterialSurfaceOptions(Material material, bool automaticRenderQueue)
        {
            SetupMaterialBlendModeInternal(material, out int renderQueue);
            
            if (automaticRenderQueue && renderQueue != material.renderQueue)
            {
                material.renderQueue = renderQueue;
            }

            // bool isShaderGraph = false;
            bool castShadows = true;

            if (material.HasProperty(LiteRPShaderProperty.CastShadows))
            {
                castShadows = material.GetFloat(LiteRPShaderProperty.CastShadows) != 0.0f;
            }
            else
            {
                if (b_IsShaderGraph)
                {
                    castShadows = true;
                }
                else
                {
                    castShadows = IsOpaque(material);
                }
            }
            
            string shadowCasterPass = "ShadowCaster";
            if (material.GetShaderPassEnabled(shadowCasterPass) != castShadows)
            {
                material.SetShaderPassEnabled(shadowCasterPass, castShadows);
                ShadowCasterPassEnabledChanged?.Invoke(material);
            }

            // Receive Shadows
            if (material.HasProperty(LiteRPShaderProperty.ReceiveShadows))
            {
                CoreUtils.SetKeyword(material, ShaderKeywordStrings.ReceiveShadowsOff, material.GetFloat(LiteRPShaderProperty.ReceiveShadows) == 0.0f);
            }
        }
        
        private static void UpdateMaterialRenderQueueControl(Material material)
        {
            //
            // Render Queue Control handling
            //
            // Check for a raw render queue (the actual serialized setting - material.renderQueue has already been converted)
            // setting of -1, indicating that the material property should be inherited from the shader.
            // If we find this, add a new property "render queue control" set to 0 so we will
            // always know to follow the surface type of the material (this matches the hand-written behavior)
            // If we find another value, add the the property set to 1 so we will know that the
            // user has explicitly selected a render queue and we should not override it.
            //
            // bool isShaderGraph = false; // Non-shadergraph materials use automatic behavior
            int rawRenderQueue = MaterialAccess.ReadMaterialRawRenderQueue(material);
            if (!b_IsShaderGraph || rawRenderQueue == -1)
            {
                material.SetFloat(LiteRPShaderProperty.QueueControl, (float)LiteRPQueueControl.Auto);
            }
            else
            {
                material.SetFloat(LiteRPShaderProperty.QueueControl, (float)LiteRPQueueControl.UserOverride);   // User has selected explicit render queue
            }
        }
        
        internal static void SetMaterialSrcDstBlendProps(Material material, BlendMode srcBlend, BlendMode dstBlend)
        {
            if (material.HasProperty(LiteRPShaderProperty.SrcBlend))
            {
                material.SetFloat(LiteRPShaderProperty.SrcBlend, (float)srcBlend);
            }

            if (material.HasProperty(LiteRPShaderProperty.DstBlend))
            {
                material.SetFloat(LiteRPShaderProperty.DstBlend, (float)dstBlend);
            }

            if (material.HasProperty(LiteRPShaderProperty.SrcBlendAlpha))
            {
                material.SetFloat(LiteRPShaderProperty.SrcBlendAlpha, (float)srcBlend);
            }

            if (material.HasProperty(LiteRPShaderProperty.DstBlendAlpha))
            {
                material.SetFloat(LiteRPShaderProperty.DstBlendAlpha, (float)dstBlend);
            }
        }

        internal static void SetMaterialSrcDstBlendProps(Material material, BlendMode srcBlendColor, BlendMode dstBlendColor, BlendMode srcBlendAlpha, BlendMode dstBlendAlpha)
        {
            if (material.HasProperty(LiteRPShaderProperty.SrcBlend))
            {
                material.SetFloat(LiteRPShaderProperty.SrcBlend, (float)srcBlendColor);
            }

            if (material.HasProperty(LiteRPShaderProperty.DstBlend))
            {
                material.SetFloat(LiteRPShaderProperty.DstBlend, (float)dstBlendColor);
            }

            if (material.HasProperty(LiteRPShaderProperty.SrcBlendAlpha))
            {
                material.SetFloat(LiteRPShaderProperty.SrcBlendAlpha, (float)srcBlendAlpha);
            }

            if (material.HasProperty(LiteRPShaderProperty.DstBlendAlpha))
            {
                material.SetFloat(LiteRPShaderProperty.DstBlendAlpha, (float)dstBlendAlpha);
            }
        }

        internal static void SetMaterialZWriteProperty(Material material, bool zWriteEnabled)
        {
            if (material.HasProperty(LiteRPShaderProperty.ZWrite))
            {
                material.SetFloat(LiteRPShaderProperty.ZWrite, zWriteEnabled ? 1.0f : 0.0f);
            }
        }

        internal static bool IsOpaque(Material material)
        {
            bool opaque = true;
            if (material.HasProperty(LiteRPShaderProperty.SurfaceType))
            {
                opaque = (LiteRPSurfaceType)material.GetFloat(LiteRPShaderProperty.SurfaceType) == LiteRPSurfaceType.Opaque;
            }
            return opaque;
        }
    }
}