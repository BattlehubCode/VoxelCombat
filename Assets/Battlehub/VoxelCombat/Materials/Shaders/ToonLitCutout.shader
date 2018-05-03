Shader "Toon/Lit Cutout" 
{
	Properties 
	{
		_Color ("Main Color", Color) = (0.5,0.5,0.5,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Ramp ("Toon Ramp (RGB)", 2D) = "gray" {} 
		_Cutoff("Alpha cutoff", Range(0,1)) = 0.0
		_FogOfWarCutoff("FogOfWar cutoff", Range(0,1)) = 0.0
	}

	SubShader 
	{
		Tags{ "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout" }
		//ZWrite Off
		// Blend SrcAlpha OneMinusSrcAlpha
		LOD 200
		
		CGPROGRAM
		#include "Common.cginc"

		#pragma surface surf ToonRamp alphatest:_Cutoff addshadow vertex:vert

		sampler2D _Ramp;

		// custom lighting function that uses a texture ramp based
		// on angle between light direction and normal
		#pragma lighting ToonRamp exclude_path:prepass
		inline half4 LightingToonRamp (SurfaceOutput s, half3 lightDir, half atten)
		{
			#ifndef USING_DIRECTIONAL_LIGHT
			lightDir = normalize(lightDir);
			#endif
	
			half d = dot (s.Normal, lightDir)*0.5 + 0.5;
			half3 ramp = tex2D (_Ramp, float2(d,d)).rgb;
	
			half4 c;
			c.rgb = s.Albedo * _LightColor0.rgb * ramp * (atten * 2);
			c.a = 0;
			return c;
		}


		sampler2D _MainTex;
		float4 _Color;

		UNITY_DECLARE_TEX2DARRAY(_FogOfWarTex);
		int _FogOfWarTexIndex;
		int _MapWeight;
		float _FogOfWarCutoff;

		struct Input 
		{
			float2 uv_MainTex : TEXCOORD0;
			float2 fogofwarcoord;
		};

		void vert(inout appdata_full v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);

			float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
			o.fogofwarcoord = GetFogOfWarUV(worldPos, _MapWeight);
		}

		void surf (Input IN, inout SurfaceOutput o)
		{
			half4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			float originalA = c.a;
			float a = 1 - UNITY_SAMPLE_TEX2DARRAY(_FogOfWarTex, float3(IN.fogofwarcoord, _FogOfWarTexIndex)).a;
			c *= a;

			clip(a - _FogOfWarCutoff);
	
			o.Albedo = c.rgb;
			o.Alpha = originalA;
		}
		ENDCG

	} 
	Fallback "Diffuse"
}
