Shader "Battlehub/Toon/Lit Cutout"
{
	Properties{
		_Color("Main Color", Color) = (0.5,0.5,0.5,1)
		_OutlineColor("Outline Color", Color) = (0,0,0,1)
		_Outline("Outline width", Range(.002, 0.03)) = .005
		_MainTex("Base (RGB)", 2D) = "white" {}
		_Ramp("Toon Ramp (RGB)", 2D) = "gray" {}
		_Cutoff("Alpha cutoff", Range(0,1)) = 0.0
		_FogOfWarTex("FogOfWarTexture", 2DArray) = "" {}
		_FogOfWarCutoff("FogOfWar cutoff", Range(0,1)) = 0.0
	}
	SubShader
	{
		Tags{ "LightMode" = "Always" "RenderType" = "TransparentCutout" }
			
		LOD 200

		Pass
		{
			Name "FORWARD"
			CGPROGRAM
			#include "Common.cginc"
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float2 fogofwarcoord : TEXCOORD1;
				float3 worldNormal : TEXCOORD2;
				float3 lightDir : TEXCOOR3;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Color;
			UNITY_DECLARE_TEX2DARRAY(_FogOfWarTex);
			int _FogOfWarTexIndex;
			int _MapWeight;
			float _FogOfWarCutoff;
			float _Cutoff;
			sampler2D _Ramp;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.fogofwarcoord = GetFogOfWarUV(worldPos, _MapWeight);

				o.lightDir = UnityWorldSpaceLightDir(v.vertex.xyz);
				o.worldNormal = UnityObjectToWorldNormal(v.normal.xyz);

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				half4 c = tex2D(_MainTex, i.uv) * _Color;
				
				//half d = dot(i.worldNormal, _WorldSpaceLightPos0.xyz)*0.5 + 0.5;
				//half3 ramp = half3(1, 1, 1);// //tex2D(_Ramp, float2(d, d)).rgb;

											//float atten = 0.1f;
				// c.rgb;// *_LightColor0.rgb;// *ramp;// *(atten * 2);

				float originalA = c.a;
				clip(originalA - _Cutoff);

				float a = 1 - UNITY_SAMPLE_TEX2DARRAY(_FogOfWarTex, float3(i.fogofwarcoord, _FogOfWarTexIndex)).a;
				c *= a;

				clip(a - _FogOfWarCutoff);
				
				c.a = originalA;


			//	c.a = 0;
				c.rgb *= 1.25f;
				return c;

			}
			ENDCG
		}
	}
}
