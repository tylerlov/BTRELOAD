using System;
using UnityEngine;

namespace HohStudios.Tools.ObjectParticleSpawner
{
    /// <summary>
    /// The Object Particle System is the ultimate object spawner system which utilizes the Unity Particle System to spawn and manipulate game objects. It uses
    /// and advanced object pooling system to manage spawning the game objects. 
    /// 
    /// A game object is spawned each time a new particle in the particle system is created. Each gameobject inherits properties from the new particle
    /// until 'Released' by either particle lifetime ending, on collision, or by code. Released objects act as indepedent objects and can be recycled back into the pool as desired.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public partial class ObjectParticleSpawner : MonoBehaviour
    {

        #region Fields and Properties

        /// ____________________ PUBLIC FIELDS ____________________///

        /// <summary>
        /// Reference to the Particle System Component attached (exposed for convenience)
        /// </summary>
        private ParticleSystem _particleSystem;

        /// <summary>
        /// The settings of the Object Particle System, representing the default settings for the entire system unless overridden by ObjectParticle.cs component
        /// </summary>
        [HideInInspector]
        public ObjectParticleSettings SystemSettings;

        /// ____________________ PRIVATE FIELDS ____________________///

        /// <summary>
        /// Cached array of size Max Particles to hold the currently alive particles each frame
        /// </summary>
        private ParticleSystem.Particle[] _aliveParticles;

        /// <summary>
        /// The current number of living particles in the particle system
        /// </summary>
        private int _numberOfAliveParticles;

        /// <summary>
        /// Cached array of all of the currently alive and not-released spawned objects (sorted so alive is up front and null is at the end)
        /// </summary>
        private ObjectParticleInfo[] _aliveObjects;

        /// <summary>
        /// The index of _aliveObjects where the entry is null for fast management (everything below the index is alive, everything after and including the index is null)
        /// </summary>
        private int _numberOfAliveObjects;

        /// <summary>
        /// Cached array of the particles random seeds so it only gets called once per particle per frame for performance
        /// </summary>
        private uint[] _cachedSeeds;

        /// <summary>
        /// This array was added later to cache indexes of ObjectParticles that should survive or should be released each frame (big performance boost dodging nested loop)
        /// Entries in the array that = 1 survive the frame, any index that holds a 0 in the target entries are released
        /// </summary>
        private int[] _survivingObjectsArray;

        /// <summary>
        /// This array was added to activate particles after the releasing of all old particles to keep alive numbers consistent. (case where number of objects == max particles)
        ///   Since we get activation data before release data, it forces activation to come before release, which can hit its upper constraint and stop activating before releasing.
        ///   So, we release a bunch of particles from the system, and then activate the un-activated particles remaining now that there is room available.
        /// </summary>
        private int[] _postReleaseActivationArray;

        /// <summary>
        /// This array was added to hold the current fixed update calls to be executed in the fixed update for rigidbody/physics manipulations
        /// </summary>
        private ObjectParticleInfo[] _fixedUpdateCalls;

        /// <summary>
        /// The current number of fixed update calls to index the array so we dont need to generate dynamic memory
        /// </summary>
        private int _numberOfFixedUpdateCalls;

        /// <summary>
        /// This array was added so that we can set parents of objects to the new release container in fixed update to avoid jittery motion
        /// </summary>
        private ObjectParticleReleaseInfo[] _fixedUpdateParentCalls;

        /// <summary>
        /// The current number of objects that need to be released in fixed update to have their parent set cleanly
        /// </summary>
        private int _numberOfFixedUpdateParentsCalls;

        /// <summary>
        /// The current size of the dynamically sized arrays to minimize memory usage by changing size to grow and shrink with demand
        /// </summary>
        private int _currentDynamicArraySize;

        #endregion Fields and Properties




        #region Functions

        /// <summary>
        /// Call reset in constructor for editor convenience
        /// </summary>
        public ObjectParticleSpawner() { Reset(); }


        private void Start()
        {
            // Cache variables
            _cachedSystemMain = _particleSystem.main;
            _cachedSimulationSpace = _cachedSystemMain.simulationSpace;

            // Initialize arrays
            _aliveParticles = new ParticleSystem.Particle[_cachedSystemMain.maxParticles];
            _cachedSeeds = new uint[_cachedSystemMain.maxParticles];
            _survivingObjectsArray = new int[_cachedSystemMain.maxParticles];
            _postReleaseActivationArray = new int[_cachedSystemMain.maxParticles / 2];

            // Initialize dynamically sized arrays
            _currentDynamicArraySize = Mathf.Max(1, ObjectPool.PoolSize);
            _aliveObjects = new ObjectParticleInfo[_currentDynamicArraySize];
            _fixedUpdateCalls = new ObjectParticleInfo[_currentDynamicArraySize];
            _fixedUpdateParentCalls = new ObjectParticleReleaseInfo[_currentDynamicArraySize];

            // Disable the particle renderer so we arent actually drawing any particles (for performance)
            _cachedSystemRenderer = _particleSystem.GetComponent<ParticleSystemRenderer>();
            _cachedSystemRenderer.renderMode = ParticleSystemRenderMode.None;

            // Get all the prewarmed particles to start
            _numberOfAliveParticles = _particleSystem.GetParticles(_aliveParticles);

            CheckIfDynamicArraysNeedResize();

            // Spawn all prewarmed objects
            for (var i = 0; i < _numberOfAliveParticles; i++)
            {
                _cachedSeeds[i] = _aliveParticles[i].randomSeed;
                ActivateObjectFromPool(i);
            }
        }

        private void LateUpdate()
        {
            // Return immediately if nothing to spawn or update
            if (ObjectPool.SpawnObjectsTotalWeight() == 0 && _numberOfAliveObjects == 0)
                return;

            _cachedSimulationSpace = _cachedSystemMain.simulationSpace;

            // Handle re-sizing the arrays if the Max Particles property of the Particle System changed
            CheckIfMaxParticlesSizeChanged();

            // Error catching assertions.. not included in build
            if (_numberOfAliveObjects < _aliveObjects.Length)
                UnityEngine.Assertions.Assert.IsTrue(_aliveObjects[_numberOfAliveObjects] == null || !_aliveObjects[_numberOfAliveObjects].IsInitialized);

            // Get the particles from the Particle System that are alive this frame
            _numberOfAliveParticles = _particleSystem.GetParticles(_aliveParticles);

            // Check if we need to resize any arrays due to changing particle count
            CheckIfDynamicArraysNeedResize();

            // Clean up the particle array before we begin to force consistent results due to changing particle emissions
            ClearParticleArrayTail();

            // Error catching assertions.. not included in build
            if (_numberOfAliveParticles < _aliveParticles.Length)
                UnityEngine.Assertions.Assert.IsTrue(_aliveParticles[_numberOfAliveParticles].remainingLifetime < 0.001f);
            if (_numberOfAliveObjects < _aliveObjects.Length)
                UnityEngine.Assertions.Assert.IsTrue(_aliveObjects[_numberOfAliveObjects] == null || !_aliveObjects[_numberOfAliveObjects].IsInitialized);

            var postReleaseActivationCount = 0;

            // Searches the living particles against the living objects. Spawns new objects / Updates surviving objects / Marks old objects to be released
            for (var i = 0; i < _numberOfAliveParticles; i++)
            {
                // Cache each particles seed once per frame for performance (slow operation)
                _cachedSeeds[i] = _aliveParticles[i].randomSeed;

                // If we have an immediate match at this index, skip to updating the object (skipping the nested loop is huge for performance)
                var particleHasMatchingObject = false;
                if (i < _aliveObjects.Length && _aliveObjects[i] != null && _aliveObjects[i].PoolInfo.ParticleId == _cachedSeeds[i])
                {
                    particleHasMatchingObject = true;
                }
                else
                {
                    // If we didn't get lucky, lets search all of the objects to see if theres a particle match somewhere
                    for (var j = 0; j < _numberOfAliveObjects; j++)
                    {
                        if (_aliveObjects[j] == null || !_aliveObjects[j].IsInitialized || _aliveObjects[j].PoolInfo.ParticleId == 0)
                        {
                            // If an object was found to be null, swap it to be the in the null tail of the array and decrement counters and reset it
                            _aliveObjects[j]?.Reset();
                            _aliveObjects.SwapEntries(j, _numberOfAliveObjects - 1);
                            _numberOfAliveObjects--;
                            j--;
                            continue;
                        }

                        // If we found the match at an arbitrary index, sort the array index so it lines up for the next frame, then update the object
                        if (_aliveObjects[j].PoolInfo.ParticleId == _cachedSeeds[i])
                        {
                            if (i < _numberOfAliveObjects)
                            {
                                _aliveObjects.SwapEntries(j, i);
                            }
                            else
                            {
                                // This was added later because when the weights go from full to 0, the AliveObjectsNumber becomes less than the AliveParticlesNumber
                                // and since we can no longer swap their index, this prevents a bunch of objects from being released on accident
                                _survivingObjectsArray[j] = 1;
                                UpdateObject(j, i);
                            }

                            particleHasMatchingObject = true;
                            break;
                        }
                    }
                }

                // If we have a particle-object match, we update the particle and flag the particle to survive this frame
                if (particleHasMatchingObject)
                {
                    if (i < _numberOfAliveObjects)
                    {
                        UpdateObject(i, i); // Apply behavior to the particle since we know its not being released
                        _survivingObjectsArray[i] = 1; // Set entry of survive array to "1" to tag the object to not be released
                    }

                }
                else
                {
                    // If we didn't find a match for the particle, Activate now if we havent hit our max particles limit
                    if (_numberOfAliveObjects < _aliveObjects.Length)
                    {
                        // Spawn and activate an object
                        if (ActivateObjectFromPool(i))
                        {
                            // We have to switch the new object index from the end of the array to the current i index to stay aligned
                            if (i < _numberOfAliveObjects)
                            {
                                _aliveObjects.SwapEntries(_numberOfAliveObjects - 1, i);
                                _survivingObjectsArray[i] = 1; // Set entry of survive array to "1" to tag the object to not be released
                            }
                        }
                    }
                    else
                    {
                        // If we did hit our activation limit this frame, lets store all the un-activated particles and activate them after we release some particles
                        if (postReleaseActivationCount < _postReleaseActivationArray.Length)
                        {
                            _postReleaseActivationArray[postReleaseActivationCount] = i;
                            postReleaseActivationCount++;
                        }
                    }
                }
            }



            // Release particles loop
            for (var i = 0; i < _numberOfAliveObjects; i++)
            {
                // The surviving array now lines up with the _aliveObjects array entries
                // If the entry is 1, we don't release it this frame. If it 0, then release at this index
                if (_survivingObjectsArray[i] == 1)
                {
                    _survivingObjectsArray[i] = 0;
                    continue;
                }

                // Release whatever object that does not survive this frame
                ReleaseObject(i);

                // We have to re-order the release array after releasing an object in the same way we re-order the _aliveObjects array so they continue to line up
                // Swap the last real array index with the one that was just released
                _survivingObjectsArray.SwapEntries(i, _numberOfAliveObjects);

                i--; // Necessary since the _numberOfAliveObjects got decremented on ReleaseObject(), dont want to skip any entries
            }



            // Post-release activation of un-activated particles. Keeps the object count consistent with the particle count.
            for (var i = 0; i < postReleaseActivationCount; i++)
            {
                // This is a necessary evil due to the fact that we get activation data before we get release data, so we activate first but can hit our max particles limit
                // and stop activating objects. Then, we get our release data and release a bunch of objects. Finally, now that there is room, we activate the un-activated objects
                if (_numberOfAliveParticles == _numberOfAliveObjects)
                    break;

                ActivateObjectFromPool(_postReleaseActivationArray[i]);
            }

            // Error catching assertions.. not included in build
            if (_numberOfAliveParticles < _aliveParticles.Length)
                UnityEngine.Assertions.Assert.IsTrue(_aliveParticles[_numberOfAliveParticles].remainingLifetime < 0.001f);
            if (_numberOfAliveObjects < _aliveObjects.Length)
                UnityEngine.Assertions.Assert.IsTrue(_aliveObjects[_numberOfAliveObjects] == null || !_aliveObjects[_numberOfAliveObjects].IsInitialized);
        }


        private void FixedUpdate()
        {
            // On fixed update, call any pending fixed update calls that accrued over the late update frames
            if (_numberOfFixedUpdateCalls > 0)
            {
                for (var i = 0; i < _numberOfFixedUpdateCalls; i++)
                {
                    // Stop it from possibly calling fixed update after it was released due to frame timing or if it is not ready
                    if (!_fixedUpdateCalls[i].IsInitialized || _fixedUpdateCalls[i].PoolInfo.IsReleased)
                        continue;

                    // Invoke the fixed update calls
                    _fixedUpdateCalls[i].ObjectParticle.OnFixedUpdate(_fixedUpdateCalls[i].ParticleInfo, this);
                    _fixedUpdateCalls[i].ObjectParticle.OnFixedUpdateEvent?.Invoke(_fixedUpdateCalls[i].ParticleInfo, this);
                }
                _numberOfFixedUpdateCalls = 0;
            }

            // On fixed update, call any pending parent calls to transfer object into release container, performed on fixed update to avoid a jittery transition
            if (_numberOfFixedUpdateParentsCalls > 0)
            {
                for (var i = 0; i < _numberOfFixedUpdateParentsCalls; i++)
                {
                    if (_fixedUpdateParentCalls[i].ObjectTransform != null)
                        _fixedUpdateParentCalls[i].ObjectTransform.SetParent(_fixedUpdateParentCalls[i].ReleaseContainer);

                    _fixedUpdateParentCalls[i] = default;
                }
                _numberOfFixedUpdateParentsCalls = 0;
            }
        }

        /// <summary>
        /// The update function call to update the ObjectParticle or the apply default behavior to an object
        /// </summary>
        private void UpdateObject(int objectIndex, int particleIndex)
        {
            // Pass the particle into the ParticleInfo class state, an expensive operation that is called once per frame
            _aliveObjects[objectIndex].ParticleInfo.Particle = _aliveParticles[particleIndex];

            if (_aliveObjects[objectIndex].IsObjectParticle)
            {
                // If we're doing a fixed update behaviour, just add it to the array to be called on fixed update, otherwise call it now
                if (_aliveObjects[objectIndex].ObjectParticle.FinalSettings.CallFixedUpdate)
                {
                    // Make sure the array doesn't already contain the fixed update call so it doesnt duplicate it
                    bool contains = false;
                    for (var i = 0; i < _numberOfFixedUpdateCalls; i++)
                    {
                        if (_fixedUpdateCalls[i].PoolInfo.ParticleId == _aliveObjects[objectIndex].PoolInfo.ParticleId)
                        {
                            // Update the currently contained info if it already exists
                            _fixedUpdateCalls[i] = _aliveObjects[objectIndex];
                            contains = true;
                            break;
                        }
                    }

                    // Add fixed update call to array if it doesnt exist
                    if (!contains && _numberOfFixedUpdateCalls < _fixedUpdateCalls.Length - 1)
                    {
                        _fixedUpdateCalls[_numberOfFixedUpdateCalls] = _aliveObjects[objectIndex];
                        _numberOfFixedUpdateCalls++;
                    }
                }

                // Always call regular update regardless of fixed update being used
                _aliveObjects[objectIndex].ObjectParticle.OnUpdate(_aliveObjects[objectIndex].ParticleInfo, this);
                _aliveObjects[objectIndex].ObjectParticle.OnUpdateEvent?.Invoke(_aliveObjects[objectIndex].ParticleInfo, this);
                _aliveObjects[objectIndex].PoolInfo.IsReleased = false;
            }
            else // Apply default behaviour to objects that don't contain an object particle component attached
            {
                if (_aliveObjects[objectIndex].ObjectTransform)
                {
                    ApplyDefaultMovement(_aliveObjects[objectIndex].ParticleInfo, _aliveObjects[objectIndex].ObjectTransform, SystemSettings);
                    ApplyDefaultRotation(_aliveObjects[objectIndex].ParticleInfo, _aliveObjects[objectIndex].ObjectTransform, SystemSettings);
                    ApplyDefaultScaling(_aliveObjects[objectIndex].ParticleInfo, _aliveObjects[objectIndex].ObjectTransform, SystemSettings);
                }
            }
        }

        /// <summary>
        /// Applies default 'inherit movement' behaviour to the object based on the associated particle and the settings given
        /// </summary>
        public void ApplyDefaultMovement(ParticleInfo info, Transform obj, ObjectParticleSettings settings)
        {
            if (!settings.InheritMovement)
                return;

            if (_cachedSimulationSpace == ParticleSystemSimulationSpace.Local)
                obj.position = _particleSystem.transform.TransformPoint(info.Particle.position);
            else if (_cachedSimulationSpace == ParticleSystemSimulationSpace.World)
                obj.position = info.Particle.position;
            else if (_cachedSimulationSpace == ParticleSystemSimulationSpace.Custom)
                obj.position = _cachedSystemMain.customSimulationSpace != null ? _cachedSystemMain.customSimulationSpace.TransformPoint(info.Particle.position) : info.Particle.position;
        }

        /// <summary>
        /// Applies default 'inherit rotation' behaviour to the object based on the associated particle and the settings given
        /// </summary>
        public void ApplyDefaultRotation(ParticleInfo info, Transform obj, ObjectParticleSettings settings)
        {
            if (!settings.InheritRotation)
                return;

            obj.rotation = Quaternion.Euler(info.Particle.rotation3D);
        }

        /// <summary>
        /// Applies default 'inherit scale' behaviour to the object based on the associated particle and the settings given
        /// </summary>
        public void ApplyDefaultScaling(ParticleInfo info, Transform obj, ObjectParticleSettings settings)
        {
            if (!settings.InheritScale)
                return;

            obj.localScale = info.Particle.GetCurrentSize3D(ParticleSystem);
        }

        /// <summary>
        /// Releases the particle from the spawner at the given object index in the _aliveObjects array.
        /// Uses the index as a parameter for optimizations of spawner-directed releasing
        /// </summary>
        private bool ReleaseObject(int objectIndex)
        {
            // Get the object at the index
            var releaseInfo = _aliveObjects[objectIndex];

            // Sorts the _aliveObjects array by taking the last non-null value of the array and moving it up to the index of the object to release
            // Keeping the array sorted means our for-loops only have to travel up to the null index, saving performance since we dont travel whole array
            if (objectIndex != _numberOfAliveObjects - 1)
                _aliveObjects.SwapEntries(_numberOfAliveObjects - 1, objectIndex);

            // Decrement the null index now that we removed an object and sorted the array to keep consistent
            if (_numberOfAliveObjects > 0)
                _numberOfAliveObjects--;

            // Only release if its a valid entry
            if (!releaseInfo.IsInitialized || releaseInfo.PoolInfo.ParticleId == 0)
                return false;

            // Get the settings to use, which is either the objects settings or the system settings
            var releaseSettings = releaseInfo.IsObjectParticle ? releaseInfo.ObjectParticle.FinalSettings : SystemSettings;

            releaseInfo.PoolInfo.IsReleased = true;

            // If the object to release has the ObjectParticle component, trigger the OnRelease events
            if (releaseInfo.ObjectParticle)
            {
                releaseInfo.ObjectParticle.Info.IsReleased = true;
                releaseInfo.ObjectParticle.OnRelease(releaseInfo.ParticleInfo, this);
                releaseInfo.ObjectParticle.OnReleaseEvent?.Invoke(releaseInfo.ParticleInfo, this);
                releaseInfo.ObjectParticle.Info.ParticleId = 0;

                if (releaseSettings.RecycleOnRelease)
                    releaseInfo.ObjectParticle.Info.UpdateInfo(false, 0,
                        releaseInfo.ObjectParticle.Info.PoolId);
            }

            OnReleaseEvent?.Invoke(releaseInfo);

            // If we're destroy the object on release, just deactivate it back to the pool
            if (releaseSettings.RecycleOnRelease)
            {
                ReturnObjectToPool(releaseInfo.PoolInfo, releaseInfo.ObjectTransform);
                releaseInfo.Reset();
                return true;
            }

            // If we're just destroying the component on release, destroy it
            if (releaseSettings.DestroyComponentOnRelease && releaseInfo.ObjectParticle)
                Destroy(releaseInfo.ObjectParticle);

            // Add fixed update call to set the parent
            if (_numberOfFixedUpdateParentsCalls < _fixedUpdateParentCalls.Length - 1)
            {
                _fixedUpdateParentCalls[_numberOfFixedUpdateParentsCalls] = new ObjectParticleReleaseInfo()
                { ObjectTransform = releaseInfo.ObjectTransform, ReleaseContainer = (releaseInfo.IsObjectParticle ? releaseInfo.ObjectParticle.FinalSettings.ReleaseContainer : SystemSettings.ReleaseContainer) };

                _numberOfFixedUpdateParentsCalls++;
            }

            releaseInfo.Reset(); // Reset the info object for re-use
            return true;
        }

        /// <summary>
        /// Releases the particle from the particle spawner, returning true on successful release. This is an overload so it can be easily called from the
        /// ObjectParticle.cs component.
        /// </summary>
        public bool ReleaseEarly(uint particleId)
        {
            if (_numberOfAliveObjects == 0)
                return false;

            // This overload needs to find the index of the objectToRelease before actually releasing it 
            for (var i = 0; i < _numberOfAliveObjects; i++)
            {
                if (!_aliveObjects[i].IsInitialized)
                    continue;

                if (_aliveObjects[i].PoolInfo.ParticleId == particleId)
                {
                    // Find the particle with the ID matching the object to release so we can remove that particle from the entire system since its being released early
                    for (var j = 0; j < _numberOfAliveParticles; j++)
                    {

                        if (_cachedSeeds[j] == _aliveObjects[i].PoolInfo.ParticleId)
                        {
                            // Remove the particle from the system by first moving the dead particle to the back of the array (so the rest of the script can adapt accordingly)
                            _aliveParticles.SwapEntries(_numberOfAliveParticles - 1, j);
                            _aliveParticles[_numberOfAliveParticles - 1] = default;

                            _cachedSeeds.SwapEntries(_numberOfAliveParticles - 1, j);
                            _cachedSeeds[_numberOfAliveParticles - 1] = 0;

                            // Decrement number of alive particles now
                            if (_numberOfAliveParticles > 0)
                                _numberOfAliveParticles--;

                            _particleSystem.SetParticles(_aliveParticles); // Set the particle system to remove that particle
                            break;
                        }
                    }
                    return ReleaseObject(i);
                }
            }
            return false;
        }

        /// <summary>
        /// Handles re-sizing the array when the Max Particles property of the Particle System changes.
        /// 
        /// Resizes the array, releases any unneeded particles, and refills the array for seamless updating.
        /// </summary>
        private void CheckIfMaxParticlesSizeChanged()
        {
            // Check if the array length no longer is the right length
            if (_aliveParticles.Length != _cachedSystemMain.maxParticles)
            {
                // Create a copy of all 3 arrays before re-initializing
                var tempParticles = _aliveParticles;
                var tempSeedCache = _cachedSeeds;

                // Update the length of the two particle arrays
                _aliveParticles = new ParticleSystem.Particle[_cachedSystemMain.maxParticles];
                _cachedSeeds = new uint[_cachedSystemMain.maxParticles];
                _postReleaseActivationArray = new int[_cachedSystemMain.maxParticles / 2];

                // Get the new number of alive particles
                var numOfAlive = _numberOfAliveParticles < _aliveParticles.Length
                    ? _numberOfAliveParticles
                    : _aliveParticles.Length;

                // Find the objects & particles that survived the re-size based on identifiers matching, release all else
                for (var i = 0; i < _numberOfAliveObjects; i++)
                {
                    var particleSurvived = false;        // Use a looping "contains" logic here instead of linq for performance

                    // Check to see if there is a matching particle to the objects particle ID to see if it survived
                    for (var j = 0; j < numOfAlive; j++)
                    {
                        if (_cachedSeeds[j] == _aliveObjects[i].PoolInfo.ParticleId)
                        {
                            particleSurvived = true; // If it survived, stop searching and do nothing
                            break;
                        }
                    }

                    if (!particleSurvived)
                    {
                        // Need to decrement i on release since ReleaseObject() will sort the alive objects array, need to search the same index again on release
                        ReleaseObject(i);
                        i--;
                    }
                }

                // Re-populate both arrays with all of the old entries that fit within the new length
                var lowerLength = _aliveParticles.Length < tempParticles.Length ? _aliveParticles.Length : tempParticles.Length;
                for (var i = 0; i < lowerLength; i++)
                {
                    _aliveParticles[i] = tempParticles[i];
                    _cachedSeeds[i] = tempSeedCache[i];
                }
            }
        }

        /// <summary>
        /// Optimally changes the size of object arrays in conjunction with the particle number to minimize memory allocation
        /// </summary>
        private void CheckIfDynamicArraysNeedResize()
        {
            var dynamicSize = _currentDynamicArraySize;

            // Decide the size of the array max (doubles each time to max particles) and mins (1) depending on number of living particles
            if (_numberOfAliveParticles >= _currentDynamicArraySize)
                dynamicSize = Mathf.Min(_cachedSystemMain.maxParticles, dynamicSize * 2);
            else if ((_numberOfAliveParticles < _currentDynamicArraySize * 0.5f) && (_numberOfAliveObjects < _currentDynamicArraySize * 0.5f))
                dynamicSize = Mathf.Max(1, dynamicSize / 2);

            // Resize as appropriate
            if (_currentDynamicArraySize != dynamicSize)
            {
                _currentDynamicArraySize = dynamicSize;

                Array.Resize(ref _aliveObjects, _currentDynamicArraySize);
                if (_numberOfAliveObjects > _currentDynamicArraySize)
                    _numberOfAliveObjects = _currentDynamicArraySize;

                Array.Resize(ref _fixedUpdateCalls, _currentDynamicArraySize);
                if (_numberOfFixedUpdateCalls > _currentDynamicArraySize)
                    _numberOfFixedUpdateCalls = _currentDynamicArraySize;

                Array.Resize(ref _fixedUpdateParentCalls, _currentDynamicArraySize);
                if (_numberOfFixedUpdateParentsCalls > _currentDynamicArraySize)
                    _numberOfFixedUpdateParentsCalls = _currentDynamicArraySize;

                Array.Resize(ref _survivingObjectsArray, _currentDynamicArraySize);
            }
        }

        #endregion Functions
    }
}