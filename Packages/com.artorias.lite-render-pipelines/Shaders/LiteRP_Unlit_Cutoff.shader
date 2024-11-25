Shader "Unlit/LiteRP_Unlit_Cutoff"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MainColor ("Color", Color) = (1,1, 1, 1)
        _AlphaCutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags
        {
            "Queue" = "AlphaTest"
            "RenderType" = "TransparentCutout"
            "IgnoreProjector" = "True"
        }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex VertexPass
            #pragma fragment FragmentPass
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            half _AlphaCutoff;
            half4 _MainColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 texCoord : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texCoord : TEXCOORD0;
                UNITY_FOG_COORDS(1)
            };
            
            Varyings VertexPass(Attributes vsIn)
            {
                Varyings vsOut = (Varyings)0;
                vsOut.positionCS = UnityObjectToClipPos(vsIn.positionOS);
                vsOut.texCoord = TRANSFORM_TEX(vsIn.texCoord, _MainTex);
                UNITY_TRANSFER_FOG(vsOut,vsOut.positionCS);
                return vsOut;
            }

            half4 FragmentPass(Varyings fsIn) : SV_Target
            {
                // sample the texture
                half4 finalColor = tex2D(_MainTex, fsIn.texCoord) * _MainColor;
                clip(finalColor.a - _AlphaCutoff);
                // apply fog
                UNITY_APPLY_FOG(fsIn.fogCoord, col);
                
                return finalColor;
            }
            ENDCG
        }
    }
}
