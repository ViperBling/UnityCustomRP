using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    private const string m_CommandBufferName = "Render Camera";
    static ShaderTagId m_UnlitShaderTagID = new ShaderTagId("SRPDefaultUnlit");
    static ShaderTagId m_LitShaderTagID = new ShaderTagId("CustomRPLit");
    
    private CommandBuffer m_CmdBuffer = new CommandBuffer { name = m_CommandBufferName };
    private ScriptableRenderContext m_Context;
    Camera m_Camera;
    CullingResults m_CullingResults;
    Lighting m_Lighting = new Lighting();

    public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing)
    {
        m_Context = context;
        m_Camera = camera;

        PrepareBuffer();
        PrepareForSceneWindow();
        if (!Cull())
        {
            return;
        }
        
        Setup();
        m_Lighting.Setup(context, m_CullingResults);
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
        DrawUnsupportedShaders();
        DrawGizmos();
        Submit();
    }

    bool Cull()
    {
        if (m_Camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            m_CullingResults = m_Context.Cull(ref p);
            return true;
        }
        return false;
    }

    void Setup()
    {
        m_Context.SetupCameraProperties(m_Camera);
        CameraClearFlags flags = m_Camera.clearFlags;
        m_CmdBuffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth,
            flags == CameraClearFlags.Color,
            flags == CameraClearFlags.Color ? m_Camera.backgroundColor.linear : Color.clear
        );
        m_CmdBuffer.BeginSample(m_SampleName);
        ExecuteBuffer();
    }

    void Submit()
    {
        m_CmdBuffer.EndSample(m_SampleName);
        ExecuteBuffer();
        m_Context.Submit();
    }

    void ExecuteBuffer()
    {
        m_Context.ExecuteCommandBuffer(m_CmdBuffer);
        m_CmdBuffer.Clear();
    }

    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
    {
        var sortingSettings = new SortingSettings(m_Camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        var drawingSettings = new DrawingSettings(m_UnlitShaderTagID, sortingSettings)
        {
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing
        };
        drawingSettings.SetShaderPassName(1, m_LitShaderTagID);
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        
        m_Context.DrawRenderers(m_CullingResults, ref drawingSettings, ref filteringSettings);
        
        m_Context.DrawSkybox(m_Camera);
        
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        
        m_Context.DrawRenderers(m_CullingResults, ref drawingSettings, ref filteringSettings);
    }
}