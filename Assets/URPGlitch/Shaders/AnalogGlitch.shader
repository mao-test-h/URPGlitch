// refered to:
//     https://github.com/keijiro/KinoGlitch.git
//     Assets/Kino/Glitch/Shader/AnalogGlitch.shader
Shader "Universal Render Pipeline/Post Effetcs/Glitch/Analog"
{
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

            float2 _ScanLineJitter; // (displacement, threshold)
            float2 _VerticalJump;   // (amount, time)
            float _HorizontalShake;
            float2 _ColorDrift;     // (amount, time)

            float nrand(float x, float y)
            {
                return frac(sin(dot(float2(x, y), float2(12.9898, 78.233))) * 43758.5453);
            }

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

                float u = i.uv.x;
                float v = i.uv.y;

                // Scan line jitter
                float jitter = nrand(v, _Time.x) * 2 - 1;
                jitter *= step(_ScanLineJitter.y, abs(jitter)) * _ScanLineJitter.x;

                // Vertical jump
                float jump = lerp(v, frac(v + _VerticalJump.y), _VerticalJump.x);

                // Horizontal shake
                float shake = (nrand(_Time.x, 2) - 0.5) * _HorizontalShake;

                // Color drift
                float drift = sin(jump + _ColorDrift.y) * _ColorDrift.x;

                half4 src1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, frac(float2(u + jitter + shake, jump)));
                #ifdef _LINEAR_TO_SRGB_CONVERSION
                src1 = LinearToSRGB(src1);
                #endif
                
                half4 src2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, frac(float2(u + jitter + shake + drift, jump)));
                #ifdef _LINEAR_TO_SRGB_CONVERSION
                src2 = LinearToSRGB(src2);
                #endif

                return half4(src1.r, src2.g, src1.b, 1);
            }

            ENDHLSL
        }
    }
}
