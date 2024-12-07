﻿Shader "LiteRPCommon/Unlit"
{
    Properties
    {
        // Shader 属性
        [MainTexture] _BaseMap ("Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Color", Color) = (1,1,1,1)
        [HDR]_EmissionColor("Emission Color", Color) = (0,0,0,1)
        [NoScaleOffset]_EmissionMap("Emission Map", 2D) = "white" {}
        _Cutoff("AlphaCutout", Range(0.0, 1.0)) = 0.5
        
        // BlendMode
        _Surface("__surface", Float) = 0.0
        _Blend("__mode", Float) = 0.0
        _Cull("__cull", Float) = 2.0
        [ToggleUI] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _BlendOp("__blendop", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _SrcBlendAlpha("__srcA", Float) = 1.0
        [HideInInspector] _DstBlendAlpha("__dstA", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _AlphaToMask("__alphaToMask", Float) = 0.0
        
        // Editmode props
        _QueueOffset("Queue offset", Float) = 0.0
    }
    
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline" = "LiteRenderPipeline"
        }
        LOD 100
        
        // Render State Commands
        Blend [_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]
        ZWrite [_ZWrite]
        Cull [_Cull]

        Pass
        {
            Name "Unlit"
            
            // Render State Commands
            AlphaToMask[_AlphaToMask]
            
            HLSLPROGRAM

            #pragma target 2.0
            #pragma vertex UnlitPassVertex
            #pragma fragment UnlitPassFragment

            // Material Keywords
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAMODULATE_ON
            #pragma shader_feature_local_fragment _EMISSION

            //Unity defined keywords
            #pragma multi_compile_fog               // make fog work
            
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.artorias.lite-render-pipelines/ShadersLibrary/DOTS.hlsl"
            
            // Includes
            #include "Packages/com.artorias.lite-render-pipelines/ShadersLibrary/UnlitForwad.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "LiteRP.Editor.UnlitShaderGUI"
}