// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Toon/Basic Cutout" {
	Properties{
		_Color("Main Color", Color) = (.5,.5,.5,1)
		_MainTex("Base (RGB)", 2D) = "white" {}
		_FogOfWarTex("FogOfWar", 2D) = "" {}
		_ToonShade("ToonShader Cubemap(RGB)", CUBE) = "" { }
		_Cutoff("Alpha cutoff", Range(0,1)) = 0.2
	}


		SubShader{
		Tags{ "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout" }
		//ZWrite Off
		//Blend SrcAlpha OneMinusSrcAlpha
		Pass{
		Name "BASE"
		Cull Off

		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma multi_compile_fog

		#include "UnityCG.cginc"

		sampler2D _MainTex;
		sampler2D _FogOfWarTex;
		samplerCUBE _ToonShade;
		float4 _MainTex_ST;
		float4 _Color;
		float _Cutoff;

		struct appdata {
			float4 vertex : POSITION;
			float2 texcoord : TEXCOORD0;
			float3 normal : NORMAL;
		};

		struct v2f {
			float4 pos : SV_POSITION;
			float2 texcoord : TEXCOORD0;
			float2 fogofwarcoord : TEXCOORD1;
		};

		float2 GetFogOfWarUV(float3 worldpos, int weight)
		{
			float mapSize = 1 << weight;
			float cellSize = 0.5f;

			int row = floor(worldpos.z / cellSize) + mapSize / 2;
			int col = floor(worldpos.x / cellSize) + mapSize / 2;

			return float2(col / (mapSize - 1), row / (mapSize - 1));
		}

		v2f vert(appdata v)
		{
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
			float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;


			float weight = 8;

			o.fogofwarcoord  = GetFogOfWarUV(worldPos, weight);





			//o.cubenormal = UnityObjectToViewPos(float4(v.normal,0));
			return o;
		}

		fixed4 frag(v2f i) : SV_Target
		{
			fixed4 c = _Color * tex2D(_MainTex, i.texcoord);

			

			float a = 1 - tex2D(_FogOfWarTex, i.fogofwarcoord).a;

			c *= a;
			c.a = a;

			return c;// float4(a, a, a, a);
			//fixed4 cube = texCUBE(_ToonShade, i.cubenormal);
			//fixed4 c =  fixed4(2.0f * cube.rgb * col.rgb, col.a);

			//if (c.a <= _Cutoff)
			//{
			//	c.a = 0;
			//}
			
			//return c;
		}
		ENDCG
		}
	}
	Fallback "VertexLit"
}

