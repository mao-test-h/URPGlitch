// refered to:
//     https://github.com/keijiro/KinoGlitch.git
//     Assets/Kino/Glitch/AnalogGlitch.cs

namespace UnityEngine.Rendering.Universal.Glitch
{
    public sealed class AnalogGlitchFeature : ScriptableRendererFeature
    {
        [SerializeField] Shader shader;
        AnalogGlitchRenderPass _scriptablePass;

        public override void Create()
        {
            _scriptablePass = new AnalogGlitchRenderPass(shader);
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(_scriptablePass);
        }

        protected override void Dispose(bool disposing)
        {
            _scriptablePass.Dispose();
        }
    }
}