#pragma once

#include "../ShaderLibrary/Common.hlsl"

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _EmissiveIntensity)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

struct VSInput
{
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID      // 对象的InstanceID
};

struct PSInput
{
    float4 positionCS : SV_POSITION;
    float2 baseUV : VAR_BASE_UV;        // 可以添加任意未使用的标识符
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

PSInput UnlitPassVertex(VSInput vsIn)
{
    PSInput vsOut;
    // 用宏提取出对象的InstanceID
    UNITY_SETUP_INSTANCE_ID(vsIn);
    // 把InstanceID从VS传到PS
    UNITY_TRANSFER_INSTANCE_ID(vsIn, vsOut);
    float3 positionWS = TransformObjectToWorld(vsIn.positionOS);
    vsOut.positionCS = TransformWorldToHClip(positionWS);

    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    vsOut.baseUV = vsIn.baseUV * baseST.xy + baseST.zw;

    return vsOut;
}

float4 UnlitPassFragment(PSInput psIn) : SV_TARGET
{
    // 再次提取
    UNITY_SETUP_INSTANCE_ID(psIn);
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, psIn.baseUV);
    // 根据InstanceID来取颜色
    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    float4 base = baseColor * baseMap * UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _EmissiveIntensity);
    
    #if defined(_CLIPPING)
        clip(base.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
    #endif
    
    return base;
}