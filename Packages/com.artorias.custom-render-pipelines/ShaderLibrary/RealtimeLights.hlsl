#pragma once

CBUFFER_START(_CustomLight)
    int _DirectionalLightCount;
    int _OtherLightCount;
CBUFFER_END

struct DirectionalLightData
{
    float4 color, directionAndMask, shadowData;
};
StructuredBuffer<DirectionalLightData> _DirectionalLightData;

struct OtherLightData
{
    float4 color, position, directionAndMask, spotAngle, shadowData;
};
StructuredBuffer<OtherLightData> _OtherLightData;

struct Light
{
    float3 color;
    float3 direction;
    float attenuation;
    uint renderingLayerMask;
};

int GetDirectionalLightCount()
{
    return _DirectionalLightCount;
}

Light GetDirectionalLight(int index)
{
    DirectionalLightData data = _DirectionalLightData[index];
    Light light;
    light.color = data.color.rgb;
    light.direction = data.directionAndMask.xyz;
    light.renderingLayerMask = asuint(data.directionAndMask.w);
    light.attenuation = 1.0;
    return light;
}

int GetOtherLightCount()
{
    return _OtherLightCount;
}

Light GetOtherLight(int index)
{
    OtherLightData data = _OtherLightData[index];
    Light light;
    light.color = data.color.rgb;
    light.direction = data.directionAndMask.xyz;
    light.renderingLayerMask = asuint(data.directionAndMask.w);
    light.attenuation = 1.0;
    return light;
}