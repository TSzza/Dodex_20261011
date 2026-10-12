// ============================================================================
//  Photon Dot  —— 光子的小方块（原游戏里那些飞出的小蓝点）
//
//  用「加色混合」(Blend SrcAlpha One)：重叠的光子会互相累加变亮，
//  复刻原游戏 canvas 里 globalCompositeOperation = 'lighter' 的效果。
//  颜色与透明度全部走顶点色，所以一次 DrawMesh 就能画完所有光子。
// ============================================================================
Shader "Dodex/2D/Photon Dot"
{
    Properties
    {
        _Color ("Tint 染色", Color) = (0.55,0.78,1,1)
    }

    SubShader
    {
        Tags
        {
            // +10：保证光子在地块之后渲染（地块是 Transparent=3000），不会被盖住
            "Queue"           = "Transparent+10"
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha One      // ← 加色：光子叠加越密越亮
        Cull Off
        ZWrite Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float3 positionOS : POSITION;
            float4 color      : COLOR;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            half4  color      : COLOR;
        };

        half4 _Color;

        Varyings PhotonVertex(Attributes v)
        {
            Varyings o = (Varyings)0;
            o.positionCS = TransformObjectToHClip(v.positionOS);
            o.color      = v.color;
            return o;
        }

        half4 PhotonFragment(Varyings i) : SV_Target
        {
            return half4(i.color.rgb * _Color.rgb, i.color.a * _Color.a);
        }
        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma vertex   PhotonVertex
            #pragma fragment PhotonFragment
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue" = "Transparent" "RenderType" = "Transparent" }
            HLSLPROGRAM
            #pragma vertex   PhotonVertex
            #pragma fragment PhotonFragment
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
