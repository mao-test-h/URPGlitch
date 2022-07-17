// refered to:
//     https://github.com/keijiro/KinoGlitch.git
//     Assets/Kino/Glitch/AnalogGlitch.cs

using System;

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

        sealed class AnalogGlitchRenderPass : ScriptableRenderPass, IDisposable
        {
            const string RenderPassName = "AnalogGlitch Render Pass";

            // Material Properties
            static readonly int ScanLineJitterID = Shader.PropertyToID("_ScanLineJitter");
            static readonly int VerticalJumpID = Shader.PropertyToID("_VerticalJump");
            static readonly int HorizontalShakeID = Shader.PropertyToID("_HorizontalShake");
            static readonly int ColorDriftID = Shader.PropertyToID("_ColorDrift");

            readonly ProfilingSampler _profilingSampler;
            readonly Material _glitchMaterial;
            readonly AnalogGlitchVolume _volume;

            float _verticalJumpTime;

            bool isActive =>
                _glitchMaterial != null &&
                _volume != null &&
                _volume.IsActive;

            public AnalogGlitchRenderPass(Shader shader)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
                _profilingSampler = new ProfilingSampler(RenderPassName);
                _glitchMaterial = CoreUtils.CreateEngineMaterial(shader);

                var volumeStack = VolumeManager.instance.stack;
                _volume = volumeStack.GetComponent<AnalogGlitchVolume>();
            }

            public void Dispose()
            {
                CoreUtils.Destroy(_glitchMaterial);
            }

            // Here you can implement the rendering logic.
            // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
            // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
            // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var isPostProcessEnabled = renderingData.cameraData.postProcessEnabled;
                var isSceneViewCamera = renderingData.cameraData.isSceneViewCamera;
                if (!isActive || !isPostProcessEnabled || isSceneViewCamera)
                {
                    return;
                }

                var cmd = CommandBufferPool.Get(RenderPassName);
                cmd.Clear();
                using (new ProfilingScope(cmd, _profilingSampler))
                {
                    var source = renderingData.cameraData.renderer.cameraColorTarget;

                    var scanLineJitter = _volume.scanLineJitter.value;
                    var verticalJump = _volume.verticalJump.value;
                    var horizontalShake = _volume.horizontalShake.value;
                    var colorDrift = _volume.colorDrift.value;

                    _verticalJumpTime += Time.deltaTime * verticalJump * 11.3f;

                    var slThresh = Mathf.Clamp01(1.0f - scanLineJitter * 1.2f);
                    var slDisp = 0.002f + Mathf.Pow(scanLineJitter, 3) * 0.05f;
                    _glitchMaterial.SetVector(ScanLineJitterID, new Vector2(slDisp, slThresh));

                    var vj = new Vector2(verticalJump, _verticalJumpTime);
                    _glitchMaterial.SetVector(VerticalJumpID, vj);
                    _glitchMaterial.SetFloat(HorizontalShakeID, horizontalShake * 0.2f);

                    var cd = new Vector2(colorDrift * 0.04f, Time.time * 606.11f);
                    _glitchMaterial.SetVector(ColorDriftID, cd);

                    Blit(cmd, ref renderingData, _glitchMaterial);

                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                }
            }
        }
    }
}