﻿using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour
{
    static int m_BaseColorID = Shader.PropertyToID("_BaseColor");
    static int m_CutoffID = Shader.PropertyToID("_Cutoff");
    static MaterialPropertyBlock m_MaterialPropertyBlock;
    
    [SerializeField]
    Color m_BaseColor = Color.white;
    [SerializeField, Range(0f, 1f)]
    float m_Cutoff = 0.5f;

    private void Awake()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        if (m_MaterialPropertyBlock == null)
        {
            m_MaterialPropertyBlock = new MaterialPropertyBlock();
        }

        m_MaterialPropertyBlock.SetColor(m_BaseColorID, m_BaseColor);
        m_MaterialPropertyBlock.SetFloat(m_CutoffID, m_Cutoff);
        GetComponent<Renderer>().SetPropertyBlock(m_MaterialPropertyBlock);
    }
}
