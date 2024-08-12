#pragma once

struct Attributes
{
    float3 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float2 texCoord   : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 normalWS   : VAR_NORMAL;
    float2 texCoord   : VAR_BASE_UV;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};