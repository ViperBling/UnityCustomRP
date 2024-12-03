#pragma once

#include "Packages/com.artorias.lite-render-pipelines/Shaders/Framework/Data/FragmentData.hlsl"

struct FVertexInputs
{
    float4 positionOS : POSITION;
    float2 texCoord0  : TEXCOORD0;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct FFragmentInputs
{
    float4 positionCS : SV_POSITION;
    float2 texCoord0  : TEXCOORD0;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};