using System;

namespace UnityEngine.Rendering.Universal.Glitch
{
    [Serializable]
    [VolumeComponentMenu("Digital Glitch")]
    public class DigitalGlitchVolume : VolumeComponent
    {
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);
    }
}