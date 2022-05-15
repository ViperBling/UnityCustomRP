using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Pipeline")]
public class CustomPipelineAsset : RenderPipelineAsset
{
    [SerializeField] private bool dynamicBatching = false;
    [SerializeField] private bool instancing = false;
    
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomPipeline(dynamicBatching, instancing);
    }
}
