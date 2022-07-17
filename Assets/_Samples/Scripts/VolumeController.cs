using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using URPGlitch.Runtime.AnalogGlitch;
using URPGlitch.Runtime.DigitalGlitch;

namespace _Samples.Scripts
{
    public class VolumeController : MonoBehaviour
    {
        [SerializeField] Volume volume;
        [SerializeField] Scrollbar digitalIntensity;
        [SerializeField] Scrollbar analogScanlineJitter;
        [SerializeField] Scrollbar analogVerticalJump;
        [SerializeField] Scrollbar analogHorizontalShake;
        [SerializeField] Scrollbar analogColorDrift;

        void Start()
        {
            volume.profile.TryGet<DigitalGlitchVolume>(out var digitalGlitchVolume);
            volume.profile.TryGet<AnalogGlitchVolume>(out var analogGlitchVolume);

            digitalIntensity.onValueChanged.AddListener(val => { digitalGlitchVolume.intensity.value = val; });

            analogScanlineJitter.onValueChanged.AddListener(val => { analogGlitchVolume.scanLineJitter.value = val; });

            analogVerticalJump.onValueChanged.AddListener(val => { analogGlitchVolume.verticalJump.value = val; });

            analogHorizontalShake.onValueChanged.AddListener(val =>
            {
                analogGlitchVolume.horizontalShake.value = val;
            });

            analogColorDrift.onValueChanged.AddListener(val => { analogGlitchVolume.colorDrift.value = val; });
        }
    }
}