using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace LiteRP.Editor
{
    public class LiteRPShaderGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            if (!(RenderPipelineManager.currentPipeline is LiteRenderPipeline))
            {
                CoreEditorUtils.DrawFixMeBox("Editing LiteRP materials is only supported when using the LiteRP.", () => SettingsService.OpenProjectSettings("Project/Graphics"));
            }
            else
            {
                OnMaterialGUI(materialEditor, properties);
            }
        }

        private void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            base.OnGUI(materialEditor, properties);
        }
    }
}