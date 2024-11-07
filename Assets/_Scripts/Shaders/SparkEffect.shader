Shader "Custom/SparkEffect"
{
  Properties
  {
    _SparkColor ("Spark Color", Color) = (1,0.7,0,1)
    _SparkSize ("Spark Size", Range(0.01, 0.1)) = 0.05
  }
  
  SubShader
  {
    Tags { "RenderType"="Transparent" "Queue"="Transparent" }
    Blend One One
    ZWrite Off
    
    Pass
    {
      HLSLPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma target 5.0
      
      #include "UnityCG.cginc"
      
      StructuredBuffer<float4> _SparkPositions;
      int _ActiveCount;
      float4 _SparkColor;
      float _SparkSize;
      
      struct v2f
      {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
        uint instanceID : SV_InstanceID;
      };
      
      v2f vert(uint id : SV_VertexID, uint inst : SV_InstanceID)
      {
        v2f o;
        float4 sparkPos = _SparkPositions[inst];
        
        // Generate quad vertices
        float2 quadPos = float2(
          (id == 1 || id == 2) ? 1 : -1,
          (id == 2 || id == 3) ? 1 : -1
        );
        
        float3 worldPos = sparkPos.xyz + float3(quadPos * _SparkSize, 0);
        o.pos = UnityWorldToClipPos(worldPos);
        o.uv = (quadPos + 1) * 0.5;
        o.instanceID = inst;
        
        return o;
      }
      
      float4 frag(v2f i) : SV_Target
      {
        float2 uv = i.uv - 0.5;
        float dist = length(uv);
        float sparkAlpha = 1 - smoothstep(0.3, 0.5, dist);
        
        return _SparkColor * sparkAlpha;
      }
      ENDHLSL
    }
  }
} 