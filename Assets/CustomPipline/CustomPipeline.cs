using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
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

    private const int MaxVisibleLights = 16;
    private static int _visibleLightColorsID = Shader.PropertyToID("_VisibleLightColors");
    private static int _visibleLightDirsOrPosID = Shader.PropertyToID("_VisibleLightDirsOrPos");
    private static int _visibleLightAttenuationsID = Shader.PropertyToID("_VisibleLightAttenuations");
    private static int _visibleLightSpotDirectionsID = Shader.PropertyToID("_VisibleLightSpotDirections");

    private Vector4[] _visibleLightColors = new Vector4[MaxVisibleLights];
    private Vector4[] _visibleLightDirsOrPos = new Vector4[MaxVisibleLights];
    private Vector4[] _visibleLightAttenuation = new Vector4[MaxVisibleLights];
    private Vector4[] _visibleLightSpotDirections = new Vector4[MaxVisibleLights];
    private Vector4[] _lightIndicesOffsetAndCount = new Vector4[MaxVisibleLights];
    
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
        // PerObjectData lightsPerObjectFlags = PerObjectData.LightData | PerObjectData.LightIndices;
        
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
        
        ConfigureLights();

        _buffer.BeginSample("Render Camera");
        // 填充缓冲
        _buffer.SetGlobalVectorArray(_visibleLightColorsID, _visibleLightColors);
        _buffer.SetGlobalVectorArray(_visibleLightDirsOrPosID, _visibleLightDirsOrPos);
        _buffer.SetGlobalVectorArray(_visibleLightAttenuationsID, _visibleLightAttenuation);
        _buffer.SetGlobalVectorArray(_visibleLightSpotDirectionsID, _visibleLightSpotDirections);
        context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();

        // Opaque
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        
        _drawingSettings.SetShaderPassName(1, new ShaderTagId("SRPDefaultUnlit"));
        _drawingSettings.sortingSettings = sortingSettings;
        
        // _drawingSettings.perObjectData = lightsPerObjectFlags;
        

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

    void ConfigureLights()
    {
        for (int i = 0; i < _culling.visibleLights.Length; i++)
        {
            if (i == MaxVisibleLights) break;
            VisibleLight light = _culling.visibleLights[i];
            _visibleLightColors[i] = light.finalColor;
            
            // 防止影响其他光源类型，设置衰减的w分量为1
            Vector4 attenuation = Vector4.zero;
            attenuation.w = 1.0f;
            
            if (light.lightType == LightType.Directional)
            {
                Vector4 v = light.localToWorldMatrix.GetColumn(2);
                v = -v;
                _visibleLightDirsOrPos[i] = v;
            }
            else
            {
                _visibleLightDirsOrPos[i] = light.localToWorldMatrix.GetColumn(3);
                attenuation.x = 1.0f / Mathf.Max(light.range * light.range, 0.00001f);

                if (light.lightType == LightType.Spot)
                {
                    Vector4 v = light.localToWorldMatrix.GetColumn(2);
                    v = -v;
                    _visibleLightSpotDirections[i] = v;

                    float outerRad = Mathf.Deg2Rad * 0.5f * light.spotAngle;
                    float outerCos = Mathf.Cos(outerRad);
                    float outerTan = Mathf.Tan(outerRad);
                    float innerCos = Mathf.Cos(Mathf.Atan((46.0f / 64.0f) * outerTan));

                    float angleRange = Mathf.Max(innerCos - outerCos, 0.001f);
                    attenuation.z = 1.0f / angleRange;
                    attenuation.w = -outerCos * attenuation.z;
                }
            }

            _visibleLightAttenuation[i] = attenuation;
        }
    }
}
