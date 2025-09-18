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
                // Get collectables from CollectableItemManager.masterList - this must work
                var collectableItemsEnumerable = GetCollectableItemsFromManager();
                if (collectableItemsEnumerable == null)
                {
                    throw new InvalidOperationException("Failed to get masterList from CollectableItemManager - this is required for proper collectable scanning");
                }

                int processedCount = 0;
                foreach (var collectableItem in collectableItemsEnumerable)
                {
                    if (collectableItem != null)
                    {
                        try
                        {
                            Type itemType = collectableItem.GetType();
                            string displayName = "";
                            string objectName = ((UnityEngine.Object)collectableItem).name.Replace("(Clone)", "").Trim();

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

                                    object result = getDisplayNameMethod.Invoke(collectableItem, new object[] { readSourceValue });
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

                            // Add if valid and not a placeholder/invalid name
                            if (!string.IsNullOrEmpty(displayName) && !IsInvalidDisplayName(displayName))
                            {
                                // Handle duplicate display names by making them unique
                                string uniqueDisplayName = displayName;
                                if (availableCollectables.Contains(displayName))
                                {
                                    // Make it unique by appending the object name in parentheses
                                    uniqueDisplayName = $"{displayName} ({objectName})";
                                }
                                
                                availableCollectables.Add(uniqueDisplayName);
                                displayNameToObjectName[uniqueDisplayName] = objectName;
                                processedCount++;
                            }
                        }
                        catch (Exception e)
                        {
                            logger.Log($"Error processing collectable {((UnityEngine.Object)collectableItem).name}: {e.Message}");
                        }
                    }
                }

                // If we didn't find collectables, something is wrong
                if (availableCollectables.Count == 0)
                {
                    throw new InvalidOperationException("No collectables found in CollectableItemManager.masterList - this indicates a problem with the collectable scanning");
                }

                // Sort alphabetically for better UX
                availableCollectables.Sort();
                collectablesScanned = true;
                logger.Log($"Collectable scanning complete using CollectableItemManager.masterList. Found {availableCollectables.Count} collectables from {processedCount} processed items");
            }
            catch (Exception e)
            {
                logger.Log($"CRITICAL ERROR: Collectable scanning failed: {e.Message}");
                logger.Log($"Stack trace: {e.StackTrace}");
                throw; // Re-throw the exception since we no longer have fallbacks
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

                // Get collectables from CollectableItemManager - this must work
                var collectableItems = GetCollectableItemsFromManager();
                if (collectableItems == null)
                {
                    throw new InvalidOperationException($"Failed to get masterList from CollectableItemManager for setting amount on {collectableDisplayName}");
                }

                foreach (var collectableItem in collectableItems)
                {
                    if (collectableItem == null) continue;

                    // Check if this is the collectable we're looking for
                    string objName = ((UnityEngine.Object)collectableItem).name.Replace("(Clone)", "").Trim();
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
                                                removeItemMethod.Invoke(managerInstance, new object[] { collectableItem, 9999 });

                                                // Then add the exact amount we want
                                                if (amount > 0)
                                                {
                                                    addItemMethod.Invoke(managerInstance, new object[] { collectableItem, amount });
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
                                                    publicAddItemMethod.Invoke(null, new object[] { collectableItem, amount });
                                                    onSuccess?.Invoke($"Added {amount} {collectableDisplayName} (note: adds to current amount)");
                                                    return true;
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            // Direct reflection fallback if manager approach fails
                            Type itemType = collectableItem.GetType();

                            // Try to directly set the amount field
                            FieldInfo currentAmountField = itemType.GetField("currentAmount", BindingFlags.NonPublic | BindingFlags.Instance);
                            if (currentAmountField == null)
                            {
                                currentAmountField = itemType.GetField("amount", BindingFlags.NonPublic | BindingFlags.Instance);
                            }

                            if (currentAmountField != null && currentAmountField.FieldType == typeof(int))
                            {
                                currentAmountField.SetValue(collectableItem, amount);
                                onSuccess?.Invoke($"Set {collectableDisplayName} to {amount} (direct field access)");
                                return true;
                            }

                            // Try property
                            PropertyInfo amountProperty = itemType.GetProperty("amount", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (amountProperty != null && amountProperty.PropertyType == typeof(int) && amountProperty.CanWrite)
                            {
                                amountProperty.SetValue(collectableItem, amount);
                                onSuccess?.Invoke($"Set {collectableDisplayName} to {amount} (direct property access)");
                                return true;
                            }
                        }
                        catch (Exception e)
                        {
                            logger.Log($"Error setting amount for {((UnityEngine.Object)collectableItem).name}: {e.Message}");
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
        /// Adds a specific amount of a collectable using the Collect method.
        /// </summary>
        /// <param name="collectableDisplayName">Display name of the collectable</param>
        /// <param name="amount">Amount to collect</param>
        /// <param name="onSuccess">Callback for success messages</param>
        /// <param name="onError">Callback for error messages</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool AddCollectableAmount(string collectableDisplayName, int amount, Action<string> onSuccess = null, Action<string> onError = null)
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

                // Get collectables from CollectableItemManager - this must work
                var collectableItems = GetCollectableItemsFromManager();
                if (collectableItems == null)
                {
                    throw new InvalidOperationException($"Failed to get masterList from CollectableItemManager for collecting {collectableDisplayName}");
                }

                foreach (var collectableItem in collectableItems)
                {
                    if (collectableItem == null) continue;

                    // Check if this is the collectable we're looking for
                    string objName = ((UnityEngine.Object)collectableItem).name.Replace("(Clone)", "").Trim();
                    if (objName == actualObjectName)
                    {
                        try
                        {
                            Type itemType = collectableItem.GetType();
                            string typeName = itemType.Name;

                            // Look for Collect method with signature: Collect(int, bool)
                            MethodInfo collectMethod = itemType.GetMethod("Collect", 
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                                null,
                                new Type[] { typeof(int), typeof(bool) },
                                null);

                            if (collectMethod != null)
                            {
                                logger.Log($"Calling Collect({amount}, true) on {typeName} instance: {objName}");
                                
                                // Call Collect(int amount, bool showCounter) - passing true to show counter
                                collectMethod.Invoke(collectableItem, new object[] { amount, true });
                                
                                onSuccess?.Invoke($"Collected {amount} {collectableDisplayName} (type: {typeName})");
                                return true;
                            }
                            else
                            {
                                // Try to find any Collect method for debugging
                                MethodInfo[] allCollectMethods = itemType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                    .Where(m => m.Name == "Collect").ToArray();
                                
                                string methodInfo = allCollectMethods.Length > 0 
                                    ? $"Found {allCollectMethods.Length} Collect methods: {string.Join(", ", allCollectMethods.Select(m => $"{m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})"))})"
                                    : "No Collect methods found";
                                
                                onError?.Invoke($"Collect(int, bool) method not found for {collectableDisplayName} (type: {typeName}). {methodInfo}");
                                logger.Log($"Collect method not found for {typeName}: {methodInfo}");
                                return false;
                            }
                        }
                        catch (Exception e)
                        {
                            logger.Log($"Error collecting {((UnityEngine.Object)collectableItem).name}: {e.Message}");
                            onError?.Invoke($"Error collecting {collectableDisplayName}: {e.Message}");
                            return false;
                        }
                    }
                }

                onError?.Invoke($"Could not find {collectableDisplayName} for collecting");
                return false;
            }
            catch (Exception e)
            {
                logger.Log($"CRITICAL ERROR: Failed to collect {collectableDisplayName}: {e.Message}");
                onError?.Invoke($"Critical error collecting {collectableDisplayName}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Takes (removes) a specific amount of a collectable using the Take method.
        /// </summary>
        /// <param name="collectableDisplayName">Display name of the collectable</param>
        /// <param name="amount">Amount to take</param>
        /// <param name="onSuccess">Callback for success messages</param>
        /// <param name="onError">Callback for error messages</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool TakeCollectableAmount(string collectableDisplayName, int amount, Action<string> onSuccess = null, Action<string> onError = null)
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

                // Get collectables from CollectableItemManager - this must work
                var collectableItems = GetCollectableItemsFromManager();
                if (collectableItems == null)
                {
                    throw new InvalidOperationException($"Failed to get masterList from CollectableItemManager for taking {collectableDisplayName}");
                }

                foreach (var collectableItem in collectableItems)
                {
                    if (collectableItem == null) continue;

                    // Check if this is the collectable we're looking for
                    string objName = ((UnityEngine.Object)collectableItem).name.Replace("(Clone)", "").Trim();
                    if (objName == actualObjectName)
                    {
                        try
                        {
                            Type itemType = collectableItem.GetType();
                            string typeName = itemType.Name;

                            // Look for Take method with specific signature: Take(int, bool)
                            MethodInfo takeMethod = itemType.GetMethod("Take", 
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                                null,
                                new Type[] { typeof(int), typeof(bool) },
                                null);

                            if (takeMethod != null)
                            {
                                logger.Log($"Calling Take({amount}, true) on {typeName} instance: {objName}");
                                
                                // Call Take(int amount, bool showCounter) - passing true to show counter
                                takeMethod.Invoke(collectableItem, new object[] { amount, true });
                                
                                onSuccess?.Invoke($"Took {amount} {collectableDisplayName} (type: {typeName})");
                                return true;
                            }
                            else
                            {
                                // Try to find any Take method for debugging
                                MethodInfo[] allTakeMethods = itemType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                    .Where(m => m.Name == "Take").ToArray();
                                
                                string methodInfo = allTakeMethods.Length > 0 
                                    ? $"Found {allTakeMethods.Length} Take methods: {string.Join(", ", allTakeMethods.Select(m => $"{m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})"))})"
                                    : "No Take methods found";
                                
                                onError?.Invoke($"Take(int, bool) method not found for {collectableDisplayName} (type: {typeName}). {methodInfo}");
                                logger.Log($"Take method not found for {typeName}: {methodInfo}");
                                return false;
                            }
                        }
                        catch (Exception e)
                        {
                            logger.Log($"Error taking {((UnityEngine.Object)collectableItem).name}: {e.Message}");
                            onError?.Invoke($"Error taking {collectableDisplayName}: {e.Message}");
                            return false;
                        }
                    }
                }

                onError?.Invoke($"Could not find {collectableDisplayName} for taking");
                return false;
            }
            catch (Exception e)
            {
                logger.Log($"CRITICAL ERROR: Failed to take {collectableDisplayName}: {e.Message}");
                onError?.Invoke($"Critical error taking {collectableDisplayName}: {e.Message}");
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

                // Get collectables from CollectableItemManager - this must work
                var collectableItems = GetCollectableItemsFromManager();
                if (collectableItems == null)
                {
                    throw new InvalidOperationException("Failed to get masterList from CollectableItemManager for maxing collectables");
                }

                int maxedCount = 0;

                foreach (var collectableItem in collectableItems)
                {
                    if (collectableItem == null) continue;

                    // Check if it's a CollectableItem (includes CollectableItemBasic subclasses)
                    try
                    {
                        string objName = ((UnityEngine.Object)collectableItem).name.Replace("(Clone)", "").Trim();
                        Type itemType = collectableItem.GetType();

                        // Special case: Growstone is a unique regenerating item - only give 0, not 99
                        // essentially skipping this item. Users can add manually if they want.
                        int targetAmount = (objName == "Growstone") ? 0 : 99;

                        // Look for AddAmount method (protected virtual, so need NonPublic flag)
                        MethodInfo addAmountMethod = itemType.GetMethod("AddAmount", BindingFlags.NonPublic | BindingFlags.Instance);

                        if (addAmountMethod != null)
                        {
                            addAmountMethod.Invoke(collectableItem, new object[] { targetAmount });
                            logger.Log($"Added {targetAmount}x {objName} {(objName == "Growstone" ? "(Manual Add)" : "")}");
                            maxedCount++;
                        }
                        else
                        {
                            logger.Log($"No AddAmount method found on {objName} (type: {itemType.Name})");
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Log($"Error maxing {((UnityEngine.Object)collectableItem).name}: {e.Message}");
                    }
                }

                logger.Log($"Collectable Max: Added items to {maxedCount} collectables (Growstone: 1x, others: 99x)");
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
        /// Helper method to get collectableItems from CollectableItemManager.masterList.
        /// </summary>
        private System.Collections.IEnumerable GetCollectableItemsFromManager()
        {
            try
            {
                // Find CollectableItemManager and get its masterList
                Type collectableItemManagerType = FindTypeInAssemblies("CollectableItemManager");
                if (collectableItemManagerType == null)
                {
                    logger.Log("CollectableItemManager type not found in assemblies");
                    return null;
                }

                // Try multiple approaches to get the instance
                object collectableItemManagerInstance = null;

                // Try static instance property
                PropertyInfo instanceProperty = collectableItemManagerType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
                if (instanceProperty != null)
                {
                    collectableItemManagerInstance = instanceProperty.GetValue(null);
                }

                if (collectableItemManagerInstance == null)
                {
                    instanceProperty = collectableItemManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
                    if (instanceProperty != null)
                    {
                        collectableItemManagerInstance = instanceProperty.GetValue(null);
                    }
                }

                if (collectableItemManagerInstance == null)
                {
                    FieldInfo instanceField = collectableItemManagerType.GetField("instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
                    if (instanceField != null)
                    {
                        collectableItemManagerInstance = instanceField.GetValue(null);
                    }
                }

                if (collectableItemManagerInstance == null)
                {
                    var findObjectMethod = typeof(UnityEngine.Object).GetMethod("FindObjectOfType", new Type[0]);
                    if (findObjectMethod != null)
                    {
                        var genericMethod = findObjectMethod.MakeGenericMethod(collectableItemManagerType);
                        collectableItemManagerInstance = genericMethod.Invoke(null, null);
                    }
                }

                if (collectableItemManagerInstance == null)
                {
                    var findObjectsMethod = typeof(Resources).GetMethod("FindObjectsOfTypeAll", new Type[] { typeof(Type) });
                    if (findObjectsMethod != null)
                    {
                        var objects = (UnityEngine.Object[])findObjectsMethod.Invoke(null, new object[] { collectableItemManagerType });
                        if (objects != null && objects.Length > 0)
                        {
                            collectableItemManagerInstance = objects[0];
                        }
                    }
                }

                if (collectableItemManagerInstance == null)
                {
                    logger.Log("Could not find CollectableItemManager instance using any method");
                    return null;
                }

                // Get the masterList field
                FieldInfo masterListField = collectableItemManagerType.GetField("masterList", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (masterListField != null)
                {
                    object masterList = masterListField.GetValue(collectableItemManagerInstance);
                    
                    if (masterList != null)
                    {
                        // masterList should be a List<CollectableItem> or similar collection
                        return masterList as System.Collections.IEnumerable;
                    }
                }

                logger.Log("Failed to access CollectableItemManager.masterList");
                return null;
            }
            catch (Exception e)
            {
                logger.Log($"Error getting masterList from CollectableItemManager: {e.Message}");
                logger.Log($"Stack trace: {e.StackTrace}");
                return null;
            }
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
