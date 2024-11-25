Shader "Unlit/LiteRP_Unlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MainColor ("Color", Color) = (1,1, 1, 1)
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Geometry"
            "RenderType" = "Opaque"
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
                half4 finalColor = tex2D(_MainTex, fsIn.texCoord);
                // apply fog
                UNITY_APPLY_FOG(fsIn.fogCoord, finalColor);
                
                return finalColor;
            }
            ENDCG
        }
    }
}
