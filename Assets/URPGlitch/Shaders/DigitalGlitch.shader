// refered to:
//     https://github.com/keijiro/KinoGlitch.git
//     Assets/Kino/Glitch/Shader/DigitalGlitch.shader
Shader "Universal Render Pipeline/Post Effetcs/Glitch/Digital"
{
    Properties
    {
        _MainTex  ("-", 2D) = "" {}
        _NoiseTex ("-", 2D) = "" {}
        _TrashTex ("-", 2D) = "" {}
        _Intensity ("Intensity", Range(0, 1)) = 1
    }
    
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}

        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM

            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex Vertex
            #pragma fragment Fragment

            #pragma multi_compile _ _LINEAR_TO_SRGB_CONVERSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #ifdef _LINEAR_TO_SRGB_CONVERSION
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #endif

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

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_TrashTex); SAMPLER(sampler_TrashTex);
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
                #if UNITY_UV_STARTS_AT_TOP 
                i.uv = float2(i.uv.x, 1.0 - i.uv.y); 
                #endif

                half4 glitch = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, i.uv);
                #ifdef _LINEAR_TO_SRGB_CONVERSION
                glitch = LinearToSRGB(glitch);
                #endif

                float thresh = 1.001 - _Intensity * 1.001;
                float w_d = step(thresh, pow(abs(glitch.z), 2.5)); // displacement glitch
                float w_f = step(thresh, pow(abs(glitch.w), 2.5)); // frame glitch
                float w_c = step(thresh, pow(abs(glitch.z), 3.5)); // color glitch

                // Displacement.
                float2 uv = frac(i.uv + glitch.xy * w_d);

                half4 source = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                #ifdef _LINEAR_TO_SRGB_CONVERSION
                source = LinearToSRGB(source);
                #endif

                half4 trash = SAMPLE_TEXTURE2D(_TrashTex, sampler_TrashTex, uv);
                #ifdef _LINEAR_TO_SRGB_CONVERSION
                trash = LinearToSRGB(trash);
                #endif

                // Mix with trash frame.
                half3 color = lerp(source, trash, w_f).rgb;

                // Shuffle color components.
                half3 neg = saturate(color.grb + (1 - dot(color, 1)) * 0.5);
                color = lerp(color, neg, w_c);

                return half4(color, source.a);
            }

            ENDHLSL
        }
    }
}
