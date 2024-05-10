//----------------------------------------------
//            	   Koreographer                 
//    Copyright © 2014-2021 Sonic Bloom, LLC    
//----------------------------------------------

using System.Collections.Generic;
using UnityEngine;


namespace SonicBloom.Koreo.Players.FMODStudio
{
    /// <summary>
    /// A collection of Koreography assets. This object provides the Koreography with the ability
    /// to handle name comparisons using the FMOD-favored UTF-8 encoding.
    /// </summary>
    public class FMODKoreographySet : ScriptableObject
    {
        /// <summary>
        /// The Koreography contained in this set.
        /// </summary>
        public List<FMODKoreoEntry> koreographies = new List<FMODKoreoEntry>();


        /// <summary>
        /// Find a Kore the name of the provided Sound with that used by the Koreography in the set.
        /// </summary>
        /// <param name="sound">The sound to compare</param>
        /// <returns><c>true</c> if the set contains a Koreography with a matching name, <c>false</c>
        /// otherwise.</returns>
        public Koreography GetKoreoEntryForSound(FMOD.Sound sound)
        {
            FMODUTF8NameRetriever NameUtil = FMODUTF8NameRetriever.Instance;

            // Get the name info for the sound in question.
            NameUtil.GetNameForSound(sound);
            byte[] nameBytes = NameUtil.Data;
            int strLen = NameUtil.StringByteLength;

            int numKoreo = koreographies.Count;
            for (int i = 0; i < numKoreo; ++i)
            {
                FMODKoreoEntry entry = koreographies[i];
                if (entry.UTF8StringEquals(nameBytes, strLen))
                {
                    return entry.koreo;
                }
            }
            return null;
        }

#if UNITY_EDITOR
        // Ensure that data is correctly configured when a modification occurs.
        void OnValidate()
        {
            InitializeEntries();
        }
#endif

        /// <summary>
        /// Initializes all FMODKoreoEntry instances in the set.
        /// </summary>
        public void InitializeEntries()
        {
            int numKoreo = koreographies.Count;
            for (int i = 0; i < numKoreo; ++i)
            {
                koreographies[i].Initialize();
            }
        }
    }

    /// <summary>
    /// A Koreography wrapper that provides access to the Koreographed audio file's name as an
    /// array of UTF-8 bytes.
    /// </summary>
    [System.Serializable]
    public class FMODKoreoEntry
    {
        /// <summary>
        /// The Koreography asset.
        /// </summary>
        public Koreography koreo = null;

        /// <summary>
        /// The name of the audio file associated with this entry's Koreography, represented as
        /// UTF8 bytes to match the format used by FMOD internally.
        /// </summary>
        [SerializeField]
        byte[] utf8Name = null;


        /// <summary>
        /// Initializes the audio name byte array for runtime use.
        /// </summary>
        public void Initialize()
        {
            if (koreo != null)
            {
                utf8Name = System.Text.Encoding.UTF8.GetBytes(koreo.SourceClipName);
            }
        }

        /// <summary>
        /// Compares the instance's <c>utf8Name</c> to the provided array of bytes with the given
        /// length for equality.
        /// </summary>
        /// <param name="str">The "string" of bytes to compare.</param>
        /// <param name="strLen">The length of the "array" to consider for equality.</param>
        /// <returns><c>true</c> if the "strings" are equal, <c>false</c> otherwise.</returns>
        public bool UTF8StringEquals(byte[] str, int strLen)
        {
            if (strLen != utf8Name.Length) return false;

            for (int idx = strLen - 1; idx >= 0; --idx)
            {
                if (utf8Name[idx] != str[idx])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
