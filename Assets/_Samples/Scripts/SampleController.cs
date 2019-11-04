using UnityEngine;
using UnityEngine.Rendering.Universal.Glitch;

namespace Samples
{
    sealed class SampleController : MonoBehaviour
    {
        [SerializeField] DigitalGlitchFeature _glitchFeature = default;
        [SerializeField, Range(0f, 1f)] float _intensity = default;

        void Update()
        {
            _glitchFeature.Intensity = _intensity;
        }
    }
}
