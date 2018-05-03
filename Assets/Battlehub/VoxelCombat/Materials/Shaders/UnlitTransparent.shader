Shader "Unlit/UnlitTransparent"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Color("Main Color", Color) = (.5,.5,.5,1)
		_FogOfWarCutoff("FogOfWar cutoff", Range(0,1)) = 0.0
	}
	SubShader
	{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		LOD 100
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog

			#include "UnityCG.cginc"
			#include "Common.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float2 fogofwarcoord : TEXCOORD1;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Color;

			UNITY_DECLARE_TEX2DARRAY(_FogOfWarTex);
			int _FogOfWarTexIndex;
			int _MapWeight;
			float _FogOfWarCutoff;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				//UNITY_TRANSFER_FOG(o,o.vertex);

				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.fogofwarcoord = GetFogOfWarUV(worldPos, _MapWeight);

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = _Color * tex2D(_MainTex, i.uv);
				float a = 1 - UNITY_SAMPLE_TEX2DARRAY(_FogOfWarTex, float3(i.fogofwarcoord, _FogOfWarTexIndex)).a;
				col *= a;

				clip(a - _FogOfWarCutoff);

				// apply fog
				//UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
