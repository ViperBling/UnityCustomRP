#pragma once

#include "UnlitPassInput.hlsl"

Varyings UnlitPassVertex(Attributes vsIn)
{
    Varyings vsOut = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(vsIn);
    UNITY_TRANSFER_INSTANCE_ID(vsIn, vsOut);

    float3 positionWS = TransformObjectToWorld(vsIn.positionOS);
    vsOut.positionCS = TransformWorldToHClip(positionWS);

    float4 baseMapST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    vsOut.texCoord = vsIn.texCoord * baseMapST.xy + baseMapST.zw;

    return vsOut;
}

float4 UnlitPassFragment(Varyings fsIn) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(fsIn);

    float4 baseMapVal = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, fsIn.texCoord);
    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    float4 base = baseMapVal * baseColor;
    #if defined(_CLIPPING)
    clip(base.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
    #endif
    return base;
}