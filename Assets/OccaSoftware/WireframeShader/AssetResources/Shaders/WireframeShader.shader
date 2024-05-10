Shader "OccaSoftware/WireframeShader"
{
    Properties
    {
        // Basic
        [Enum(OccaSoftware.Wireframe.Runtime.LightingMode)] _LightingMode("Lighting Mode", Float) = 0
        [HDR] _Color("Diffuse Color", Color) = (1,1,1,1)
        [HDR] _Emission("Emission", Color) = (0,0,0,1)
        _Opacity("Opacity", Range(0.0, 1.0)) = 1
        
        // Fade out
        [Toggle(_FadingEnabled)] _FadingEnabled ("Fade by Distance", Float) = 1
        _FadeMinMaxDistance("Fade Start and End Distance", Vector) = (10, 20, 0, 0)
        
        //Wireframe
        [Toggle(_PreferQuadsEnabled)] _PreferQuadsEnabled("Attempt to Render Quads when Possible", Float) = 1
        _WireframeSize("Line Width", Float) = 1
        
        //Surface
        [Toggle(_CastShadowsEnabled)] _CastShadowsEnabled("Cast Shadows", Float) = 1
        [Toggle(_ReceiveShadowsEnabled)] _ReceiveShadowsEnabled("Receive Shadows", Float) = 1
        [Toggle(_ReceiveDirectLightingEnabled)] _ReceiveDirectLightingEnabled("Receive Direct Lighting", Float) = 1
        [Toggle(_ReceiveAmbientLightingEnabled)] _ReceiveAmbientLightingEnabled("Receive Ambient Lighting", Float) = 1
        [Toggle(_ReceiveFogEnabled)] _ReceiveFogEnabled("Receive Fog", Float) = 1
        
        
        //Advanced
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendSrc ("Blend Mode (Source)", Float) = 5 // Default to "SrcAlpha"
        [Enum(UnityEngine.Rendering.BlendMode)] _BlendDst ("Blend Mode (Dest)", Float) = 10 // Default to "OneMinusSrcAlpha"
        [Enum(OccaSoftware.Wireframe.Runtime.State]_ZWrite ("ZWrite", Float) = 1.0 // Default to "ZWrite On"
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("Depth Test", Float) = 4 // Default to "LEqual"
        [Enum(UnityEngine.Rendering.CullMode)] _Culling ("Culling", Float) = 2 // Default to "Cull Back"
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalRenderPipeline" }
        
        Blend [_BlendSrc] [_BlendDst]
        Cull [_Culling]
        ZWrite [_ZWrite]
        ZTest [_ZTest]
        ZClip Off
        
        Pass
        {
            Name "ForwardLit"
            Tags {"LightMode" = "UniversalForwardOnly"}
            
            
            HLSLPROGRAM
            #pragma target 4.0
            #pragma vertex Vertex
            #pragma geometry Geometry
            #pragma fragment Fragment
            
            
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_instancing
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            
            #include "wireframe.hlsl"  

            ENDHLSL
        }
        
        
        Pass
        {
            Name "ShadowCaster"
            
            Tags {"LightMode" = "ShadowCaster"}
            
            HLSLPROGRAM
            #pragma target 4.0
            #pragma multi_compile_instancing
            
            #pragma vertex Vertex
            #pragma geometry Geometry
            #pragma fragment FragmentDepthOnly
            
            
            #include "wireframe.hlsl"
            
            ENDHLSL
        }
        
        Pass
        {
            Name "DepthOnly"
            
            Tags {"LightMode" = "DepthOnly"}
            
            
            HLSLPROGRAM
            #pragma target 4.0
            #pragma multi_compile_instancing
            
            #pragma vertex Vertex
            #pragma geometry Geometry
            #pragma fragment FragmentDepthOnly
            
            #include "wireframe.hlsl"
            
            ENDHLSL
        }
        
        Pass
        {
            Name "DepthNormalsOnly"
            
            Tags {"LightMode" = "DepthNormalsOnly"}
            
            
            HLSLPROGRAM
            #pragma target 4.0
            #pragma multi_compile_instancing
            
            #pragma vertex Vertex
            #pragma geometry Geometry
            #pragma fragment FragmentDepthNormalsOnly
            
            #include "wireframe.hlsl"
            
            ENDHLSL
        }
    }
    
    CustomEditor "OccaSoftware.Wireframe.Editor.WireframeMaterialEditorGUI"
}