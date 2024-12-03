using UnityEngine;
using UnityEngine.Rendering;

namespace LiteRP
{
    public static class ShaderKeywordStrings
    {
        //Global Keyword String
        public const string MainLightShadows = "_MAIN_LIGHT_SHADOWS";                   //非Cascade阴影
        public const string MainLightShadowCascades = "_MAIN_LIGHT_SHADOWS_CASCADE";    //Cascade阴影
        
        public const string SoftShadows = "_SHADOWS_SOFT";                              //使用软阴影
        public const string SoftShadowsLow = "_SHADOWS_SOFT_LOW";                       //使用软阴影-低质量
        public const string SoftShadowsMedium = "_SHADOWS_SOFT_MEDIUM";                 //使用软阴影-中质量
        public const string SoftShadowsHigh = "_SHADOWS_SOFT_HIGH";                     //使用软阴影-高质量

        public const string AdditionalLights = "_ADDITIONAL_LIGHTS";                    //使用辅助光源
        
        //Material Keyword String
        public const string AlphaTestOn = "_ALPHATEST_ON";                            //AlphaTest开启
        public const string AlphaPreMultiplyOn = "_ALPHAPREMULTIPLY_ON";              //Alpha预乘开启
        public const string AlphaModulateOn = "_ALPHAMODULATE_ON";                    //Alpha调制开启
        public const string SurfaceTypeTransparent = "_SURFACE_TYPE_TRANSPARENT";     //透明表面类型
        
        public const string useSpecularWorkflow = "_SPECULAR_SETUP";                   //使用高光工作流
        public const string SmoothnessTextureAlbedoChannelA = "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A";        //使用AlbedoMap的Alpha通道作为Smoothness
        public const string MetallicSpecGlossMap = "_METALLICSPECGLOSSMAP";            //使用GlossMap 
        public const string NormalMap = "_NORMALMAP";                                  //使用NormalMap
        public const string OcclusionMap = "_OCCLUSIONMAP";                            //使用OcclusionMap
        public const string ParallaxMap = "_PARALLAXMAP";                              //使用ParallaxMap
        public const string Emission = "_EMISSION";                                    //使用自发光
        public const string ClearCoat = "_CLEARCOAT";                                  //使用清漆层
        public const string ClearCoatMap = "_CLEARCOATMAP";                            //使用ClearCoatMap
        public const string ReceiveShadowsOff = "_RECEIVE_SHADOWS_OFF";                //接收阴影
        
        public const string SpecularHighLightsOff = "_SPECULARHIGHLIGHTS_OFF";              //不开启高光 
        public const string EnvironmentReflectionsOff = "_ENVIRONMENTREFLECTIONS_OFF";      //不开启环境反射
        public const string OptimizedBRDFOff = "_OPTIMIZED_BRDF_OFF";                       //不开启BRDF优化
    }

    internal static class ShaderPropertyIDs
    {
        public static readonly int _alphaToMaskAvailable = Shader.PropertyToID("_AlphaToMaskAvailable");
    }
}