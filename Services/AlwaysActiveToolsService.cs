using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SilkSong.Interfaces;

namespace SilkSong.Services
{
    /// <summary>
    /// Service for managing the 18 Always Active Yellow Tools system.
    /// Provides passive exploration and support benefits through the game's SetExtraEquippedTool system.
    /// </summary>
    public class AlwaysActiveToolsService
    {
        private readonly IModLogger logger;
        
        // Core system
        private List<string> alwaysActiveTools = new List<string>();
        
        // Individual tool states (18 Yellow Tools)
        private bool compassActive = false;
        private bool magnetiteBroochActive = false;
        private bool shardPendantActive = false;
        private bool weightedBeltActive = false;
        private bool barbedBraceletActive = false;
        private bool deadBugsPurseActive = false;
        private bool shellSatchelActive = false;
        private bool magnetiteDiceActive = false;
        private bool scuttlebraceActive = false;
        private bool ascendantsGripActive = false;
        private bool spiderStringsActive = false;
        private bool silkspeedAnkletsActive = false;
        private bool thiefsMarkActive = false;
        private bool longclawActive = false;
        private bool pinBadgeActive = false;
        private bool quickSlingActive = false;
        private bool weavelightActive = false;
        private bool wardingBellActive = false;

        // Tool mapping - display name to internal game object name
        private readonly Dictionary<string, string> toolMapping = new Dictionary<string, string>
        {
            { "Compass", "Compass" },
            { "Magnetite Brooch", "Rosary Magnet" },
            { "Shard Pendant", "Bone Necklace" },
            { "Weighted Belt", "Weighted Anklet" },
            { "Barbed Bracelet", "Barbed Wire" },
            { "Dead Bug's Purse", "Dead Mans Purse" },
            { "Shell Satchel", "Shell Satchel" },
            { "Magnetite Dice", "Magnetite Dice" },
            { "Scuttlebrace", "Scuttlebrace" },
            { "Ascendant's Grip", "Wallcling" },
            { "Spider Strings", "Musician Charm" },
            { "Silkspeed Anklets", "Sprintmaster" },
            { "Thief's Mark", "Thief Charm" },
            { "Longclaw", "Longneedle" },
            { "Pin Badge", "Pinstress Tool" },
            { "Quick Sling", "Quick Sling" },
            { "Weavelight", "White Ring" },
            { "Warding Bell", "Bell Bind" }
        };

        public AlwaysActiveToolsService(IModLogger logger)
        {
            this.logger = logger;
        }

        #region Properties

        /// <summary>
        /// Gets the current count of active tools.
        /// </summary>
        public int ActiveToolCount => alwaysActiveTools.Count;

        /// <summary>
        /// Gets whether Compass is active.
        /// </summary>
        public bool IsCompassActive => compassActive;

        /// <summary>
        /// Gets whether Magnetite Brooch is active.
        /// </summary>
        public bool IsMagnetiteBroochActive => magnetiteBroochActive;

        /// <summary>
        /// Gets whether Shard Pendant is active.
        /// </summary>
        public bool IsShardPendantActive => shardPendantActive;

        /// <summary>
        /// Gets whether Weighted Belt is active.
        /// </summary>
        public bool IsWeightedBeltActive => weightedBeltActive;

        /// <summary>
        /// Gets whether Barbed Bracelet is active.
        /// </summary>
        public bool IsBarbedBraceletActive => barbedBraceletActive;

        /// <summary>
        /// Gets whether Dead Bug's Purse is active.
        /// </summary>
        public bool IsDeadBugsPurseActive => deadBugsPurseActive;

        /// <summary>
        /// Gets whether Shell Satchel is active.
        /// </summary>
        public bool IsShellSatchelActive => shellSatchelActive;

        /// <summary>
        /// Gets whether Magnetite Dice is active.
        /// </summary>
        public bool IsMagnetiteDiceActive => magnetiteDiceActive;

        /// <summary>
        /// Gets whether Scuttlebrace is active.
        /// </summary>
        public bool IsScuttlebraceActive => scuttlebraceActive;

        /// <summary>
        /// Gets whether Ascendant's Grip is active.
        /// </summary>
        public bool IsAscendantsGripActive => ascendantsGripActive;

        /// <summary>
        /// Gets whether Spider Strings is active.
        /// </summary>
        public bool IsSpiderStringsActive => spiderStringsActive;

        /// <summary>
        /// Gets whether Silkspeed Anklets is active.
        /// </summary>
        public bool IsSilkspeedAnkletsActive => silkspeedAnkletsActive;

        /// <summary>
        /// Gets whether Thief's Mark is active.
        /// </summary>
        public bool IsThiefsMarkActive => thiefsMarkActive;

        /// <summary>
        /// Gets whether Longclaw is active.
        /// </summary>
        public bool IsLongclawActive => longclawActive;

        /// <summary>
        /// Gets whether Pin Badge is active.
        /// </summary>
        public bool IsPinBadgeActive => pinBadgeActive;

        /// <summary>
        /// Gets whether Quick Sling is active.
        /// </summary>
        public bool IsQuickSlingActive => quickSlingActive;

        /// <summary>
        /// Gets whether Weavelight is active.
        /// </summary>
        public bool IsWeavelightActive => weavelightActive;

        /// <summary>
        /// Gets whether Warding Bell is active.
        /// </summary>
        public bool IsWardingBellActive => wardingBellActive;

        #endregion

        #region Individual Tool Toggles

        /// <summary>
        /// Toggles Compass on/off.
        /// </summary>
        public bool ToggleCompass(Action<string> onSuccess = null, Action<string> onError = null)
        {
            return ToggleTool("Compass", ref compassActive, onSuccess, onError);
        }

        /// <summary>
        /// Toggles Magnetite Brooch on/off.
        /// </summary>
        public bool ToggleMagnetiteBrooch(Action<string> onSuccess = null, Action<string> onError = null)
        {
            return ToggleTool("Magnetite Brooch", ref magnetiteBroochActive, onSuccess, onError);
        }

        /// <summary>
        /// Toggles Shard Pendant on/off.
        /// </summary>
        public bool ToggleShardPendant(Action<string> onSuccess = null, Action<string> onError = null)
        {
            return ToggleTool("Shard Pendant", ref shardPendantActive, onSuccess, onError);
        }

        /// <summary>
        /// Toggles Weighted Belt on/off.
        /// </summary>
        public bool ToggleWeightedBelt(Action<string> onSuccess = null, Action<string> onError = null)
        {
            return ToggleTool("Weighted Belt", ref weightedBeltActive, onSuccess, onError);
        }

        /// <summary>
        /// Toggles Barbed Bracelet on/off.
        /// </summary>
        public bool ToggleBarbedBracelet(Action<string> onSuccess = null, Action<string> onError = null)
        {
            return ToggleTool("Barbed Bracelet", ref barbedBraceletActive, onSuccess, onError);
        }

        /// <summary>
        /// Toggles Dead Bug's Purse on/off.
        /// </summary>
        public bool ToggleDeadBugsPurse(Action<string> onSuccess = null, Action<string> onError = null)
        {
            return ToggleTool("Dead Bug's Purse", ref deadBugsPurseActive, onSuccess, onError);
        }

        /// <summary>
        /// Toggles Shell Satchel on/off.
        /// </summary>
        public bool ToggleShellSatchel(Action<string> onSuccess = null, Action<string> onError = null)
        {
            return ToggleTool("Shell Satchel", ref shellSatchelActive, onSuccess, onError);
        }

        /// <summary>
        /// Toggles Magnetite Dice on/off.
        /// </summary>
        public bool ToggleMagnetiteDice(Action<string> onSuccess = null, Action<string> onError = null)
        {
            return ToggleTool("Magnetite Dice", ref magnetiteDiceActive, onSuccess, onError);
        }

        /// <summary>
        /// Toggles Scuttlebrace on/off.
        /// </summary>
        public bool ToggleScuttlebrace(Action<string> onSuccess = null, Action<string> onError = null)
        {
            return ToggleTool("Scuttlebrace", ref scuttlebraceActive, onSuccess, onError);
        }

        /// <summary>
        /// Toggles Ascendant's Grip on/off.
        /// </summary>
        public bool ToggleAscendantsGrip(Action<string> onSuccess = null, Action<string> onError = null)
        {
            return ToggleTool("Ascendant's Grip", ref ascendantsGripActive, onSuccess, onError);
        }

        /// <summary>
        /// Toggles Spider Strings on/off.
        /// </summary>
        public bool ToggleSpiderStrings(Action<string> onSuccess = null, Action<string> onError = null)
        {
            return ToggleTool("Spider Strings", ref spiderStringsActive, onSuccess, onError);
        }

        /// <summary>
        /// Toggles Silkspeed Anklets on/off.
        /// </summary>
        public bool ToggleSilkspeedAnklets(Action<string> onSuccess = null, Action<string> onError = null)
        {
            return ToggleTool("Silkspeed Anklets", ref silkspeedAnkletsActive, onSuccess, onError);
        }

        /// <summary>
        /// Toggles Thief's Mark on/off.
        /// </summary>
        public bool ToggleThiefsMark(Action<string> onSuccess = null, Action<string> onError = null)
        {
            return ToggleTool("Thief's Mark", ref thiefsMarkActive, onSuccess, onError);
        }

        /// <summary>
        /// Toggles Longclaw on/off.
        /// </summary>
        public bool ToggleLongclaw(Action<string> onSuccess = null, Action<string> onError = null)
        {
            return ToggleTool("Longclaw", ref longclawActive, onSuccess, onError);
        }

        /// <summary>
        /// Toggles Pin Badge on/off.
        /// </summary>
        public bool TogglePinBadge(Action<string> onSuccess = null, Action<string> onError = null)
        {
            return ToggleTool("Pin Badge", ref pinBadgeActive, onSuccess, onError);
        }

        /// <summary>
        /// Toggles Quick Sling on/off.
        /// </summary>
        public bool ToggleQuickSling(Action<string> onSuccess = null, Action<string> onError = null)
        {
            return ToggleTool("Quick Sling", ref quickSlingActive, onSuccess, onError);
        }

        /// <summary>
        /// Toggles Weavelight on/off.
        /// </summary>
        public bool ToggleWeavelight(Action<string> onSuccess = null, Action<string> onError = null)
        {
            return ToggleTool("Weavelight", ref weavelightActive, onSuccess, onError);
        }

        /// <summary>
        /// Toggles Warding Bell on/off.
        /// </summary>
        public bool ToggleWardingBell(Action<string> onSuccess = null, Action<string> onError = null)
        {
            return ToggleTool("Warding Bell", ref wardingBellActive, onSuccess, onError);
        }

        #endregion

        #region Bulk Operations

        /// <summary>
        /// Clears all always active tools.
        /// </summary>
        public bool ClearAllTools(Action<string> onSuccess = null, Action<string> onError = null)
        {
            try
            {
                // Reset all tool states
                compassActive = false;
                magnetiteBroochActive = false;
                shardPendantActive = false;
                weightedBeltActive = false;
                barbedBraceletActive = false;
                deadBugsPurseActive = false;
                shellSatchelActive = false;
                magnetiteDiceActive = false;
                scuttlebraceActive = false;
                ascendantsGripActive = false;
                spiderStringsActive = false;
                silkspeedAnkletsActive = false;
                thiefsMarkActive = false;
                longclawActive = false;
                pinBadgeActive = false;
                quickSlingActive = false;
                weavelightActive = false;
                wardingBellActive = false;

                // Clear the tools list and apply
                alwaysActiveTools.Clear();
                bool success = ApplyAlwaysActiveTools();
                
                if (success)
                {
                    onSuccess?.Invoke("All always-active tools disabled");
                    logger.Log("All always-active tools disabled via ClearAllTools");
                }
                else
                {
                    onError?.Invoke("Failed to clear always-active tools");
                }
                
                return success;
            }
            catch (Exception e)
            {
                onError?.Invoke($"Error clearing all tools: {e.Message}");
                logger.Log($"Error in ClearAllTools: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Initializes default tools (Compass + Magnetite Brooch).
        /// </summary>
        public bool InitializeDefaultTools(Action<string> onSuccess = null, Action<string> onError = null)
        {
            try
            {
                // Initialize compass and magnetite brooch as enabled by default (most popular Yellow Tools)
                // Only enable the most essential navigation/collection tools by default
                compassActive = true;
                magnetiteBroochActive = true;

                // Reset all other Yellow Tools to false (user can enable as needed)
                shardPendantActive = false;
                weightedBeltActive = false;
                barbedBraceletActive = false;
                deadBugsPurseActive = false;
                shellSatchelActive = false;
                magnetiteDiceActive = false;
                scuttlebraceActive = false;
                ascendantsGripActive = false;
                spiderStringsActive = false;
                silkspeedAnkletsActive = false;
                thiefsMarkActive = false;
                longclawActive = false;
                pinBadgeActive = false;
                quickSlingActive = false;
                weavelightActive = false;
                wardingBellActive = false;

                // Clear any existing tools and add defaults
                alwaysActiveTools.Clear();
                alwaysActiveTools.Add("Compass");
                alwaysActiveTools.Add("Rosary Magnet");

                bool success = ApplyAlwaysActiveTools();
                
                if (success)
                {
                    onSuccess?.Invoke("Initialized default Yellow Tools (Compass + Magnetite Brooch)");
                    logger.Log("Initialized default Yellow Tools (Compass + Magnetite Brooch) - 16 additional tools available for manual activation");
                }
                else
                {
                    onError?.Invoke("Failed to initialize default tools");
                }
                
                return success;
            }
            catch (Exception e)
            {
                onError?.Invoke($"Error initializing default tools: {e.Message}");
                logger.Log($"Error in InitializeDefaultTools: {e.Message}");
                return false;
            }
        }

        #endregion

        #region Scene Transition Support

        /// <summary>
        /// Reapplies the current always active tools without resetting their state.
        /// Used when transitioning between scenes to maintain user selections.
        /// </summary>
        public bool ReapplyCurrentTools(Action<string> onSuccess = null, Action<string> onError = null)
        {
            try
            {
                bool success = ApplyAlwaysActiveTools();
                
                if (success)
                {
                    onSuccess?.Invoke($"Reapplied {alwaysActiveTools.Count} always-active tools after scene load");
                    logger.Log($"Reapplied {alwaysActiveTools.Count} always-active tools after scene load");
                }
                else
                {
                    onError?.Invoke("Failed to reapply always-active tools");
                }
                
                return success;
            }
            catch (Exception e)
            {
                onError?.Invoke($"Error reapplying tools: {e.Message}");
                logger.Log($"Error in ReapplyCurrentTools: {e.Message}");
                return false;
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Generic tool toggle method.
        /// </summary>
        private bool ToggleTool(string displayName, ref bool toolState, Action<string> onSuccess = null, Action<string> onError = null)
        {
            try
            {
                toolState = !toolState;

                if (!toolMapping.TryGetValue(displayName, out string internalName))
                {
                    onError?.Invoke($"Unknown tool: {displayName}");
                    return false;
                }

                if (toolState)
                {
                    if (!alwaysActiveTools.Contains(internalName))
                    {
                        alwaysActiveTools.Add(internalName);
                        bool success = ApplyAlwaysActiveTools();
                        if (success)
                        {
                            onSuccess?.Invoke($"{displayName} enabled!");
                            logger.Log($"{displayName} enabled via always active tools");
                        }
                        else
                        {
                            // Revert state on failure
                            toolState = !toolState;
                            onError?.Invoke($"Failed to enable {displayName}");
                        }
                        return success;
                    }
                }
                else
                {
                    alwaysActiveTools.Remove(internalName);
                    bool success = ApplyAlwaysActiveTools();
                    if (success)
                    {
                        onSuccess?.Invoke($"{displayName} disabled!");
                        logger.Log($"{displayName} disabled - removed from always active tools");
                    }
                    else
                    {
                        // Revert state on failure
                        toolState = !toolState;
                        onError?.Invoke($"Failed to disable {displayName}");
                    }
                    return success;
                }
                
                return true;
            }
            catch (Exception e)
            {
                // Revert state on error
                toolState = !toolState;
                onError?.Invoke($"Error toggling {displayName}: {e.Message}");
                logger.Log($"Error toggling {displayName}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Applies all currently active tools using the game's SetExtraEquippedTool system.
        /// </summary>
        private bool ApplyAlwaysActiveTools()
        {
            try
            {
                // Find ToolItemManager type
                Type toolItemManagerType = FindTypeInAssemblies("ToolItemManager");
                if (toolItemManagerType == null)
                {
                    logger.Log("ToolItemManager type not found for applying always active tools");
                    return false;
                }

                // Get the SetExtraEquippedTool method
                MethodInfo setExtraEquippedToolMethod = toolItemManagerType.GetMethod("SetExtraEquippedTool",
                    BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string), typeof(string) }, null);

                if (setExtraEquippedToolMethod == null)
                {
                    logger.Log("SetExtraEquippedTool method not found");
                    return false;
                }

                // Clear existing extra equipped tools first
                ClearAlwaysActiveToolSlots(setExtraEquippedToolMethod);

                // Apply each active tool to an extra slot
                for (int i = 0; i < alwaysActiveTools.Count; i++)
                {
                    string slotId = $"AlwaysActive_{i}";
                    string toolName = alwaysActiveTools[i];

                    setExtraEquippedToolMethod.Invoke(null, new object[] { slotId, toolName });
                    logger.Log($"Applied always active tool: {toolName} to slot {slotId}");
                }

                logger.Log($"Applied {alwaysActiveTools.Count} always active tools");
                return true;
            }
            catch (Exception e)
            {
                logger.Log($"Error in ApplyAlwaysActiveTools: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clears all always active tool slots.
        /// </summary>
        private bool ClearAlwaysActiveToolSlots(MethodInfo setExtraEquippedToolMethod = null)
        {
            try
            {
                // Get method if not provided
                if (setExtraEquippedToolMethod == null)
                {
                    Type toolItemManagerType = FindTypeInAssemblies("ToolItemManager");
                    if (toolItemManagerType == null)
                    {
                        return false;
                    }

                    setExtraEquippedToolMethod = toolItemManagerType.GetMethod("SetExtraEquippedTool",
                        BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string), typeof(string) }, null);

                    if (setExtraEquippedToolMethod == null)
                    {
                        return false;
                    }
                }

                // Clear all our always active slots (support up to 20 tools)
                for (int i = 0; i < 20; i++)
                {
                    string slotId = $"AlwaysActive_{i}";
                    setExtraEquippedToolMethod.Invoke(null, new object[] { slotId, string.Empty });
                }

                logger.Log("Cleared all always active tool slots");
                return true;
            }
            catch (Exception e)
            {
                logger.Log($"Error in ClearAlwaysActiveToolSlots: {e.Message}");
                return false;
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

        #endregion
    }
}
