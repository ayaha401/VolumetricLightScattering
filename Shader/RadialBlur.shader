Shader "Hidden/RadialBlur"
{
    Properties
    {
        _Color("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _BlurWidth("Blur Width", Range(0, 1)) = 0.85
        _Intensity("Intensity", Range(0, 1)) = 1
        _Center("Center", Vector) = (0.5, 0.5, 0, 0)
        _NumSamples("Number of Samples", Range(50, 200)) = 100
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        Blend One One


        Pass
        {
            Name "RadialBlur"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_RadialTexture);
            SAMPLER(sampler_RadialTexture);
            half4 _Color;
            float _BlurWidth;
            float _Intensity;
            float4 _Center;
            int _NumSamples;

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 color = half4(0.0, 0.0, 0.0, 1.0);
                float2 texCoord = input.texcoord;

                float2 ray = texCoord - _Center.xy;
                float denom = 1.0 / float(_NumSamples) * _BlurWidth;

                for (int i = 0; i < _NumSamples; i++)
                {
                    float scale = 1.0f - float(i) * denom;
                    half3 texCol = SAMPLE_TEXTURE2D_X(_RadialTexture, sampler_RadialTexture, (ray * scale) + _Center.xy).xyz;
                    color.xyz += texCol * denom;
                }
                
                return color * _Intensity * _Color;
            }
            ENDHLSL
        }
    }
}