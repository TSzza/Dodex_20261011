// ============================================================================
//  SpriteGlow — 2D 精灵自发光材质（URP 14 / Unity 2022.3）
//
//  干什么用的：
//    给 SpriteRenderer 用一个"自己在发光"的材质。和 URP 自带的
//    Sprite-Lit-Default 不同 —— 那个是"被 Light2D 照亮"，没有任何自发光通道。
//    这个 shader 不做光照计算，输出的是 HDR 颜色，亮的部件交给 Bloom 溢出。
//
//  三层发光叠加（可以只用其中一层）：
//    1) 底色发光   _GlowColor × _GlowIntensity          —— 整体亮起来
//    2) 边缘光     _RimWidth / _RimIntensity            —— 贴图矩形边缘更亮，像霓虹管
//    3) 呼吸       _PulseSpeed / _PulseAmount           —— 亮暗循环（速度 0 = 关闭）
//
//  和内置 Sprites/Default 的关系：
//    Blend / ZWrite / Cull 与 URP 官方 2D shader 保持一致，可以随便替换。
//    同时保留了 _Color / _RendererColor / _Flip 等 legacy 属性，
//    SpriteRenderer 的 Tint 和朝向翻转都正常。
//
//  位置：Assets/Shaders/SpriteGlow.shader
// ============================================================================

Shader "Dodex/2D/Sprite Glow"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _GlowMask ("发光遮罩 R (可选，白=全亮)", 2D) = "white" {}

        [Header(Glow)]
        [HDR] _GlowColor ("发光颜色 (HDR)", Color) = (1, 1, 1, 1)
        _GlowIntensity ("发光强度", Range(0, 8)) = 1.2

        [Header(Rim)]
        _RimWidth ("边缘宽度", Range(0.001, 0.5)) = 0.12
        _RimIntensity ("边缘强度", Range(0, 8)) = 1.5
        _RimAlphaEdge ("轮廓光（贴图带透明时调大）", Range(0, 1)) = 0

        [Header(Pulse)]
        _PulseSpeed ("呼吸速度（0 = 不闪）", Range(0, 10)) = 0
        _PulseAmount ("呼吸幅度", Range(0, 1)) = 0.35

        // ---- Legacy：SpriteRenderer 会写入这些，保留以兼容内置精灵管线 ----
        [HideInInspector] _Color ("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1, 1, 1, 1)
        [HideInInspector] _Flip ("Flip", Vector) = (1, 1, 1, 1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        TEXTURE2D(_GlowMask);
        SAMPLER(sampler_GlowMask);

        // Unity 会自动填充 _MainTex_TexelSize（1/宽, 1/高, 宽, 高）
        float4 _MainTex_TexelSize;
        float4 _MainTex_ST;

        half4 _GlowColor;
        half  _GlowIntensity;
        half  _RimWidth;
        half  _RimIntensity;
        half  _RimAlphaEdge;
        half  _PulseSpeed;
        half  _PulseAmount;

        float4 _Color;
        half4  _RendererColor;

        struct Attributes
        {
            float3 positionOS : POSITION;
            float4 color      : COLOR;
            float2 uv         : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            half4  color      : COLOR;
            float2 uv         : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings GlowVertex(Attributes v)
        {
            Varyings o = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

        #ifdef UNITY_INSTANCING_ENABLED
            v.positionOS = UnityFlipSprite(v.positionOS, unity_SpriteFlip);
        #endif

            o.positionCS = TransformObjectToHClip(v.positionOS);
            o.uv         = TRANSFORM_TEX(v.uv, _MainTex);
            o.color      = v.color * _Color * _RendererColor;

        #ifdef UNITY_INSTANCING_ENABLED
            o.color *= unity_SpriteColor;
        #endif

            return o;
        }

        half4 GlowFragment(Varyings i) : SV_Target
        {
            half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
            half  mask   = SAMPLE_TEXTURE2D(_GlowMask, sampler_GlowMask, i.uv).r;

            // ---- 呼吸：_PulseSpeed = 0 时 cos(0) = 1 → pulse 恒为 1，即完全不闪 ----
            half pulse = 1.0 - _PulseAmount * 0.5 * (1.0 - cos(_Time.y * _PulseSpeed * 6.2831853));

            // ---- 边缘光：按 UV 到矩形边缘的距离取渐变 ----
            half2 d    = abs(i.uv - 0.5) * 2.0;      // 中心 0 → 边缘 1
            half  edge = max(d.x, d.y);
            half  rim  = smoothstep(1.0 - _RimWidth, 1.0, edge);

            // ---- 轮廓光（可选）：比较自身 alpha 与四邻域 alpha 的差。
            //      实心贴图（例如内置 Square）处处 alpha=1，这一项恒为 0，不干扰。----
            if (_RimAlphaEdge > 0.001)
            {
                // 兜底：SpriteRenderer 是通过 MaterialPropertyBlock 注入 _MainTex 的，
                // 这种情况下 Unity 不一定填充 _MainTex_TexelSize（会得到 0），
                // 偏移为 0 就等于取样自身，轮廓光会静默失效。给个下界保证它还能动。
                half2 texel = max(_MainTex_TexelSize.xy, half2(0.002, 0.002));
                half  aC    = sprite.a;
                half  aN    = max(
                    max(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + half2(texel.x, 0)).a,
                        SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv - half2(texel.x, 0)).a),
                    max(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + half2(0, texel.y)).a,
                        SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv - half2(0, texel.y)).a));
                rim = max(rim, saturate((aC - aN) * 8.0) * _RimAlphaEdge);
            }

            // ---- 合成：底色 + 发光（边缘更强）----
            // 故意不 saturate：把 >1 的 HDR 值留给 Bloom，
            // 否则亮度会被钳在 1，后处理拿不到"过曝"信息，就只剩个白块。
            half3 glow = _GlowColor.rgb * (_GlowIntensity * pulse * mask);
            glow *= (1.0 + rim * _RimIntensity);

            return half4(sprite.rgb + glow, sprite.a);
        }
        ENDHLSL

        // 2D Renderer 走这个 Pass
        Pass
        {
            Name "Universal2D"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex GlowVertex
            #pragma fragment GlowFragment
            #pragma multi_compile_instancing
            ENDHLSL
        }

        // Forward Renderer（如果哪天把 Renderer 换成 Forward）走这个
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex GlowVertex
            #pragma fragment GlowFragment
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
