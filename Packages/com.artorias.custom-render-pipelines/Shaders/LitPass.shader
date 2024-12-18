﻿Shader "CustomRP/Lit"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" {}
		_BaseColor("Color", Color) = (0.5, 0.5, 0.5, 1.0)
		_Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		[Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0

		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
		[Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
    }
    
    SubShader
    {
        Pass
        {
            Tags
            {
                "LightMode" = "CustomRPLit"
            }
            
            Blend [_SrcBlend] [_DstBlend], One OneMinusSrcAlpha
			ZWrite [_ZWrite]
			
            HLSLPROGRAM
            #pragma shader_feature _CLIPPING
			#pragma multi_compile_instancing
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            #include "LitPassForward.hlsl"
            
            ENDHLSL
        }
    }

    CustomEditor "CustomRP.CustomShaderGUI"
}