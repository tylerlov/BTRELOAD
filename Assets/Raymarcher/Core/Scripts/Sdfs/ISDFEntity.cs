using UnityEngine;

namespace Raymarcher.Objects
{
    /// <summary>
    /// Identifier for the base SDF entity, used in regular Sdf objects, Sdf fractals, and Sdf modifiers.
    /// Implement this interface to create a new SDF entity with custom behavior and data retrieval.
    /// </summary>
    public interface ISDFEntity
    {
        public enum SDFUniformType { Float, Float2, Float3, Float4, Sampler2D, DefineByRenderType, Sampler3D, Float4x4 };

        public struct SDFUniformField
        {
            public string fieldName;
            public string methodParameterName;
            public SDFUniformType uniformType;

            public bool dontCreateUniformVariable;
            public bool uniformFieldHoldsOtherSdfObjectData;
            public bool lowPrecision;

            /// <summary>
            /// Define a specific uniform field
            /// </summary>
            /// <param name="fieldName">Exact field name</param>
            /// <param name="fieldType">Exact field type</param>
            /// <param name="dontCreateUniformVariable">If enabled, the uniform variable won't be generated in the SdfObjectBuffer (will be used only in the method parameters)</param>
            /// <param name="methodParameterName">In case you would like to differ the uniform field name & method parameter name, you can define the original parameter name here</param>
            /// <param name="uniformFieldHoldsOtherSdfObjectData">Is this field completely defined for specific sdf object? This should be true if this field will hold other's sdf-object data</param>
            /// <param name="lowPrecision">Use low precision for floating fields</param>
            public SDFUniformField(
                string fieldName,
                SDFUniformType fieldType,
                bool dontCreateUniformVariable = false,
                string methodParameterName = "",
                bool uniformFieldHoldsOtherSdfObjectData = false,
                bool lowPrecision = true)
            {
                this.fieldName = fieldName;
                uniformType = fieldType;
                this.dontCreateUniformVariable = dontCreateUniformVariable;
                this.methodParameterName = methodParameterName;
                this.uniformFieldHoldsOtherSdfObjectData = uniformFieldHoldsOtherSdfObjectData;
                this.lowPrecision = lowPrecision;
            }
        }

        /// <summary>
        /// Specifies a method name for this entity. This method will be called and represent this entity formula
        /// </summary>
        public abstract string SdfMethodName { get; }

        /// <summary>
        /// Specifies an array of uniform fields for this entity. These variables will be used and modified at runtime in the formula
        /// </summary>
        public abstract SDFUniformField[] SdfUniformFields { get; }

        /// <summary>
        /// Specifies a method body for this entity. This defines the actual behavior of the formula
        /// </summary>
        public abstract string SdfMethodBody { get; }

        /// <summary>
        /// Specifies an optional method extension for this entity. Use this to inline all helper and utility methods at the top of the declaration list (optional)
        /// </summary>
        public string SdfMethodExtension { get; }

        /// <summary>
        /// Invoked when the SdfObjectBuffer is recompiled in the Unity Editor
        /// </summary>
        public abstract void SdfBufferRecompiled();

        /// <summary>
        /// Sets Float/Vector/Color/Texture values in the raymarcher session material with the given iterationIndex
        /// </summary>
        /// <param name="raymarcherSessionMaterial">The current raymarcher scene session material</param>
        /// <param name="iterationIndex">The current iteration index (combine this with your property name)</param>
        public abstract void PushSdfEntityToShader(in Material raymarcherSessionMaterial, in string iterationIndex);
    }
}