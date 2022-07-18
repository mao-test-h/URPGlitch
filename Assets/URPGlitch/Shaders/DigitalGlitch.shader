// refered to:
//     https://github.com/keijiro/KinoGlitch.git
//     Assets/Kino/Glitch/Shader/DigitalGlitch.shader
Shader "URPGlitch/RenderFeature//Digital"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        Pass
        {
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                half4 positionCS : SV_POSITION;
                half2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            TEXTURE2D(_TrashTex);
            SAMPLER(sampler_TrashTex);

            float _Intensity;

            Varyings Vertex(Attributes i)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                output.uv = i.uv;
                return output;
            }

            half4 Fragment(Varyings i) : SV_Target
            {
                float4 glitch = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, i.uv);

                float thresh = 1.001 - _Intensity * 1.001;
                float w_d = step(thresh, pow(abs(glitch.z), 2.5)); // displacement glitch
                float w_f = step(thresh, pow(abs(glitch.w), 2.5)); // frame glitch
                float w_c = step(thresh, pow(abs(glitch.z), 3.5)); // color glitch

                // Displacement.
                float2 uv = frac(i.uv + glitch.xy * w_d);
                float4 source = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                float4 trash = SAMPLE_TEXTURE2D(_TrashTex, sampler_TrashTex, uv);

                // Mix with trash frame.
                float3 color = lerp(source, trash, w_f).rgb;

                // Shuffle color components.
                float3 neg = saturate(color.grb + (1 - dot(color, 1)) * 0.5);
                color = lerp(color, neg, w_c);

                return float4(color, source.a);
            }
            ENDHLSL
        }
    }
}