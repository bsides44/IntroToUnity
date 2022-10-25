Shader "MARS/AR/Mask"
{
	SubShader
	{
		// Render the mask after regular geometry, but before masked geometry and
		// transparent things.
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry+10" "IgnoreProjector" = "True" }
        ZWrite On
        ZTest LEqual
        ColorMask 0 // Don't draw in the RGBA channels; just the depth buffer

		// Do nothing specific in the pass:
		Pass
		{
		    Name "Occlusion"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                return fixed4(0.0, 0.0, 0.0, 0.0);
            }
            ENDCG
		}
	}
}
