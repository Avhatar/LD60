using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LD60.Rendering
{
    [Serializable]
    [VolumeComponentMenu("Post-processing/Wide Screen Bars")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class WideScreenBars : VolumeComponent, IPostProcessComponent
    {
        public const float NoBarsSize = 0.7071068f;

        [Tooltip("Half height of the visible image, measured from the screen centre along the diagonal. 0.8 leaves the whole frame visible, smaller values slide the black bars in from the top and the bottom. Fade the bars in by animating this down from 0.8.")]
        public ClampedFloatParameter size = new ClampedFloatParameter(0.8f, 0f, 0.8f);

        [Tooltip("Width of the band over which a bar fades into the image. Small values give a hard cinema bar, large values a soft gradient.")]
        public ClampedFloatParameter smoothness = new ClampedFloatParameter(0.01f, 0.001f, 0.4f);

        public bool IsActive() => size.value < NoBarsSize;

        public bool IsTileCompatible() => false;
    }
}
