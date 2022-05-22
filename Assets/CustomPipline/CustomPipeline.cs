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

    private CommandBuffer _shadowBuffer = new CommandBuffer
    {
        name = "Render Shadows"
    };

    private const int MaxVisibleLights = 16;
    private static int _visibleLightColorsID = Shader.PropertyToID("_VisibleLightColors");
    private static int _visibleLightDirsOrPosID = Shader.PropertyToID("_VisibleLightDirsOrPos");
    private static int _visibleLightAttenuationsID = Shader.PropertyToID("_VisibleLightAttenuations");
    private static int _visibleLightSpotDirectionsID = Shader.PropertyToID("_VisibleLightSpotDirections");
    private static int _lightData = Shader.PropertyToID("unity_LightData");

    private static int _shadowMapID = Shader.PropertyToID("_ShadowMap");
    private static int _worldToShadowMatrixID = Shader.PropertyToID("_WorldToShadowMatrix");
    private static int _shadowBiasID = Shader.PropertyToID("_ShadowBias");
    private static int _shadowStrengthID = Shader.PropertyToID("_ShadowStrength");
    private static int _shadowMapSizeID = Shader.PropertyToID("_ShadowMapSize");
    private const string ShadowsSoftKeyword = "_SHADOWS_SOFT";

    private int _shadowMapSize;

    private Vector4[] _visibleLightColors = new Vector4[MaxVisibleLights];
    private Vector4[] _visibleLightDirsOrPos = new Vector4[MaxVisibleLights];
    private Vector4[] _visibleLightAttenuation = new Vector4[MaxVisibleLights];
    private Vector4[] _visibleLightSpotDirections = new Vector4[MaxVisibleLights];

    private RenderTexture _shadowMap;

    public CustomPipeline(bool dynamicBatching, bool instancing, bool perObjectLight, int shadowMapSize)
    {
        _drawingSettings.enableDynamicBatching = dynamicBatching;
        _drawingSettings.enableInstancing = instancing;
        _drawingSettings.perObjectData =
            perObjectLight ? PerObjectData.LightData | PerObjectData.LightIndices : PerObjectData.None;
        
        _shadowMapSize = shadowMapSize;
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

        RenderShadows(context);
        
        context.SetupCameraProperties(camera);
        
        CameraClearFlags clearFlags = camera.clearFlags;
        _buffer.ClearRenderTarget(
            (clearFlags & CameraClearFlags.Depth) != 0,
            (clearFlags & CameraClearFlags.Color) != 0,
            camera.backgroundColor
        );

        if (_culling.visibleLights.Length > 0)
        {
            ConfigureLights();
        }
        else
        {
            _buffer.SetGlobalVector(_lightData, Vector4.zero);
        }
        
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

        if (_shadowMap)
        {
            RenderTexture.ReleaseTemporary(_shadowMap);
            _shadowMap = null;
        }
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

        if (_culling.visibleLights.Length > MaxVisibleLights)
        {
            NativeArray<int> lightIndices = _culling.GetLightIndexMap(Allocator.Temp);
            for (int i = MaxVisibleLights; i < _culling.visibleLights.Length; i++)
            {
                lightIndices[i] = -1;
            }
            _culling.SetLightIndexMap(lightIndices);
        }
    }

    void RenderShadows(ScriptableRenderContext context)
    {
        _shadowMap = RenderTexture.GetTemporary(_shadowMapSize, _shadowMapSize, 32, RenderTextureFormat.Shadowmap);
        _shadowMap.filterMode = FilterMode.Bilinear;
        _shadowMap.wrapMode = TextureWrapMode.Clamp;
        
        // 告知GPU将shadowbuffer渲染到shadowmap
        CoreUtils.SetRenderTarget(
            _shadowBuffer, _shadowMap,
            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
            ClearFlag.Depth);
        
        _shadowBuffer.BeginSample("Render Shadows");
        context.ExecuteCommandBuffer(_shadowBuffer);
        _shadowBuffer.Clear();

        Matrix4x4 viewMatrix, projectionMatrix;
        ShadowSplitData splitData;
        _culling.ComputeSpotShadowMatricesAndCullingPrimitives(
            0, out viewMatrix, out projectionMatrix, out splitData);
        _shadowBuffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        _shadowBuffer.SetGlobalFloat(_shadowBiasID, _culling.visibleLights[0].light.shadowBias);
        context.ExecuteCommandBuffer(_shadowBuffer);
        _shadowBuffer.Clear();

        var shadowSettings = new ShadowDrawingSettings(_culling, 0);
        context.DrawShadows(ref shadowSettings);

        if (SystemInfo.usesReversedZBuffer)
        {
            projectionMatrix.m20 = -projectionMatrix.m20;
            projectionMatrix.m21 = -projectionMatrix.m21;
            projectionMatrix.m22 = -projectionMatrix.m22;
            projectionMatrix.m23 = -projectionMatrix.m23;
        }

        // var scaleOffset = Matrix4x4.TRS(Vector3.one * 0.5f, Quaternion.identity, Vector3.one * 0.5f);
        var scaleOffset = Matrix4x4.identity;
        scaleOffset.m00 = scaleOffset.m11 = scaleOffset.m22 = 0.5f;
        scaleOffset.m03 = scaleOffset.m13 = scaleOffset.m23 = 0.5f;
        Matrix4x4 worldToShadowMatrix = scaleOffset * (projectionMatrix * viewMatrix);
        
        _shadowBuffer.SetGlobalMatrix(_worldToShadowMatrixID, worldToShadowMatrix);
        _shadowBuffer.SetGlobalTexture(_shadowMapID, _shadowMap);
        _shadowBuffer.SetGlobalFloat(_shadowStrengthID, _culling.visibleLights[0].light.shadowStrength);

        float invShadowMapSize = 1.0f / _shadowMapSize;
        _shadowBuffer.SetGlobalVector(
            _shadowMapSizeID, new Vector4(
                invShadowMapSize, invShadowMapSize, _shadowMapSize, _shadowMapSize));

        CoreUtils.SetKeyword(
            _shadowBuffer, ShadowsSoftKeyword, _culling.visibleLights[0].light.shadows == LightShadows.Soft);
        
        _shadowBuffer.EndSample("Render Shadows");
        context.ExecuteCommandBuffer(_shadowBuffer);
        _shadowBuffer.Clear();
    }
}
