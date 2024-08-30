#pragma once

struct SurfaceData
{
    half3 albedo;
    half3 normal;
    half  alpha;

    #ifdef CUSTOM_SURFACE_DATA_INPUT
    CUSTOM_SURFACE_DATA_INPUT
    #endif
};