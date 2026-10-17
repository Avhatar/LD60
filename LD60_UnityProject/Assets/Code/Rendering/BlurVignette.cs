using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LD60.Rendering
{
    [Serializable]
    [VolumeComponentMenu("Post-processing/Blur Vignette")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class BlurVignette : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("How much of the blurred image reaches the fully blurred edge of the gradient. 0 switches the effect off completely and costs nothing.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("Screen point that stays in focus. Screen center is [0.5, 0.5]. Move it to keep an off-center subject sharp.")]
        public Vector2Parameter center = new Vector2Parameter(new Vector2(0.5f, 0.5f));

        [Tooltip("Radius of the fully sharp area, in fractions of the screen height. Everything inside it is left untouched.")]
        public ClampedFloatParameter radius = new ClampedFloatParameter(0.35f, 0f, 1f);

        [Tooltip("Width of the band over which the image goes from sharp to fully blurred. Small values give a hard focus ring, large values a gentle falloff.")]
        public ClampedFloatParameter smoothness = new ClampedFloatParameter(0.3f, 0.01f, 1f);

        [Tooltip("Shape of the sharp area: 0 stretches it to the screen aspect ratio, 1 keeps it a perfect circle on screen.")]
        public ClampedFloatParameter roundness = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("Number of halving steps in the blur pyramid. Each step roughly doubles the maximum blur size and costs one more pair of small blits. Does not blend with volume weight: an integer cannot interpolate smoothly, and every crossed step would show up as a jump in blur size. Fade the effect with Intensity instead.")]
        public NoInterpClampedIntParameter steps = new NoInterpClampedIntParameter(5, 1, 8);

        [Tooltip("Sample offset of a single blur step, in texels. 0.5 is the neutral value the filter is designed around; higher values blur further but can show faint ringing on thin bright details.")]
        public ClampedFloatParameter spread = new ClampedFloatParameter(0.5f, 0f, 1f);

        public bool IsActive() => intensity.value > 0f;

        public bool IsTileCompatible() => false;
    }
}
