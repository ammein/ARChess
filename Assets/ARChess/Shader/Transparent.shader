Shader "AR/Transparent"
{
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        Tags { "Queue"="Transparent" }
        ZWrite On
        ZTest LEqual
        ColorMask 0

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Set the color and alpha (0.0 for fully transparent)
                return fixed4(0.0, 0.0, 0.0, 0.0); // Example: semi-transparent black
            }
            ENDCG
        }
    }
}
