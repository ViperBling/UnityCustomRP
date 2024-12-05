using System;
using UnityEditor;
using UnityEngine;

namespace LiteRP.Editor
{
    internal class UnlitShaderGUI : LiteRPShaderGUI
    {
        public override void ValidateMaterial(Material material)
        {
            LiteRPShaderUtilities.SetMaterialKeywords(material);
        }
        
        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // 清空材质关键字来刷新
            material.shaderKeywords = null;
            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
            {
                LiteRPShaderUtilities.SetupMaterialBlendMode(material);
                return;
            }

            SurfaceType surfaceType = (SurfaceType)material.GetFloat(LiteRPShaderProperty.SurfaceType);
            BlendMode blendMode = (BlendMode)material.GetFloat(LiteRPShaderProperty.BlendMode);
            if (oldShader.name.Contains("/Transparent/Cutout/"))
            {
                surfaceType = SurfaceType.Opaque;
                material.SetFloat("_AlphaClip", 1);
            }
            else if (oldShader.name.Contains("/Transparent/"))
            {
                surfaceType = SurfaceType.Transparent;
                blendMode = BlendMode.Alpha;
            }
            material.SetFloat("_Blend", (float)blendMode);
            material.SetFloat("_Surface", (float)surfaceType);

            if (surfaceType == SurfaceType.Opaque)
            {
                material.DisableKeyword(ShaderKeywordStrings.SurfaceTypeTransparent);
            }
            else
            {
                material.EnableKeyword(ShaderKeywordStrings.SurfaceTypeTransparent);
            }
        }

        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);
            
            LiteRPShaderGUIUtilities.DrawEmissionProperties(m_MaterialEditor, m_EmissionMapProperty, m_EmissionColorProperty, true);
            LiteRPShaderGUIUtilities.DrawTileOffset(m_MaterialEditor, m_BaseMapProperty);
        }
    }
}