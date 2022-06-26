// refered to:
//     https://github.com/keijiro/KinoGlitch.git
//     Assets/Kino/Glitch/DigitalGlitch.cs

using System;

namespace UnityEngine.Rendering.Universal.Glitch
{
    [Serializable]
    public sealed class DigitalGlitchFeature : ScriptableRendererFeature
    {
        [SerializeField] Shader shader;
        [SerializeField] PostprocessTiming timing = PostprocessTiming.AfterOpaque;
        [SerializeField] bool applyToSceneView = true;

        DigitalGlitchRenderPass _scriptablePass;

        public override void Create()
        {
            _scriptablePass = new DigitalGlitchRenderPass(applyToSceneView, shader);
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            _scriptablePass.Setup(renderer.cameraColorTarget, timing);
            renderer.EnqueuePass(_scriptablePass);
        }

        protected override void Dispose(bool disposing)
        {
            _scriptablePass.Dispose();
        }

        sealed class DigitalGlitchRenderPass : ScriptableRenderPass, IDisposable
        {
            const string RenderPassName = nameof(DigitalGlitchRenderPass);

            // Material Properties
            static readonly int MainTexID = Shader.PropertyToID("_MainTex");
            static readonly int NoiseTexID = Shader.PropertyToID("_NoiseTex");
            static readonly int TrashTexID = Shader.PropertyToID("_TrashTex");
            static readonly int IntensityID = Shader.PropertyToID("_Intensity");

            readonly ProfilingSampler _profilingSampler;

            readonly bool _applyToSceneView;
            readonly System.Random _random;
            readonly Material _glitchMaterial;
            readonly Texture2D _noiseTexture;

            RenderTargetIdentifier _cameraColorTarget;
            RenderTargetHandle _afterPostProcessTexture;
            RenderTargetHandle _mainFrame;
            RenderTargetHandle _trashFrame1;
            RenderTargetHandle _trashFrame2;
            DigitalGlitchVolume _volume;

            public DigitalGlitchRenderPass(bool applyToSceneView, Shader shader)
            {
                _profilingSampler = new ProfilingSampler(RenderPassName);
                _applyToSceneView = applyToSceneView;
                _random = new System.Random();
                _glitchMaterial = CoreUtils.CreateEngineMaterial(shader);

                _noiseTexture = new Texture2D(64, 32, TextureFormat.ARGB32, false)
                {
                    hideFlags = HideFlags.DontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Point
                };

                // RenderPassEvent.AfterRenderingではポストエフェクトを掛けた後のカラーテクスチャがこの名前で取得できる
                _afterPostProcessTexture.Init("_AfterPostProcessTexture");
                _mainFrame.Init("_MainFrame");
                _trashFrame1.Init("_TrashFrame1");
                _trashFrame2.Init("_TrashFrame2");

                UpdateNoiseTexture();
            }

            public void Dispose()
            {
                CoreUtils.Destroy(_glitchMaterial);
                CoreUtils.Destroy(_noiseTexture);
            }

            public void Setup(RenderTargetIdentifier cameraColorTarget, PostprocessTiming timing)
            {
                _cameraColorTarget = cameraColorTarget;
                renderPassEvent = GetRenderPassEvent(timing);

                var volumeStack = VolumeManager.instance.stack;
                _volume = volumeStack.GetComponent<DigitalGlitchVolume>();
            }


            // This method is called by the renderer before executing the render pass.
            // Override this method if you need to to configure render targets and their clear state, and to create temporary render target textures.
            // If a render pass doesn't override this method, this render pass renders to the active Camera's render target.
            // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                var r = (float)_random.NextDouble();
                if (r > Mathf.Lerp(0.9f, 0.5f, _volume.intensity.value))
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
                if (_glitchMaterial == null) return;
                if (!renderingData.cameraData.postProcessEnabled) return;
                if (!_applyToSceneView && renderingData.cameraData.cameraType == CameraType.SceneView) return;

                // コマンドバッファの取得
                var cmd = CommandBufferPool.Get(RenderPassName);
                cmd.Clear();

                // TODO: まだ動いていない
                using (new ProfilingScope(cmd, _profilingSampler))
                {
                    // `renderPassEvent`が`AfterRendering`の場合には
                    // カメラのカラーターゲットではなく`_AfterPostProcessTexture`を使う
                    var source = (renderPassEvent == RenderPassEvent.AfterRendering
                                  && renderingData.cameraData.resolveFinalTarget)
                        ? _afterPostProcessTexture.Identifier()
                        : _cameraColorTarget;

                    // カメラのターゲットと同じDescription(Depthは無し)のRenderTextureを取得
                    var tempTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                    tempTargetDescriptor.depthBufferBits = 0;
                    cmd.GetTemporaryRT(_mainFrame.id, tempTargetDescriptor);
                    cmd.GetTemporaryRT(_trashFrame1.id, tempTargetDescriptor);
                    cmd.GetTemporaryRT(_trashFrame2.id, tempTargetDescriptor);

                    // 現在のレンダリング結果を保持 (Materialに元絵も渡す必要があるので変換用とTextureの2枚)
                    Blit(cmd, source, _mainFrame.Identifier());

                    // Update trash frames on a constant interval.
                    var frameCount = Time.frameCount;
                    if (frameCount % 13 == 0) Blit(cmd, source, _trashFrame1.Identifier());
                    if (frameCount % 73 == 0) Blit(cmd, source, _trashFrame2.Identifier());

                    // Materialに必要な情報を渡しつつ書き込み.
                    var r = (float)_random.NextDouble();
                    var blitTrashHandle = r > 0.5f ? _trashFrame1 : _trashFrame2;
                    _glitchMaterial.SetFloat(IntensityID, _volume.intensity.value);
                    _glitchMaterial.SetTexture(NoiseTexID, _noiseTexture);
                    cmd.SetGlobalTexture(MainTexID, _mainFrame.Identifier());
                    cmd.SetGlobalTexture(TrashTexID, blitTrashHandle.Identifier());
                    Blit(cmd, _mainFrame.Identifier(), source, _glitchMaterial);

                    cmd.ReleaseTemporaryRT(_mainFrame.id);
                    cmd.ReleaseTemporaryRT(_trashFrame1.id);
                    cmd.ReleaseTemporaryRT(_trashFrame2.id);

                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                }
            }

            RenderPassEvent GetRenderPassEvent(PostprocessTiming postprocessTiming)
            {
                switch (postprocessTiming)
                {
                    case PostprocessTiming.AfterOpaque:
                        return RenderPassEvent.AfterRenderingSkybox;
                    case PostprocessTiming.BeforePostprocess:
                        return RenderPassEvent.BeforeRenderingPostProcessing;
                    case PostprocessTiming.AfterPostprocess:
                        return RenderPassEvent.AfterRendering;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(postprocessTiming), postprocessTiming, null);
                }
            }

            void UpdateNoiseTexture()
            {
                var color = randomColor;

                for (var y = 0; y < _noiseTexture.height; y++)
                {
                    for (var x = 0; x < _noiseTexture.width; x++)
                    {
                        var r = (float)_random.NextDouble();
                        if (r > 0.89f)
                        {
                            color = randomColor;
                        }

                        _noiseTexture.SetPixel(x, y, color);
                    }
                }

                _noiseTexture.Apply();
            }

            Color randomColor
            {
                get
                {
                    var r = (float)_random.NextDouble();
                    var g = (float)_random.NextDouble();
                    var b = (float)_random.NextDouble();
                    var a = (float)_random.NextDouble();
                    return new Color(r, g, b, a);
                }
            }
        }
    }
}