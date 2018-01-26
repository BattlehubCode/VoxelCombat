Shader "Toon/Lit Outline Cutout" {
	Properties {
		_Color ("Main Color", Color) = (0.5,0.5,0.5,1)
		_OutlineColor ("Outline Color", Color) = (0,0,0,1)
		_Outline ("Outline width", Range (.002, 0.03)) = .005
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Ramp ("Toon Ramp (RGB)", 2D) = "gray" {} 
		_Cutoff("Alpha cutoff", Range(0,1)) = 0.2
	}

	SubShader {
		Tags{ "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout" }
		//ZWrite Off
		//Blend SrcAlpha OneMinusSrcAlpha
		UsePass "Toon/Lit Cutout/FORWARD"
		UsePass "Toon/Basic Outline Cutout/OUTLINE"
	} 
	
	Fallback "Toon/Lit Cutout"
}
