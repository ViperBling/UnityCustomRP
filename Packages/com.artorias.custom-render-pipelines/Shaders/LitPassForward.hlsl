#pragma once

#include "LitPassInput.hlsl"

Varyings LitPassVertex(Attributes vsIn)
{
    Varyings vsOut = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(vsIn);
    UNITY_TRANSFER_INSTANCE_ID(vsIn, vsOut);

    float3 positionWS = TransformObjectToWorld(vsIn.positionOS);
    vsOut.positionCS = TransformWorldToHClip(positionWS);
    vsOut.normalWS = TransformObjectToWorldNormal(vsIn.normalOS);

    float4 baseMapST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    vsOut.texCoord = vsIn.texCoord * baseMapST.xy + baseMapST.zw;

    return vsOut;
}

float4 LitPassFragment(Varyings fsIn) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(fsIn);

    float4 baseMapVal = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, fsIn.texCoord);
    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    float4 base = baseMapVal * baseColor;
    #if defined(_CLIPPING)
    clip(base.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
    #endif
    // 虽然顶点已经归一化了Normal，但是在这里还是要再次归一化，因为在顶点着色器和片段着色器之间，Normal会被插值，插值后的Normal不一定是单位向量
    base.rgb = normalize(fsIn.normalWS);
    return base;
}