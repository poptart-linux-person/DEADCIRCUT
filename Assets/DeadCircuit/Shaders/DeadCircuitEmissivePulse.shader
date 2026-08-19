Shader "DeadCircuit/EmissivePulse"
{
    Properties
    {
        _Color ("Base", Color) = (0.08,0.08,0.1,1)
        _Emission ("Emission", Color) = (0.2,0.6,1,1)
        _Power ("Power", Range(0,8)) = 2
        _Speed ("Pulse Speed", Range(0,12)) = 4
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        fixed4 _Color;
        fixed4 _Emission;
        half _Power, _Speed;
        struct Input { float2 uv_MainTex; };
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            half pulse = 0.55 + 0.45 * sin(_Time.y * _Speed);
            o.Albedo = _Color.rgb;
            o.Metallic = 0.35;
            o.Smoothness = 0.78;
            o.Emission = _Emission.rgb * (_Power * pulse);
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Standard"
}
