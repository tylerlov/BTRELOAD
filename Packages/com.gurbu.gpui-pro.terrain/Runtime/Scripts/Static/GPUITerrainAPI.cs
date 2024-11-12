// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancerPro.TerrainModule
{
    public static class GPUITerrainAPI
    {
        #region Terrain Methods

        /// <summary>
        /// Adds the given terrain collection to the Detail or Tree Manager.
        /// </summary>
        /// <param name="terrainManager">The Tree or Detail Manager to which the terrain will be added</param>
        /// <param name="terrains">Collection of terrains to add to the manager</param>
        public static void AddTerrains<T>(GPUITerrainManager<T> terrainManager, IEnumerable<Terrain> terrains) where T : GPUIPrototypeData, new()
        {
            terrainManager.AddTerrains(terrains);
        }

        /// <summary>
        /// Adds the given terrain collection to the Detail or Tree Manager.
        /// </summary>
        /// <param name="terrainManager">The Tree or Detail Manager to which the terrain will be added</param>
        /// <param name="gpuiTerrains">Collection of GPUI terrains to add to the manager</param>
        public static void AddTerrains<T>(GPUITerrainManager<T> terrainManager, IEnumerable<GPUITerrain> gpuiTerrains) where T : GPUIPrototypeData, new()
        {
            terrainManager.AddTerrains(gpuiTerrains);
        }

        /// <summary>
        /// Adds the given terrain to the Detail or Tree Manager.
        /// </summary>
        /// <param name="terrainManager">The Tree or Detail Manager to which the terrain will be added</param>
        /// <param name="terrain">Terrain to add to the manager</param>
        public static bool AddTerrain<T>(GPUITerrainManager<T> terrainManager, Terrain terrain) where T : GPUIPrototypeData, new()
        {
            return terrainManager.AddTerrain(terrain);
        }

        /// <summary>
        /// Adds the given terrain to the Detail or Tree Manager.
        /// </summary>
        /// <param name="terrainManager">The Tree or Detail Manager to which the terrain will be added</param>
        /// <param name="gpuiTerrain">GPUI terrain to add to the manager</param>
        public static bool AddTerrain<T>(GPUITerrainManager<T> terrainManager, GPUITerrain gpuiTerrain) where T : GPUIPrototypeData, new()
        {
            return terrainManager.AddTerrain(gpuiTerrain);
        }

        /// <summary>
        /// Removes the given terrain from the Detail or Tree Manager.
        /// </summary>
        /// <param name="terrainManager">The Tree or Detail Manager from which the terrain will be removed</param>
        /// <param name="terrain">Terrain to remove from the manager</param>
        public static bool RemoveTerrain<T>(GPUITerrainManager<T> terrainManager, Terrain terrain) where T : GPUIPrototypeData, new()
        {
            return terrainManager.RemoveTerrain(terrain);
        }

        /// <summary>
        /// Removes the given terrain from the Detail or Tree Manager.
        /// </summary>
        /// <param name="terrainManager">The Tree or Detail Manager from which the terrain will be removed</param>
        /// <param name="gpuiTerrain">GPUI terrain to remove from the manager</param>
        public static bool RemoveTerrain<T>(GPUITerrainManager<T> terrainManager, GPUITerrain gpuiTerrain) where T : GPUIPrototypeData, new()
        {
            return terrainManager.RemoveTerrain(gpuiTerrain);
        }

        /// <param name="terrainManager">The Tree or Detail Manager to check the existence of the terrains</param>
        /// <param name="terrains">Collection of terrains to check</param>
        /// <returns>True, if all the terrains in the given list is already added to the manager.</returns>
        public static bool ContainsTerrains<T>(GPUITerrainManager<T> terrainManager, IEnumerable<Terrain> terrains) where T : GPUIPrototypeData, new()
        {
            return terrainManager.ContainsTerrains(terrains);
        }

        /// <param name="terrainManager">The Tree or Detail Manager to check the existence of the terrains</param>
        /// <param name="terrain">The terrains to check</param>
        /// <returns>True, if the terrain is already added to the manager.</returns>
        public static bool ContainsTerrain<T>(GPUITerrainManager<T> terrainManager, Terrain terrain) where T : GPUIPrototypeData, new()
        {
            return terrainManager.ContainsTerrain(terrain);
        }

        /// <summary>
        /// This method can be used to notify the Detail Manager about changes to terrain details at runtime, such as when the terrain height is modified.
        /// </summary>
        /// <param name="detailManager">The Detail Manager to update.</param>
        /// <param name="forceImmediateUpdate">(Optional) When true, the Detail Manager will update the detail instances immediately instead of waiting for asynchronous GPU readback.</param>
        public static void RequireUpdate(GPUIDetailManager detailManager, bool forceImmediateUpdate = false)
        {
            detailManager.RequireUpdate(forceImmediateUpdate);
        }

        /// <summary>
        /// This method can be used to notify the Tree Manager about changes to terrain trees at runtime, such as when the terrain height is modified.
        /// </summary>
        /// <param name="treeManager">The Tree Manager to update.</param>
        /// <param name="reloadTreeInstances">(Optional) When true, the Tree Manager will reload the tree instances from the terrain instead of using cached data.</param>
        public static void RequireUpdate(GPUITreeManager treeManager, bool reloadTreeInstances = true)
        {
            treeManager.RequireUpdate(reloadTreeInstances);
        }

        #endregion Terrain Methods
    }
}