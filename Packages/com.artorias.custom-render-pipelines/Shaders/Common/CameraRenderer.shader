Shader "Hidden/CustomRP/CameraRenderer"
{
    SubShader
    {
        Cull Off
        ZTest Always
        ZWrite Off
        
        HLSLINCLUDE
        #include "Packages/com.artorias.custom-render-pipelines/ShaderLibrary/Common.hlsl"
        #include "Packages/com.artorias.custom-render-pipelines/Shaders/Common/CameraRendererPass.hlsl"
        ENDHLSL
        
        Pass
        {
            Name "Copy"
            
            Blend [_CameraSrcBlend] [_CameraDstBlend]
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment CopyPassFragment
            ENDHLSL
        }
        
        Pass
        {
            Name "Copy Depth"
            
            ColorMask 0
            ZWrite On
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment CopyDepthPassFragment
            ENDHLSL
        }
    }
}