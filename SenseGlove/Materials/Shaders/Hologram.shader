// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Hologram" {
	Properties {
         _Color ("First Color", Color) = (1,1,1,1)
         _SecondColor ("Second Color", Color) = (1, 1, 1, 1)
		 _ThirdColor ("Third Color", Color) = (1, 1, 1, 1)
         _RimWidth ("Rim Width", Range (0, 2)) = 0.9
         _FallOffTex ("Fall Off (RGB)", 2D) = "white" {}
     }
     SubShader {    
	 	Tags { "RenderType"="Transparent" "Queue"="Transparent+1" "IgnoreProjector"="True" "LightMode" = "ForwardBase"}
	 	LOD 800
		Blend SrcAlpha OneMinusSrcAlpha

	
     	Pass {
			ZWrite On

     		CGPROGRAM
         	#pragma vertex vert
         	#pragma fragment frag
         	#include "UnityCG.cginc"

	         struct appdata { 
	             float4 vertex : POSITION; 
	             float3 normal : NORMAL; 
	             float2 texcoord : TEXCOORD0; 
	         };
 
	         struct v2f { 
	             float4 pos : SV_POSITION;
	             float2 uv : TEXCOORD1;
	             float3 color : COLOR;
	         };      
 
	         uniform float4 _FallOffTex_ST;
	         half _RimWidth;          

	         v2f vert (appdata_base v) {
	             v2f o;
	             o.pos = UnityObjectToClipPos (v.vertex);

	             float3 viewDir = normalize(ObjSpaceViewDir(v.vertex));
	             float dotProduct = 1 - dot(v.normal, viewDir);
	             float rimWidth = _RimWidth;
	             o.color = smoothstep(1 - rimWidth, 1.0, dotProduct);

	             o.uv = TRANSFORM_TEX(v.texcoord, _FallOffTex);

	             return o;
	         }

	         uniform sampler2D _FallOffTex;
	         uniform float4 _Color;

				float4 frag(v2f i) : COLOR {
					float4 falloffcol = tex2D(_FallOffTex, i.uv);

					clip(falloffcol.a - 0.01f);

					falloffcol *= _Color;

					falloffcol.a = ((i.color.r + i.color.g + i.color.b) * falloffcol.a) * _Color.a;

					return falloffcol;
				}
             ENDCG
        }

		Pass {   
			ZWrite On

     		CGPROGRAM
         	#pragma vertex vert
         	#pragma fragment frag
         	#include "UnityCG.cginc"

	         struct appdata { 
	             float4 vertex : POSITION; 
	             float3 normal : NORMAL; 
	             float2 texcoord : TEXCOORD0; 
	         };
 
	         struct v2f { 
	             float4 pos : SV_POSITION;
	             float2 uv : TEXCOORD1;
	             float3 color : COLOR;
	         };      
 
	         uniform float4 _FallOffTex_ST;
	         uniform float4 _SecondColor;  
	         half _RimWidth;          

	         v2f vert (appdata_base v) {
	             v2f o;
	             o.pos = UnityObjectToClipPos (v.vertex);

	             float3 viewDir = normalize(ObjSpaceViewDir(v.vertex));
	             float dotProduct = 1 - dot(v.normal, viewDir);
	             float rimWidth = _RimWidth;
	             o.color = smoothstep(1 - rimWidth, 1.0, dotProduct);

	             o.color *= _SecondColor;

	             o.uv = TRANSFORM_TEX(v.texcoord, _FallOffTex);

	             return o;
	         }

	         uniform sampler2D _FallOffTex;

	         float4 frag(v2f i) : COLOR {
	             float4 falloffcol = tex2D(_FallOffTex, i.uv);

				 clip(falloffcol.a - 0.01f);

	             falloffcol.rgb = i.color;

	             falloffcol.a = ((i.color.r + i.color.g + i.color.b) * falloffcol.a) * _SecondColor.a;

	             return falloffcol;
	         }
             ENDCG
        }

		Pass {   
			ZWrite On

     		CGPROGRAM
         	#pragma vertex vert
         	#pragma fragment frag
         	#include "UnityCG.cginc"

	         struct appdata { 
	             float4 vertex : POSITION; 
	             float3 normal : NORMAL; 
	             float2 texcoord : TEXCOORD0; 
	         };
 
	         struct v2f { 
	             float4 pos : SV_POSITION;
	             float2 uv : TEXCOORD1;
	             float3 color : COLOR;
	         };      
 
	         uniform float4 _FallOffTex_ST;
			 uniform float4 _ThirdColor;
	         half _RimWidth;          

	         v2f vert (appdata_base v) {
	             v2f o;
	             o.pos = UnityObjectToClipPos (v.vertex);

	             float3 viewDir = normalize(ObjSpaceViewDir(v.vertex));
	             float dotProduct = 1 - dot(v.normal, viewDir);
	             float rimWidth = _RimWidth * 0.5f;
	             o.color = smoothstep(1 - rimWidth, 1.0, dotProduct);

	             o.color *= _ThirdColor;

	             o.uv = TRANSFORM_TEX(v.texcoord, _FallOffTex);

	             return o;
	         }

	         uniform sampler2D _FallOffTex;

	         float4 frag(v2f i) : COLOR {
	             float4 falloffcol = tex2D(_FallOffTex, i.uv);

				 clip(falloffcol.a - 0.01f);

	             falloffcol.rgb = i.color;

	             falloffcol.a = ((i.color.r + i.color.g + i.color.b) * falloffcol.a) * _ThirdColor.a;

	             return falloffcol;
	         }
             ENDCG
        }


     }
	 Fallback "Legacy Shaders/Transparent/Cutout/Diffuse"
 }