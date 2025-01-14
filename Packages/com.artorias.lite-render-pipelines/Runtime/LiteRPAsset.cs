using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LiteRP
{
    [CreateAssetMenu(menuName = "Rendering/Lite Render Pipeline Asset")]
#if UNITY_EDITOR
    [UnityEditor.ShaderKeywordFilter.ApplyRulesIfTagsEqual("RenderPipeline", "LiteRenderPipeline")]
#endif
    public class LiteRPAsset : RenderPipelineAsset<LiteRenderPipeline>
    {
        public override string renderPipelineShaderTag => LiteRenderPipeline.k_ShaderTagName;
        
        #region RenderPipelineSettings

        [SerializeField] private RenderingPath m_RenderingPath = RenderingPath.Forward;
        public RenderingPath renderingPath
        {
            get => m_RenderingPath;
            set
            {
                if (value == m_RenderingPath) return;
                m_RenderingPath = value;
                OnValidate();
            }
        }
        
        [SerializeField] bool m_UseSRPBatcher = true;
        public bool UseSRPBatcher
        {
            get => m_UseSRPBatcher;
            set => m_UseSRPBatcher = value;
        }

        [SerializeField] private GPUResidentDrawerMode m_GPUDrivenMode = GPUResidentDrawerMode.Disabled;
        public GPUResidentDrawerMode GPUResidentDrawerMode
        {
            get => m_GPUDrivenMode;
            set
            {
                if (value == m_GPUDrivenMode) return;
                m_GPUDrivenMode = value;
                OnValidate();
            }
        }

        [SerializeField] private float m_SmallMeshScreenPercentage = 0.0f;
        public float SmallMeshScreenPercentage
        {
            get => m_SmallMeshScreenPercentage;
            set
            {
                if (Math.Abs(value - m_SmallMeshScreenPercentage) < float.Epsilon) return;
                m_SmallMeshScreenPercentage = Mathf.Clamp(value, 0.0f, 20.0f);
                OnValidate();
            }
        }

        [SerializeField] bool m_GPUDrivenEnableOcclusionCullingInCams;
        public bool GPUResidentDrawerEnableOcclusionCullingInCams
        {
            get => m_GPUDrivenEnableOcclusionCullingInCams;
            set
            {
                if (value == m_GPUDrivenEnableOcclusionCullingInCams) return;
                m_GPUDrivenEnableOcclusionCullingInCams = value;
                OnValidate();
            }
        }
        
        #endregion

        #region QualitySettings
        [SerializeField] bool m_SupportsHDR = true;
        public bool supportsHDR
        {
            get => m_SupportsHDR;
            set => m_SupportsHDR = value;
        }
        
        [SerializeField] MsaaQuality m_MSAA = MsaaQuality.Disabled;
        public int msaaSampleCount
        {
            get => (int)m_MSAA;
            set => m_MSAA = (MsaaQuality)value;
        }
        
        [SerializeField] int m_AntiAliasing = 1;
        public int AntiAliasing
        {
            get => m_AntiAliasing;
            set => m_AntiAliasing = value;
        }
        
        [SerializeField] bool m_ConservativeEnclosingSphere = true;
        public bool conservativeEnclosingSphere
        {
            get => m_ConservativeEnclosingSphere;
            set => m_ConservativeEnclosingSphere = value;
        }
        
        [SerializeField] int m_NumIterationsEnclosingSphere = 64;
        public int numIterationsEnclosingSphere
        {
            get => m_NumIterationsEnclosingSphere;
            set => m_NumIterationsEnclosingSphere = value;
        }
        #endregion

        #region LightingSettings

        internal const int k_MaxPerObjectLights = 8;
        [SerializeField] private int m_AdditionalLightPerObjectLimit = 4;
        public int maxAdditionalLightsCount
        {
            get => m_AdditionalLightPerObjectLimit;
            set => m_AdditionalLightPerObjectLimit = Mathf.Max(0, Math.Min(value, k_MaxPerObjectLights));
        }
        #endregion
        
        #region ShadowSettings
        [SerializeField] bool m_MainLightShadowEnable = true;
        public bool mainLightShadowEnabled
        {
            get => m_MainLightShadowEnable;
            internal set
            {
                m_MainLightShadowEnable = value;
#if UNITY_EDITOR
                m_AnyShadowsSupported = m_MainLightShadowEnable;
#endif
            }
        }
        
        [SerializeField] ShadowResolution m_MainLightShadowmapResolution = LiteRP.ShadowResolution._2048;
        public int mainLightShadowmapResolution
        {
            get => (int)m_MainLightShadowmapResolution;
            set => m_MainLightShadowmapResolution = (ShadowResolution)value;
        }
        
        [SerializeField] float m_MainLightShadowDistance = 50;
        public float mainLightShadowDistance
        {
            get => m_MainLightShadowDistance;
            set => m_MainLightShadowDistance = Mathf.Max(0.0f, value);
        }
        
        // TODO : internal
        public const int k_ShadowCascadeMinCount = 1;
        public const int k_ShadowCascadeMaxCount = 4;
        [SerializeField] int m_MainLightShadowCascadesCount = k_ShadowCascadeMaxCount;
        public int mainLightShadowCascadesCount
        {
            get => m_MainLightShadowCascadesCount;
            set
            {
                if (value < k_ShadowCascadeMinCount || value > k_ShadowCascadeMaxCount)
                {
                    throw new ArgumentException($"Value ({value}) needs to be between {k_ShadowCascadeMinCount} and {k_ShadowCascadeMaxCount}.");
                }
                m_MainLightShadowCascadesCount = value;
            }
        }
        
        [SerializeField] float m_MainLightCascade2Split = 0.25f;
        public float mainLightCascade2Split
        {
            get => m_MainLightCascade2Split;
            set => m_MainLightCascade2Split = value;
        }
        
        [SerializeField] Vector2 m_MainLightCascade3Split = new Vector2(0.1f, 0.3f);
        public Vector2 mainLightCascade3Split
        {
            get => m_MainLightCascade3Split;
            set => m_MainLightCascade3Split = value;
        }
        
        [SerializeField] Vector3 m_MainLightCascade4Split = new Vector3(0.067f, 0.2f, 0.467f);
        public Vector3 mainLightCascade4Split
        {
            get => m_MainLightCascade4Split;
            set => m_MainLightCascade4Split = value;
        }
        
        [SerializeField] float m_MainLightCascadeBorder = 0.2f;
        public float mainLightCascadeBorder
        {
            get => m_MainLightCascadeBorder;
            set => m_MainLightCascadeBorder = value;
        }
        
        // TODO : internal
        public const float k_MaxShadowBias = 10.0f;
        [SerializeField] float m_MainLightShadowDepthBias = 1.0f;
        public float mainLightShadowDepthBias
        {
            get => m_MainLightShadowDepthBias;
            set => m_MainLightShadowDepthBias = Mathf.Max(0.0f, Mathf.Min(value, k_MaxShadowBias));
        }
        
        [SerializeField] float m_MainLightShadowNormalBias = 1.0f;
        public float mainLightShadowNormalBias
        {
            get => m_MainLightShadowNormalBias;
            set => m_MainLightShadowNormalBias = Mathf.Max(0.0f, Mathf.Min(value, k_MaxShadowBias));
        }
        
#if UNITY_EDITOR // multi_compile_fragment _ _SHADOWS_SOFT
        [UnityEditor.ShaderKeywordFilter.RemoveIf(false, keywordNames: ShaderKeywordStrings.SoftShadows)]
        [SerializeField] bool m_AnyShadowsSupported = true;
        // No option to force soft shadows -> we'll need to keep the off variant around
        [UnityEditor.ShaderKeywordFilter.RemoveIf(false, keywordNames: ShaderKeywordStrings.SoftShadows)]
#endif
        [SerializeField] private bool m_SoftShadowSupported = false;
        public bool supportsSoftShadows
        {
            get => m_SoftShadowSupported;
            internal set => m_SoftShadowSupported = value;
        }
        [SerializeField] SoftShadowQuality m_SoftShadowQuality = SoftShadowQuality.Medium;
        public SoftShadowQuality softShadowQuality
        {
            get => m_SoftShadowQuality;
            set => m_SoftShadowQuality = value;
        }
        #endregion
        
        // OtherSettings
        #region OtherSettings
        /// <summary>
        /// Returns the selected update mode for volumes.
        /// </summary>
        [SerializeField] VolumeFrameworkUpdateMode m_VolumeFrameworkUpdateMode = VolumeFrameworkUpdateMode.EveryFrame;
        public VolumeFrameworkUpdateMode volumeFrameworkUpdateMode => m_VolumeFrameworkUpdateMode;

        /// <summary>
        /// A volume profile that can be used to override global default volume profile values. This provides a way e.g.
        /// to have different volume default values per quality level without having to place global volumes in scenes.
        /// </summary>
        [SerializeField] VolumeProfile m_VolumeProfile;
        public VolumeProfile volumeProfile
        {
            get => m_VolumeProfile;
            set => m_VolumeProfile = value;
        }
        #endregion
        
        /// <summary>
        /// Ensures Global Settings are ready and registered into GraphicsSettings
        /// </summary>
        protected override void EnsureGlobalSettings()
        {
            base.EnsureGlobalSettings();
#if UNITY_EDITOR
            LiteRPGlobalSettings.Ensure();
#endif
        }

        protected override RenderPipeline CreatePipeline()
        {
            QualitySettings.antiAliasing = 1;
            return new LiteRenderPipeline(this);
        }
    }
}
