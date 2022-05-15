using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstancedColor : MonoBehaviour
{
    [SerializeField] Color color = Color.white;

    private static MaterialPropertyBlock _propertyBlock;
    private static int _colorID = Shader.PropertyToID("_Color");
    private void Awake()
    {
        // 组件加载或者更新时，在编辑器模式调用
        OnValidate();
    }

    private void OnValidate()
    {
        if (_propertyBlock == null)
        {
            _propertyBlock = new MaterialPropertyBlock();
        }
        _propertyBlock.SetColor(_colorID, color);
        GetComponent<MeshRenderer>().SetPropertyBlock(_propertyBlock);
    }
}
