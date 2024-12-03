using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using LiteRPStyles = LiteRP.Editor.LiteRPShaderGUIUtilities.Styles;

namespace LiteRP.Editor
{
    public class LiteRPShaderGUI : ShaderGUI
    {
        #region EnumsAndClasses
        [Flags]
        public enum Expandable
        {
            SurfaceOptions = 1 << 0,
            SurfaceInputs = 1 << 1,
            Advanced = 1 << 2,
            Details = 1 << 3,
        }
        public enum SurfaceType
        {
            Opaque,
            Transparent
        }

        public enum ZWriteControl
        {
            Auto = 0,
            ForceEnabled = 1,
            ForceDisabled = 2
        }
        public enum ZTestMode
        {
            Disabled = 0,
            Never = 1,
            Less = 2,
            Equal = 3,
            LEqual = 4,     // 默认
            Greater = 5,
            NotEqual = 6,
            GEqual = 7,
            Always = 8,
        }

        public enum BlendMode
        {
            Alpha,
            Premultiply,
            Additive,
            Multiply,
        }
        public enum RenderFace
        {
            Front = 2,
            Back = 1,
            Both = 0
        }
        public enum QueueControl
        {
            Auto = 0,
            UserOverride = 1
        }
        #endregion

        private readonly MaterialHeaderScopeList m_MaterialScopeList = new MaterialHeaderScopeList(uint.MaxValue & ~(uint)Expandable.Advanced);
        private bool m_FirstTimeApply = true;
        private const int m_QueueOffsetRange = 50;
        
        protected MaterialEditor m_MaterialEditor { get; set; }
        protected virtual uint m_MaterialFilter => uint.MaxValue;
        
        #region CommonProperties
        //编辑器用材质属性
        protected MaterialProperty m_SurfaceTypeProperty { get; set; }
        protected MaterialProperty m_BlendModeProperty { get; set; }
        protected MaterialProperty m_PreserveSpecProperty { get; set; }
        protected MaterialProperty m_CullingProperty { get; set; }
        protected MaterialProperty m_ZTestProperty { get; set; }
        protected MaterialProperty m_ZWriteProperty { get; set; }
        protected MaterialProperty m_AlphaClipProperty { get; set; }
        protected MaterialProperty m_AlphaCutoffProperty { get; set; }
        protected MaterialProperty m_CastShadowsProperty { get; set; }
        protected MaterialProperty m_ReceiveShadowsProperty { get; set; }
       
        // 通用Surface Input属性
        protected MaterialProperty m_BaseMapProperty { get; set; }
        protected MaterialProperty m_BaseColorProperty { get; set; }
        protected MaterialProperty m_EmissionMapProperty { get; set; }
        protected MaterialProperty m_EmissionColorProperty { get; set; }
        protected MaterialProperty m_QueueOffsetProperty { get; set; }
        protected MaterialProperty m_QueueControlProperty { get; set; }
        #endregion
        
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            if (!(RenderPipelineManager.currentPipeline is LiteRenderPipeline))
            {
                CoreEditorUtils.DrawFixMeBox("Editing LiteRP materials is only supported when using the LiteRP.", () => SettingsService.OpenProjectSettings("Project/Graphics"));
            }
            else
            {
                OnMaterialGUI(materialEditor, properties);
            }
        }

        private void OnMaterialGUI(MaterialEditor inMaterialEditor, MaterialProperty[] properties)
        {
            if (inMaterialEditor == null)
            {
                throw new ArgumentNullException("inMaterialEditor");
            }
            m_MaterialEditor = inMaterialEditor;
            var material = m_MaterialEditor.target as Material;
            if (material == null) return;
            if (m_FirstTimeApply)
            {
                InitializeShaderGUI(material, inMaterialEditor);
                m_FirstTimeApply = false;
            }
            FindCommonProperties(properties);
            FindProperties(properties);
            
            m_MaterialScopeList.DrawHeaders(m_MaterialEditor, material);
        }

        private void FindCommonProperties(MaterialProperty[] properties)
        {
            m_SurfaceTypeProperty = FindProperty(LiteRPShaderProperty.SurfaceType, properties, false);
            m_BlendModeProperty = FindProperty(LiteRPShaderProperty.BlendMode, properties, false);
            m_PreserveSpecProperty = FindProperty(LiteRPShaderProperty.BlendModePreserveSpecular, properties, false);
            m_CullingProperty = FindProperty(LiteRPShaderProperty.CullMode, properties, false);
            m_ZWriteProperty = FindProperty(LiteRPShaderProperty.ZWriteControl, properties, false);
            m_ZTestProperty = FindProperty(LiteRPShaderProperty.ZTest, properties, false);
            m_AlphaClipProperty = FindProperty(LiteRPShaderProperty.AlphaClip, properties, false);
            

            // ShaderGraph Lit and Unlit Subtargets only
            m_CastShadowsProperty = FindProperty(LiteRPShaderProperty.CastShadows, properties, false);
            m_QueueControlProperty = FindProperty(LiteRPShaderProperty.QueueControl, properties, false);

            // ShaderGraph Lit, and Lit.shader
            m_ReceiveShadowsProperty = FindProperty(LiteRPShaderProperty.ReceiveShadows, properties, false);

            // The following are not mandatory for shadergraphs (it's up to the user to add them to their graph)
            m_AlphaCutoffProperty = FindProperty(LiteRPShaderProperty.Cutoff, properties, false);
            m_BaseMapProperty = FindProperty(LiteRPShaderProperty.BaseMap, properties, false);
            m_BaseColorProperty = FindProperty(LiteRPShaderProperty.BaseColor, properties, false);
            m_EmissionMapProperty = FindProperty(LiteRPShaderProperty.EmissionMap, properties, false);
            m_EmissionColorProperty = FindProperty(LiteRPShaderProperty.EmissionColor, properties, false);
            m_QueueOffsetProperty = FindProperty(LiteRPShaderProperty.QueueOffset, properties, false);
        }

        protected virtual void FindProperties(MaterialProperty[] properties) { }
        
        protected virtual void FillAdditionalFoldouts(MaterialHeaderScopeList materialScopesList) { }
        
        protected virtual void InitializeShaderGUI(Material material, MaterialEditor inMaterialEditor)
        {
            var filter = (Expandable)m_MaterialFilter;

            if (filter.HasFlag(Expandable.SurfaceOptions))
            {
                m_MaterialScopeList.RegisterHeaderScope(LiteRPStyles.SurfaceOptions, Expandable.SurfaceOptions, DrawSurfaceOptions);
            }
            if (filter.HasFlag(Expandable.SurfaceInputs))
            {
                m_MaterialScopeList.RegisterHeaderScope(LiteRPStyles.SurfaceInputs, Expandable.SurfaceInputs, DrawSurfaceInputs);
            }
            if (filter.HasFlag(Expandable.Advanced))
            {
                m_MaterialScopeList.RegisterHeaderScope(LiteRPStyles.AdvancedLabel, Expandable.Advanced, DrawAdvancedOptions);
            }
            if (filter.HasFlag(Expandable.Details))
            {
                FillAdditionalFoldouts(m_MaterialScopeList);
            }
        }
        
        // 绘制Surface options GUI
        public virtual void DrawSurfaceOptions(Material material)
        {
            
        }
        
        // 绘制Surface inputs GUI
        public virtual void DrawSurfaceInputs(Material material)
        {
            
        }

        public virtual void DrawAdvancedOptions(Material material)
        {
            
        }
    }
}