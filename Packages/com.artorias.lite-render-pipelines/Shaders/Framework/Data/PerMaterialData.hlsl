#pragma once

CBUFFER_START(UnityPerMaterial)
    #define LITERP_INSERT_PER_MATERIAL
        #include "Packages/com.artorias.lite-render-pipelines/ShadersLibrary/SurfaceData.hlsl"
    #undef LITERP_INSERT_PER_MATERIAL

    #if defined(CUSTOM_UNITY_PER_MATERIAL)
        CUSTOM_UNITY_PER_MATERIAL
    #endif

    int _ReceiveNoShadow;
    int _AlphaBlend_On;
CBUFFER_END

#ifdef UNITY_DOTS_INSTANCING_ENABLED
    UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
        UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
        UNITY_DOTS_INSTANCED_PROP(float4, _EmissionColor)
        UNITY_DOTS_INSTANCED_PROP(float , _Cutoff)
        UNITY_DOTS_INSTANCED_PROP(float , _Surface)
    UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
    
    static float4 unity_DOTS_Sampled_BaseColor;
    static float4 unity_DOTS_Sampled_EmissionColor;
    static float  unity_DOTS_Sampled_Cutoff;
    static float  unity_DOTS_Sampled_Surface;
    
    void SetupDOTSUnlitMaterialPropertyCaches()
    {
        unity_DOTS_Sampled_BaseColor     = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4 , _BaseColor);
        unity_DOTS_Sampled_EmissionColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4 , _EmissionColor);
        unity_DOTS_Sampled_Cutoff        = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _Cutoff);
        unity_DOTS_Sampled_Surface       = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float  , _Surface);
    }
    
    #undef UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES
    #define UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES() SetupDOTSUnlitMaterialPropertyCaches()
    
    #define _BaseColor          unity_DOTS_Sampled_BaseColor
    #define _EmissionColor      unity_DOTS_Sampled_EmissionColor
    #define _Cutoff             unity_DOTS_Sampled_Cutoff
    #define _Surface            unity_DOTS_Sampled_Surface

#endif

#if defined(UNITY_INSTANCING_ENABLED)
UNITY_INSTANCING_BUFFER_START(UnityPerInstance)
    #define LITERP_INSERT_PER_INSTANCE
        // TODO 
    #undef LITERP_INSERT_PER_INSTANCE

    #if defined (CUSTOM_UNITY_PER_INSTANCE)
        CUSTOM_UNITY_PER_INSTANCE
    #endif
UNITY_INSTANCING_BUFFER_END(UnityPerInstance)
#endif