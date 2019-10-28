// This shader shows collection icons assembled from three layers

Shader "CollectionIcon"
{
	Properties
	{
		// _MainTex is not used by the shader but it is required by the UI system:
		_MainTex ("Main Texture", 2D) = "White" {}

		_Texture0 ("Base Texture", 2D) = "White" {}
		[NoScaleOffset] _Texture1 ("Texture 1", 2D) = "black" {}
		[NoScaleOffset] _Texture2 ("Texture 2", 2D) = "black" {}
		_Color0 ("Base Tint", Color) = (1,1,1,1)
		_Color1 ("Tint 1", Color) = (1,0,0,1)
		_Color1Pattern0 ("Tint 1 Pattern 0", Color) = (1,1,1,1)
		_Color1Pattern1 ("Tint 1 Pattern 1", Color) = (1,1,1,1)
		_Color2 ("Tint 2", Color) = (0,1,0,1)
		_Color2Pattern0 ("Tint 2 Pattern 0", Color) = (1,1,1,1)
		_Color2Pattern1 ("Tint 2 Pattern 1", Color) = (1,1,1,1)
		_OutlineColor ("Outline Color", Color) = (1,1,1,1)

		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255
		_ColorMask ("Color Mask", Float) = 15
	}

	SubShader
	{
		Tags
		{
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="False"
		}

		Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest Off
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

		Pass
		{
			Name "Default"

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			#pragma multi_compile __ UNITY_UI_CLIP_RECT
			#pragma multi_compile __ EDITOR_CLIP

			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				float2 texcoord  : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
#ifdef EDITOR_CLIP
				float2 clipUV : TEXCOORD2;
#endif
				UNITY_VERTEX_OUTPUT_STEREO
			};

			fixed4 _Color0;
			fixed4 _Color1;
			fixed4 _Color1Pattern0;
			fixed4 _Color1Pattern1;
			fixed4 _Color2;
			fixed4 _Color2Pattern0;
			fixed4 _Color2Pattern1;
			fixed4 _OutlineColor;
			float4 _ClipRect;

			sampler2D _Texture0;
			float4 _Texture0_ST;

			sampler2D _Texture1;
			sampler2D _Texture2;

			sampler2D _GUIClipTexture;
			uniform float4x4 unity_GUIClipTextureMatrix;

			v2f vert(appdata_t v)
			{
				v2f OUT;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				OUT.worldPosition = v.vertex;
				OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
				OUT.texcoord = TRANSFORM_TEX (v.texcoord, _Texture0);
				OUT.color = v.color;

#ifdef EDITOR_CLIP
				float3 eyePos = UnityObjectToViewPos(v.vertex);
				OUT.clipUV = mul(unity_GUIClipTextureMatrix, float4(eyePos.xy, 0, 1.0));
#endif

				return OUT;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				half4 texture0 = tex2D(_Texture0, IN.texcoord);
				half4 texture1 = tex2D(_Texture1, IN.texcoord);
				half4 texture2 = tex2D(_Texture2, IN.texcoord);

				// Modulate each alpha channel so layers can be turned off by
				// setting the alpha value of a color to 0:
				texture1.a *= _Color1.a;
				texture2.a *= _Color2.a;

				// Tint
				half4 tint;
				texture0.rgb = texture0 * _Color0;

				tint = lerp(_Color1, _Color1Pattern0, texture1.g * _Color1Pattern0.a);
				tint = lerp(tint, _Color1Pattern1, texture1.b * _Color1Pattern1.a);
				texture1.rgb = texture1.r * tint;

				tint = lerp(_Color2, _Color2Pattern0, texture2.g * _Color2Pattern0.a);
				tint = lerp(tint, _Color2Pattern1, texture2.b * _Color2Pattern1.a);
				texture2.rgb = texture2.r * tint;

				half3 masks = half3(texture0.a, texture1.a, texture2.a);
				// the alpha channels contain a combined mask for icon and outline. Modify the masks so we get the icon mask only:
				half3 iconMasks = smoothstep(0.7, 1, masks);

				half4 color = lerp(texture0, texture1, iconMasks.g);
				color = lerp(color, texture2, iconMasks.b);

				// create combined masks:
				float mask = max(max(masks.r, masks.g), masks.b);
				float iconMask = smoothstep(0.7, 1, mask);
				float outlineMask = smoothstep(0.1, 0.2, mask);

				half4 outline = _OutlineColor;
				outline.a = outlineMask;
				color = lerp(outline, color, iconMask);
				color.a = outlineMask;

#ifdef EDITOR_CLIP
				color.a *= tex2D(_GUIClipTexture, IN.clipUV).a;
#endif

#ifdef UNITY_UI_CLIP_RECT
				color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
#endif

				color *= IN.color;

				return color;
			}
			ENDCG
		}
	}
}
