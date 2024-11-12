/*
 * Copyright (c) Léo CHAUMARTIN 2024
 * All Rights Reserved
 * 
 * File: ImpostorPreset.cs
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirage.Impostors.Core
{
    public enum SphereType
    {
        UV = 0,
        PseudoFibonacci
    }
    public enum LightingMethod
    {
        SurfaceEstimation = 0,
        UseSunSource
    }

    /// <summary>
    /// The ImpostorPreset class keeps baking parameters in a scriptable object 
    /// </summary>
    public class ImpostorPreset : ScriptableObject
    {
        public int resIndex = 3;
        public int latitudeSamples = 4;
        public int latitudeOffset = 3;
        public float latitudeAngularStep = 5f;
        public int longitudeSamples = 36;
        public SphereType type = SphereType.UV;
        public LightingMethod lightingMethod = LightingMethod.SurfaceEstimation;

        public bool FibonacciSphere { get { return type == SphereType.PseudoFibonacci; } }

        public static ImpostorPreset Clone(ImpostorPreset source)
        {
            var clone = CreateInstance<ImpostorPreset>();
            clone.resIndex = source.resIndex;
            clone.latitudeSamples = source.latitudeSamples;
            clone.latitudeOffset = source.latitudeOffset;
            clone.latitudeAngularStep = source.latitudeAngularStep;
            clone.longitudeSamples = source.longitudeSamples;
            clone.type = source.type;
            clone.lightingMethod = source.lightingMethod;
            return clone;
        }

        // Equality operator
        public static bool operator ==(ImpostorPreset lhs, ImpostorPreset rhs)
        {
            if (ReferenceEquals(lhs, null))
            {
                return ReferenceEquals(rhs, null);
            }

            if (ReferenceEquals(rhs, null))
            {
                return false;
            }

            return lhs.resIndex == rhs.resIndex &&
                   lhs.latitudeSamples == rhs.latitudeSamples &&
                   lhs.latitudeOffset == rhs.latitudeOffset &&
                   lhs.latitudeAngularStep == rhs.latitudeAngularStep &&
                   lhs.longitudeSamples == rhs.longitudeSamples &&
                   lhs.type == rhs.type &&
                   lhs.lightingMethod == rhs.lightingMethod;
        }

        public static bool operator !=(ImpostorPreset lhs, ImpostorPreset rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return this == (ImpostorPreset)obj;
        }

        public override int GetHashCode()
        {
            return resIndex.GetHashCode() ^
                   latitudeSamples.GetHashCode() ^
                   latitudeOffset.GetHashCode() ^
                   latitudeAngularStep.GetHashCode() ^
                   longitudeSamples.GetHashCode() ^
                   type.GetHashCode() ^
                   lightingMethod.GetHashCode();
        }
    }
}