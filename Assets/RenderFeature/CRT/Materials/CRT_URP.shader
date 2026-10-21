// URP port (Unity 6) of BrewedInk CRTUnlit.
// Original: Built-in RP, CGPROGRAM + OnRenderImage + _MainTex.
// This variant: URP RenderGraph fullscreen pass, HLSL + _BlitTexture,
// driven by CRTFeature/CRTPass. Property names are kept identical so the
// original CRTData presets and CPU sync code work unchanged.
Shader "Hidden/BrewedInk/CRT_URP"
{
    Properties
    {
        _BorderTex ("BorderTexture", 2D) = "white" {}
        _BorderTint ("BorderTint", Color) = (1,1,1,1)

        _MaxColorsRed("MaxRedColors", Range(0, 256)) = 0
        _MaxColorsGreen("MaxGreenColors", Range(0, 256)) = 0
        _MaxColorsBlue("MaxBlueColors", Range(0, 256)) = 0

        _Curvature ("Curvature", Range(0, 20)) = .2
        _Curvature2 ("Curvature2", Range(0, .2)) = .05
        _VigSize ("VigSize", Range(0, 1)) = .1
        _ColorScans ("ColorScans", Vector) = (0, 0, 0, 0)

        _BorderZoom("BorderZoom", Range(.5, 2.5)) = 1
        _Desaturation("Desaturation", Range(0, 1)) = 0
        _Pixelation("Pixelation (URP, 0/1 = off)", Float) = 0

        _BorderOutterSizeX("BorderOutterSizeX", Range(0, .5)) = .2
        _BorderOutterSizeY("BorderOutterSizeY", Range(0, .5)) = .2
        _BorderOutterRound("BorderOutterRound", Range(0, .2)) = .01

        _BorderInnerSizeX("BorderInnerSizeX", Range(0, .5)) = .2
        _BorderInnerSizeY("BorderInnerSizeY", Range(0, .5)) = .2
        _BorderInnerDarkerAmount("BorderInnerDarkerAmmount", Range(0, 1)) = .5

        _BorderInnerSharpness("BorderInnerSharpness", Range(0, 1)) = .2
        _BorderOutterSharpness("BorderOutterSharpness", Range(0, 1)) = .2

        _CrtReflectionCurve("CrtReflectionCurve", Range(0, 10)) = .1
        _CrtReflectionRadius("CrtReflectionRadius", Range(-.1, .1)) = .05
        _CrtReflectionFalloff("CrtReflectionFalloff", Range(0, 1)) = 0
        _CrtGlowAmount("CrtGlowAmount", Range(0, .2)) = .1

        _Spread("DitherSpread4", Range(0, 1)) = .5
        _Spread8("DitherSpread8", Range(0, 1)) = .5
        _DitherScreenScale("DitherScreenScale", Range(.5, 2)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "CRT_URP"
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_BorderTex);
            SAMPLER(sampler_BorderTex);
            float4 _BorderTint;
            float4 _BorderTex_ST;
            float _Curvature;
            float4 _ColorScans;
            float _VigSize;
            float _Curvature2;

            float _BorderOutterSizeX;
            float _BorderOutterSizeY;
            float _BorderInnerSizeY;
            float _BorderInnerSizeX;
            float _BorderOutterRound;

            float _BorderZoom;

            float _BorderInnerSharpness;
            float _BorderOutterSharpness;
            float _Desaturation;
            float _Pixelation;

            float _CrtReflectionFalloff;
            float _CrtReflectionCurve;
            float _CrtReflectionRadius;
            float _CrtGlowAmount;
            float _BorderInnerDarkerAmount;
            float _Spread;
            float _Spread8;
            float _DitherScreenScale;

            float _MaxColorsRed;
            float _MaxColorsGreen;
            float _MaxColorsBlue;

            float _BrewedInk_Bayer4[16];
            float _BrewedInk_Bayer8[64];

            float roundBox(float2 p, float2 b, float r)
            {
                return length(max(abs(p) - b, 0.0)) - r;
            }

            float2 borderReflect(float2 p, float r)
            {
                float eps = 0.0001;
                float2 epsx = float2(eps, 0.0);
                float2 epsy = float2(0.0, eps);
                float2 b = (1.0 + float2(r, r)) * 0.5;
                r /= 3.0;

                p -= 0.5;
                float2 normal = float2(roundBox(p - epsx, b, r) - roundBox(p + epsx, b, r),
                                       roundBox(p - epsy, b, r) - roundBox(p + epsy, b, r)) / eps;
                float d = roundBox(p, b, r);
                p += 0.5;
                return p + d * normal;
            }

            float2 CurvedSurface(float2 uv, float r)
            {
                return r * uv / sqrt(r * r - dot(uv, uv));
            }

            float2 crtCurve(float2 uv, float r)
            {
                r = 3 * r;
                uv = CurvedSurface(uv, r);
                uv = (uv / 2.0) + 0.5;
                return uv;
            }

            float4 sampleColor(float2 screenUv, float2 warpedUv)
            {
                int x4 = (int)fmod(screenUv.x * _ScreenParams.x * _DitherScreenScale, 4.0);
                int y4 = (int)fmod(screenUv.y * _ScreenParams.y * _DitherScreenScale, 4.0);
                float m4 = _BrewedInk_Bayer4[y4 * 4 + x4] * (1.0 / 16.0) - 0.5;

                int x8 = (int)fmod(screenUv.x * _ScreenParams.x * _DitherScreenScale, 8.0);
                int y8 = (int)fmod(screenUv.y * _ScreenParams.y * _DitherScreenScale, 8.0);
                float m8 = _BrewedInk_Bayer8[y8 * 8 + x8] * (1.0 / 64.0) - 0.5;

                float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, warpedUv) + m4 * _Spread + m8 * _Spread8;

                col.r = _MaxColorsRed <= 0 ? col.r : floor(col.r * (_MaxColorsRed - 1) + .5) / (_MaxColorsRed - 1);
                col.g = _MaxColorsGreen <= 0 ? col.g : floor(col.g * (_MaxColorsGreen - 1) + .5) / (_MaxColorsGreen - 1);
                col.b = _MaxColorsBlue <= 0 ? col.b : floor(col.b * (_MaxColorsBlue - 1) + .5) / (_MaxColorsBlue - 1);
                col.rgb = clamp(col.rgb, 0, 1);
                float grey = 0.21 * col.r + 0.71 * col.g + 0.07 * col.b;

                col.rgb = lerp(col.rgb, grey, _Desaturation);

                float t = _Time.z * _ColorScans.w;
                float s = (sin(_ScreenParams.y * screenUv.y * _ColorScans.z + t) + 1) * _ColorScans.x + 1;
                float c = (cos(_ScreenParams.y * screenUv.y * _ColorScans.z + t) + 1) * _ColorScans.y + 1;

                col.g *= s;
                col.rb *= c;

                float2 absUv = abs(warpedUv * 2 - 1);
                float2 invertAbsUv = 1 - absUv;
                float vigSize = lerp(0, 500, _VigSize);
                float2 v = float2(vigSize / _ScreenParams.x, vigSize / _ScreenParams.y);
                float2 vig = smoothstep(0, v, invertAbsUv);
                float vigMask = vig.x * vig.y;

                col = col * vigMask;
                col = clamp(col, 0, 1);

                return col;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float2 p = i.texcoord * 2 - 1;

                p *= _BorderZoom;
                p += p * dot(p, p) * _Curvature2;

                float2 borderUv = p;
                float boundOut = roundBox(borderUv, float2(1 + _BorderOutterSizeX, 1 + _BorderOutterSizeY), _BorderOutterRound) * lerp(5, 100, _BorderOutterSharpness);
                boundOut = clamp(boundOut, 0., 1.0);

                float innerBoarderScale = lerp(5, 100, _BorderInnerSharpness);
                float boundIn = roundBox(borderUv, float2(1 - _BorderInnerSizeX, 1 - _BorderInnerSizeY), _BorderOutterRound) * innerBoarderScale;

                boundIn = clamp(boundIn, 0.0, 1.0);
                float insideMask = boundIn - boundOut;
                float outsideMask = boundOut;

                float insideArg = 4 * (1 - roundBox(borderUv, float2(1 - _BorderInnerSizeX, 1 - _BorderInnerSizeY), _BorderOutterRound) * lerp(1, 70, _CrtReflectionFalloff));
                insideArg = clamp(insideArg, 0, 1);
                float4 borderColor = SAMPLE_TEXTURE2D(_BorderTex, sampler_BorderTex, p * _BorderTex_ST.xy + _BorderTex_ST.zw);
                borderColor.rgb = lerp(borderColor.rgb * _BorderTint.rgb, _BorderTint.rgb, 1 - _BorderTint.a);

                float2 uv = p * (1 - _BorderOutterRound);
                float2 offset = uv / _Curvature;
                float2 curvedSpace = uv + uv * offset * offset;
                float2 mappedUv = curvedSpace * .5 + .5;

                float2 crt = crtCurve(curvedSpace, _CrtReflectionCurve);
                float2 qUv = borderReflect(crt, _CrtReflectionRadius);

                // URP port: single-pass pixelation via UV quantize (the
                // Built-in path used a two-target point-filtered downsample).
                if (_Pixelation > 1.0)
                {
                    float2 pxRes = _ScreenParams.xy / _Pixelation;
                    mappedUv = floor(mappedUv * pxRes) / pxRes;
                    qUv = floor(qUv * pxRes) / pxRes;
                }

                float4 qColor = insideMask * insideArg * sampleColor(i.texcoord, qUv);

                float4 col = sampleColor(i.texcoord, mappedUv);
                float screenMask = 1 - boundIn;

                return col * screenMask + (_CrtGlowAmount * qColor + _BorderInnerDarkerAmount * borderColor * insideMask) + (borderColor * outsideMask);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
