Shader "Moho/TransparentShader"
{
    Properties
    {
		[Toggle(USE_MASK)]_MASK("UseMask",float) = 0
		_Stencil ("Stencil ID", Int) = 0 
		_StencilMask ("StencilMask ID", Int) = 0 
		[KeywordEnum(Disabled, Never, Less, Equal, LessEqual, Greater, NotEqual, GreaterEqual, Always)] _StencilComp("Stencil Comparison", Int) = 8
		[KeywordEnum(Keep, Zero, Replace, IncrSat, DecrSat, Invert, IncrWrap, DecrWrap)] _StencilOp("Stencil Operation", Int) = 0


		[Enum(UnityEngine.Rendering.CompareFunction)]_ZTest("ZTest", Float) = 4              // LEqual
		[Enum(UnityEngine.Rendering.CullMode)]_CullMode("Culling",Float)= 2
		[Enum(Off,0,On,1)]_ZWrite("ZWrite",float)=1

		_OffsetFactor("OffsetFactor",float) = 0
		_OffsetUnits("OffsetUnits",float) = 0

		_Color("Color",color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
		_Z("add Dist",float) = 0
    }
    SubShader
    {
        /*Tags {
			"Queue"="AlphaTest"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
		}*/

		Tags {
			"Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
		}

		Offset [_OffsetFactor],[_OffsetUnits]
		Cull [_CullMode]
        Lighting Off
        ZWrite [_ZWrite]
		ZTest [_ZTest]
		AlphaToMask On

		Blend SrcAlpha OneMinusSrcAlpha

        LOD 100

        Pass
        {
			Stencil
			{
				Ref [_Stencil]
				ReadMask [_StencilMask]
				WriteMask [_StencilMask]
				Comp [_StencilComp]
				Pass [_StencilOp]
			}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
			#pragma target 2.0
            #pragma multi_compile_fog
			#pragma shader_feature USE_MASK

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
				float4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
				float4 color :TEXCOORD2;
				UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			float4 _Color;
			float _Z;

            v2f vert (appdata v)
            {
                v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
				float3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos)) * _Z;
				worldPos += float4(worldViewDir,0);

                //o.vertex = UnityObjectToClipPos(v.vertex);

				o.vertex = UnityObjectToClipPos(mul(unity_WorldToObject, worldPos));

				//float3 forward = normalize(ObjSpaceViewDir(v.vertex)) * _Z;
				//float4 vec = mul(unity_ObjectToWorld,v.vertex) + float4(forward,0);

				//o.vertex = UnityObjectToClipPos(vec);

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv) * _Color * i.color;

				#ifdef USE_MASK
					if(col.a < 0.001)
						discard;
				#endif

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;// * col.a;
            }
            ENDCG
        }
    }
}
