Shader "DeadCircuit/Flicker"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Flicker ("Flicker", Range(0,1)) = 0.2
        _Speed ("Speed", Range(0,20)) = 9
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;
        half _Flicker, _Speed;
        struct Input { float2 uv_MainTex; };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed3 c = tex2D(_MainTex, IN.uv_MainTex).rgb * _Color.rgb;
            half n = frac(sin(dot(IN.uv_MainTex + _Time.y * _Speed, float2(12.9898,78.233))) * 43758.5453);
            half mask = lerp(1.0, step(_Flicker, n), _Flicker);
            o.Albedo = c * mask;
            o.Smoothness = 0.45;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
