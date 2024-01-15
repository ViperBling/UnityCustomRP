using Palmmedia.ReportGenerator.Core;
using UnityEngine;
using UnityEngine.Rendering;

/*
 *  阴影类
 */
public class Shadows
{
    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
    {
        _context = context;
        _cullingResults = cullingResults;
        _settings = shadowSettings;
        
        _shadowedDirectionalLightCount = 0;
    }

    public void Render()
    {
        if (_shadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
        else
        {
            _buffer.GetTemporaryRT(_directionalShadowAtlasID, 1, 1, 
                32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        }
    }

    void RenderDirectionalShadows()
    {
        int atlasSize = (int)_settings.directional.atlasSize;
        // 创建临时RT来渲染阴影贴图
        _buffer.GetTemporaryRT(_directionalShadowAtlasID, atlasSize, atlasSize,
            32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        // 设置阴影RT资源状态
        _buffer.SetRenderTarget(
            _directionalShadowAtlasID, 
            RenderBufferLoadAction.DontCare, 
            RenderBufferStoreAction.Store);
        _buffer.ClearRenderTarget(true, false, Color.clear);
        _buffer.BeginSample(BufferName);
        ExecuteBuffer();

        // 根据级联阴影的数量确定Tile和Split
        int tiles = _shadowedDirectionalLightCount * _settings.directional.cascadeCount;
        int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
        int tileSize = atlasSize / split;
        for (int i = 0; i < _shadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }
        _buffer.SetGlobalInt(_cascadeCountID, _settings.directional.cascadeCount);
        _buffer.SetGlobalVectorArray(_cascadeCullingSpheresID, _cascadeCullingSpheres);
        _buffer.SetGlobalVectorArray(_cascadeDataID, _cascadeData);
        _buffer.SetGlobalMatrixArray(_directionalShadowMatricesID, _directionalShadowMatrices);
        float fade = 1.0f - _settings.directional.cascadeFade;
        _buffer.SetGlobalVector(_shadowDistanceFadeID, new Vector4(
            1.0f / _settings.maxDistance, 
            1.0f / _settings.distanceFade, 
            1.0f / (1.0f - fade * fade)));
        SetKeywords(directionalFilterKeywords, (int)_settings.directional.filter - 1);
        SetKeywords(cascadeBlendKeywords, (int)_settings.directional.cascadeBlend - 1);
        _buffer.SetGlobalVector(shadowAtlasSizeID, new Vector4(atlasSize, 1.0f / atlasSize));
        _buffer.EndSample(BufferName);
        ExecuteBuffer();
    }

    void SetKeywords(string[] keywords, int enabledIndex)
    {
        for (int i = 0; i < keywords.Length; i++)
        {
            if (i == enabledIndex)
            {
                _buffer.EnableShaderKeyword(keywords[i]);
            }
            else
            {
                _buffer.DisableShaderKeyword(keywords[i]);
            }
        }
    }

    void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        ShadowedDirectionalLight light = _shadowedDirectionalLights[index];
        // 新版本API添加了投射矩阵类型参数
        var shadowSettings = new ShadowDrawingSettings(_cullingResults, light.VisibleLightIndex, BatchCullingProjectionType.Orthographic);
        int cascadeCount = _settings.directional.cascadeCount;
        int tileOffset = index * cascadeCount;
        Vector3 ratios = _settings.directional.CascadeRatios;

        float cullingFactor = Mathf.Max(0f, 0.8f - _settings.directional.cascadeFade);
        for (int i = 0; i < cascadeCount; i++)
        {
            // 根据一个视图和投影矩阵来确定阴影的投射范围
            // 第一个参数是光源索引，第二、三、四用来控制级联阴影
            _cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                light.VisibleLightIndex, 
                i, cascadeCount, ratios, tileSize, light.NearPlaneOffset,
                out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData splitData);
            splitData.shadowCascadeBlendCullingFactor = cullingFactor;
            shadowSettings.splitData = splitData;
            // 从splitData中获取剔除球
            if (index == 0)
            {
                SetCascadeData(i, splitData.cullingSphere, tileSize);
            }
            int tileIndex = tileOffset + i;
            // 根据每个光源的不同矩阵来分配
            _directionalShadowMatrices[tileIndex] = ConvertToAtlasMatrix(
                projMatrix * viewMatrix, SetTileViewport(tileIndex, split, tileSize), split);
            _buffer.SetViewProjectionMatrices(viewMatrix, projMatrix);
            _buffer.SetGlobalDepthBias(0.0f, light.SlopeScaleBias);
            ExecuteBuffer();
            _context.DrawShadows(ref shadowSettings);
            _buffer.SetGlobalDepthBias(0.0f, 0.0f);
        }
    }

    void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
    {
        float texelSize = 2.0f * cullingSphere.w / tileSize;
        float filterSize = texelSize * ((float)_settings.directional.filter + 1.0f);
        cullingSphere.w -= filterSize;
        cullingSphere.w *= cullingSphere.w;
        _cascadeCullingSpheres[index] = cullingSphere;
        _cascadeData[index] = new Vector4(1.0f / cullingSphere.w, filterSize * 1.4142136f);
    }

    Vector2 SetTileViewport(int index, int split, float tileSize)
    {
        Vector2 offset = new Vector2(index % split, index / split);
        _buffer.SetViewport(new Rect(
            offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
        return offset;
    }

    Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 mat, Vector2 offset, int split)
    {
        if (SystemInfo.usesReversedZBuffer) {
            mat.m20 = -mat.m20;
            mat.m21 = -mat.m21;
            mat.m22 = -mat.m22;
            mat.m23 = -mat.m23;
        }
        float scale = 1f / split;
        mat.m00 = (0.5f * (mat.m00 + mat.m30) + offset.x * mat.m30) * scale;
        mat.m01 = (0.5f * (mat.m01 + mat.m31) + offset.x * mat.m31) * scale;
        mat.m02 = (0.5f * (mat.m02 + mat.m32) + offset.x * mat.m32) * scale;
        mat.m03 = (0.5f * (mat.m03 + mat.m33) + offset.x * mat.m33) * scale;
        mat.m10 = (0.5f * (mat.m10 + mat.m30) + offset.y * mat.m30) * scale;
        mat.m11 = (0.5f * (mat.m11 + mat.m31) + offset.y * mat.m31) * scale;
        mat.m12 = (0.5f * (mat.m12 + mat.m32) + offset.y * mat.m32) * scale;
        mat.m13 = (0.5f * (mat.m13 + mat.m33) + offset.y * mat.m33) * scale;
        mat.m20 = 0.5f * (mat.m20 + mat.m30);
        mat.m21 = 0.5f * (mat.m21 + mat.m31);
        mat.m22 = 0.5f * (mat.m22 + mat.m32);
        mat.m23 = 0.5f * (mat.m23 + mat.m33);
        
        return mat;
    }

    void ExecuteBuffer()
    {
        _context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }

    // 获取哪些光源会产生阴影
    public Vector3 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        // 判断条件：光源可以投射阴影且阴影强度大于0 && GetShadowCasterBounds检查光源是否在阴影投射范围
        if (_shadowedDirectionalLightCount < MaxShadowedDirLightCount &&
            light.shadows != LightShadows.None && light.shadowStrength > 0f &&
            _cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds bounds))
        {
            _shadowedDirectionalLights[_shadowedDirectionalLightCount] = new ShadowedDirectionalLight
            {
                VisibleLightIndex = visibleLightIndex,
                SlopeScaleBias = light.shadowBias,
                NearPlaneOffset = light.shadowNearPlane
            };
            return new Vector3(light.shadowStrength, _settings.directional.cascadeCount * _shadowedDirectionalLightCount++, light.shadowNormalBias);
        }
        return Vector3.zero;
    }

    public void Cleanup()
    {
        if (_shadowedDirectionalLightCount > 0)
        {
            _buffer.ReleaseTemporaryRT(_directionalShadowAtlasID);
            ExecuteBuffer();
        }
    }

    struct ShadowedDirectionalLight
    {
        public int VisibleLightIndex;
        public float SlopeScaleBias;
        public float NearPlaneOffset;   // 防止光源的视锥体近平面裁剪物体出现阴影错误
    }
    ShadowedDirectionalLight[] _shadowedDirectionalLights = new ShadowedDirectionalLight[MaxShadowedDirLightCount];

    // 当前投射阴影的直射光数量
    int _shadowedDirectionalLightCount;
    
    const string BufferName = "Shadows";
    CommandBuffer _buffer = new CommandBuffer
    {
        name = BufferName
    };

    ScriptableRenderContext _context;
    CullingResults _cullingResults;
    ShadowSettings _settings;

    // 最多投射阴影的光源数量
    const int MaxShadowedDirLightCount = 4, MaxCascades = 4;

    // 使用_DirectionalShadowAtlas来在shader中引用阴影图集
    int _directionalShadowAtlasID = Shader.PropertyToID("_DirectionalShadowAtlas");
    // 每个光源的阴影矩阵
    int _directionalShadowMatricesID = Shader.PropertyToID("_DirectionalShadowMatrices");
    // 定义光源的剔除球
    int _cascadeCountID = Shader.PropertyToID("_CascadeCount");
    int _cascadeCullingSpheresID = Shader.PropertyToID("_CascadeCullingSpheres");
    int _cascadeDataID = Shader.PropertyToID("_CascadeData");
    int shadowAtlasSizeID = Shader.PropertyToID("_ShadowAtlasSize");
    int _shadowDistanceFadeID = Shader.PropertyToID("_ShadowDistanceFade");
    
    // 每个级联阴影贴图都有自己的Matrix，所以对每栈光要乘上其级联阴影贴图的数量
    Matrix4x4[] _directionalShadowMatrices = new Matrix4x4[MaxShadowedDirLightCount * MaxCascades];
    Vector4[] _cascadeCullingSpheres = new Vector4[MaxCascades];
    Vector4[] _cascadeData = new Vector4[MaxCascades];
    string[] directionalFilterKeywords = {"_DIRECTIONAL_PCF3", "_DIRECTIONAL_PCF5", "_DIRECTIONAL_PCF7"};
    static string[] cascadeBlendKeywords = { "_CASCADE_BLEND_SOFT", "_CASCADE_BLEND_DITHER" };
}
