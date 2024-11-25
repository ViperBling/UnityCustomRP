using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LiteRP
{
    [CreateAssetMenu(menuName = "Rendering/Lite Render Pipeline Asset")]
    public class LiteRPAsset : RenderPipelineAsset<LiteRenderPipeline>
    {
        protected override RenderPipeline CreatePipeline()
        {
            return new LiteRenderPipeline();
        }
    }
}
