using UnityEngine;


/*
 *  阴影设置类，用于管理阴影各类参数
 *  TextureSize : 阴影贴图大小
 *  maxDistance : 最大阴影绘制距离
 *  Directional : 直射光的阴影配置，单个纹理多阴影贴图
 */
[System.Serializable]
public class ShadowSettings
{
    public enum TextureSize
    {
        _256 = 256, _512 = 512, _1024 = 1024,
        _2048 = 2048, _4096 = 4096, _8192 = 8192
    }

    public enum FilterMode
    {
        PCF2x2, PCF3x3, PCF5x5, PCF7x7
    }
    
    [Min(0.0001f)] public float maxDistance = 100.0f;
    [Range(0.0001f, 1f)] public float distanceFade = 0.1f;

    [System.Serializable]
    public struct Directional
    {
        public TextureSize atlasSize;
        public FilterMode filter;

        [Range(1, 4)] public int cascadeCount;
        // 级联阴影比例，Unity支持4个级联，最后一个级联使用完整比例，默认为1
        [Range(0.0f, 1.0f)] public float cascadeRatio1, cascadeRatio2, cascadeRatio3;
        [Range(0.0001f, 1f)] public float cascadeFade;
        public Vector3 CascadeRatios => new Vector3(cascadeRatio1, cascadeRatio2, cascadeRatio3);

        public enum CascadeBlendMode
        {
            Hard, Soft, Dither
        }
        public CascadeBlendMode cascadeBlend;
    }

    public Directional directional = new Directional
    {
        atlasSize = TextureSize._1024,
        filter = FilterMode.PCF2x2,
        cascadeCount = 4,
        cascadeRatio1 = 0.1f,
        cascadeRatio2 = 0.25f,
        cascadeRatio3 = 0.5f,
        cascadeFade = 0.1f,
        cascadeBlend = Directional.CascadeBlendMode.Hard,
    };
}
