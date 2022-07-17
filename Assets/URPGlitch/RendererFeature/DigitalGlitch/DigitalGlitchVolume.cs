using System;

namespace UnityEngine.Rendering.Universal.Glitch
{
    [Serializable]
    [VolumeComponentMenu("Digital Glitch")]
    public class DigitalGlitchVolume : VolumeComponent
    {
        public ClampedFloatParameter intensity = new(0f, 0f, 1f);

        public bool IsActive => intensity.value > 0f;
    }
}