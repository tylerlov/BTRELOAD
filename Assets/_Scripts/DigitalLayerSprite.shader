Shader "Custom/DigitalLayerSprite"
{
  Properties
  {
    _MainTex ("Sprite Texture", 2D) = "white" {}
  }
  
  SubShader
  {
    Tags { "RenderType"="Transparent" "Queue"="Transparent" }
    Blend SrcAlpha OneMinusSrcAlpha
    ZWrite Off
    
    Pass
    {
      HLSLPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma target 5.0
      
      #include "UnityCG.cginc"
      
      struct SpriteData
      {
        float4 position;
        float4 color;
      };
      
      StructuredBuffer<SpriteData> _SpriteBuffer;
      sampler2D _MainTex;
      
      struct v2f
      {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
        float4 color : COLOR;
      };
      
      v2f vert(uint vid : SV_VertexID, uint instanceID : SV_InstanceID)
      {
        v2f o;
        
        SpriteData sprite = _SpriteBuffer[instanceID];
        float rotation = sprite.position.w;
        float scale = sprite.color.w;
        
        // Generate quad vertices
        float2 quadPos = float2(
          (vid == 1 || vid == 2) ? 1 : -1,
          (vid == 2 || vid == 3) ? 1 : -1
        );
        
        // Apply rotation
        float2 rotatedPos;
        float s = sin(rotation);
        float c = cos(rotation);
        rotatedPos.x = quadPos.x * c - quadPos.y * s;
        rotatedPos.y = quadPos.x * s + quadPos.y * c;
        
        float3 worldPos = sprite.position.xyz + float3(rotatedPos * scale, 0);
        o.pos = UnityWorldToClipPos(float4(worldPos, 1));
        o.uv = (quadPos + 1) * 0.5;
        o.color = sprite.color;
        
        return o;
      }
      
      float4 frag(v2f i) : SV_Target
      {
        float4 texColor = tex2D(_MainTex, i.uv);
        return texColor * i.color;
      }
      ENDHLSL
    }
  }
} 