// Unity
using UnityEngine;

namespace GUPS.EasyPerformanceMonitor.Singleton
{
    /// <summary>
    /// A thread-safe singleton active throughout the whole application.
    /// </summary>
    /// <typeparam name="T">The type of the singleton.</typeparam>
    public class PersistentSingleton<T> : MonoBehaviour
        where T : MonoBehaviour
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        private static T singleton;

        /// <summary>
        /// Lock for thread safety.
        /// </summary>
        private static object lockHandle = new object();

        /// <summary>
        /// Returns an active singleton of this instance or creates a new one.
        /// </summary>
        public static T Singleton
        {
            get
            {
                lock (lockHandle)
                {
                    // The GameObject got destroyed, so the singleton is null.
                    if (singleton != null && singleton.gameObject == null)
                    {
                        singleton = null;
                    }

                    // If there is no singleton, create a new one.
                    if (singleton == null)
                    {
                        singleton = (T)FindObjectOfType(typeof(T));

                        if (FindObjectsOfType(typeof(T)).Length > 1)
                        {
                            return singleton;
                        }

                        if (singleton == null)
                        {
                            Create<T>();
                        }
                    }

                    return singleton;
                }
            }
        }

        /// <summary>
        /// Returns true if a singleton exists.
        /// </summary>
        public static bool Exists
        {
            get
            {
                return singleton != null;
            }
        }

        /// <summary>
        /// On awake, check if there is already a singleton.
        /// If there is one and it is not this, destroy the GameObject.
        /// </summary>
        protected virtual void Awake()
        {
            // If a singleton already exists and this is not the singleton, destroy it immediately. Else keep it.
            if (Exists)
            {
                if (this != singleton && this.gameObject != null)
                {
                    DestroyImmediate(this.gameObject);
                }
            }
            else
            {
                singleton = this as T;
            }
        }

        /// <summary>
        /// Create a GameObject adding T1 and set the singleton to the value T1.
        /// </summary>
        /// <typeparam name="T1">The type of the singleton.</typeparam>
        private static void Create<T1>() where T1 : T
        {
            // Already exists, just return.
            if (Exists)
            {
                return;
            }

            // Create a GameObject.
            GameObject var_Singleton = new GameObject();

            // Add the singleton component to it.
            singleton = var_Singleton.AddComponent<T1>();
            var_Singleton.name = "(PersistentSingleton) " + typeof(T1).ToString();

            // Mark the GameObject as 'do not destroy'.
            DontDestroyOnLoad(var_Singleton);
        }
    }
}
