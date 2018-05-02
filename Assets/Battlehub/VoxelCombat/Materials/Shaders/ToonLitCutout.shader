Shader "Toon/Lit Cutout" {
	Properties {
		_Color ("Main Color", Color) = (0.5,0.5,0.5,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_FogOfWarTex("FogOfWar", 2D) = "" {}
		_Ramp ("Toon Ramp (RGB)", 2D) = "gray" {} 
		_Cutoff("Alpha cutoff", Range(0,1)) = 0.2
	}

	SubShader {
		Tags{ "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout" }
		//ZWrite Off
		// Blend SrcAlpha OneMinusSrcAlpha
		LOD 200
		
CGPROGRAM
#pragma surface surf ToonRamp alphatest:_Cutoff addshadow 

sampler2D _Ramp;
sampler2D _FogOfWarTex;

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

struct Input {
	float2 uv_MainTex : TEXCOORD0;
	float2 uv_FogOfWarTex : TEXCOORD1;
};

void surf (Input IN, inout SurfaceOutput o) {
	half4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
	c *= tex2D(_FogOfWarTex, IN.uv_FogOfWarTex);

	o.Albedo = c.rgb;
	o.Alpha = c.a;
}
ENDCG

	} 

	Fallback "Diffuse"
}


/*

Before fog of war

Shader "Toon/Lit Cutout" {
Properties {
_Color ("Main Color", Color) = (0.5,0.5,0.5,1)
_MainTex ("Base (RGB)", 2D) = "white" {}
_Ramp ("Toon Ramp (RGB)", 2D) = "gray" {}
_Cutoff("Alpha cutoff", Range(0,1)) = 0.2
}

SubShader {
Tags{ "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout" }
//ZWrite Off
// Blend SrcAlpha OneMinusSrcAlpha
LOD 200

CGPROGRAM
#pragma surface surf ToonRamp alphatest:_Cutoff addshadow

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

struct Input {
float2 uv_MainTex : TEXCOORD0;
};

void surf (Input IN, inout SurfaceOutput o) {
half4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
o.Albedo = c.rgb;
o.Alpha = c.a;
}
ENDCG

}

Fallback "Diffuse"
}


*/