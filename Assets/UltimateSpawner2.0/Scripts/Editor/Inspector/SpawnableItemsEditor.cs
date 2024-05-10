using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UltimateSpawner.Spawning;

namespace UltimateSpawner.Editor
{
    [CustomEditor(typeof(SpawnableItems))]
    public class SpawnableItemsEditor : UnityEditor.Editor
    {
        // Private
        private const int deleteButtonWidth = 24;

        private Color light = new Color(0.9f, 0.9f, 0.9f);
        private Color dark = new Color(0.7f, 0.7f, 0.7f);

        // Methods
        public override void OnInspectorGUI()
        {
            // Get the spawnable items
            SpawnableItems spawnableItems = target as SpawnableItems;

            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Items", EditorStyles.boldLabel);

                Rect last = GUILayoutUtility.GetLastRect();

                Handles.color = new Color(0.4f, 0.4f, 0.4f);
                Handles.DrawLine(new Vector3(last.x, last.y + last.height), new Vector3(last.x + last.width, last.y + last.height));

                GUILayout.Space(5);

                bool narrow = EditorGUIUtility.currentViewWidth < 360;

                Color old = GUI.backgroundColor;
                
                if(narrow == true)
                {
                    DrawItemsNarrow(spawnableItems);
                }
                else
                {
                    DrawItemsWide(spawnableItems);
                }

                // Restore original background colour
                GUI.backgroundColor = old;

                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();

                    if(GUILayout.Button("Add Prefab") == true)
                    {
                        AddSpawnableItem<PrefabSpawnableItemProvider>(spawnableItems);
                    }

                    if(GUILayout.Button("Add Provider") == true)
                    {
                        AddSpawnableItem<SpawnableItemProvider>(spawnableItems);
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            DrawItemWarnings(spawnableItems);


            // Push to bottom
            GUILayout.FlexibleSpace();

            // Create a drag drop area
            GUILayout.Label("Drag prefabs or item providers here!", EditorStyles.helpBox, GUILayout.Height(60));

            // Get the rectangle
            Rect dropArea = GUILayoutUtility.GetLastRect();

            switch(Event.current.type)
            {
                default: break;
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    {
                        // Check if we are inside the area
                        if(dropArea.Contains(Event.current.mousePosition) == true)
                        {
                            // Update the visual mode
                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                            if(Event.current.type == EventType.DragPerform)
                            {
                                // Accept the drop
                                DragAndDrop.AcceptDrag();

                                // Process each dropped item
                                foreach(Object obj in DragAndDrop.objectReferences)
                                {
                                    // Check for game object
                                    if(obj is GameObject)
                                    {
                                        // Get the game object
                                        GameObject go = obj as GameObject;

#if UNITY_2018_3_OR_NEWER
                                        if(PrefabUtility.GetPrefabAssetType(go) != PrefabAssetType.NotAPrefab &&
                                            PrefabUtility.GetPrefabAssetType(go) != PrefabAssetType.MissingAsset)
                                        {
                                            // Add a new item
                                            SpawnableItem item = AddSpawnableItem<PrefabSpawnableItemProvider>(spawnableItems);

                                            // Update the prefab
                                            ((PrefabSpawnableItemProvider)item.provider).prefab = go;

                                            // Save changes
                                            EditorUtility.SetDirty(spawnableItems);
                                        }
#else
                                        // Check for prefab
                                        if (PrefabUtility.GetPrefabType(go) == PrefabType.Prefab)
                                        {
                                            // Add a new item
                                            SpawnableItem item = AddSpawnableItem<PrefabSpawnableItemProvider>(spawnableItems);

                                            // Update the prefab
                                            ((PrefabSpawnableItemProvider)item.provider).prefab = go;

                                            // Save changes
                                            EditorUtility.SetDirty(spawnableItems);
                                        }
#endif
                                    }
                                    else if(obj is SpawnableItemProvider)
                                    {
                                        // Add a new custom provider
                                        SpawnableItem item = AddSpawnableItem<SpawnableItemProvider>(spawnableItems);

                                        // Assign the provider
                                        item.provider = obj as SpawnableItemProvider;

                                        // Save changes
                                        EditorUtility.SetDirty(spawnableItems);
                                    }
                                }
                            }
                        }
                        break;
                    }
            }
        }

        private void DrawItemsWide(SpawnableItems spawnableItems)
        {
            // Draw all items
            for(int i = 0; i < spawnableItems.items.Length; i++)
            {
                // Get the item
                SpawnableItem item = spawnableItems.items[i];

                // Check for prefab provider
                bool isPrefabProvider = item.provider is PrefabSpawnableItemProvider;

                // Staggered item colours
                GUI.backgroundColor = (i % 2 == 0) ? light : dark;
                
                // Begin the item layout
                GUILayout.BeginHorizontal(EditorStyles.helpBox);
                {
                    if (isPrefabProvider == true)
                    {
                        PrefabSpawnableItemProvider itemProvider = (PrefabSpawnableItemProvider)item.provider;

                        // Draw the prefab label
                        GUILayout.Label(new GUIContent("Prefab:", "The prefab for the spawnable item"));

                        // Draw the prefab field
                        GameObject result = EditorGUILayout.ObjectField(itemProvider.prefab, typeof(GameObject), false) as GameObject;

                        if (result != itemProvider.prefab)
                        {
                            itemProvider.prefab = result;
                            EditorUtility.SetDirty(spawnableItems);
                        }
                    }
                    else
                    {
                        GUILayout.Label(new GUIContent("Provider:", "The spawnable item provider for the spawnable item which is responsible for create and destroying spawnable item instances"));

                        // Display provider field
                        SpawnableItemProvider result = EditorGUILayout.ObjectField(item.provider, typeof(SpawnableItemProvider), false) as SpawnableItemProvider;

                        if(result != item.provider)
                        {
                            item.provider = result;
                            EditorUtility.SetDirty(spawnableItems);
                        }
                    }                   


                    // Draw the spawn chance label
                    GUILayout.Label(string.Format("Spawn Chance ({0}%):", (int)(item.spawnChance * 100)));

                    // Draw the spawn chance slider
                    item.spawnChance = GUILayout.HorizontalSlider(item.spawnChance, 0, 1, GUILayout.MinWidth(60));

                    // Draw the remove button
                    if (GUILayout.Button("X", GUILayout.Width(deleteButtonWidth)) == true)
                    {
                        // Remove the item
                        RemoveSpawnableItem(spawnableItems, item);
                    }
                }
                GUILayout.EndHorizontal();
            }
        }

        private void DrawItemsNarrow(SpawnableItems spawnableItems)
        {
            // Draw all items
            for(int i = 0; i < spawnableItems.items.Length; i++)
            {
                // Get the item
                SpawnableItem item = spawnableItems.items[i];

                // Check for prefab provider
                bool isPrefabProvider = item.provider is PrefabSpawnableItemProvider;

                // Staggered item colours
                GUI.backgroundColor = (i % 2 == 0) ? light : dark;

                // Begin the item layout
                GUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    // Prefab layout
                    GUILayout.BeginHorizontal();
                    {
                        if (isPrefabProvider == true)
                        {
                            // Draw the prefab label
                            GUILayout.Label("Prefab:");

                            // Draw the prefab field
                            ((PrefabSpawnableItemProvider)item.provider).prefab = EditorGUILayout.ObjectField(((PrefabSpawnableItemProvider)item.provider).prefab, typeof(GameObject), false) as GameObject;
                        }
                        else
                        {
                            GUILayout.Label("Provider:");

                            item.provider = EditorGUILayout.ObjectField(item.provider, typeof(SpawnableItemProvider), false) as SpawnableItemProvider;
                        }

                        // Draw the remove button
                        if (GUILayout.Button("X", GUILayout.Width(deleteButtonWidth)) == true)
                        {
                            // Remove the item
                            RemoveSpawnableItem(spawnableItems, item);
                        }
                    }
                    GUILayout.EndHorizontal();


                    // Spawn chance layout
                    GUILayout.BeginHorizontal();
                    {
                        // Draw the spawn chance label
                        GUILayout.Label(string.Format("Spawn Chance ({0}%):", (int)(item.spawnChance * 100)));

                        // Draw the spawn chance slider
                        item.spawnChance = GUILayout.HorizontalSlider(item.spawnChance, 0, 1, GUILayout.MinWidth(60));
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }            
        }

        private void DrawItemWarnings(SpawnableItems items)
        {
            List<SpawnableItem> zeroChance = new List<SpawnableItem>();

            // FInd all items that have 0 spawn chance
            foreach (SpawnableItem item in items.items)
                if (item.spawnChance == 0)
                    zeroChance.Add(item);

            // Check for no warnings
            if (zeroChance.Count == 0)
                return;

            // Display a warning
            string message = "";
            int warnings = 0;

            for (int i = 0; i < zeroChance.Count; i++)
            {
                // Check for unassigned prefab
                if (zeroChance[i].provider == null || zeroChance[i].provider.IsAssigned == false)
                    continue;

                if (zeroChance.Count == 1 || i == 0)
                {
                    message += "'" + zeroChance[i].provider.ItemName + "'";
                }
                else if (i == zeroChance.Count - 1)
                {
                    message += " and '" + zeroChance[i].provider.ItemName + "'";
                }
                else
                {
                    message += ", '" + zeroChance[i].provider.ItemName + "'";
                }

                // Increase number of warnings
                warnings++;
            }

            // Check for any errors
            if (warnings > 0)
            {
                message += (zeroChance.Count == 1) ? " has" : " have";

                EditorGUILayout.HelpBox(message + " a spawn chance of 0 and will not be spawnable", MessageType.Warning);
            }
        }

        private SpawnableItem AddSpawnableItem<T>(SpawnableItems items) where T : SpawnableItemProvider
        {
            // Generate a unique id
            int id = 0;

            while(true)
            {
                foreach (SpawnableItem value in items.items)
                {
                    if (value.SpawnableID == id)
                    {
                        id++;
                        continue;
                    }
                }
                break;
            }

            T providerInstance = null;

            // Dont create an instance of 'SpawnableItemProvider' as it is abstract and should be null
            if (typeof(T).IsAbstract == false)
                providerInstance = CreateInstance<T>();

            // Create a new item
            SpawnableItem item = new SpawnableItem(id)
            {
                provider = providerInstance,
                spawnChance = 0.5f,                
            };

            // Add to array
            ArrayUtility.Add(ref items.items, item);

            EditorUtility.SetDirty(items);

            // Save the scriptable object in the same asset
            if (providerInstance is PrefabSpawnableItemProvider)
            {
                PrefabSpawnableItemProvider prefabProvider = providerInstance as PrefabSpawnableItemProvider;

                // Add to asset
                AssetDatabase.AddObjectToAsset(prefabProvider, items);

                
                EditorUtility.SetDirty(prefabProvider);

                
            }

            // Save now or the changes may be lost
            AssetDatabase.SaveAssets();

            return item;
        }

        private void RemoveSpawnableItem(SpawnableItems items, SpawnableItem item)
        {
            // Remove from array
            ArrayUtility.Remove(ref items.items, item);

            // Mark asset as dirty
            EditorUtility.SetDirty(target);
			
			if (item.provider is PrefabSpawnableItemProvider)
            {
                // Remove from asset
                AssetDatabase.RemoveObjectFromAsset(item.provider);

                EditorUtility.SetDirty(item.provider);
            }

        }
    }
}
