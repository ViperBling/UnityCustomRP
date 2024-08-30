using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{
    const string m_BufferName = "Lighting";
    const int m_MaxDirLightCount = 4;
    
    static int m_DirLightCountID = Shader.PropertyToID("_DirectionalLightCount");
    static int m_DirLightColorsID = Shader.PropertyToID("_DirectionalLightColors");
    static int m_DirLightDirectionsID = Shader.PropertyToID("_DirectionalLightDirections");
    
    static Vector4[] m_DirLightColors = new Vector4[m_MaxDirLightCount];
    static Vector4[] m_DirLightDirections = new Vector4[m_MaxDirLightCount];
    
    CommandBuffer m_Buffer = new CommandBuffer
    {
        name = m_BufferName
    };
    
    CullingResults m_CullingResults;

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults)
    {
        m_CullingResults = cullingResults;
        m_Buffer.BeginSample(m_BufferName);
        SetupLights();
        m_Buffer.EndSample(m_BufferName);
        context.ExecuteCommandBuffer(m_Buffer);
        m_Buffer.Clear();
    }

    void SetupLights()
    {
        NativeArray<VisibleLight> visibleLights = m_CullingResults.visibleLights;
        int dirLightCount = 0;
        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight light = visibleLights[i];
            if (light.lightType == LightType.Directional)
            {
                SetupDirectionalLight(dirLightCount++, ref light);
                if (dirLightCount >= m_MaxDirLightCount) break;
            }
        }
        m_Buffer.SetGlobalInt(m_DirLightCountID, dirLightCount);
        m_Buffer.SetGlobalVectorArray(m_DirLightColorsID, m_DirLightColors);
        m_Buffer.SetGlobalVectorArray(m_DirLightDirectionsID, m_DirLightDirections);
    }

    void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
    {
        m_DirLightColors[index] = visibleLight.finalColor;
        m_DirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
    }
}