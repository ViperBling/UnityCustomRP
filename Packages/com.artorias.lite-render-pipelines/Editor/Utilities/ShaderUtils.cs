using UnityEditor;
using UnityEngine;

namespace LiteRP.Editor
{
    public static class ShaderUtils
    {
        internal enum ShaderID
        {
            Unknown = -1,
            
            Unlit = ShaderPathID.Unlit,
            Lit = ShaderPathID.Lit,
            ParticlesUnlit = ShaderPathID.ParticlesUnlit
        }

        internal static bool IsShaderGraph(this ShaderID id)
        {
            return false;
        }

        internal static ShaderID GetShaderID(Shader shader)
        {
            ShaderPathID pathID = LiteRP.ShaderUtils.GetEnumFromPath(shader.name);
            return (ShaderID)pathID;
        }
        
        internal enum MaterialUpdateType
        {
            CreatedNewMaterial,
            ChangedAssignedShader,
            ModifiedShader,
            ModifiedMaterial
        }
        
        // internal static void UpdateMaterial(Material material, MaterialUpdateType updateType, UnityEngine.Object assetWithLiteRPMetaData)
        // {
        //     var currentShaderId = ShaderUtils.ShaderID.Unknown;
        //     if (assetWithLiteRPMetaData != null)
        //     {
        //         var path = AssetDatabase.GetAssetPath(assetWithLiteRPMetaData);
        //         foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
        //         {
        //             if (asset is LiteRPMetadata metadataAsset)
        //             {
        //                 currentShaderId = metadataAsset.shaderID;
        //                 break;
        //             }
        //         }
        //     }
        //
        //     UpdateMaterial(material, updateType, currentShaderId);
        // }
        
        internal static void UpdateMaterial(Material material, MaterialUpdateType updateType,
            ShaderID shaderID = ShaderID.Unknown)
        {
            // if unknown, look it up from the material's shader
            // NOTE: this will only work for asset-based shaders..
            if (shaderID == ShaderID.Unknown)
                shaderID = GetShaderID(material.shader);

            switch (shaderID)
            {
                case ShaderID.Unlit:
                    LiteRPShaderUtilities.SetMaterialKeywords(material);
                    break;
                case ShaderID.Lit:
                    // LiteRPShaderUtilities.SetMaterialKeywords(material, LiteRPShaderUtilities.SetMaterialKeywords);
                    break;
                case ShaderID.ParticlesUnlit:
                    // LiteRPShaderUtilities.SetMaterialKeywords(material, LiteRPShaderUtilities.SetMaterialKeywords);
                    break;
                default:
                    break;
            }
        }
    }
}