#pragma once

float3 LightingLambert(float3 lightColor, float3 lightDir, float3 normal)
{
    float NoL = saturate(dot(normal, lightDir));
    return NoL * lightColor;
}

float3 CalculateLighting(SurfaceData surfaceData)
{
    float3 color = float3(0, 0, 0);
    for (int i = 0; i < GetDirectionalLightCount(); i++)
    {
        Light light = GetDirectionalLight(i);
        color += LightingLambert(light.color, light.direction, surfaceData.normal);
    }
    
    return color;
}