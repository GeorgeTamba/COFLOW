Shader "Custom/DarkTheater"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0,0)
    }
    SubShader
    {
        // Queue 2999: Digambar tepat sebelum UI Canvas (3000)
        Tags { "Queue"="Transparent-1" "RenderType"="Transparent" "IgnoreProjector"="True" }
        
        LOD 100
        
        // --- KUNCI UTAMA EFEK INI ---
        ZWrite Off       // Tidak memblokir objek di belakangnya
        ZTest Always     // SELALU tembus menutupi objek di depannya (meja, tangan)
        Cull Off         // Render luar dan DALAM kotak (tanpa perlu script InvertMesh)
        Blend SrcAlpha OneMinusSrcAlpha // Aktifkan sistem transparansi

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

            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _Color; // Terapkan warna dari script
            }
            ENDCG
        }
    }
}