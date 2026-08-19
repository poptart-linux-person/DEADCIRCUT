Shader "DeadCircuit/WetGrime"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Color ("Tint", Color) = (0.35,0.38,0.4,1)
        _Gloss ("Gloss", Range(0,1)) = 0.75
        _Wetness ("Wetness", Range(0,1)) = 0.45
        _Grime ("Grime", Range(0,1)) = 0.35
        _GrimeTex ("Grime", 2D) = "gray" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex, _GrimeTex;
        fixed4 _Color;
        half _Gloss, _Wetness, _Grime;
        struct Input { float2 uv_MainTex; float2 uv_GrimeTex; };
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 baseCol = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            half grime = tex2D(_GrimeTex, IN.uv_GrimeTex).r * _Grime;
            baseCol.rgb = lerp(baseCol.rgb, baseCol.rgb * 0.22, grime);
            o.Albedo = baseCol.rgb;
            o.Metallic = lerp(0.02, 0.35, _Wetness);
            o.Smoothness = lerp(0.22, _Gloss, _Wetness);
            o.Occlusion = lerp(1.0, 0.62, grime);
            o.Alpha = baseCol.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
