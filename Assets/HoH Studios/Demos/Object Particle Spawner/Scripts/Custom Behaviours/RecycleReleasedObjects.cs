using UnityEngine;

namespace HohStudios.Tools.ObjectParticleSpawner.Demo
{
    /// <summary>
    /// 
    /// This class exists with the intention to attach it to the object particle system's
    /// "Released Objects" container to automatically recycle older released objects
    /// once the number of released children exceed the "MaxNumberOfObjects".
    /// 
    /// This stops infinite objects from being spawned indefinitely.
    /// 
    /// IMPORTANT: 
    ///     The released objects must contain an ObjectParticle component attached to be recycled, otherwise they get destroyed.
    /// 
    /// </summary>
    public class RecycleReleasedObjects : MonoBehaviour
    {
        public int MaxNumberOfObjects = 100;

        private void Update()
        {
            if (MaxNumberOfObjects <= 0)
                return;

            // If the number of children in the Released Objects container exceeds the limit
            if (transform.childCount > MaxNumberOfObjects)
            {
                // Recycle the oldest object
                var objParticle = transform.GetChild(0).GetComponent<ObjectParticle>();

                if (objParticle)
                    objParticle.Recycle();
                else
                    Destroy(transform.GetChild(0).gameObject);
            }
        }
    }
}