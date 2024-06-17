using Raymarcher.Materials;

namespace Raymarcher.Objects
{
    /// <summary>
    /// Implement this interface to a sdf/modifier that will manually add/remove materials in the Raymarcher.
    /// </summary>
    public interface ISDFModifierMaterialHandler
    {
        public RMMaterialBase Material1 { get; }
        public RMMaterialBase Material2 { get; }
        public RMMaterialBase Material3 { get; }
        public RMMaterialBase Material4 { get; }
    }

    public static class ISDFModifierMaterialHandlerExtensions
    {
        public const int AVAILABLE_MATERIAL_CACHES = 4;

        public static RMMaterialBase HandleMatIndex(this ISDFModifierMaterialHandler mhandler, int index)
        {
            switch(index)
            {
                case 0: return mhandler.Material1;
                case 1: return mhandler.Material2;
                case 2: return mhandler.Material3;
                case 3: return mhandler.Material4;
            }
            return mhandler.Material1;
        }
    }
}