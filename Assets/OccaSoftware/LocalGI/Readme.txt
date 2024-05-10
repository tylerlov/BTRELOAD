README
================================

Contents
--------------------------------
About
Installation Instructions
FAQs
Support


Aboutis
--------------------------------
Thank you for purchasing our Local Global Illuination asset! With this asset, you can bring your game to life with stunning dynamic global illumination.
This powerful asset creates a dynamic global illumination effect that automatically applies global illumination within a certain range around the GI Handler.
The GI Handler which can be positioned on your camera or set to follow a character or other target in your scene. 
This creates a stunning and immersive gaming experience for your players.

Please leave a review on the Unity Asset Store :)


Installation Instructions (Deferred)
--------------------------------
1. Import the asset into your project.
2. (If Deferred: Set your Rendering Path to Deferred.)
3. Add the Local Global Illumination Render Feature to your Universal Renderer Data.
4. (If Forward: Change the Render Path setting on your Renderer Feature to Forward.)
5. Add the Local GI Handler to any game object in your scene. The Local GI will be sampled from the transform position of this game object.
6. Configure the Local GI Handler.


Usage with Forward Render Pass
--------------------------------
For best results, we recommend using this asset with the deferred render path. 
The deferred render path renders an albedo texture. We use the albedo texture to apply the lighting in the most accurate way possible.

However, you can also use it with the forward render path.
The forward render path is slightly less accurate, since it uses the screen texture as a substitute for the albedo. But, it still works great.

You have two options for usage with the Forward Render Path
Option 1 (Recommended):
Set Renderer mode to Forward
In your Renderer Feature, open the Settings menu, then switch the Render Path from Deferred to Forward.

Option 2 (Advanced):
Extend Shader
We have included an example of extending Unity URP's default lit shader to incorporate the Global Illumination data in DemoResources/ForwardCompatibilityExample/~.
You can also use the .subgraph to sample the results in Shader Graph.
Note that you should NOT enable the Renderer Feature when using this method.

In short, you need to make a copy of the relevant shader and any key include files. For the Lit shader, this is the LitForwardPass.hlsl file.
Then, you need to rename the Lit shader to something identifiable. For example, "My Custom Lit Shader", by changing the Shader "..." statement at the top of the .shader file.
Then, you need to update the Lit shader. The Lit shader includes references to the original include files as #include "...." statements. You need to replace the target of the include declaration with the path of the new copy you created.
Finally, you need to update the include file's fragment pass to access the Local GI data. This consists of two steps:

Include the Local GI file by adding an #include declaration. Mine looks like this: 
//... I include this declaration after the Lighting.hlsl include...
#include "Assets/OccaSoftware/LocalGI/AssetResources/Resources/LocalGI.hlsl"
//...And before the #define statements...

Update the fragment pass by calling the appropriate method. Mine looks like this:

//...I compute the color after the call to UniversalFragmentPBR(...);
float3 irradiance;
GetLocalGI(inputData.normalWS, inputData.positionWS, irradiance);
color.rgb += irradiance * surfaceData.albedo;
//...and before the MixFog call...


More details
--------------------------------
See our docs @ https://docs.occasoftware.com/local-global-illumination/


Support
--------------------------------
If you need any support, we are here to help.
Contact us at hello@occasoftware.com, or join our Discord at http://occasoftware.com/discord.