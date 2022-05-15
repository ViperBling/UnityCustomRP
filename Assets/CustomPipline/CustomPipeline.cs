using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using Conditional = System.Diagnostics.ConditionalAttribute;

public class CustomPipeline : RenderPipeline
{
    private CullingResults _culling;
    private Material _errorMaterial;

    private DrawingSettings _drawingSettings = new DrawingSettings();
    
    private CommandBuffer _buffer = new CommandBuffer
    {
        name = "Render Camera"
    };
    
    public CustomPipeline(bool dynamicBatching, bool instancing)
    {
        _drawingSettings.enableDynamicBatching = dynamicBatching;
        _drawingSettings.enableInstancing = instancing;
    }

    // 新版的Unity要求实现这个抽象方法
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
    }

    // 真正的实现在这里
    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        base.Render(context, cameras);
        foreach (var camera in cameras)
        {
            Render(context, camera);
        }
    }

    void Render(ScriptableRenderContext context, Camera camera)
    {
        ScriptableCullingParameters cullingParameters;
        if (!camera.TryGetCullingParameters(out cullingParameters))
        {
            return;
        }
        
#if UNITY_EDITOR
        if (camera.cameraType == CameraType.SceneView) {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
#endif

        _culling = context.Cull(ref cullingParameters);
        
        context.SetupCameraProperties(camera);
        
        CameraClearFlags clearFlags = camera.clearFlags;
        _buffer.ClearRenderTarget(
            (clearFlags & CameraClearFlags.Depth) != 0,
            (clearFlags & CameraClearFlags.Color) != 0,
            camera.backgroundColor
        );
        _buffer.BeginSample("Render Camera");
        context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();

        // Opaque
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        
        _drawingSettings.SetShaderPassName(1, new ShaderTagId("SRPDefaultUnlit"));
        _drawingSettings.sortingSettings = sortingSettings;

        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        context.DrawRenderers(
            _culling, ref _drawingSettings, ref filteringSettings);

        context.DrawSkybox(camera);
        
        // Transparent
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        _drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        
        context.DrawRenderers(
            _culling, ref _drawingSettings, ref filteringSettings);

        DrawDefaultPipeline(context, camera);
        
        _buffer.EndSample("Render Camera");
        context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();

        context.Submit();
    }

    [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
    void DrawDefaultPipeline(ScriptableRenderContext context, Camera camera)
    {
        if (_errorMaterial == null)
        {
            Shader errorShader = Shader.Find("Hidden/InternalErrorShader");
            _errorMaterial = new Material(errorShader) {
                hideFlags = HideFlags.HideAndDontSave
            };
        }
        
        var drawSettings = new DrawingSettings(
            new ShaderTagId("ForwardBase"), new SortingSettings(camera));

        // 其他shader通道，类似ForwardBase，如果不正确的话也显示errorMaterial
        drawSettings.SetShaderPassName(1, new ShaderTagId("PrepassBase"));
        drawSettings.SetShaderPassName(2, new ShaderTagId("Always"));
        drawSettings.SetShaderPassName(3, new ShaderTagId("Vertex"));
        drawSettings.SetShaderPassName(4, new ShaderTagId("VertexLMRGBM"));
        drawSettings.SetShaderPassName(5, new ShaderTagId("VertexLM"));
        drawSettings.overrideMaterial = _errorMaterial;

        var filterSettings = new FilteringSettings(RenderQueueRange.all);
        
        context.DrawRenderers(
            _culling, ref drawSettings, ref filterSettings);
    }
}
