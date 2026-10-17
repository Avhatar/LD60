using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace LD60.Rendering
{
    public class BlurVignetteRendererFeature : ScriptableRendererFeature
    {
        [Tooltip("Shader that performs the blur pyramid and the final composite. Assign BlurVignette.shader from Assets/Shaders.")]
        [SerializeField] private Shader _shader;

        [Tooltip("Where the blur is injected. Before post-processing keeps it in HDR scene colour, so colour grading, bloom and the regular vignette still apply on top of the blurred edges.")]
        [SerializeField] private RenderPassEvent _renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        [Tooltip("Also blur the Scene view. Off keeps the Scene view sharp while authoring a level.")]
        [SerializeField] private bool _renderInSceneView;

        private BlurVignettePass _pass;
        private Material _material;
        private bool _isReady;

        public override void Create()
        {
            _isReady = false;

            if (_shader == null)
            {
                Debug.LogError($"{nameof(BlurVignetteRendererFeature)}: {nameof(_shader)} is null in {nameof(Create)}. " +
                               "Every pass of this feature blits through that shader, so nothing can be rendered without it. " +
                               "Fix: assign BlurVignette.shader from Assets/Shaders to the Shader field of this renderer feature on the renderer asset.");
                return;
            }

            _material = CoreUtils.CreateEngineMaterial(_shader);
            _pass = new BlurVignettePass(_material);
            _isReady = true;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!_isReady)
                return;

            CameraType cameraType = renderingData.cameraData.cameraType;

            if (cameraType == CameraType.Reflection || cameraType == CameraType.Preview)
                return;

            if (cameraType == CameraType.SceneView && !_renderInSceneView)
                return;

            BlurVignette settings = VolumeManager.instance.stack.GetComponent<BlurVignette>();

            if (settings == null)
            {
                Debug.LogError($"{nameof(BlurVignetteRendererFeature)}: {nameof(BlurVignette)} is null in {nameof(AddRenderPasses)}. " +
                               "The volume stack always carries every override type, so a null here means the override type failed to register. " +
                               "Fix: make sure BlurVignette.cs compiled without errors and reopen the project.");
                return;
            }

            if (!settings.IsActive())
                return;

            _pass.Setup(settings);
            _pass.renderPassEvent = _renderPassEvent;
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_material);

            _material = null;
            _pass = null;
            _isReady = false;
            base.Dispose(disposing);
        }

        private sealed class BlurVignettePass : ScriptableRenderPass
        {
            private const int MaxSteps = 8;
            private const int DownsampleShaderPass = 0;
            private const int UpsampleShaderPass = 1;
            private const int CompositeShaderPass = 2;
            private const int CopyShaderPass = 3;
            private const string CompositeTextureName = "_BlurVignetteComposite";
            private const string CompositePassName = "Blur Vignette Composite";
            private const string ResolvePassName = "Blur Vignette Resolve";

            private static readonly int ShapeId = Shader.PropertyToID("_BlurVignetteShape");
            private static readonly int ParamsId = Shader.PropertyToID("_BlurVignetteParams");
            private static readonly int TexelSizeId = Shader.PropertyToID("_BlurVignetteTexelSize");
            private static readonly int BlurTextureId = Shader.PropertyToID("_BlurVignetteBlurTexture");

            private static readonly Vector4 FullScreenScaleBias = new Vector4(1f, 1f, 0f, 0f);
            private static readonly BaseRenderFunc<PassData, RasterGraphContext> ExecuteFunc = ExecutePass;

            private static readonly string[] PyramidTextureNames =
            {
                "_BlurVignettePyramid0",
                "_BlurVignettePyramid1",
                "_BlurVignettePyramid2",
                "_BlurVignettePyramid3",
                "_BlurVignettePyramid4",
                "_BlurVignettePyramid5",
                "_BlurVignettePyramid6",
                "_BlurVignettePyramid7"
            };

            private static readonly string[] DownsamplePassNames =
            {
                "Blur Vignette Downsample 0",
                "Blur Vignette Downsample 1",
                "Blur Vignette Downsample 2",
                "Blur Vignette Downsample 3",
                "Blur Vignette Downsample 4",
                "Blur Vignette Downsample 5",
                "Blur Vignette Downsample 6",
                "Blur Vignette Downsample 7"
            };

            private static readonly string[] UpsamplePassNames =
            {
                "Blur Vignette Upsample 0",
                "Blur Vignette Upsample 1",
                "Blur Vignette Upsample 2",
                "Blur Vignette Upsample 3",
                "Blur Vignette Upsample 4",
                "Blur Vignette Upsample 5",
                "Blur Vignette Upsample 6",
                "Blur Vignette Upsample 7"
            };

            private readonly Material _material;
            private readonly TextureHandle[] _pyramid = new TextureHandle[MaxSteps];
            private readonly int[] _levelWidths = new int[MaxSteps];
            private readonly int[] _levelHeights = new int[MaxSteps];
            private readonly ProfilingSampler _profilingSampler = new ProfilingSampler("Blur Vignette");

            private BlurVignette _settings;

            public BlurVignettePass(Material material)
            {
                _material = material;
                requiresIntermediateTexture = true;
            }

            public void Setup(BlurVignette settings)
            {
                _settings = settings;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (_settings == null)
                {
                    Debug.LogError($"{nameof(BlurVignettePass)}: {nameof(_settings)} is null in {nameof(RecordRenderGraph)}. " +
                                   "The pass reads every blur parameter from the resolved volume override. " +
                                   "Fix: enqueue the pass only right after Setup has been called with the resolved override.");
                    return;
                }

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                TextureHandle source = resourceData.activeColorTexture;

                if (!source.IsValid())
                {
                    Debug.LogError($"{nameof(BlurVignettePass)}: the active colour texture is invalid in {nameof(RecordRenderGraph)}. " +
                                   "The pass reads the rendered frame from it and writes the composited result back into it. " +
                                   "Fix: move the pass to a render pass event at which the camera colour target exists, such as BeforeRenderingPostProcessing.");
                    return;
                }

                RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
                descriptor.msaaSamples = 1;
                descriptor.depthBufferBits = 0;
                descriptor.useMipMap = false;
                descriptor.autoGenerateMips = false;

                int steps = CreatePyramid(renderGraph, descriptor);

                if (steps == 0)
                    return;

                TextureHandle composite = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, CompositeTextureName, false, FilterMode.Bilinear, TextureWrapMode.Clamp);
                ApplyParameters();

                AddFullScreenPass(renderGraph, DownsamplePassNames[0], source, _pyramid[0], DownsampleShaderPass, descriptor.width, descriptor.height, default, false);

                for (int i = 1; i < steps; i++)
                    AddFullScreenPass(renderGraph, DownsamplePassNames[i], _pyramid[i - 1], _pyramid[i], DownsampleShaderPass, _levelWidths[i - 1], _levelHeights[i - 1], default, false);

                for (int i = steps - 1; i > 0; i--)
                    AddFullScreenPass(renderGraph, UpsamplePassNames[i], _pyramid[i], _pyramid[i - 1], UpsampleShaderPass, _levelWidths[i], _levelHeights[i], default, false);

                AddFullScreenPass(renderGraph, CompositePassName, source, composite, CompositeShaderPass, descriptor.width, descriptor.height, _pyramid[0], true);
                AddFullScreenPass(renderGraph, ResolvePassName, composite, source, CopyShaderPass, descriptor.width, descriptor.height, default, false);
            }

            private int CreatePyramid(RenderGraph renderGraph, RenderTextureDescriptor descriptor)
            {
                int requestedSteps = Mathf.Clamp(_settings.steps.value, 1, MaxSteps);
                int width = descriptor.width;
                int height = descriptor.height;
                int steps = 0;

                for (int i = 0; i < requestedSteps; i++)
                {
                    int levelWidth = width >> 1;
                    int levelHeight = height >> 1;

                    if (levelWidth < 2 || levelHeight < 2)
                        break;

                    width = levelWidth;
                    height = levelHeight;

                    RenderTextureDescriptor levelDescriptor = descriptor;
                    levelDescriptor.width = levelWidth;
                    levelDescriptor.height = levelHeight;

                    _pyramid[i] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, levelDescriptor, PyramidTextureNames[i], false, FilterMode.Bilinear, TextureWrapMode.Clamp);
                    _levelWidths[i] = levelWidth;
                    _levelHeights[i] = levelHeight;
                    steps++;
                }

                return steps;
            }

            private void AddFullScreenPass(RenderGraph renderGraph, string passName, TextureHandle source, TextureHandle destination, int shaderPass, int sourceWidth, int sourceHeight, TextureHandle blurTexture, bool bindBlurTexture)
            {
                using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass(passName, out PassData data, _profilingSampler))
                {
                    data.Source = source;
                    data.BlurTexture = blurTexture;
                    data.Material = _material;
                    data.ShaderPass = shaderPass;
                    data.TexelSize = new Vector4(1f / sourceWidth, 1f / sourceHeight, sourceWidth, sourceHeight);
                    data.BindBlurTexture = bindBlurTexture;

                    builder.UseTexture(source);

                    if (bindBlurTexture)
                        builder.UseTexture(blurTexture);

                    builder.SetRenderAttachment(destination, 0);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc(ExecuteFunc);
                }
            }

            private void ApplyParameters()
            {
                Vector2 center = _settings.center.value;
                _material.SetVector(ShapeId, new Vector4(center.x, center.y, _settings.radius.value, _settings.smoothness.value));
                _material.SetVector(ParamsId, new Vector4(_settings.roundness.value, _settings.intensity.value, _settings.spread.value, 0f));
            }

            private static void ExecutePass(PassData data, RasterGraphContext context)
            {
                context.cmd.SetGlobalVector(TexelSizeId, data.TexelSize);

                if (data.BindBlurTexture)
                    context.cmd.SetGlobalTexture(BlurTextureId, data.BlurTexture);

                Blitter.BlitTexture(context.cmd, data.Source, FullScreenScaleBias, data.Material, data.ShaderPass);
            }

            private class PassData
            {
                public TextureHandle Source;
                public TextureHandle BlurTexture;
                public Material Material;
                public int ShaderPass;
                public Vector4 TexelSize;
                public bool BindBlurTexture;
            }
        }
    }
}
