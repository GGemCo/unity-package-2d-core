Shader "Custom/GrayscaleSprite"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Alpha ("Alpha", Range(0,1)) = 1.0
    }
    
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off Lighting Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc" // UI 전용 기능 포함

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR; // UI에서 설정한 color.a 값을 받음
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float _Alpha; // 수동 Alpha 값

            v2f vert (appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.uv = IN.uv;
                OUT.color = IN.color; // UI의 색상 정보 유지
                return OUT;
            }

            fixed4 frag (v2f IN) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, IN.uv);
                float gray = dot(col.rgb, float3(0.3, 0.59, 0.11)); // 그레이스케일 변환

                // UI의 Alpha 값과 Material의 Alpha 값을 둘 다 반영
                float finalAlpha = col.a * IN.color.a * _Alpha; 
                
                return fixed4(gray, gray, gray, finalAlpha);
            }
            ENDCG
        }
    }
}
