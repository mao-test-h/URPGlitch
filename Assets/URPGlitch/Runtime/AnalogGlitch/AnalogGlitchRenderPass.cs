// refered to:
//     https://github.com/keijiro/KinoGlitch.git
//     Assets/Kino/Glitch/AnalogGlitch.cs

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace URPGlitch.Runtime.AnalogGlitch
{
    sealed class AnalogGlitchRenderPass : ScriptableRenderPass, IDisposable
    {
        const string RenderPassName = "AnalogGlitch RenderPass";

        // Material Properties
        static readonly int MainTexID = Shader.PropertyToID("_MainTex");
        static readonly int ScanLineJitterID = Shader.PropertyToID("_ScanLineJitter");
        static readonly int VerticalJumpID = Shader.PropertyToID("_VerticalJump");
        static readonly int HorizontalShakeID = Shader.PropertyToID("_HorizontalShake");
        static readonly int ColorDriftID = Shader.PropertyToID("_ColorDrift");

        readonly ProfilingSampler _profilingSampler;
        readonly Material _glitchMaterial;
        readonly AnalogGlitchVolume _volume;

        RenderTargetHandle _mainFrame;
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

            _mainFrame.Init("_MainFrame");
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

            // TODO: Swap Bufferの検証
            var cmd = CommandBufferPool.Get(RenderPassName);
            cmd.Clear();
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                var source = renderingData.cameraData.renderer.cameraColorTarget;

                var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                cameraTargetDescriptor.depthBufferBits = 0;
                cmd.GetTemporaryRT(_mainFrame.id, cameraTargetDescriptor);
                cmd.Blit(source, _mainFrame.Identifier());

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

                cmd.SetGlobalTexture(MainTexID, _mainFrame.Identifier());
                cmd.Blit(_mainFrame.Identifier(), source, _glitchMaterial);
                cmd.ReleaseTemporaryRT(_mainFrame.id);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}