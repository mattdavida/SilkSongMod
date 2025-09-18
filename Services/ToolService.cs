using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using SilkSong.Interfaces;

namespace SilkSong.Services
{
    /// <summary>
    /// Service for managing tool unlock operations and tool-related functionality.
    /// Handles crests, tools, items, fast travel, maps, and specific tool operations.
    /// </summary>
    public class ToolService
    {
        private readonly IModLogger logger;

        // Tool scanning and management state
        private bool toolsScanned = false;
        private List<string> availableTools = new List<string>();
        private string[] toolNames = new string[0];
        private List<string> availableSkills = new List<string>();
        private List<string> availableBasicTools = new List<string>();
        private Dictionary<string, string> toolDisplayNameToObjectName = new Dictionary<string, string>();
        
        // Cache for ammo checking to prevent repeated scanning
        private int lastAmmoCheckToolIndex = -1;
        private bool lastAmmoCheckResult = false;
        private int lastStorageCheckToolIndex = -1;
        private int lastStorageCheckResult = 0;

        public ToolService(IModLogger logger)
        {
            this.logger = logger;
        }

        #region Public Properties

        /// <summary>
        /// Gets whether tools have been scanned.
        /// </summary>
        public bool ToolsScanned => toolsScanned;

        /// <summary>
        /// Gets the available tool names.
        /// </summary>
        public string[] ToolNames => toolNames;

        /// <summary>
        /// Gets the available skills list.
        /// </summary>
        public List<string> AvailableSkills => availableSkills;

        /// <summary>
        /// Gets the available basic tools list.
        /// </summary>
        public List<string> AvailableBasicTools => availableBasicTools;

        #endregion

        #region Tool Scanning and Management

        /// <summary>
        /// Scans for available tools using ToolItemManager.toolItems.
        /// </summary>
        public void ScanTools()
        {
            if (toolsScanned) return;
            availableTools.Clear();
            availableSkills.Clear();
            availableBasicTools.Clear();
            toolDisplayNameToObjectName.Clear();

            try
            {
                // Get tools from ToolItemManager - this must work
                var toolItemsEnumerable = GetToolItemsFromManager();
                if (toolItemsEnumerable == null)
                {
                    throw new InvalidOperationException("Failed to get toolItems from ToolItemManager - this is required for proper tool scanning");
                }

                int processedCount = 0;
                foreach (var toolItem in toolItemsEnumerable)
                {
                    if (toolItem != null)
                    {
                        try
                        {
                            string typeName = toolItem.GetType().Name;
                            string objectName = ((UnityEngine.Object)toolItem).name;
                            string displayName = GetToolDisplayName(toolItem as UnityEngine.Object);
                            
                            if (!string.IsNullOrEmpty(displayName) && !IsInvalidDisplayName(displayName))
                            {
                                // Handle duplicate display names by making them unique
                                string uniqueDisplayName = displayName;
                                if (availableTools.Contains(displayName))
                                {
                                    // Make it unique by appending the object name in parentheses
                                    uniqueDisplayName = $"{displayName} ({objectName})";
                                }
                                
                                availableTools.Add(uniqueDisplayName);
                                toolDisplayNameToObjectName[uniqueDisplayName] = objectName;

                                // Categorize by type - include ALL tool types, not just Basic and Skill
                                if (typeName == "ToolItemSkill")
                                {
                                    availableSkills.Add(uniqueDisplayName);
                                }
                                else if (typeName == "ToolItemBasic")
                                {
                                    availableBasicTools.Add(uniqueDisplayName);
                                }
                                else
                                {
                                    // Other tool types (ToolItemLerpStates, ToolItemToggleState, etc.)
                                    availableBasicTools.Add(uniqueDisplayName);
                                }
                                
                                processedCount++;
                            }
                            // Skip tools with empty or invalid display names
                        }
                        catch (Exception e)
                        {
                            logger.Log($"Error processing tool item {((UnityEngine.Object)toolItem).name}: {e.Message}");
                        }
                    }
                }

                // If we didn't find tools, something is wrong
                if (availableTools.Count == 0)
                {
                    throw new InvalidOperationException("No tools found in ToolItemManager.toolItems - this indicates a problem with the tool scanning");
                }

                // Sort alphabetically for better UX
                availableTools.Sort();
                availableSkills.Sort();
                availableBasicTools.Sort();
                toolNames = availableTools.ToArray();

                toolsScanned = true;
                logger.Log($"Tool scanning complete using ToolItemManager.toolItems. Found {availableTools.Count} tools ({availableSkills.Count} skills, {availableBasicTools.Count} basic tools) from {processedCount} processed items");
            }
            catch (Exception e)
            {
                logger.Log($"CRITICAL ERROR: Tool scanning failed: {e.Message}");
                logger.Log($"Stack trace: {e.StackTrace}");
                throw; // Re-throw the exception since we no longer have fallbacks
            }
        }

        /// <summary>
        /// Gets the display name for a tool object.
        /// </summary>
        public string GetToolDisplayName(UnityEngine.Object toolObj)
        {
            if (toolObj == null) return null;

            try
            {
                Type toolType = toolObj.GetType();

                // Try DisplayName property first (might be a property on ToolItemSkill)
                PropertyInfo displayNameProperty = toolType.GetProperty("DisplayName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (displayNameProperty != null)
                {
                    try
                    {
                        object propertyValue = displayNameProperty.GetValue(toolObj);
                        if (propertyValue != null && !string.IsNullOrEmpty(propertyValue.ToString()))
                        {
                            return propertyValue.ToString();
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Log($"Error getting DisplayName property: {e.Message}");
                    }
                }

                // Try displayName field (LocalisedString)
                FieldInfo displayNameField = toolType.GetField("displayName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (displayNameField != null)
                {
                    object displayNameValue = displayNameField.GetValue(toolObj);
                    if (displayNameValue != null)
                    {
                        // Try GetDisplayName method if it's a LocalisedString
                        Type displayNameType = displayNameValue.GetType();
                        MethodInfo getDisplayNameMethod = displayNameType.GetMethod("GetDisplayName", BindingFlags.Public | BindingFlags.Instance);
                        if (getDisplayNameMethod != null)
                        {
                            try
                            {
                                // Try with ReadSource parameter
                                object result = getDisplayNameMethod.Invoke(displayNameValue, new object[] { 0 });
                                if (result != null && !string.IsNullOrEmpty(result.ToString()))
                                {
                                    return result.ToString();
                                }
                            }
                            catch
                            {
                                // Try without parameters
                                try
                                {
                                    object result = getDisplayNameMethod.Invoke(displayNameValue, null);
                                    if (result != null && !string.IsNullOrEmpty(result.ToString()))
                                    {
                                        return result.ToString();
                                    }
                                }
                                catch { }
                            }
                        }

                        // Fallback to ToString
                        string displayStr = displayNameValue.ToString();
                        if (!string.IsNullOrEmpty(displayStr) && displayStr != displayNameType.Name)
                        {
                            return displayStr;
                        }
                    }
                }

                // Fallback to name field
                FieldInfo nameField = toolType.GetField("name", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (nameField != null)
                {
                    object nameValue = nameField.GetValue(toolObj);
                    if (nameValue != null)
                    {
                        return nameValue.ToString();
                    }
                }

                // Last resort: use object name
                return toolObj.name;
            }
            catch (Exception)
            {
                return toolObj.name;
            }
        }

        /// <summary>
        /// Checks if a display name is invalid (placeholder or special characters).
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
        /// Filters tools based on search criteria and skill-only setting.
        /// </summary>
        public string[] FilterTools(string searchFilter, bool showSkillsOnly)
        {
            List<string> sourceList = new List<string>();

            if (showSkillsOnly)
            {
                // Get tools from ToolItemManager - this must work
                var toolItems = GetToolItemsFromManager();
                if (toolItems == null)
                {
                    throw new InvalidOperationException("Failed to get toolItems from ToolItemManager for filtering");
                }
                
                foreach (var toolItem in toolItems)
                {
                    if (toolItem != null)
                    {
                        string typeName = toolItem.GetType().Name;
                        // Only include ToolItemSkill for skills-only filter
                        if (typeName == "ToolItemSkill")
                        {
                            try
                            {
                                string displayName = GetToolDisplayName(toolItem as UnityEngine.Object);
                                if (!string.IsNullOrEmpty(displayName) && !IsInvalidDisplayName(displayName))
                                {
                                    if (!sourceList.Contains(displayName))
                                    {
                                        sourceList.Add(displayName);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                logger.Log($"Error processing skill tool for filtering: {e.Message}");
                            }
                        }
                    }
                }
                sourceList.Sort();
            }
            else
            {
                // Use all tools
                sourceList = availableTools;
            }

            if (string.IsNullOrEmpty(searchFilter))
            {
                return sourceList.ToArray();
            }
            else
            {
                return sourceList.Where(name =>
                    name.ToLower().Contains(searchFilter.ToLower())).ToArray();
            }
        }

        /// <summary>
        /// Unlocks a specific tool by display name using ToolItemManager.toolItems.
        /// </summary>
        public bool UnlockSpecificTool(string toolDisplayName)
        {
            try
            {
                // Get tools from ToolItemManager - this must work
                var toolItems = GetToolItemsFromManager();
                if (toolItems == null)
                {
                    throw new InvalidOperationException($"Failed to get toolItems from ToolItemManager for unlocking {toolDisplayName}");
                }

                // Get the actual object name from our mapping (for cases like "Silkshot (WebShot Architect)" -> "WebShot Architect")
                string targetObjectName = null;
                if (toolDisplayNameToObjectName.ContainsKey(toolDisplayName))
                {
                    targetObjectName = toolDisplayNameToObjectName[toolDisplayName];
                }
                
                foreach (var toolItem in toolItems)
                {
                    if (toolItem != null)
                    {
                        try
                        {
                            string objectName = ((UnityEngine.Object)toolItem).name;
                            string objectDisplayName = GetToolDisplayName(toolItem as UnityEngine.Object);
                            
                            // Match either by display name directly OR by object name (for unique variants)
                            bool isMatch = (objectDisplayName == toolDisplayName) || 
                                          (targetObjectName != null && objectName == targetObjectName);
                            
                            if (isMatch)
                            {
                                // Found the matching tool
                                Type toolType = toolItem.GetType();
                                MethodInfo unlockMethod = toolType.GetMethod("Unlock");

                                if (unlockMethod != null)
                                {
                                    // Find PopupFlags enum in nested types
                                    Type popupFlagsType = GetPopupFlagsType(toolType);

                                    if (popupFlagsType != null)
                                    {
                                        object itemGetFlag = Enum.Parse(popupFlagsType, "ItemGet");
                                        unlockMethod.Invoke(toolItem, new object[] { null, itemGetFlag });
                                    }
                                    else
                                    {
                                        unlockMethod.Invoke(toolItem, new object[] { null, null });
                                    }

                                    logger.Log($"Unlocked {toolDisplayName} using ToolItemManager!");
                                    return true;
                                }
                                else
                                {
                                    logger.Log($"No Unlock method found on {toolDisplayName}");
                                    return false;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            logger.Log($"Error processing tool item {((UnityEngine.Object)toolItem).name}: {e.Message}");
                        }
                    }
                }

                logger.Log($"Could not find tool: {toolDisplayName}");
                return false;
            }
            catch (Exception e)
            {
                logger.Log($"CRITICAL ERROR: Failed to unlock {toolDisplayName}: {e.Message}");
                throw; // Re-throw since we no longer have fallbacks
            }
        }

        /// <summary>
        /// Sets tool storage amount for a specific tool using ToolItemManager.toolItems.
        /// </summary>
        public bool SetToolStorage(string toolDisplayName, int amount)
        {
            try
            {
                // Try to get tools from ToolItemManager first
                var toolItems = GetToolItemsFromManager();
                
                if (toolItems != null)
                {
                    foreach (var toolItem in toolItems)
                    {
                        if (toolItem != null)
                        {
                            try
                            {
                                string objectName = ((UnityEngine.Object)toolItem).name;
                                string objectDisplayName = GetToolDisplayName(toolItem as UnityEngine.Object);

                                // Get the actual object name from our mapping (for cases like "Silkshot (WebShot Architect)" -> "WebShot Architect")
                                string targetObjectName = null;
                                if (toolDisplayNameToObjectName.ContainsKey(toolDisplayName))
                                {
                                    targetObjectName = toolDisplayNameToObjectName[toolDisplayName];
                                }

                                // Match either by display name directly OR by object name (for unique variants)
                                bool isMatch = (objectDisplayName == toolDisplayName) || 
                                              (targetObjectName != null && objectName == targetObjectName);

                                if (isMatch)
                                {
                                    // Find baseStorageAmount field in the inheritance hierarchy
                                    FieldInfo baseStorageField = null;
                                    Type currentType = toolItem.GetType();

                                    while (currentType != null && baseStorageField == null)
                                    {
                                        baseStorageField = currentType.GetField("baseStorageAmount",
                                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                                        currentType = currentType.BaseType;
                                    }

                                    if (baseStorageField != null)
                                    {
                                        // Check if this tool actually has storage (> 0) before allowing modification
                                        int currentAmount = (int)baseStorageField.GetValue(toolItem);

                                        if (currentAmount > 0)
                                        {
                                            baseStorageField.SetValue(toolItem, amount);
                                            logger.Log($"Set {toolDisplayName} storage to {amount} using ToolItemManager!");
                                            return true;
                                        }
                                        else
                                        {
                                            logger.Log($"{toolDisplayName} doesn't use storage (action tool)");
                                            return false;
                                        }
                                    }
                                    else
                                    {
                                        logger.Log($"{toolDisplayName} has no storage field");
                                        return false;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                logger.Log($"Error processing tool item {((UnityEngine.Object)toolItem).name}: {e.Message}");
                            }
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Failed to get toolItems from ToolItemManager for setting storage on {toolDisplayName}");
                }

                logger.Log($"Could not find tool: {toolDisplayName}");
                return false;
            }
            catch (Exception e)
            {
                logger.Log($"CRITICAL ERROR: Failed to set storage for {toolDisplayName}: {e.Message}");
                throw; // Re-throw since we no longer have fallbacks
            }
        }

        /// <summary>
        /// Refills ammo for a specific tool using ToolItemManager.toolItems.
        /// </summary>
        public bool RefillToolAmmo(string toolDisplayName)
        {
            try
            {
                // Get tools from ToolItemManager - this must work
                var toolItems = GetToolItemsFromManager();
                if (toolItems == null)
                {
                    throw new InvalidOperationException($"Failed to get toolItems from ToolItemManager for refilling ammo on {toolDisplayName}");
                }

                foreach (var toolItem in toolItems)
                {
                    if (toolItem != null)
                    {
                        try
                        {
                            string objectName = ((UnityEngine.Object)toolItem).name;
                            string objectDisplayName = GetToolDisplayName(toolItem as UnityEngine.Object);

                            // Get the actual object name from our mapping (for cases like "Silkshot (WebShot Architect)" -> "WebShot Architect")
                            string targetObjectName = null;
                            if (toolDisplayNameToObjectName.ContainsKey(toolDisplayName))
                            {
                                targetObjectName = toolDisplayNameToObjectName[toolDisplayName];
                            }

                            // Match either by display name directly OR by object name (for unique variants)
                            bool isMatch = (objectDisplayName == toolDisplayName) || 
                                          (targetObjectName != null && objectName == targetObjectName);

                            if (isMatch)
                            {
                                // Check if this tool has storage before trying to refill
                                FieldInfo baseStorageField = null;
                                Type currentType = toolItem.GetType();

                                while (currentType != null && baseStorageField == null)
                                {
                                    baseStorageField = currentType.GetField("baseStorageAmount",
                                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                                    currentType = currentType.BaseType;
                                }

                                if (baseStorageField != null)
                                {
                                    int currentAmount = (int)baseStorageField.GetValue(toolItem);

                                    if (currentAmount > 0)
                                    {
                                        // Tool has storage, use CollectFree to refill ammo
                                        Type toolType = toolItem.GetType();
                                        MethodInfo collectFreeMethod = toolType.GetMethod("CollectFree", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                                        if (collectFreeMethod != null)
                                        {
                                            collectFreeMethod.Invoke(toolItem, new object[] { 9999 });
                                            logger.Log($"Refilled {toolDisplayName} ammo using ToolItemManager!");
                                            return true;
                                        }
                                        else
                                        {
                                            logger.Log($"CollectFree method not found for {toolDisplayName}");
                                            return false;
                                        }
                                    }
                                    else
                                    {
                                        logger.Log($"{toolDisplayName} doesn't use ammo (action tool)");
                                        return false;
                                    }
                                }
                                else
                                {
                                    logger.Log($"{toolDisplayName} has no storage field");
                                    return false;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            logger.Log($"Error processing tool item {((UnityEngine.Object)toolItem).name}: {e.Message}");
                        }
                    }
                }

                logger.Log($"Could not find tool: {toolDisplayName}");
                return false;
            }
            catch (Exception e)
            {
                logger.Log($"CRITICAL ERROR: Failed to refill ammo for {toolDisplayName}: {e.Message}");
                throw; // Re-throw since we no longer have fallbacks
            }
        }

        /// <summary>
        /// Checks if a tool at the specified index uses ammo using ToolItemManager.toolItems.
        /// </summary>
        public bool SelectedToolUsesAmmo(int selectedToolIndex)
        {
            if (selectedToolIndex >= toolNames.Length) return false;

            // Use cache to avoid repeated scanning
            if (lastAmmoCheckToolIndex == selectedToolIndex)
            {
                return lastAmmoCheckResult;
            }

            try
            {
                string selectedTool = toolNames[selectedToolIndex];

                // Get tools from ToolItemManager - this must work
                var toolItems = GetToolItemsFromManager();
                if (toolItems == null)
                {
                    throw new InvalidOperationException($"Failed to get toolItems from ToolItemManager for ammo checking on {selectedTool}");
                }

                foreach (var toolItem in toolItems)
                {
                    if (toolItem != null)
                    {
                        string objectName = ((UnityEngine.Object)toolItem).name;
                        string objectDisplayName = GetToolDisplayName(toolItem as UnityEngine.Object);

                        // Get the actual object name from our mapping (for cases like "Silkshot (WebShot Architect)" -> "WebShot Architect")
                        string targetObjectName = null;
                        if (toolDisplayNameToObjectName.ContainsKey(selectedTool))
                        {
                            targetObjectName = toolDisplayNameToObjectName[selectedTool];
                        }

                        // Match either by display name directly OR by object name (for unique variants)
                        bool isMatch = (objectDisplayName == selectedTool) || 
                                      (targetObjectName != null && objectName == targetObjectName);

                        if (isMatch)
                        {
                            string typeName = toolItem.GetType().Name;
                            
                            // ToolItemSkill objects don't use ammo (they're skills, not consumables)
                            if (typeName == "ToolItemSkill")
                            {
                                lastAmmoCheckToolIndex = selectedToolIndex;
                                lastAmmoCheckResult = false;
                                return false;
                            }
                            
                            // ToolItemBasic objects might use ammo if they have storage > 0
                            if (typeName == "ToolItemBasic")
                            {
                                // Check if tool has baseStorageAmount > 0
                                FieldInfo baseStorageField = null;
                                Type currentType = toolItem.GetType();

                                while (currentType != null && baseStorageField == null)
                                {
                                    baseStorageField = currentType.GetField("baseStorageAmount",
                                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                                    currentType = currentType.BaseType;
                                }

                                if (baseStorageField != null)
                                {
                                    int currentAmount = (int)baseStorageField.GetValue(toolItem);
                                    bool hasAmmo = currentAmount > 0;
                                    lastAmmoCheckToolIndex = selectedToolIndex;
                                    lastAmmoCheckResult = hasAmmo;
                                    return hasAmmo; // Tool uses ammo if storage > 0
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.Log($"Error checking ammo for tool index {selectedToolIndex}: {e.Message}");
            }

            // Cache the result
            lastAmmoCheckToolIndex = selectedToolIndex;
            lastAmmoCheckResult = false;
            return false;
        }

        /// <summary>
        /// Gets the current storage amount for a tool at the specified index using ToolItemManager.toolItems.
        /// </summary>
        public int GetSelectedToolCurrentStorage(int selectedToolIndex)
        {
            if (selectedToolIndex >= toolNames.Length) return 0;

            // Use cache to avoid repeated scanning
            if (lastStorageCheckToolIndex == selectedToolIndex)
            {
                return lastStorageCheckResult;
            }

            try
            {
                string selectedTool = toolNames[selectedToolIndex];

                // Get tools from ToolItemManager - this must work
                var toolItems = GetToolItemsFromManager();
                if (toolItems == null)
                {
                    throw new InvalidOperationException($"Failed to get toolItems from ToolItemManager for storage checking on {selectedTool}");
                }

                foreach (var toolItem in toolItems)
                {
                    if (toolItem != null)
                    {
                        string objectName = ((UnityEngine.Object)toolItem).name;
                        string objectDisplayName = GetToolDisplayName(toolItem as UnityEngine.Object);

                        // Get the actual object name from our mapping (for cases like "Silkshot (WebShot Architect)" -> "WebShot Architect")
                        string targetObjectName = null;
                        if (toolDisplayNameToObjectName.ContainsKey(selectedTool))
                        {
                            targetObjectName = toolDisplayNameToObjectName[selectedTool];
                        }

                        // Match either by display name directly OR by object name (for unique variants)
                        bool isMatch = (objectDisplayName == selectedTool) || 
                                      (targetObjectName != null && objectName == targetObjectName);

                        if (isMatch)
                        {
                            // Get current baseStorageAmount value
                            FieldInfo baseStorageField = null;
                            Type currentType = toolItem.GetType();

                            while (currentType != null && baseStorageField == null)
                            {
                                baseStorageField = currentType.GetField("baseStorageAmount",
                                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                                currentType = currentType.BaseType;
                            }

                            if (baseStorageField != null)
                            {
                                int result = (int)baseStorageField.GetValue(toolItem);
                                lastStorageCheckToolIndex = selectedToolIndex;
                                lastStorageCheckResult = result;
                                return result;
                            }
                            else
                            {
                                // Tool without baseStorageAmount field = no storage (likely ToolItemSkill)
                                lastStorageCheckToolIndex = selectedToolIndex;
                                lastStorageCheckResult = 0;
                                return 0;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.Log($"Error getting storage for tool index {selectedToolIndex}: {e.Message}");
            }

            lastStorageCheckToolIndex = selectedToolIndex;
            lastStorageCheckResult = 0;
            return 0;
        }

        /// <summary>
        /// Research method for analyzing ToolItemBasic objects.
        /// </summary>
        public void ResearchToolItemBasics()
        {
            try
            {
                logger.Log("=== RESEARCHING TOOLITEMBASIC OBJECTS ===");
                
                // Find all ToolItemBasic objects
                UnityEngine.Object[] allObjects = Resources.FindObjectsOfTypeAll(typeof(ToolItemBasic));
                logger.Log($"Found {allObjects.Length} ToolItemBasic objects total");

                List<string> zeroStorageTools = new List<string>();
                List<string> nonZeroStorageTools = new List<string>();
                
                foreach (UnityEngine.Object obj in allObjects)
                {
                    if (obj == null) continue;

                    try
                    {
                        string displayName = GetToolDisplayName(obj);
                        string objectName = obj.name.Replace("(Clone)", "").Trim();
                        
                        // Get baseStorageAmount field
                        FieldInfo baseStorageField = null;
                        Type currentType = obj.GetType();

                        while (currentType != null && baseStorageField == null)
                        {
                            baseStorageField = currentType.GetField("baseStorageAmount",
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                            currentType = currentType.BaseType;
                        }

                        if (baseStorageField != null)
                        {
                            int storageAmount = (int)baseStorageField.GetValue(obj);
                            
                            if (storageAmount == 0)
                            {
                                zeroStorageTools.Add($"{displayName} ({objectName})");
                                logger.Log($"  ZERO STORAGE: {displayName} ({objectName})");
                            }
                            else
                            {
                                nonZeroStorageTools.Add($"{displayName} ({objectName}) - Storage: {storageAmount}");
                            }
                        }
                        else
                        {
                            logger.Log($"  NO STORAGE FIELD: {displayName} ({objectName})");
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Log($"Error processing {obj.name}: {e.Message}");
                    }
                }

                logger.Log($"\n=== RESEARCH SUMMARY ===");
                logger.Log($"Total ToolItemBasic objects: {allObjects.Length}");
                logger.Log($"Zero storage (always-active candidates): {zeroStorageTools.Count}");
                logger.Log($"Non-zero storage (consumable tools): {nonZeroStorageTools.Count}");

                logger.Log($"\n=== ZERO STORAGE TOOLS (Always-Active Candidates) ===");
                foreach (string tool in zeroStorageTools)
                {
                    logger.Log($"  • {tool}");
                }

                if (zeroStorageTools.Count <= 20)
                {
                    logger.Log($"\n✅ Good news: {zeroStorageTools.Count} zero-storage tools fits within the 20-slot SetExtraEquippedTool limit!");
                }
                else
                {
                    logger.Log($"\n⚠️  Warning: {zeroStorageTools.Count} zero-storage tools exceeds the 20-slot limit. We'll need to prioritize.");
                }
            }
            catch (Exception e)
            {
                logger.Log($"Error in ResearchToolItemBasics: {e.Message}");
            }
        }

        #endregion

        #region Tool Operations

        /// <summary>
        /// Unlocks all crests using the master ToolCrestList.UnlockAll method.
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool UnlockAllCrests()
        {
            try
        {
                logger.Log("=== UNLOCKING ALL CRESTS ===");

                // Find all ToolCrestList objects using Resources.FindObjectsOfTypeAll
                UnityEngine.Object[] allObjects = Resources.FindObjectsOfTypeAll(typeof(ToolCrestList));
                logger.Log($"Found {allObjects.Length} ToolCrestList objects");

                foreach (UnityEngine.Object obj in allObjects)
                {
                    if (obj == null) continue;

                    Type type = obj.GetType();

                    if (type.Name == "ToolCrestList")
                    {
                        MethodInfo unlockAllMethod = type.GetMethod("UnlockAll");

                        if (unlockAllMethod != null)
                        {
                            unlockAllMethod.Invoke(obj, null);
                            logger.Log($"Called ToolCrestList.UnlockAll() on {obj.name} - All crests unlocked!");
                            return true;
                        }
                        else
                        {
                            logger.Log("ToolCrestList found but UnlockAll method not found");
                        }
                    }
                }

                logger.Log("ToolCrestList component not found");
                return false;
            }
            catch (Exception e)
            {
                logger.Log($"Error unlocking all crests: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Unlocks all tools (ToolItemSkill objects).
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool UnlockAllTools()
        {
            try
            {
                logger.Log("=== UNLOCKING ALL TOOLS ===");

                // Find all ToolItemSkill objects
                UnityEngine.Object[] allObjects = Resources.FindObjectsOfTypeAll(typeof(ToolItemSkill));
                logger.Log($"Found {allObjects.Length} ToolItemSkill objects");

                int unlockedCount = 0;

                foreach (UnityEngine.Object obj in allObjects)
                {
                    // Cast to the specific type
                    if (obj.GetType().Name == "ToolItemSkill")
                    {
                        try
                        {
                            Type toolType = obj.GetType();
                            MethodInfo unlockMethod = toolType.GetMethod("Unlock");

                            if (unlockMethod != null)
                            {
                                // Find PopupFlags enum in nested types
                                Type popupFlagsType = GetPopupFlagsType(toolType);

                                if (popupFlagsType != null)
                                {
                                    object itemGetFlag = Enum.Parse(popupFlagsType, "ItemGet");
                                    unlockMethod.Invoke(obj, new object[] { null, itemGetFlag });
                                }
                                else
                                {
                                    unlockMethod.Invoke(obj, new object[] { null, null });
                                }

                                logger.Log($"Unlocked tool: {obj.name}");
                                unlockedCount++;
                            }
                            else
                            {
                                logger.Log($"No Unlock method found on {obj.name}");
                            }
                        }
                        catch (Exception e)
                        {
                            logger.Log($"Error unlocking {obj.name}: {e.Message}");
                        }
                    }
                }

                logger.Log($"Tool Unlock: Unlocked {unlockedCount} tools");
                return unlockedCount > 0;
            }
            catch (Exception e)
            {
                logger.Log($"Error unlocking tools: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Unlocks all items (ToolItemBasic objects).
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool UnlockAllItems()
        {
            try
            {
                logger.Log("=== UNLOCKING ALL ITEMS ===");

                // Find all ToolItemBasic objects
                UnityEngine.Object[] allObjects = Resources.FindObjectsOfTypeAll(typeof(ToolItemBasic));
                logger.Log($"Found {allObjects.Length} ToolItemBasic objects");

                int unlockedCount = 0;

                foreach (UnityEngine.Object obj in allObjects)
                {
                    if (obj.GetType().Name == "ToolItemBasic")
                    {
                        try
                        {
                            Type toolType = obj.GetType();
                            MethodInfo unlockMethod = toolType.GetMethod("Unlock");

                            if (unlockMethod != null)
                            {
                                // Find PopupFlags enum in nested types
                                Type popupFlagsType = GetPopupFlagsType(toolType);

                                if (popupFlagsType != null)
                                {
                                    object itemGetFlag = Enum.Parse(popupFlagsType, "ItemGet");
                                    unlockMethod.Invoke(obj, new object[] { null, itemGetFlag });
                                }
                                else
                                {
                                    unlockMethod.Invoke(obj, new object[] { null, null });
                                }

                                logger.Log($"Unlocked item: {obj.name}");
                                unlockedCount++;
                            }
                            else
                            {
                                logger.Log($"No Unlock method found on {obj.name}");
                            }
                        }
                        catch (Exception e)
                        {
                            logger.Log($"Error unlocking {obj.name}: {e.Message}");
                        }
                    }
                }

                logger.Log($"Item Unlock: Unlocked {unlockedCount} items");
                return unlockedCount > 0;
            }
            catch (Exception e)
            {
                logger.Log($"Error unlocking items: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Unlocks all fast travel locations by setting PlayerData booleans.
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool UnlockAllFastTravel()
        {
            try
            {
                logger.Log("=== UNLOCKING ALL FAST TRAVEL ===");

                // List of all fast travel station PlayerData booleans
                string[] fastTravelBools = {
                    "UnlockedAqueductStation",
                    "UnlockedShadowStation",
                    "UnlockedCityStation",
                    "UnlockedPeakStation",
                    "UnlockedCoralTowerStation",
                    "UnlockedShellwoodStation",
                    "UnlockedBelltownStation",
                    "UnlockedGreymoorStation",
                    "UnlockedBoneforestEastStation",
                    "UnlockedDocksStation"
                };

                int unlockedCount = 0;
                foreach (string boolName in fastTravelBools)
                {
                    if (SetPlayerDataBool(boolName, true))
                    {
                        logger.Log($"Unlocked: {boolName}");
                        unlockedCount++;
                    }
                    else
                    {
                        logger.Log($"Failed to unlock: {boolName}");
                    }
                }

                logger.Log($"Successfully unlocked {unlockedCount} fast travel locations");
                return unlockedCount > 0;
            }
            catch (Exception e)
            {
                logger.Log($"Error unlocking fast travel: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Unlocks all map items by calling SetPurchased on ShopItem objects.
        /// </summary>
        /// <returns>Number of map items unlocked</returns>
        public int UnlockAllMapItems()
        {
            try
            {
                // Find all ShopItem objects (which are actually map items)
                UnityEngine.Object[] allMapItems = Resources.FindObjectsOfTypeAll(typeof(ScriptableObject));
                int unlockedCount = 0;

                foreach (UnityEngine.Object obj in allMapItems)
                {
                    if (obj.GetType().Name == "ShopItem")
                    {
                        // Try to call SetPurchased method
                        Type mapItemType = obj.GetType();
                        MethodInfo setPurchasedMethod = mapItemType.GetMethod("SetPurchased",
                            BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(Action), typeof(int) }, null);

                        if (setPurchasedMethod != null)
                        {
                            // Call SetPurchased with null action and 0 subitem index
                            setPurchasedMethod.Invoke(obj, new object[] { null, 0 });
                            unlockedCount++;
                        }
                    }
                }

                logger.Log($"Unlocked {unlockedCount} map items using SetPurchased");
                return unlockedCount;
            }
            catch (Exception e)
            {
                logger.Log($"Error unlocking map items: {e.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Helper method to get toolItems from ToolItemManager.
        /// </summary>
        private System.Collections.IEnumerable GetToolItemsFromManager()
        {
            try
            {
                // Find ToolItemManager and get its toolItems list
                Type toolItemManagerType = FindTypeInAssemblies("ToolItemManager");
                if (toolItemManagerType == null)
                {
                    logger.Log("ToolItemManager type not found in assemblies");
                    return null;
                }

                // Found ToolItemManager type

                // Try multiple approaches to get the instance
                object toolItemManagerInstance = null;

                // Try multiple approaches to get the instance
                PropertyInfo instanceProperty = toolItemManagerType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
                if (instanceProperty != null)
                {
                    toolItemManagerInstance = instanceProperty.GetValue(null);
                }

                if (toolItemManagerInstance == null)
                {
                    instanceProperty = toolItemManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
                    if (instanceProperty != null)
                    {
                        toolItemManagerInstance = instanceProperty.GetValue(null);
                    }
                }

                if (toolItemManagerInstance == null)
                {
                    FieldInfo instanceField = toolItemManagerType.GetField("instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
                    if (instanceField != null)
                    {
                        toolItemManagerInstance = instanceField.GetValue(null);
                    }
                }

                if (toolItemManagerInstance == null)
                {
                    var findObjectMethod = typeof(UnityEngine.Object).GetMethod("FindObjectOfType", new Type[0]);
                    if (findObjectMethod != null)
                    {
                        var genericMethod = findObjectMethod.MakeGenericMethod(toolItemManagerType);
                        toolItemManagerInstance = genericMethod.Invoke(null, null);
                    }
                }

                if (toolItemManagerInstance == null)
                {
                    var findObjectsMethod = typeof(Resources).GetMethod("FindObjectsOfTypeAll", new Type[] { typeof(Type) });
                    if (findObjectsMethod != null)
                    {
                        var objects = (UnityEngine.Object[])findObjectsMethod.Invoke(null, new object[] { toolItemManagerType });
                        if (objects != null && objects.Length > 0)
                        {
                            toolItemManagerInstance = objects[0];
                        }
                    }
                }

                if (toolItemManagerInstance == null)
                {
                    logger.Log("Could not find ToolItemManager instance using any method");
                    return null;
                }

                // Get the toolItems field (it's a [SerializeField] private ToolItemList toolItems)
                FieldInfo toolItemsField = toolItemManagerType.GetField("toolItems", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (toolItemsField != null)
                {
                    object toolItemsList = toolItemsField.GetValue(toolItemManagerInstance);
                    
                    if (toolItemsList != null)
                    {
                        // ToolItemList inherits from NamedScriptableObjectList<ToolItem>
                        // The actual list is accessed via base.List property
                        Type toolItemListType = toolItemsList.GetType();
                        PropertyInfo listProperty = null;
                        Type currentType = toolItemListType;
                        
                        // Look for List property in the inheritance hierarchy
                        while (currentType != null && listProperty == null)
                        {
                            listProperty = currentType.GetProperty("List", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                            currentType = currentType.BaseType;
                        }
                        
                        if (listProperty != null)
                        {
                            toolItemsList = listProperty.GetValue(toolItemsList);
                            return toolItemsList as System.Collections.IEnumerable;
                        }
                    }
                }

                logger.Log("Failed to access ToolItemManager.toolItems");
                return null;
            }
            catch (Exception e)
            {
                logger.Log($"Error getting toolItems from ToolItemManager: {e.Message}");
                logger.Log($"Stack trace: {e.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Helper method to find PopupFlags enum type in nested types.
        /// </summary>
        private Type GetPopupFlagsType(Type toolType)
        {
            Type[] nestedTypes = toolType.GetNestedTypes();
            foreach (Type nested in nestedTypes)
            {
                if (nested.Name.Contains("PopupFlags"))
                {
                    return nested;
                }
            }
            return null;
        }

        /// <summary>
        /// Helper method to set PlayerData boolean values.
        /// </summary>
        private bool SetPlayerDataBool(string boolName, bool value)
        {
            try
            {
                Type playerDataType = FindTypeInAssemblies("PlayerData");
                if (playerDataType == null) 
                {
                    logger.Log($"SetPlayerDataBool({boolName}): PlayerData type not found");
                    return false;
                }

                PropertyInfo instanceProperty = playerDataType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceProperty == null) 
                {
                    logger.Log($"SetPlayerDataBool({boolName}): instance property not found");
                    return false;
                }

                object playerDataInstance = instanceProperty.GetValue(null);
                if (playerDataInstance == null) 
                {
                    logger.Log($"SetPlayerDataBool({boolName}): PlayerData instance is null");
                    return false;
                }

                // Try to set the field directly
                FieldInfo field = playerDataType.GetField(boolName, BindingFlags.Public | BindingFlags.Instance);
                if (field != null && field.FieldType == typeof(bool))
                {
                    field.SetValue(playerDataInstance, value);
                    logger.Log($"SetPlayerDataBool({boolName}): Successfully set to {value}");
                    return true;
                }

                logger.Log($"SetPlayerDataBool({boolName}): Field not found or not bool type");
                return false;
            }
            catch (Exception ex)
            {
                logger.Log($"SetPlayerDataBool({boolName}): Exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Helper method to find types in loaded assemblies.
        /// </summary>
        public Type FindTypeInAssemblies(string typeName)
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

        #endregion
    }
}
