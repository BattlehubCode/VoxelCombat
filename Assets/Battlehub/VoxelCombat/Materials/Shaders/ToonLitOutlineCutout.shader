Shader "Toon/Lit Outline Cutout" {
	Properties {
		_Color ("Main Color", Color) = (0.5,0.5,0.5,1)
		_OutlineColor ("Outline Color", Color) = (0,0,0,1)
		_Outline ("Outline width", Range (.002, 0.03)) = .005
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Ramp ("Toon Ramp (RGB)", 2D) = "gray" {} 
		_Cutoff("Alpha cutoff", Range(0,1)) = 0.0
		_FogOfWarCutoff("FogOfWar cutoff", Range(0,1)) = 0.0
	}

	SubShader {

		Tags{"Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout" }
		//Tags{ "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout" }
		//ZWrite Off
		///Blend SrcAlpha OneMinusSrcAlpha
		//UsePass "Battlehub/Toon/Lit Cutout/FORWARD"
		UsePass "Toon/Lit Cutout/FORWARD"
		UsePass "Toon/Basic Outline Cutout/OUTLINE"

		// Pass to render object as a shadow caster
		Pass{
			Name "Caster"
			Tags{ "LightMode" = "ShadowCaster" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile_instancing // allow instanced shadow pass for most of the shaders
			#include "UnityCG.cginc"
			#include "Common.cginc"

			struct v2f {
				V2F_SHADOW_CASTER;
				float2  uv : TEXCOORD1;
				float2 fogofwarcoord : TEXCOORD2;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			uniform float4 _MainTex_ST;

			UNITY_DECLARE_TEX2DARRAY(_FogOfWarTex);
			int _FogOfWarTexIndex;
			int _MapWeight;
			float _FogOfWarCutoff;

			v2f vert(appdata_base v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.fogofwarcoord = GetFogOfWarUV(worldPos, _MapWeight);
				return o;
			}

			uniform sampler2D _MainTex;
			float _Cutoff;
		
			uniform fixed4 _Color;
			

			float4 frag(v2f i) : SV_Target
			{
				fixed4 texcol = tex2D(_MainTex, i.uv);
				float a = texcol.a *_Color.a;
				
				clip(a - _Cutoff);
				
				a = (1 - UNITY_SAMPLE_TEX2DARRAY(_FogOfWarTex, float3(i.fogofwarcoord, _FogOfWarTexIndex)).a);
		
				clip(a - _FogOfWarCutoff);
			
				SHADOW_CASTER_FRAGMENT(i)
			}

			
			ENDCG
		}
	} 
	//Fallback "Toon/Lit Cutout"
}
