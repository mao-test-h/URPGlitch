// refered to:
//     https://github.com/keijiro/KinoGlitch.git
//     Assets/Kino/Glitch/AnalogGlitch.cs

namespace UnityEngine.Rendering.Universal.Glitch
{
    public sealed class AnalogGlitchFeature : ScriptableRendererFeature
    {
        sealed class CustomRenderPass : ScriptableRenderPass
        {
            // CommandBufferPoolから取得するタイミング
            const string FinalBlitPassTag = "Final Blit Pass";

            // Material Properties
            static readonly int MainTexID = Shader.PropertyToID("_MainTex");
            static readonly int ScanLineJitterID = Shader.PropertyToID("_ScanLineJitter");
            static readonly int VerticalJumpID = Shader.PropertyToID("_VerticalJump");
            static readonly int HorizontalShakeID = Shader.PropertyToID("_HorizontalShake");
            static readonly int ColorDriftID = Shader.PropertyToID("_ColorDrift");

            readonly AnalogGlitchFeature _glitchFeature;
            readonly RenderTexture _mainTexture;
            float _verticalJumpTime;

            Material GlitchMaterial => _glitchFeature.MaterialInstance;
            float ScanLineJitter => _glitchFeature.ScanLineJitter;
            float VerticalJump => _glitchFeature.VerticalJump;
            float HorizontalShake => _glitchFeature.HorizontalShake;
            float ColorDrift => _glitchFeature.ColorDrift;

            public CustomRenderPass(AnalogGlitchFeature glitchFeature)
            {
                _glitchFeature = glitchFeature;

                _mainTexture = new RenderTexture(Screen.width, Screen.height, 0);
                _mainTexture.hideFlags = HideFlags.DontSave;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var material = GlitchMaterial;
                if (material == null) return;

                var cmd = CommandBufferPool.Get(FinalBlitPassTag);

                // Copy & Set MainTex.
                var camera = renderingData.cameraData.camera;
                var activeTexture = camera.activeTexture;
                cmd.Blit(activeTexture, _mainTexture);
                material.SetTexture(MainTexID, _mainTexture);

                // Calc Glitch.
                {
                    _verticalJumpTime += Time.deltaTime * VerticalJump * 11.3f;

                    var sl_thresh = Mathf.Clamp01(1.0f - ScanLineJitter * 1.2f);
                    var sl_disp = 0.002f + Mathf.Pow(ScanLineJitter, 3) * 0.05f;
                    material.SetVector(ScanLineJitterID, new Vector2(sl_disp, sl_thresh));

                    var vj = new Vector2(VerticalJump, _verticalJumpTime);
                    material.SetVector(VerticalJumpID, vj);

                    material.SetFloat(HorizontalShakeID, HorizontalShake * 0.2f);

                    var cd = new Vector2(ColorDrift * 0.04f, Time.time * 606.11f);
                    material.SetVector(ColorDriftID, cd);
                }
                cmd.Blit(_mainTexture, camera.activeTexture, material);

                // Execute CmdBuff.
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        [SerializeField] Material _material = default;
        CustomRenderPass _scriptablePass;
        Material _materialInstance;

        public float ScanLineJitter { get; set; } = 0f;
        public float VerticalJump { get; set; } = 0f;
        public float HorizontalShake { get; set; } = 0f;
        public float ColorDrift { get; set; } = 0f;

        Material MaterialInstance
        {
            get
            {
                if (_materialInstance == null)
                {
                    _materialInstance = Instantiate(_material);
                }

                return _materialInstance;
            }
        }

        public override void Create()
        {
            _scriptablePass = new CustomRenderPass(this)
            {
                renderPassEvent = RenderPassEvent.AfterRendering + 1, // 必ず最後に実行されるように調整.
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
            => renderer.EnqueuePass(_scriptablePass);
    }
}
