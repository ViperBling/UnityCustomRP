#pragma once

#include "Packages/com.artorias.custom-render-pipelines/ShaderLibrary/Common.hlsl"
#include "Packages/com.artorias.custom-render-pipelines/ShaderLibrary/SurfaceData.hlsl"
#include "Packages/com.artorias.custom-render-pipelines/ShaderLibrary/RealtimeLights.hlsl"
#include "Packages/com.artorias.custom-render-pipelines/ShaderLibrary/Lighting.hlsl"


TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

#include "Packages/com.artorias.custom-render-pipelines/ShaderLibrary/VaryingsInput.hlsl"