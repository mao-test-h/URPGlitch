using UnityEngine;
using UnityEngine.Rendering.Universal.Glitch;

namespace Samples
{
    sealed class SampleController : MonoBehaviour
    {
        [SerializeField] AnalogGlitchFeature _analogGlitchFeature = default;

        [Header("Analog")]
        [SerializeField, Range(0f, 1f)] float _scanLineJitter = default;
        [SerializeField, Range(0f, 1f)] float _verticalJump = default;
        [SerializeField, Range(0f, 1f)] float _horizontalShake = default;
        [SerializeField, Range(0f, 1f)] float _colorDrift = default;

        void Update()
        {
            _analogGlitchFeature.ScanLineJitter = _scanLineJitter;
            _analogGlitchFeature.VerticalJump = _verticalJump;
            _analogGlitchFeature.HorizontalShake = _horizontalShake;
            _analogGlitchFeature.ColorDrift = _colorDrift;
        }
    }
}
