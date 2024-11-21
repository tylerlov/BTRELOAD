Shader "Custom/PixelateTransition"
{
  Properties
  {
    _MainTex ("Texture", 2D) = "white" {}
    _PixelSize ("Pixel Size", Float) = 1
    _Distortion ("Distortion", Range(0, 1)) = 0
    _Alpha ("Alpha", Range(0, 1)) = 1
  }
  
  SubShader
  {
    Tags { "Queue"="Transparent" "RenderType"="Transparent" }
    Blend SrcAlpha OneMinusSrcAlpha
    
    Pass
    {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #include "UnityCG.cginc"
      
      struct appdata
      {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
      };
      
      struct v2f
      {
        float2 uv : TEXCOORD0;
        float4 vertex : SV_POSITION;
      };
      
      sampler2D _MainTex;
      float4 _MainTex_TexelSize;
      float _PixelSize;
      float _Distortion;
      float _Alpha;
      
      v2f vert (appdata v)
      {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv;
        return o;
      }
      
      fixed4 frag (v2f i) : SV_Target
      {
        float2 screenSize = _MainTex_TexelSize.zw;
        float2 pixelatedUV = i.uv;
        
        float2 pixels = screenSize / _PixelSize;
        pixelatedUV = floor(pixelatedUV * pixels) / pixels;
        
        float2 center = i.uv - 0.5;
        float distortionAmount = length(center) * _Distortion;
        float2 distortion = float2(
          sin(pixelatedUV.y * 50.0 + _Time.y) * distortionAmount * 0.02,
          cos(pixelatedUV.x * 50.0 + _Time.y) * distortionAmount * 0.02
        );
        
        fixed4 col = tex2D(_MainTex, pixelatedUV + distortion);
        
        col.a = _Alpha * (1.0 - distortionAmount * 0.5);
        
        return col;
      }
      ENDCG
    }
  }
} 