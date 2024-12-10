using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace LiteRP
{
    [DisplayInfo(name = "LiteRP Global Settings Asset", order = CoreUtils.Sections.section4 + 2)]
    [SupportedOnRenderPipeline(typeof(LiteRPAsset))]
    [DisplayName("LiteRP")]
    public class LiteRPGlobalSettings : RenderPipelineGlobalSettings<LiteRPGlobalSettings, LiteRenderPipeline>
    {
        public const string m_DefaultAssetName = "LiteRPGlobalSettings";

        [SerializeField] RenderPipelineGraphicsSettingsContainer m_Settings = new();
        protected override List<IRenderPipelineGraphicsSettings> settingsList => m_Settings.settingsList;
        
#if UNITY_EDITOR
        internal static string m_DefaultPath => $"Package/com.artorias.lite-render-pipelines/Settings/{m_DefaultAssetName}.asset";

        internal static LiteRPGlobalSettings Ensure(bool canCreateNewAsset = true)
        {
            LiteRPGlobalSettings currentSettings = GraphicsSettings.GetSettingsForRenderPipeline<LiteRenderPipeline>() as LiteRPGlobalSettings;

            if (RenderPipelineGlobalSettingsUtils.TryEnsure<LiteRPGlobalSettings, LiteRenderPipeline>(ref currentSettings, m_DefaultPath, canCreateNewAsset))
            {
                if (currentSettings != null && !currentSettings.IsAtLastVersion())
                {
                    UpgradeAsset(currentSettings.GetInstanceID());
                    AssetDatabase.SaveAssetIfDirty(currentSettings);
                }

                return currentSettings;
            }

            return null;
        }
#endif
        
        #region Version system
        internal bool IsAtLastVersion() => k_LastVersion == m_AssetVersion;
        internal const int k_LastVersion = 0;
        
#pragma warning disable CS0414
        [SerializeField][FormerlySerializedAs("k_AssetVersion")]
        internal int m_AssetVersion = k_LastVersion;
#pragma warning restore CS0414
        
#if UNITY_EDITOR
        public static void UpgradeAsset(int assetInstanceID)
        {
            if (EditorUtility.InstanceIDToObject(assetInstanceID) is not LiteRPGlobalSettings asset)
                return;

            int assetVersionBeforeUpgrade = asset.m_AssetVersion;
            
            //未来写升级迁移设置的地方
            /*if (asset.m_AssetVersion < 0)
            {
                asset.m_AssetVersion = 0;
            })*/

            // If the asset version has changed, means that a migration step has been executed
            if (assetVersionBeforeUpgrade != asset.m_AssetVersion)
                EditorUtility.SetDirty(asset);
        }
#endif
        
        #endregion
    }
}