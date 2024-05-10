Thank you for downloading !
This package turn your gameobject to wireframe when you touch it.

Vertex and fragment shader wireframe, include 5 shaders for low-end device that not support geometry shader:
=> "Selected Effect --- Wireframe/Cull Backface" display wireframe of gameobject with backface cull.
=> "Selected Effect --- Wireframe/Cull Off" display wireframe of gameobject without cull, so you can see the backside of gameobject.
=> "Selected Effect --- Wireframe/Double Side Color" display wireframe of gameobject without cull, plus support different color between inside and outside of gameobject.
=> "Selected Effect --- Wireframe/Overlay" display wireframe of gameobject on top of mesh's original material.
=> "Selected Effect --- Wireframe/Stylized" display stylized wireframe of mesh.

Geometry shader based wireframe, should work with average mobile device nowadays:
=> "Selected Effect --- Wireframe/GeometryWireframe", use geometry shader to produce barycentric distances info.
Note it only works with platform that support geometry shader.

"Wireframe.cs" and "WireframeSkinnedMesh.cs" are helper components used to change materials and manage parameters.
Open demo scene and play, hold left mouse button and move control rotation of camera. Hold W,S,A,D,Q,E to move camera.
Hold right mouse button could select the target object and turn it into wireframe rendering mode.

We also open for customer's requests. If you need new features, please let us know.

If you like it, please give it a good review on asset store. Thanks so much !
Any suggestion or improvement you want, please contact qq_d_y@163.com.
Hope we can help more and more game developers.