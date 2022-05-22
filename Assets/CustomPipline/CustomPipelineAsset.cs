using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Pipeline")]
public class CustomPipelineAsset : RenderPipelineAsset
{
    [SerializeField] private bool dynamicBatching = false;
    [SerializeField] private bool instancing = false;
    [SerializeField] private bool perObjectLight = false;

    public enum ShadowMapSize
    {
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096
    }

    [SerializeField] private ShadowMapSize shadowMapSize = ShadowMapSize._1024;
    
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomPipeline(dynamicBatching, instancing, perObjectLight, (int)shadowMapSize);
    }
}
