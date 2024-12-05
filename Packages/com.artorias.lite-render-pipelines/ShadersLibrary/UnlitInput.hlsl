#pragma once

#define CUSTOM_UNITY_PER_MATERIAL \
    float4 _BaseMap_ST;\
    half4 _BaseColor;\
    half4 _EmissionColor;\
    half _Cutoff;\
    half _Surface;\

#include "Packages/com.artorias.lite-render-pipelines/Shaders/Framework/Data/PerMaterialData.hlsl"


