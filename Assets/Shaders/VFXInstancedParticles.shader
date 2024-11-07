Shader "Custom/VFXInstancedParticles"
{
  Properties
  {
    _MainTex ("Texture", 2D) = "white" {}
    _Color ("Color", Color) = (1,1,1,1)
  }
  SubShader
  {
    Tags { "RenderType"="Transparent" "Queue"="Transparent" }
    Blend SrcAlpha OneMinusSrcAlpha
    ZWrite Off

    Pass
    {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma multi_compile_instancing
      
      #include "UnityCG.cginc"

      struct appdata
      {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };

      struct v2f
      {
        float2 uv : TEXCOORD0;
        float4 vertex : SV_POSITION;
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };

      sampler2D _MainTex;
      float4 _MainTex_ST;
      float4 _Color;
      
      StructuredBuffer<float4x4> _Matrices;

      v2f vert (appdata v, uint instanceID : SV_InstanceID)
      {
        v2f o;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_TRANSFER_INSTANCE_ID(v, o);

        float4 worldPos = mul(_Matrices[instanceID], v.vertex);
        o.vertex = UnityObjectToClipPos(worldPos);
        o.uv = TRANSFORM_TEX(v.uv, _MainTex);
        return o;
      }

      fixed4 frag (v2f i) : SV_Target
      {
        UNITY_SETUP_INSTANCE_ID(i);
        fixed4 col = tex2D(_MainTex, i.uv) * _Color;
        return col;
      }
      ENDCG
    }
  }
} 