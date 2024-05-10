//----------------------------------------------
//            	   Koreographer                 
//    Copyright © 2014-2021 Sonic Bloom, LLC    
//----------------------------------------------

using System;
using System.Runtime.InteropServices;


namespace SonicBloom.Koreo.Players.FMODStudio
{
    /// <summary>
    /// Settings for Koreographer's FMOD Integration. Some of these must be manually adjusted to
    /// support special cases on a project-by-project basis.
    /// </summary>
    public static class FMODIntegrationSettings
    {
        /// <summary>
        /// The maximum length of string [in characters] used for a file name in the FMOD project.
        /// Default 255.
        /// </summary>
        const int FMOD_MAX_FILE_NAME_STRING_LEN = 255;

        // This represents a best-effort guess at picking a default size for memory.
        /// <summary>
        /// The maximum length of UTF8 string [in bytes] for a file name in the FMOD project.
        /// </summary>
        public readonly static int FMOD_MAX_FILE_NAME_BYTE_LEN = System.Text.Encoding.UTF8.GetMaxByteCount(FMOD_MAX_FILE_NAME_STRING_LEN) + 1;    // +1 for null terminator.
    }


    /// <summary>
    /// The FMODUTF8NameRetriever class is a utility class for getting the name of a Sound instance
    /// as a plain array of bytes from FMOD. The basic C# integration converts the string names to
    /// UTF-16 to create a standard C# managed string. As this process allocates memory, retrieving
    /// the bytes as-is allows us to do byte-wise comparisons without allocating memory to
    /// transform between string representations.
    /// </summary>
    public class FMODUTF8NameRetriever
    {
        #region Static Fields


        /// <summary>
        /// The singleton instance of the Name Retriever.
        /// </summary>
        public readonly static FMODUTF8NameRetriever Instance = new FMODUTF8NameRetriever(FMODIntegrationSettings.FMOD_MAX_FILE_NAME_BYTE_LEN);


        #endregion
        #region Fields


        /// <summary>
        /// The Garbage Collector handle pointing to the pinned resource.
        /// </summary>
        GCHandle handle;
        /// <summary>
        /// The byte array used to contain the retrieved data.
        /// </summary>
        byte[] data;
        /// <summary>
        /// A pointer to the beginning of the byte array.
        /// </summary>
        IntPtr dataPtr;
        /// <summary>
        /// The length of the UTF8 string [in bytes].
        /// </summary>
        int strLen;


        #endregion
        #region Properties


        /// <summary>
        /// The pointer to (location of) the data.
        /// </summary>
        /// <value>A pointer to the beginning of the byte array.</value>
        public IntPtr Location { get => dataPtr; }
        /// <summary>
        /// The bytes of the retrieved name.
        /// </summary>
        /// <value>Array of bytes containing a retrieved name.</value>
        public byte[] Data { get => data; }
        /// <summary>
        /// The length of the retrieved string in bytes.
        /// </summary>
        /// <value>Length of the retrieved string in bytes.</value>
        public int StringByteLength { get => strLen; }


        #endregion
        #region Constructors/Finalizer


        /// <summary>
        /// Creates a new Name Retriever. This involves allocating a byte array, pinning the
        /// byte array in memory, and retrieving the pointer address of the pinned array.
        /// </summary>
        /// <param name="sizeOfDataInBytes">The number of "bytes" of data to reserve.</param>
        FMODUTF8NameRetriever(int sizeOfDataInBytes)
        {
            data = new byte[sizeOfDataInBytes];
            handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            dataPtr = handle.AddrOfPinnedObject();
        }

        ~FMODUTF8NameRetriever()
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }


        #endregion
        #region Static Methods


        /// <summary>
        /// Copies the up to the requested number of name bytes for the Sound instance into the
        /// specified location.
        /// </summary>
        /// <param name="sound">A pointer to the sound instance for which to retrieve the name.</param>
        /// <param name="name">A pointer to the location where the name should be written.</param>
        /// <param name="namelen">The maximum number of bytes to write.</param>
        /// <returns></returns>
        [DllImport(FMOD.VERSION.dll)]
        static extern FMOD.RESULT FMOD5_Sound_GetName(IntPtr sound, IntPtr name, int namelen);


        #endregion
        #region Methods


        /// <summary>
        /// Fills the instance's byte array with data retreived from the native FMOD API request for
        /// the name of the provided Sound instance.
        /// </summary>
        /// <param name="sound">The FMOD Sound instance for which to retrieve the name info.</param>
        public void GetNameForSound(FMOD.Sound sound)
        {
            int dataLen = data.Length;
            FMOD5_Sound_GetName(sound.handle, dataPtr, dataLen);

            int idx = 0;
            while (idx < dataLen && data[idx] != 0)
            {
                idx++;
            }
            strLen = idx;
        }


        #endregion
    }
}
