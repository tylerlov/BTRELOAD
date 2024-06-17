namespace ProjectDawn.Impostor
{
    //
    // Summary:
    //     Modes available for submitting when making a render request.
    public enum RenderMode
    {
        //
        // Summary:
        //     Default value for a request.
        None,
        //
        // Summary:
        //     The render request outputs a depth value.
        Depth,
        //
        // Summary:
        //     The render request outputs the materials albedo / base color.
        BaseColor,
        //
        // Summary:
        //     The render request outputs the materials alpha color.
        Alpha,
        //
        // Summary:
        //     The render outputs the materials metal value.
        Metallic,
        //
        // Summary:
        //     The render request outputs the per pixel normal.
        Normal,
        //
        // Summary:
        //     The render request returns the materials smoothness buffer.
        Smoothness,
        //
        // Summary:
        //     The render request returns the material ambient occlusion buffer.
        Occlusion,
        //
        // Summary:
        //     The render request returns the material emission buffer.
        Emission,
        //
        // Summary:
        //     The render request outputs the materials diffuse color.
        DiffuseColor,
    }
}