using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomShaderGUI : ShaderGUI 
{
	bool Clipping 
	{
		set => SetProperty("_Clipping", "_CLIPPING", value);
	}

	bool HasPremultiplyAlpha => HasProperty("_PremulAlpha");

	bool PremultiplyAlpha 
	{
		set => SetProperty("_PremulAlpha", "_PREMULTIPLY_ALPHA", value);
	}

	BlendMode SrcBlend 
	{
		set => SetProperty("_SrcBlend", (float)value);
	}

	BlendMode DstBlend 
	{
		set => SetProperty("_DstBlend", (float)value);
	}

	bool ZWrite
	{
		set => SetProperty("_ZWrite", value ? 1f : 0f);
	}

	enum ShadowMode
	{
		On, Clip, Dither, Off
	}
	ShadowMode Shadows
	{
		set
		{
			if (SetProperty("_Shadows", (float)value))
			{
				SetKeyword("_SHADOWS_CLIP", value == ShadowMode.Clip);
				SetKeyword("_SHADOWS_DITHER", value == ShadowMode.Dither);
			}
		}
	}

	public override void OnGUI (MaterialEditor materialEditor, MaterialProperty[] properties)
	{
		EditorGUI.BeginChangeCheck();
		base.OnGUI(materialEditor, properties);
		_editor = materialEditor;
		_materials = materialEditor.targets;
		this._properties = properties;

		EditorGUILayout.Space();
		_showPresets = EditorGUILayout.Foldout(_showPresets, "Presets", true);
		if (_showPresets) 
		{
			OpaquePreset();
			ClipPreset();
			FadePreset();
			TransparentPreset();
		}
		if (EditorGUI.EndChangeCheck())
		{
			SetShadowCasterPass();
		}
	}

	void OpaquePreset () 
	{
		if (PresetButton("Opaque")) 
		{
			Clipping = false;
			PremultiplyAlpha = false;
			SrcBlend = BlendMode.One;
			DstBlend = BlendMode.Zero;
			ZWrite = true;
			RenderQueue = RenderQueue.Geometry;
		}
	}

	void ClipPreset () 
	{
		if (PresetButton("Clip")) 
		{
			Clipping = true;
			PremultiplyAlpha = false;
			SrcBlend = BlendMode.One;
			DstBlend = BlendMode.Zero;
			ZWrite = true;
			RenderQueue = RenderQueue.AlphaTest;
		}
	}

	void FadePreset () 
	{
		if (PresetButton("Fade")) 
		{
			Clipping = false;
			PremultiplyAlpha = false;
			SrcBlend = BlendMode.SrcAlpha;
			DstBlend = BlendMode.OneMinusSrcAlpha;
			ZWrite = false;
			RenderQueue = RenderQueue.Transparent;
		}
	}

	void TransparentPreset () 
	{
		if (HasPremultiplyAlpha && PresetButton("Transparent")) 
		{
			Clipping = false;
			PremultiplyAlpha = true;
			SrcBlend = BlendMode.One;
			DstBlend = BlendMode.OneMinusSrcAlpha;
			ZWrite = false;
			RenderQueue = RenderQueue.Transparent;
		}
	}

	bool PresetButton (string name) 
	{
		if (GUILayout.Button(name)) 
		{
			_editor.RegisterPropertyChangeUndo(name);
			return true;
		}
		return false;
	}

	void SetShadowCasterPass() 
	{
		MaterialProperty shadows = FindProperty("_Shadows", _properties, false);
		if (shadows == null || shadows.hasMixedValue) { return; }

		bool enabled = shadows.floatValue < (float)ShadowMode.Off;
		foreach (Material m in _materials)
		{
			m.SetShaderPassEnabled("ShadowCaster", enabled);
		}
	}

	bool HasProperty (string name) =>
		FindProperty(name, _properties, false) != null;

	void SetProperty (string name, string keyword, bool value) 
	{
		if (SetProperty(name, value ? 1f : 0f)) 
		{
			SetKeyword(keyword, value);
		}
	}

	bool SetProperty (string name, float value)
	{
		MaterialProperty property = FindProperty(name, _properties, false);
		if (property != null) 
		{
			property.floatValue = value;
			return true;
		}
		return false;
	}

	void SetKeyword (string keyword, bool enabled)
	{
		if (enabled)
		{
			foreach (Material mat in _materials) 
			{
				mat.EnableKeyword(keyword);
			}
		}
		else 
		{
			foreach (Material mat in _materials)
			{
				mat.DisableKeyword(keyword);
			}
		}
	}
	
	RenderQueue RenderQueue 
	{
		set {
			foreach (Material mat in _materials) 
			{
				mat.renderQueue = (int)value;
			}
		}
	}
	
	MaterialEditor _editor;
	Object[] _materials;
	MaterialProperty[] _properties;

	bool _showPresets;
}