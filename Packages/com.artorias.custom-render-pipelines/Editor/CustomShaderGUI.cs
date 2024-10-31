using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP
{
    public class CustomShaderGUI : ShaderGUI
    {
        private MaterialEditor m_Editor;
        
        Object[] m_Materials;

        MaterialProperty[] m_Properties;
        
        bool m_ShowPresets;

        bool m_Clipping
        {
            set => SetProperty("_Clipping", "_CLIPPING", value);
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            base.OnGUI(materialEditor, properties);
        }

        void SetProperty(string name, string keyword, bool value)
        {
            if (SetProperty(name, value ? 1f : 0f))
            {
                SetKeyword(keyword, value);
            }
        }

        bool SetProperty(string name, Color value)
        {
            MaterialProperty property = FindProperty(name, m_Properties, false);
            if (property != null)
            {
                property.colorValue = value;
                return true;
            }
            return false;
        }
        
        bool SetProperty(string name, float value)
        {
            MaterialProperty property = FindProperty(name, m_Properties, false);
            if (property != null)
            {
                property.floatValue = value;
                return true;
            }
            return false;
        }
        
        bool SetProperty(string name, int value)
        {
            MaterialProperty property = FindProperty(name, m_Properties, false);
            if (property != null)
            {
                property.floatValue = value;
                return true;
            }
            return false;
        }
        
        bool SetProperty(string name, Vector4 value)
        {
            MaterialProperty property = FindProperty(name, m_Properties, false);
            if (property != null)
            {
                property.vectorValue = value;
                return true;
            }
            return false;
        }
        
        bool SetProperty(string name, Texture value)
        {
            MaterialProperty property = FindProperty(name, m_Properties, false);
            if (property != null)
            {
                property.textureValue = value;
                return true;
            }
            return false;
        }
        
        void SetKeyword(string keyword, bool enabled)
        {
            if (enabled)
            {
                foreach (Material m in m_Materials)
                {
                    m.EnableKeyword(keyword);
                }
            }
            else
            {
                foreach (Material m in m_Materials)
                {
                    m.DisableKeyword(keyword);
                }
            }
        }
    }
}