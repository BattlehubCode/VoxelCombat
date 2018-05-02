Shader "Toon/Basic Outline Cutout" {
	Properties {
		_Color ("Main Color", Color) = (.5,.5,.5,1)
		_OutlineColor ("Outline Color", Color) = (0,0,0,1)
		_Outline ("Outline width", Range (.002, 0.03)) = .005
		_MainTex ("Base (RGB)", 2D) = "white" { }
		_ToonShade ("ToonShader Cubemap(RGB)", CUBE) = "" { }
		_Cutoff("Alpha cutoff", Range(0,1)) = 0.2
	}
	
	CGINCLUDE
	#include "UnityCG.cginc"
	
	struct appdata {
		float4 vertex : POSITION;
		float2 texcoord : TEXCOORD0;
		float3 normal : NORMAL;
	};

	struct v2f {
		float4 pos : SV_POSITION;
		float2 texcoord : TEXCOORD0;
		UNITY_FOG_COORDS(1)
		fixed4 color : COLOR;
	};
	
	uniform float _Outline;
	uniform float4 _OutlineColor;
	sampler2D _MainTex;
	float4 _MainTex_ST;
	float _Cutoff;
	
	v2f vert(appdata v) {
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);

		float3 norm   = normalize(mul ((float3x3)UNITY_MATRIX_IT_MV, v.normal));
		float2 offset = TransformViewToProjection(norm.xy);

		#ifdef UNITY_Z_0_FAR_FROM_CLIPSPACE //to handle recent standard asset package on older version of unity (before 5.5)
			o.pos.xy += offset * UNITY_Z_0_FAR_FROM_CLIPSPACE(o.pos.z) * _Outline;
		#else
			o.pos.xy += offset * o.pos.z * _Outline;
		#endif

		o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
		o.color = _OutlineColor;
		UNITY_TRANSFER_FOG(o,o.pos);
		return o;
	}
	ENDCG

	SubShader {
		Tags{ "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout" }

		UsePass "Toon/Basic Cutout/BASE"
		Pass {
			Name "OUTLINE"
			Tags{ "LightMode" = "Always" "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout" }
			Cull Front
			ZWrite On
			ColorMask RGB
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = i.color * tex2D(_MainTex, i.texcoord);
				clip(col.a - _Cutoff);
				//UNITY_APPLY_FOG(i.fogCoord, col);
				//return col;
				return col;
			}
			ENDCG
		}
	}
	
	//Fallback "Toon/Basic"
}
