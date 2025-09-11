using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using SilkSong.Interfaces;

namespace SilkSong.Services
{
    /// <summary>
    /// Service for managing collectable item operations including scanning, setting amounts, and maxing all collectables.
    /// </summary>
    public class CollectableService
    {
        private readonly IModLogger logger;

        // Collectable management state
        private List<string> availableCollectables = new List<string>();
        private Dictionary<string, string> displayNameToObjectName = new Dictionary<string, string>();
        private bool collectablesScanned = false;

        public CollectableService(IModLogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Gets the list of available collectables. Scans if not already done.
        /// </summary>
        public List<string> GetAvailableCollectables()
        {
            if (!collectablesScanned)
            {
                ScanCollectables();
            }
            return new List<string>(availableCollectables);
        }

        /// <summary>
        /// Gets the display name to object name mapping.
        /// </summary>
        public Dictionary<string, string> GetDisplayNameMapping()
        {
            return new Dictionary<string, string>(displayNameToObjectName);
        }

        /// <summary>
        /// Forces a rescan of collectables (useful when scene changes).
        /// </summary>
        public void ForceScan()
        {
            collectablesScanned = false;
            ScanCollectables();
        }

        /// <summary>
        /// Scans for all available CollectableItemBasic objects and builds name mappings.
        /// </summary>
        public void ScanCollectables()
        {
            if (collectablesScanned) return;

            availableCollectables.Clear();
            displayNameToObjectName.Clear();

            try
            {
                // Find all CollectableItemBasic objects specifically (they have displayName)
                UnityEngine.Object[] allObjects = Resources.FindObjectsOfTypeAll(typeof(CollectableItemBasic));
                foreach (UnityEngine.Object obj in allObjects)
                {
                    if (obj != null)
                    {
                        try
                        {
                            Type itemType = obj.GetType();
                            string displayName = "";
                            string objectName = obj.name.Replace("(Clone)", "").Trim();

                            // Try GetDisplayName method first (most reliable for CollectableItemBasic)
                            MethodInfo getDisplayNameMethod = itemType.GetMethod("GetDisplayName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (getDisplayNameMethod != null && getDisplayNameMethod.ReturnType == typeof(string))
                            {
                                try
                                {
                                    // Find ReadSource enum and use default value
                                    Type readSourceType = FindTypeInAssemblies("ReadSource");
                                    object readSourceValue = null;

                                    if (readSourceType != null && readSourceType.IsEnum)
                                    {
                                        Array enumValues = Enum.GetValues(readSourceType);
                                        if (enumValues.Length > 0)
                                        {
                                            readSourceValue = enumValues.GetValue(0);
                                        }
                                    }

                                    object result = getDisplayNameMethod.Invoke(obj, new object[] { readSourceValue });
                                    if (result != null && !string.IsNullOrEmpty(result.ToString().Trim()))
                                    {
                                        displayName = result.ToString().Trim();
                                    }
                                }
                                catch (Exception)
                                {
                                    // Silent fallback
                                }
                            }

                            // Fallback to object name if GetDisplayName failed
                            if (string.IsNullOrEmpty(displayName))
                            {
                                displayName = objectName;
                            }

                            // Add if valid, not duplicate, and not a placeholder/invalid name
                            if (!string.IsNullOrEmpty(displayName) &&
                                !availableCollectables.Contains(displayName) &&
                                !IsInvalidDisplayName(displayName))
                            {
                                availableCollectables.Add(displayName);
                                displayNameToObjectName[displayName] = objectName;
                            }
                        }
                        catch (Exception e)
                        {
                            logger.Log($"Error processing collectable {obj.name}: {e.Message}");
                        }
                    }
                }

                // Sort alphabetically for better UX
                availableCollectables.Sort();
                collectablesScanned = true;
                logger.Log($"Scanned {availableCollectables.Count} collectables");
            }
            catch (Exception e)
            {
                logger.Log($"Error scanning collectables: {e.Message}");
            }
        }

        /// <summary>
        /// Sets a specific collectable to a specific amount using advanced reflection techniques.
        /// </summary>
        /// <param name="collectableDisplayName">Display name of the collectable</param>
        /// <param name="amount">Amount to set</param>
        /// <param name="onSuccess">Callback for success messages</param>
        /// <param name="onError">Callback for error messages</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool SetCollectableAmount(string collectableDisplayName, int amount, Action<string> onSuccess = null, Action<string> onError = null)
        {
            try
            {
                // Get the actual object name from the display name
                string actualObjectName = "";
                if (displayNameToObjectName.ContainsKey(collectableDisplayName))
                {
                    actualObjectName = displayNameToObjectName[collectableDisplayName];
                }
                else
                {
                    // Fallback: treat as object name if no mapping found
                    actualObjectName = collectableDisplayName;
                }

                // Find the CollectableItemBasic object
                UnityEngine.Object[] allObjects = Resources.FindObjectsOfTypeAll(typeof(CollectableItemBasic));

                foreach (UnityEngine.Object obj in allObjects)
                {
                    if (obj == null) continue;

                    // Check if this is the collectable we're looking for
                    string objName = obj.name.Replace("(Clone)", "").Trim();
                    if (objName == actualObjectName)
                    {
                        try
                        {
                            // Try using CollectableItemManager to set amount properly
                            Type managerType = FindTypeInAssemblies("CollectableItemManager");
                            if (managerType != null)
                            {
                                // Get the singleton instance using ManagerSingleton pattern
                                Type managerSingletonType = FindTypeInAssemblies("ManagerSingleton`1");
                                if (managerSingletonType != null)
                                {
                                    Type genericManagerType = managerSingletonType.MakeGenericType(managerType);
                                    PropertyInfo instanceProperty = genericManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);

                                    if (instanceProperty != null)
                                    {
                                        object managerInstance = instanceProperty.GetValue(null);
                                        if (managerInstance != null)
                                        {
                                            // Get the internal methods for precise control
                                            MethodInfo removeItemMethod = managerType.GetMethod("InternalRemoveItem", BindingFlags.NonPublic | BindingFlags.Instance);
                                            MethodInfo addItemMethod = managerType.GetMethod("InternalAddItem", BindingFlags.NonPublic | BindingFlags.Instance);

                                            if (removeItemMethod != null && addItemMethod != null)
                                            {
                                                // First, remove a large amount to clear current inventory (the method has bounds checking)
                                                removeItemMethod.Invoke(managerInstance, new object[] { obj, 9999 });

                                                // Then add the exact amount we want
                                                if (amount > 0)
                                                {
                                                    addItemMethod.Invoke(managerInstance, new object[] { obj, amount });
                                                }

                                                onSuccess?.Invoke($"Set {collectableDisplayName} to {amount}");
                                                return true;
                                            }
                                            else
                                            {
                                                // Fallback to public AddItem method (will add to current)
                                                MethodInfo publicAddItemMethod = managerType.GetMethod("AddItem", BindingFlags.Public | BindingFlags.Static);
                                                if (publicAddItemMethod != null)
                                                {
                                                    publicAddItemMethod.Invoke(null, new object[] { obj, amount });
                                                    onSuccess?.Invoke($"Added {amount} {collectableDisplayName} (note: adds to current amount)");
                                                    return true;
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            // Direct reflection fallback if manager approach fails
                            Type itemType = obj.GetType();

                            // Try to directly set the amount field
                            FieldInfo currentAmountField = itemType.GetField("currentAmount", BindingFlags.NonPublic | BindingFlags.Instance);
                            if (currentAmountField == null)
                            {
                                currentAmountField = itemType.GetField("amount", BindingFlags.NonPublic | BindingFlags.Instance);
                            }

                            if (currentAmountField != null && currentAmountField.FieldType == typeof(int))
                            {
                                currentAmountField.SetValue(obj, amount);
                                onSuccess?.Invoke($"Set {collectableDisplayName} to {amount} (direct field access)");
                                return true;
                            }

                            // Try property
                            PropertyInfo amountProperty = itemType.GetProperty("amount", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (amountProperty != null && amountProperty.PropertyType == typeof(int) && amountProperty.CanWrite)
                            {
                                amountProperty.SetValue(obj, amount);
                                onSuccess?.Invoke($"Set {collectableDisplayName} to {amount} (direct property access)");
                                return true;
                            }
                        }
                        catch (Exception e)
                        {
                            logger.Log($"Error setting amount for {obj.name}: {e.Message}");
                        }
                    }
                }

                onError?.Invoke($"Could not find or set amount for {collectableDisplayName}");
                return false;
            }
            catch (Exception e)
            {
                onError?.Invoke($"Error setting {collectableDisplayName}: {e.Message}");
                logger.Log($"Error setting collectable amount: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Maximizes all collectables by adding 99 to each using their AddAmount method.
        /// </summary>
        /// <returns>Number of collectables that were successfully maxed</returns>
        public int MaxAllCollectables()
        {
            try
            {
                logger.Log("=== MAXING ALL COLLECTABLES ===");

                // Find all CollectableItem objects directly (base class with AddAmount method)
                UnityEngine.Object[] allObjects = Resources.FindObjectsOfTypeAll(typeof(CollectableItem));
                logger.Log($"Found {allObjects.Length} CollectableItem objects");

                int maxedCount = 0;

                foreach (UnityEngine.Object obj in allObjects)
                {
                    if (obj == null) continue;

                    // Check if it's a CollectableItem (includes CollectableItemBasic subclasses)
                    try
                    {
                        Type itemType = obj.GetType();

                        // Look for AddAmount method (protected virtual, so need NonPublic flag)
                        MethodInfo addAmountMethod = itemType.GetMethod("AddAmount", BindingFlags.NonPublic | BindingFlags.Instance);

                        if (addAmountMethod != null)
                        {
                            addAmountMethod.Invoke(obj, new object[] { 99 });
                            logger.Log($"Added 99x {obj.name}");
                            maxedCount++;
                        }
                        else
                        {
                            logger.Log($"No AddAmount method found on {obj.name} (type: {itemType.Name})");
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Log($"Error maxing {obj.name}: {e.Message}");
                    }
                }

                logger.Log($"Collectable Max: Added 99x to {maxedCount} collectables");
                return maxedCount;
            }
            catch (Exception e)
            {
                logger.Log($"Error maxing collectables: {e.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Filters collectables by search term.
        /// </summary>
        /// <param name="searchFilter">Search term to filter by</param>
        /// <returns>Array of filtered collectable names</returns>
        public string[] FilterCollectables(string searchFilter = "")
        {
            if (!collectablesScanned)
            {
                ScanCollectables();
            }

            if (string.IsNullOrEmpty(searchFilter))
            {
                return availableCollectables.ToArray();
            }
            else
            {
                return availableCollectables.Where(name =>
                    name.ToLower().Contains(searchFilter.ToLower())).ToArray();
            }
        }

        /// <summary>
        /// Checks if a display name is invalid/placeholder.
        /// </summary>
        private bool IsInvalidDisplayName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName)) return true;

            // Filter out common placeholder/invalid display names
            string[] invalidNames = { "!!/!!", "!!!", "???", "N/A", "NULL", "PLACEHOLDER", "TBD", "TODO" };

            foreach (string invalid in invalidNames)
            {
                if (displayName.Equals(invalid, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // Filter out names that are just special characters or symbols
            if (displayName.Trim().All(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c)))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Helper method to find types in loaded assemblies.
        /// </summary>
        private Type FindTypeInAssemblies(string typeName)
        {
            try
            {
                // Search Assembly-CSharp first
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.GetName().Name == "Assembly-CSharp")
                    {
                        Type foundType = assembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
                        if (foundType != null) return foundType;
                    }
                }

                // Search all other assemblies
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        Type foundType = assembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
                        if (foundType != null) return foundType;
                    }
                    catch (Exception)
                    {
                        // Skip assemblies that can't be searched
                    }
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
