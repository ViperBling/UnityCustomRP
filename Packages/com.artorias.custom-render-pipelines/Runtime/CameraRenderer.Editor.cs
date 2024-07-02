using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

partial class CameraRenderer
{
    partial void DrawGizmos();
    partial void DrawUnsupportedShaders();
    partial void PrepareForSceneWindow();
    partial void PrepareBuffer();
    
#if UNITY_EDITOR

    private static ShaderTagId[] m_LegacyShaderTagIDs =
    {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };

    private static Material m_ErrorMaterial;
    string m_SampleName { get; set; }

    partial void DrawGizmos()
    {
        if (Handles.ShouldRenderGizmos())
        {
            m_Context.DrawGizmos(m_Camera, GizmoSubset.PreImageEffects);
            m_Context.DrawGizmos(m_Camera, GizmoSubset.PostImageEffects);
        }
    }

    partial void DrawUnsupportedShaders()
    {
        if (m_ErrorMaterial == null)
        {
            m_ErrorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        var drawingSettings = new DrawingSettings(m_LegacyShaderTagIDs[0], new SortingSettings(m_Camera))
        {
            overrideMaterial = m_ErrorMaterial
        };
        for (int i = 1; i < m_LegacyShaderTagIDs.Length; i++)
        {
            drawingSettings.SetShaderPassName(i, m_LegacyShaderTagIDs[i]);
        }

        var filteringSettings = FilteringSettings.defaultValue;
        m_Context.DrawRenderers(m_CullingResults, ref drawingSettings, ref filteringSettings);
    }
    
    partial void PrepareForSceneWindow()
    {
        if (m_Camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(m_Camera);
        }
    }

    partial void PrepareBuffer()
    {
        Profiler.BeginSample("Editor Only");
        m_CmdBuffer.name = m_SampleName = m_Camera.name;
        Profiler.EndSample();
    }

#else
    const string m_SampleName = m_CommandBufferName;
#endif
}