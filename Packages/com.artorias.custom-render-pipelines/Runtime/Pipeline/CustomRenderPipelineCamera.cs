using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP
{
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public class CustomRenderPipelineCamera : MonoBehaviour
    {
        [SerializeField] CameraSettings m_Settings = default;
        
        [System.NonSerialized] ProfilingSampler m_ProfilingSampler;

        public ProfilingSampler m_Sampler => m_ProfilingSampler ??= new(GetComponent<Camera>().name);

        public CameraSettings m_CameraSettings => m_Settings ??= new();
        
#if UNITY_EDITOR
        void OnEnable() => m_ProfilingSampler = null;
#endif
    }
}