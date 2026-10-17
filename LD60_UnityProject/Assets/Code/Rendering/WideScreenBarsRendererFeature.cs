using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace LD60.Rendering
{
    public class WideScreenBarsRendererFeature : ScriptableRendererFeature
    {
        [Tooltip("Shader that draws the bars. Assign WideScreenBars.shader from Assets/Shaders.")]
        [SerializeField] private Shader _shader;

        [Tooltip("Where the bars are drawn. After post-processing keeps them pure black, because colour grading and bloom no longer run on top of them.")]
        [SerializeField] private RenderPassEvent _renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

        [Tooltip("Also letterbox the Scene view. Off keeps the Scene view uncropped while authoring a level.")]
        [SerializeField] private bool _renderInSceneView;

        private WideScreenBarsPass _pass;
        private Material _material;
        private bool _isReady;

        public override void Create()
        {
            _isReady = false;

            if (_shader == null)
            {
                Debug.LogError($"{nameof(WideScreenBarsRendererFeature)}: {nameof(_shader)} is null in {nameof(Create)}. " +
                               "The bars are drawn by blitting through that shader, so nothing can be rendered without it. " +
                               "Fix: assign WideScreenBars.shader from Assets/Shaders to the Shader field of this renderer feature on the renderer asset.");
                return;
            }

            _material = CoreUtils.CreateEngineMaterial(_shader);
            _pass = new WideScreenBarsPass(_material);
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

            WideScreenBars settings = VolumeManager.instance.stack.GetComponent<WideScreenBars>();

            if (settings == null)
            {
                Debug.LogError($"{nameof(WideScreenBarsRendererFeature)}: {nameof(WideScreenBars)} is null in {nameof(AddRenderPasses)}. " +
                               "The volume stack always carries every override type, so a null here means the override type failed to register. " +
                               "Fix: make sure WideScreenBars.cs compiled without errors and reopen the project.");
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

        private sealed class WideScreenBarsPass : ScriptableRenderPass
        {
            private const int BarsShaderPass = 0;
            private const string BarsPassName = "Wide Screen Bars";

            private static readonly int ParamsId = Shader.PropertyToID("_WideScreenBarsParams");
            private static readonly Vector4 FullScreenScaleBias = new Vector4(1f, 1f, 0f, 0f);
            private static readonly BaseRenderFunc<PassData, RasterGraphContext> ExecuteFunc = ExecutePass;

            private readonly Material _material;
            private readonly ProfilingSampler _profilingSampler = new ProfilingSampler(BarsPassName);

            private WideScreenBars _settings;

            public WideScreenBarsPass(Material material)
            {
                _material = material;
                requiresIntermediateTexture = true;
            }

            public void Setup(WideScreenBars settings)
            {
                _settings = settings;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (_settings == null)
                {
                    Debug.LogError($"{nameof(WideScreenBarsPass)}: {nameof(_settings)} is null in {nameof(RecordRenderGraph)}. " +
                                   "The pass reads the bar size and softness from the resolved volume override. " +
                                   "Fix: enqueue the pass only right after Setup has been called with the resolved override.");
                    return;
                }

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                TextureHandle target = resourceData.activeColorTexture;

                if (!target.IsValid())
                {
                    Debug.LogError($"{nameof(WideScreenBarsPass)}: the active colour texture is invalid in {nameof(RecordRenderGraph)}. " +
                                   "The pass blends the bars straight into the rendered frame. " +
                                   "Fix: move the pass to a render pass event at which the camera colour target exists, such as AfterRenderingPostProcessing.");
                    return;
                }

                using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass(BarsPassName, out PassData data, _profilingSampler))
                {
                    data.Material = _material;
                    data.Params = new Vector4(_settings.size.value, _settings.smoothness.value, 0f, 0f);

                    builder.SetRenderAttachment(target, 0, AccessFlags.ReadWrite);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc(ExecuteFunc);
                }
            }

            private static void ExecutePass(PassData data, RasterGraphContext context)
            {
                context.cmd.SetGlobalVector(ParamsId, data.Params);
                Blitter.BlitTexture(context.cmd, FullScreenScaleBias, data.Material, BarsShaderPass);
            }

            private class PassData
            {
                public Material Material;
                public Vector4 Params;
            }
        }
    }
}
