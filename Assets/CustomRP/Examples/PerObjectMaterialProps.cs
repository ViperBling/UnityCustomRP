using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 其实类似GPUScene的概念，mesh数据保存在了GPU缓存上
// 完全合批要求所有材质的着色器都是相同的，材质的内存布局相同
[DisallowMultipleComponent]
public class PerObjectMaterialProps : MonoBehaviour
{
    private void OnValidate()
    {
        if (_block == null)
        {
            _block = new MaterialPropertyBlock();
        }
        if (baseMap != null)
        {
            _block.SetTexture(_baseMapID, baseMap);
        }
        _block.SetColor(_baseColorID, baseColor);
        _block.SetFloat(_emissiveID, EmissiveIntensity);
        _block.SetFloat(_cutoffID, cutoff);
        _block.SetFloat(_metallicID, metallic);
        _block.SetFloat(_smoothnessID, smoothness);
        GetComponent<Renderer>().SetPropertyBlock(_block);
    }

    private void Awake()
    {
        OnValidate();
    }

    // 和Shader中的属性标识符对应，根据这个ID来设置对应的属性
    static int 
        _baseColorID = Shader.PropertyToID("_BaseColor"),
        _emissiveID = Shader.PropertyToID("_EmissiveIntensity"),
        _baseMapID = Shader.PropertyToID("_BaseMap"),
        _cutoffID = Shader.PropertyToID("_Cutoff"),
        _metallicID = Shader.PropertyToID("_Metallic"),
        _smoothnessID = Shader.PropertyToID("_Smoothness");

    [SerializeField]
    Color baseColor = Color.white;
    [SerializeField]
    float EmissiveIntensity = 1.0f;
    [SerializeField]
    Texture baseMap;

    [SerializeField, Range(0.0f, 1.0f)]
        float cutoff = 0.5f,
        metallic = 0.0f,
        smoothness = 0.5f;

    static MaterialPropertyBlock _block;
}
