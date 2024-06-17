using System;

using UnityEngine;

namespace Raymarcher.Materials
{
    /// <summary>
    /// Base class for a specific material identifier.
    /// Inherit from this class to define all the required properties for the compiler to create a custom material in Raymarcher.
    /// Written by Matej Vanco, November 2023.
    /// </summary>
    [Serializable]
    public abstract class RMMaterialIdentifier
    {
        public enum MaterialUniformType { Float, Float2, Float3, Float4, NestedContainer };

        [Serializable]
        public struct MaterialGlobalKeywords
        {
            public bool enabled;
            public string keyword;
            public string displayName;

            public int performanceCost;

            public MaterialGlobalKeywords(string keyword, bool enabledByDefault, string displayName, int performanceCost = 0)
            {
                this.keyword = keyword;
                enabled = enabledByDefault;
                this.displayName = displayName;
                this.performanceCost = performanceCost;
            }
        }

        [Serializable]
        public struct MaterialUniformField
        {
            public string uniformName;
            public MaterialUniformType uniformType;
            public bool lowPrecision;
            public RMMaterialIdentifier nestedContainerParent;

            public MaterialUniformField(string uniformName, MaterialUniformType uniformType, bool lowPrecision = true, RMMaterialIdentifier nestedContainerParent = null)
            {
                this.uniformName = uniformName;
                this.uniformType = uniformType;
                this.lowPrecision = lowPrecision;
                this.nestedContainerParent = nestedContainerParent;
            }
            public MaterialUniformField(string uniformName, RMMaterialIdentifier nestedContainerParent)
            {
                this.uniformName = uniformName;
                uniformType = MaterialUniformType.NestedContainer;
                lowPrecision = true;
                this.nestedContainerParent = nestedContainerParent;
            }
        }

        [Serializable]
        public struct MaterialMethodContainer
        {
            public string methodName;
            public string methodContent;

            public MaterialMethodContainer(string methodName, string methodContent)
            {
                this.methodName = methodName;
                this.methodContent = methodContent;
            }
        }

        /// <summary>
        /// Represents the name of this material type
        /// </summary>
        public abstract string MaterialTypeName { get; }

        /// <summary>
        /// If this material type will use abstraction, this property will represent the common name of this material type family (especially while filtering out the common material types).
        /// If this is empty/null, there is no inheritation involved and the material type will be ignored during the filtering process.
        /// </summary>
        public virtual string MaterialFamilyName { get; }

        /// <summary>
        /// (Optional) Representative icon for this material type - just for editor purposes
        /// </summary>
        public virtual Texture2D MaterialEditorIcon { get; } = null;

        /// <summary>
        /// (Optional) Specify global keywords that will control the material properties through the render master
        /// </summary>
        public virtual MaterialGlobalKeywords[] ShaderKeywordFeaturesGlobal { get; } = null;

        /// <summary>
        /// (Optional) Specify a total performance cost of the global shader keyword features - just for editor purposes
        /// </summary>
        public virtual int ShaderKeywordFeaturesGlobalPerformanceCost { get; } = 0;

        /// <summary>
        /// (Optional) Specify global uniforms that will pass values globally through the render master
        /// </summary>
        public virtual MaterialUniformField[] MaterialUniformFieldsGlobal { get; } = null;

        /// <summary>
        /// Specify uniforms that will pass values per unique material instance
        /// </summary>
        public abstract MaterialUniformField[] MaterialUniformFieldsPerInstance { get; }

        /// <summary>
        /// (Optional) Specify other methods that will contain calculations for this material type
        /// </summary>
        public virtual MaterialMethodContainer[] MaterialOtherMethods { get; } = null;

        /// <summary>
        /// Specify the main core method that will contain the base calculation for this material type
        /// </summary>
        public abstract MaterialMethodContainer MaterialMainMethod { get; }

        /// <summary>
        /// (Optional) Specify a post-material method with its content that will contain post-material-renderer calculations. Use this to access the RM_RenderMaterials method
        /// </summary>
        public virtual MaterialMethodContainer? PostMaterialMainMethod { get; } = null;

        /// <summary>
        /// Define your custom global data container that will be used in the scene scope through the render master
        /// </summary>
        public virtual (string containerType, string displayName) MaterialDataContainerTypeGlobal { get; }

        /// <summary>
        /// Define your custom per-instance data container type that will be used in the iteration array for each material instance
        /// </summary>
        public abstract string MaterialDataContainerTypePerInstance { get; }

        /// <summary>
        /// (Optional) Once the PostMaterialMainMethod is defined, you can allow this method to support global material renderer. Do not forget to use #RAYMARCHER-METHOD-MODIFIABLE# for your main method name
        /// </summary>
        public virtual bool PostMaterialMainMethodGlobalSupported { get; } = false;

        /// <summary>
        /// Does this material type use any textures?
        /// </summary>
        public abstract bool MaterialIsUsingTexturesPerInstance { get; }
    }
}