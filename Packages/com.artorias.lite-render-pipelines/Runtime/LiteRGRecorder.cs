using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace LiteRP
{
    partial class LiteRGRecorder : IRenderGraphRecorder
    {
        public void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            AddGeometryPass(renderGraph, frameData);
        }
    }
}