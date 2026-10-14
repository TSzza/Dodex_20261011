// ============================================================================
//  Sprite Lit By Field  —— 按「光照场」着色的地块材质
//
//  它和 URP 自带的 Sprite-Lit-Default 的区别：
//    Sprite-Lit-Default  由场景里的 Light2D 决定亮度 —— 光走了立刻变暗，没有记忆
//    本 shader           由 CPU 算好的 _LitAmount 决定亮度 —— 这个值带 TTL，
//                        所以光扫过之后地块会「停留一会儿再线性暗下去」
//
//  合成公式（和原游戏 beat-light.html 的 canvas 画法一一对应）：
//      颜色 = 环境底 + 填充色×填充强度 + 描边色×描边强度×描边遮罩
//      最后整体乘 _Brightness（>1 会超过泛光阈值，亮部才会「溢出来」）
//
//  ⚠ 不要用精灵自身的颜色去「乘」光色。
//    这个工程的 SpriteRenderer 各自带颜色（红 / 白 / 青），
//    乘上去红色地块会算成 (0.4, 0.11, 0)，看起来就是"很暗"。
//    贴图颜色是否参与、参与多少，由 _TintInfluence 单独控制（默认只掺 25%）。
//
//  注意：ShaderLab 的 [Header(...)] 括号里**不能出现中文，也不能有空格**
//        （会 Parse error 变粉色），所以这个文件里不用 Header，
//        分组靠属性显示名的前缀。
// ============================================================================
Shader "Dodex/2D/Sprite Lit By Field"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint 染色", Color) = (1,1,1,1)

        // ---- 下面四组由 LitCell2D 按地块类型逐个写入（MPB 覆盖材质值）----
        _LitColor ("Lit Color 单元格色（脚本写入）", Color) = (0.40,0.70,1,1)
        _RimColor ("Rim Color 描边色（脚本写入）", Color) = (0.71,0.83,1,1)
        _FillStrength ("Fill Strength 填充强度（脚本写入）", Range(0,2)) = 0.55
        _RimStrength ("Rim Strength 描边强度（脚本写入）", Range(0,4)) = 0.55
        _LitAmount ("Lit Amount 光照强度（脚本写入）", Range(0,1)) = 0

        // ---- 下面是美术手感旋钮（在材质上直接调）----
        _Ambient ("Ambient 未照亮亮度（0=纯黑）", Range(0,1)) = 0.05
        _AmbientColor ("Ambient Color 未照亮色调", Color) = (0.22,0.30,0.52,1)
        _Brightness ("Brightness 整体亮度（>1 触发泛光）", Range(0,10)) = 3.2
        _TintInfluence ("Tint Influence 贴图颜色参与度", Range(0,1)) = 0.25
        _RimWidth ("Rim Width 描边宽度", Range(0,0.5)) = 0.09

        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType"     = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        // 两个 Pass 共用同一套 HLSL；下面每个 Pass 只需声明用哪对入口函数。
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        float4 _MainTex_ST;
        half4  _Color;
        half4  _RendererColor;
        half4  _AmbientColor;
        half4  _LitColor;
        half4  _RimColor;
        float  _Ambient;
        float  _LitAmount;
        float  _FillStrength;
        float  _RimStrength;
        float  _Brightness;
        float  _TintInfluence;
        float  _RimWidth;

        Varyings LitFieldVertex(Attributes v)
        {
            Varyings o = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

            o.positionCS = TransformObjectToHClip(v.positionOS);
            o.uv         = TRANSFORM_TEX(v.uv, _MainTex);
            o.color      = v.color * _Color * _RendererColor;
            return o;
        }

        half4 LitFieldFragment(Varyings i) : SV_Target
        {
            half4 tex  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
            half  mask = tex.a;                       // 精灵形状遮罩（像素形状）

            // 描边：越靠近地块边框越亮。原游戏每个格子亮起来时带一圈更亮的边，
            // 这是「格子感」的来源 —— 少了它，相邻地块会糊成一片。
            float2 duv  = min(i.uv, 1.0 - i.uv);
            float  edge = min(duv.x, duv.y);
            float  rim  = 1.0 - smoothstep(0.0, max(1e-4, _RimWidth), edge);

            // ① 未照亮：只剩环境色。_Ambient 调到 0 就是纯黑（原游戏的观感）
            half3 col = _AmbientColor.rgb * _Ambient;

            // ② 被照亮：填充色铺满整格
            col += _LitColor.rgb * (_FillStrength * _LitAmount);

            // ③ 被照亮：边框再叠一层更亮的描边色
            col += _RimColor.rgb * (_RimStrength * _LitAmount * rim);

            // ④ 整体增亮。>1 会超过泛光阈值 → 亮部溢出成光晕，这才是「发光」的观感
            col *= _Brightness;

            // ⑤ 贴图自身的明暗掺一点进来（纯白方块贴图时这项无影响）。
            //    注意是 lerp 到 1 而不是直接乘 —— 直接乘会被深色贴图整片压暗。
            col *= lerp(half3(1.0h, 1.0h, 1.0h), tex.rgb, (half)_TintInfluence);

            return half4(col, mask);
        }
        ENDHLSL

        // ① 2D Renderer 走这个 Pass
        Pass
        {
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma vertex   LitFieldVertex
            #pragma fragment LitFieldFragment
            ENDHLSL
        }

        // ② 3D Renderer / 其他情况走这个 Pass（保证换个渲染器也不会变粉）
        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue" = "Transparent" "RenderType" = "Transparent" }
            HLSLPROGRAM
            #pragma vertex   LitFieldVertex
            #pragma fragment LitFieldFragment
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
