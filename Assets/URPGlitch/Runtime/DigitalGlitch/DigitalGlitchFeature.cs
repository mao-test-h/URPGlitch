// refered to:
//     https://github.com/keijiro/KinoGlitch.git
//     Assets/Kino/Glitch/DigitalGlitch.cs

using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace URPGlitch.Runtime.DigitalGlitch
{
    [Serializable]
    public sealed class DigitalGlitchFeature : ScriptableRendererFeature
    {
        [SerializeField] Shader shader;
        DigitalGlitchRenderPass _scriptablePass;

        public override void Create()
        {
            _scriptablePass = new DigitalGlitchRenderPass(shader);
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