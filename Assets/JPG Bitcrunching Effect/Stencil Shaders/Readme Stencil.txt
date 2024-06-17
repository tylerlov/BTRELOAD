If you're not using Only Stenciled mode you can ignore all these shaders.

>>Standard + JPG.shader - A copy of Built-In Standard shader that masks out (puts the effect on) the rendered object for stenciled mode.

>>URP Lit + JPG.shader - A copy of URP Lit shader that masks out the rendered object for stenciled mode.
    
>>UI + JPG.shader - You can use this shader on your Camera space or World space Canvas to mask out a square from the screen or a sprite shape (alpha culling) for stenciled mode. Put it as a material on an Image. This shader is unfortunately unmaskable by UI Masks.


Modifying a custom shader to stencil is very easy:
1) Add this into SubShader { .. }
    Stencil
    {
        Comp Always
        Pass Replace
        Ref 32
    }
2) Add "Queue" = "Geometry+100" into root SubShader Tags
For example:
    Tags { "RenderType"="Opaque" }
Becomes:
    Tags { "RenderType"="Opaque" "Queue" = "Geometry+100" }
3) In URP, add this into shader Properties { .. }
    _QueueOffset("Queue offset", Float) = 10.0