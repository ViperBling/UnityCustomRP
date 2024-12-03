#pragma once

struct FGIData
{
    // half shadowMask;
};

struct FFragmentData
{
    float2 texCoord;
    float3 normalWS;
    float3 positionWS;
    float3 viewDirectionWS;

    FSurfaceData surfaceData;
};

#include "Packages/com.artorias.lite-render-pipelines/Shaders/Framework/Data/PerMaterialData.hlsl"