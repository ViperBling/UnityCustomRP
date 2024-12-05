using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LiteRP
{
    [CreateAssetMenu(menuName = "Rendering/Lite Render Pipeline Asset")]
    public class LiteRPAsset : RenderPipelineAsset<LiteRenderPipeline>
    {
        #region RenderPipelineSettings
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
        
        [SerializeField] int m_AntiAliasing = 1;
        public int AntiAliasing
        {
            get => m_AntiAliasing;
            set => m_AntiAliasing = value;
        }

        protected override RenderPipeline CreatePipeline()
        {
            QualitySettings.antiAliasing = 1;
            return new LiteRenderPipeline(this);
        }
    }
}
