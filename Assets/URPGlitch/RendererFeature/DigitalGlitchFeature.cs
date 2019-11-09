// refered to:
//     https://github.com/keijiro/KinoGlitch.git
//     Assets/Kino/Glitch/DigitalGlitch.cs

using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal.Glitch
{
    using Random = Unity.Mathematics.Random;

    public sealed class DigitalGlitchFeature : ScriptableRendererFeature
    {
        sealed class CustomRenderPass : ScriptableRenderPass
        {
            // CommandBufferPoolから取得するタイミング
            const string FinalBlitPassTag = "Final Blit Pass";

            // Material Properties
            static readonly int MainTexID = Shader.PropertyToID("_MainTex");
            static readonly int NoiseTexID = Shader.PropertyToID("_NoiseTex");
            static readonly int TrashTexID = Shader.PropertyToID("_TrashTex");
            static readonly int IntensityID = Shader.PropertyToID("_Intensity");

            readonly DigitalGlitchFeature _glitchFeature;
            Random _random;

            Texture2D _noiseTexture;
            RenderTexture _mainTexture;
            RenderTexture _trashFrame1;
            RenderTexture _trashFrame2;

            Color RandomColor
            {
                get
                {
                    var r = _random.NextFloat4();
                    return new Color(r.x, r.y, r.z, r.w);
                }
            }

            Material GlitchMaterial => _glitchFeature.MaterialInstance;
            float Intensity => _glitchFeature.Intensity;

            public CustomRenderPass(DigitalGlitchFeature glitchFeature)
            {
                _glitchFeature = glitchFeature;
                _random = new Random((uint) System.DateTime.Now.Ticks);

                SetUpResources();
                UpdateNoiseTexture();
            }

            // This method is called before executing the render pass.
            // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
            // When empty this render pass will render to the active camera render target.
            // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
            // The render pipeline will ensure target setup and clearing happens in an performance manner.
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                if (GlitchMaterial == null) return;
                if (_random.NextFloat() > math.lerp(0.9f, 0.5f, Intensity))
                {
                    UpdateNoiseTexture();
                }
            }

            // Here you can implement the rendering logic.
            // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
            // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
            // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var material = GlitchMaterial;
                if (material == null) return;

                // 「PostProcessing完了後」のCommandBufferを取得
                var cmd = CommandBufferPool.Get(FinalBlitPassTag);

                // 現在のレンダリング結果を保持(Materialに元絵も渡す必要があるので変換用とTextureの2枚)
                var camera = renderingData.cameraData.camera;
                var activeTexture = camera.activeTexture;
                cmd.Blit(activeTexture, _mainTexture);

                // Update trash frames on a constant interval.
                var frameCount = Time.frameCount;
                if (frameCount % 13 == 0) cmd.Blit(activeTexture, _trashFrame1);
                if (frameCount % 73 == 0) cmd.Blit(activeTexture, _trashFrame2);

                // Materialに必要な情報を渡しつつ書き込み.
                material.SetFloat(IntensityID, Intensity);
                material.SetTexture(MainTexID, _mainTexture);
                material.SetTexture(NoiseTexID, _noiseTexture);
                material.SetTexture(TrashTexID, _random.NextFloat() > 0.5f ? _trashFrame1 : _trashFrame2);
                cmd.Blit(_mainTexture, camera.activeTexture, material);

                // CommandBufferの実行.
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            void SetUpResources()
            {
                _noiseTexture = new Texture2D(64, 32, TextureFormat.ARGB32, false);
                _noiseTexture.hideFlags = HideFlags.DontSave;
                _noiseTexture.wrapMode = TextureWrapMode.Clamp;
                _noiseTexture.filterMode = FilterMode.Point;

                _trashFrame1 = new RenderTexture(Screen.width, Screen.height, 0);
                _trashFrame2 = new RenderTexture(Screen.width, Screen.height, 0);
                _mainTexture = new RenderTexture(Screen.width, Screen.height, 0);
                _trashFrame1.hideFlags = HideFlags.DontSave;
                _trashFrame2.hideFlags = HideFlags.DontSave;
                _mainTexture.hideFlags = HideFlags.DontSave;
            }

            void UpdateNoiseTexture()
            {
                var color = RandomColor;

                for (var y = 0; y < _noiseTexture.height; y++)
                {
                    for (var x = 0; x < _noiseTexture.width; x++)
                    {
                        if (_random.NextFloat() > 0.89f)
                        {
                            color = RandomColor;
                        }

                        _noiseTexture.SetPixel(x, y, color);
                    }
                }

                _noiseTexture.Apply();
            }
        }

        [SerializeField] Material _material = default;
        CustomRenderPass _scriptablePass;
        Material _materialInstance;

        public float Intensity { get; set; } = 0f;

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
                // Configures where the render pass should be injected.
                renderPassEvent = RenderPassEvent.AfterRendering + 1, // 必ず最後に実行されるように調整.
            };
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
            => renderer.EnqueuePass(_scriptablePass);
    }
}
