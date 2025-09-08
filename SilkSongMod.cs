using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Text;
using UniverseLib;
using UniverseLib.UI;

namespace SilkSong
{
    public class SilkSongMod : MelonMod
    {
        private Component heroController;
        private bool autoRefillSilk = false;
        private float silkRefillTimer = 0f;
        private const float SILK_REFILL_INTERVAL = 2.0f;
        // GUI Variables
        private bool showGUI = false;
       
        private Rect windowRect = new Rect(Screen.width - 420, (Screen.height * 0.01f), 400, (Screen.height * 0.98f));
        private UIBase uiBase;
        private bool universeLibInitialized = false;
        
        // Toast notification system
        private string lastToastMessage = "";
        private float toastTimer = 0f;
        private const float TOAST_DURATION = 3f;
        
        // Scroll position for the GUI
        private Vector2 scrollPosition = Vector2.zero;
        private Vector2 balanceScrollPosition = Vector2.zero;
        
        // Tab system
        private int selectedTab = 0;
        private string[] tabNames = { "Cheats", "Balance" };
        
        // Balance/multiplier system
        private string globalMultiplierText = "1.0";
        private float globalMultiplier = 1.0f;
        private List<FieldInfo> damageFields = new List<FieldInfo>();
        private List<MonoBehaviour> damageBehaviours = new List<MonoBehaviour>();
        private Dictionary<FieldInfo, object> originalValues = new Dictionary<FieldInfo, object>();
        private bool fieldsScanned = false;
        private bool showDetails = false;
        
        // One Hit Kill toggle system
        private bool oneHitKillEnabled = false;
        private Dictionary<string, object> oneHitKillOriginalValues = new Dictionary<string, object>();
        
        // Infinite Air Jump toggle system
        private bool infiniteAirJumpEnabled = false;
        
        // Input field variables
        private string healthAmount = "1";
        private string moneyAmount = "1000";
        private string shardAmount = "1000";
        private string setHealthAmount = "11"; // For setting exact health
        
        // Set Collectable Amount variables
        private string collectableAmount = "1";
        private List<string> availableCollectables = new List<string>();
        private string[] collectableNames = new string[0];
        private int selectedCollectableIndex = 0;
        private bool collectablesScanned = false;
        private bool showCollectableDropdown = false;
        private Vector2 collectableDropdownScroll = Vector2.zero;
        private string collectableSearchFilter = "";
        private string[] filteredCollectableNames = new string[0];
        private Dictionary<string, string> displayNameToObjectName = new Dictionary<string, string>();
        
        // Crest Tools variables
        private bool toolsScanned = false;
        private List<string> availableTools = new List<string>();
        private string[] toolNames = new string[0];
        private int selectedToolIndex = 0;
        private bool showToolDropdown = false;
        private Vector2 toolDropdownScroll = Vector2.zero;
        
        // Cache for ammo checking to prevent repeated scanning
        private int lastAmmoCheckToolIndex = -1;
        private bool lastAmmoCheckResult = false;
        private int lastStorageCheckToolIndex = -1;
        private int lastStorageCheckResult = 0;
        private string toolSearchFilter = "";
        private string[] filteredToolNames = new string[0];
        private string toolStorageAmount = "100";
        private int lastSelectedToolIndex = -1;
        private Dictionary<string, string> toolDisplayNameToObjectName = new Dictionary<string, string>();
        
        // Tool type filtering
        private bool showSkillsOnly = false;
        private List<string> availableSkills = new List<string>();
        private List<string> availableBasicTools = new List<string>();
        
        // Confirmation modal system
        private bool showConfirmModal = false;
        private string confirmMessage = "";
        private string confirmActionName = "";
        private System.Action pendingAction = null;
        private static Texture2D solidBlackTexture = null;
        private float modalCooldownTime = 0f;
        private Rect modalWindowRect = new Rect(0, 0, 400, 200);
        
        // Collapsible section states
        private bool showToggleFeatures = true;
        private bool showActionAmounts = true;
        private bool showCollectibleItems = true;
        private bool showCrestTools = true;
        private bool showPlayerSkills = true;
        private bool showQuickActions = true;
        private bool showKeybindSettings = false;
        
        // Keybind variables
        private KeyCode[] currentKeybinds = new KeyCode[] 
        {
            KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4, KeyCode.F5, 
            KeyCode.F6, KeyCode.F8, KeyCode.F9, KeyCode.F10, KeyCode.F11, KeyCode.F12
        };
        private string[] keybindNames = new string[]
        {
            "Add Health", "Set Health", "Refill Health", "Toggle One Hit Kill", "Add Money",
            "Add Shards", "Unlock Crests", "Unlock Crest Skills", "Unlock Crest Tools", "Max Collectables", "Auto Silk"
        };
        private bool isSettingKeybind = false;
        private int keybindToSet = -1;

        [System.Obsolete]
        public override void OnApplicationStart()
        {
            MelonLogger.Msg("Silksong Health Mod v1.0 - Ready!");
            MelonLogger.Msg("Controls: F1=Add Health, F2=Set Health to 11, F3=Refill Health, F4=Toggle One Hit Kill Mode, F5=Add 1000 Money, F6=Add 1000 Shards, F8=Unlock All Crests, F9=Unlock All Crest Skills, F10=Unlock All Crest Tools, F11=Max All Collectables, F12=Toggle Auto Silk Refill, INSERT/TILDE=Toggle GUI");
        }

        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();
            MelonLogger.Msg("Health mod initialized!");
        }

        public override void OnUpdate()
        {
            // GUI Toggle (Insert or Tilde)
            if (Input.GetKeyDown(KeyCode.Insert) || Input.GetKeyDown(KeyCode.BackQuote))
            {
                showGUI = !showGUI;
                
                // Initialize UniverseLib on first GUI open
                if (showGUI && !universeLibInitialized)
                {
                    try
                    {
                        // Configure UniverseLib with proper settings for UI
                        var config = new UniverseLib.Config.UniverseLibConfig()
                        {
                            Disable_EventSystem_Override = false,
                            Force_Unlock_Mouse = true
                        };
                        
                        float startupDelay = 1f;
                        Universe.Init(startupDelay, OnUniverseLibInitialized, LogHandler, config);
                        MelonLogger.Msg("UniverseLib initialization started...");
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Msg($"Failed to initialize UniverseLib: {e.Message}");
                        // Fall back to basic cursor management
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                    }
                }
                
                // Use UniversalUI if available, otherwise basic cursor management
                if (universeLibInitialized && uiBase != null)
                {
                    UniversalUI.SetUIActive("SilkSongCheatGUI", showGUI);
                }
                else
                {
                    // Fallback cursor management
                    if (showGUI)
                    {
                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                    }
                    else
                    {
                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                    }
                }
                
                MelonLogger.Msg($"GUI {(showGUI ? "Enabled" : "Disabled")}");
            }

            // Cache the hero controller reference
            if (heroController == null)
            {
                GameObject player = GameObject.Find("Hero_Hornet(Clone)");
                if (player != null)
                {
                    heroController = player.GetComponent("HeroController");
                    if (heroController != null)
                    {
                        MelonLogger.Msg("Hero found! Health controls active.");
                    }
                }
            }

            // Keybind detection for setting new keys
            if (isSettingKeybind)
            {
                foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(key) && key != KeyCode.Insert && key != KeyCode.Escape)
                    {
                        currentKeybinds[keybindToSet] = key;
                        MelonLogger.Msg($"Keybind for {keybindNames[keybindToSet]} set to {key}");
                        isSettingKeybind = false;
                        keybindToSet = -1;
                        break;
                    }
                }
                
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    isSettingKeybind = false;
                    keybindToSet = -1;
                    MelonLogger.Msg("Keybind setting cancelled");
                }
            }

            // Health modification controls using dynamic keybinds
            if (heroController != null && !isSettingKeybind)
            {
                if (Input.GetKeyDown(currentKeybinds[0])) // Add Health
                {
                    if (int.TryParse(healthAmount, out int health))
                        AddHealth(health);
                }

                if (Input.GetKeyDown(currentKeybinds[1])) // Set Health
                {
                    if (int.TryParse(setHealthAmount, out int targetHealth))
                        SetMaxHealthExact(targetHealth);
                }

                if (Input.GetKeyDown(currentKeybinds[2])) // Refill Health
                {
                    RefillHealth();
                }

                if (Input.GetKeyDown(currentKeybinds[3])) // One Hit Kill
                {
                    EnableOneHitKill();
                }

                if (Input.GetKeyDown(currentKeybinds[4])) // Add Money
                {
                    if (int.TryParse(moneyAmount, out int money))
                        AddMoney(money);
                }

                if (Input.GetKeyDown(currentKeybinds[5])) // Add Shards
                {
                    if (int.TryParse(shardAmount, out int shards))
                        AddShards(shards);
                }

                if (Input.GetKeyDown(currentKeybinds[6])) // Unlock Crests
                {
                    UnlockAllCrests();
                }

                if (Input.GetKeyDown(currentKeybinds[7])) // Unlock Crest Skills
                {
                    UnlockAllTools();
                }

                if (Input.GetKeyDown(currentKeybinds[8])) // Unlock Crest Tools
                {
                    UnlockAllItems();
                }

                if (Input.GetKeyDown(currentKeybinds[9])) // Max Collectables
                {
                    MaxAllCollectables();
                }

                if (Input.GetKeyDown(currentKeybinds[10])) // Auto Silk
                {
                    ToggleAutoSilkRefill();
                }
            }

            // Handle auto silk refill timer
            if (autoRefillSilk && heroController != null)
            {
                silkRefillTimer += Time.deltaTime;
                if (silkRefillTimer >= SILK_REFILL_INTERVAL)
                {
                    RefillSilk();
                    silkRefillTimer = 0f;
                }
            }
            
            // Update toast timer
            if (toastTimer > 0f)
            {
                toastTimer -= Time.deltaTime;
            }
        }
        
        private void ShowToast(string message)
        {
            lastToastMessage = message;
            toastTimer = TOAST_DURATION;
        }
        
        private void ShowConfirmation(string actionName, string message, System.Action action)
        {
            // Prevent rapid-fire modal creation
            if (Time.time < modalCooldownTime)
            {
                return;
            }
            
            confirmActionName = actionName;
            confirmMessage = message;
            pendingAction = action;
            showConfirmModal = true;
        }

        private void OnUniverseLibInitialized()
        {
            try
            {
                uiBase = UniversalUI.RegisterUI("SilkSongCheatGUI", null);
                universeLibInitialized = true;
                MelonLogger.Msg("UniverseLib initialized successfully!");
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Failed to register UI with UniverseLib: {e.Message}");
            }
        }

        private void LogHandler(string message, UnityEngine.LogType type)
        {
            // Forward UniverseLib logs to MelonLoader
            MelonLogger.Msg($"[UniverseLib] {message}");
        }
        
        private void ScanCollectables()
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
                            MelonLogger.Msg($"Error processing collectable {obj.name}: {e.Message}");
                        }
                    }
                }
                
                // Sort alphabetically for better UX
                availableCollectables.Sort();
                collectableNames = availableCollectables.ToArray();
                FilterCollectables(); // Initialize filtered list
                
                collectablesScanned = true;
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Error scanning collectables: {e.Message}");
            }
        }
        
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
        
        private void FilterCollectables()
        {
            if (string.IsNullOrEmpty(collectableSearchFilter))
            {
                filteredCollectableNames = collectableNames;
            }
            else
            {
                var filtered = availableCollectables.Where(name => 
                    name.ToLower().Contains(collectableSearchFilter.ToLower())).ToArray();
                filteredCollectableNames = filtered;
            }
        }
        
        private void ScanTools()
        {
            if (toolsScanned) return;
            
            availableTools.Clear();
            availableSkills.Clear();
            availableBasicTools.Clear();
            toolDisplayNameToObjectName.Clear();
            
            try
            {
                // Find all ToolItemBasic objects (they have display names and inherit baseStorageAmount from ToolItem)
                UnityEngine.Object[] basicObjects = Resources.FindObjectsOfTypeAll(typeof(ToolItemBasic));
                
                foreach (UnityEngine.Object obj in basicObjects)
                {
                    if (obj != null)
                    {
                        try
                        {
                            string displayName = GetToolDisplayName(obj);
                            if (!string.IsNullOrEmpty(displayName) && !IsInvalidDisplayName(displayName))
                            {
                                if (!availableTools.Contains(displayName))
                                {
                                    availableTools.Add(displayName);
                                    availableBasicTools.Add(displayName);
                                    toolDisplayNameToObjectName[displayName] = obj.name;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            MelonLogger.Msg($"Error processing ToolItemBasic {obj.name}: {e.Message}");
                        }
                    }
                }
                
                // Also find all ToolItemSkill objects (like Silk Spear, etc.)
                UnityEngine.Object[] skillObjects = Resources.FindObjectsOfTypeAll(typeof(ToolItemSkill));
                
                foreach (UnityEngine.Object obj in skillObjects)
                {
                    if (obj != null)
                    {
                        try
                        {
                            string displayName = GetToolDisplayName(obj);
                            if (!string.IsNullOrEmpty(displayName) && !IsInvalidDisplayName(displayName))
                            {
                                if (!availableTools.Contains(displayName))
                                {
                                    availableTools.Add(displayName);
                                    availableSkills.Add(displayName);
                                    toolDisplayNameToObjectName[displayName] = obj.name;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            MelonLogger.Msg($"Error processing ToolItemSkill {obj.name}: {e.Message}");
                        }
                    }
                }
                
                // If we didn't find tools, add some default ones
                if (availableTools.Count == 0)
                {
                    availableTools.AddRange(new string[] { 
                        "Silk", "Needle", "Healing Bandage", "Binding", "Cocoon", 
                        "Thread", "Weaving", "Warp", "Bind", "Wrap" 
                    });
                }
                
                // Sort alphabetically for better UX
                availableTools.Sort();
                availableSkills.Sort();
                availableBasicTools.Sort();
                toolNames = availableTools.ToArray();
                
                
                FilterTools(); // Initialize filtered list
                
                toolsScanned = true;
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Error scanning tools: {e.Message}");
            }
        }
        
        private string GetToolDisplayName(UnityEngine.Object toolObj)
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
                        MelonLogger.Msg($"Error getting DisplayName property: {e.Message}");
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
        
        private void FilterTools()
        {
            List<string> sourceList = new List<string>();
            
            if (showSkillsOnly)
            {
                // Build skills list fresh from ToolItemSkill objects (same as unlock method)
                UnityEngine.Object[] skillObjects = Resources.FindObjectsOfTypeAll(typeof(ToolItemSkill));
                
                foreach (UnityEngine.Object obj in skillObjects)
                {
                    if (obj != null)
                    {
                        try
                        {
                            string displayName = GetToolDisplayName(obj);
                            if (!string.IsNullOrEmpty(displayName) && !IsInvalidDisplayName(displayName))
                            {
                                if (!sourceList.Contains(displayName))
                                {
                                    sourceList.Add(displayName);
                                }
                            }
                        }
                        catch { }
                    }
                }
                sourceList.Sort();
            }
            else
            {
                // Use all tools
                sourceList = availableTools;
            }
            
            if (string.IsNullOrEmpty(toolSearchFilter))
            {
                filteredToolNames = sourceList.ToArray();
            }
            else
            {
                var filtered = sourceList.Where(name => 
                    name.ToLower().Contains(toolSearchFilter.ToLower())).ToArray();
                filteredToolNames = filtered;
            }
            
        }
        
        private void UnlockSpecificTool(string toolDisplayName)
        {
            try
            {
                bool found = false;
                
                // First try ToolItemBasic objects
                UnityEngine.Object[] basicObjects = Resources.FindObjectsOfTypeAll(typeof(ToolItemBasic));
                
                foreach (UnityEngine.Object obj in basicObjects)
                {
                    if (obj != null && obj.GetType().Name == "ToolItemBasic")
                    {
                        try
                        {
                            string objectDisplayName = GetToolDisplayName(obj);
                            if (objectDisplayName == toolDisplayName)
                            {
                                // Found the matching tool, use the same unlock logic as UnlockAllItems
                                Type toolType = obj.GetType();
                                MethodInfo unlockMethod = toolType.GetMethod("Unlock");
                                
                                if (unlockMethod != null)
                                {
                                    // Find PopupFlags enum in nested types (same as UnlockAllItems)
                                    Type popupFlagsType = null;
                                    Type[] nestedTypes = toolType.GetNestedTypes();
                                    foreach (Type nested in nestedTypes)
                                    {
                                        if (nested.Name.Contains("PopupFlags"))
                                        {
                                            popupFlagsType = nested;
                                            break;
                                        }
                                    }
                                    
                                    if (popupFlagsType != null)
                                    {
                                        object itemGetFlag = Enum.Parse(popupFlagsType, "ItemGet");
                                        unlockMethod.Invoke(obj, new object[] { null, itemGetFlag });
                                    }
                                    else
                                    {
                                        unlockMethod.Invoke(obj, new object[] { null, null });
                                    }
                                    
                                    ShowToast($"Unlocked {toolDisplayName}!");
                                    found = true;
                                    return;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            MelonLogger.Msg($"Error processing tool {obj.name}: {e.Message}");
                        }
                    }
                }
                
                // If not found in ToolItemBasic, try ToolItemSkill objects
                if (!found)
                {
                    UnityEngine.Object[] skillObjects = Resources.FindObjectsOfTypeAll(typeof(ToolItemSkill));
                    
                    foreach (UnityEngine.Object obj in skillObjects)
                    {
                        if (obj != null && obj.GetType().Name == "ToolItemSkill")
                        {
                            try
                            {
                                string objectDisplayName = GetToolDisplayName(obj);
                                if (objectDisplayName == toolDisplayName)
                                {
                                    // Found the matching ToolItemSkill, use the same unlock logic as F9 hotkey
                                    Type toolType = obj.GetType();
                                    MethodInfo unlockMethod = toolType.GetMethod("Unlock");
                                    
                                    if (unlockMethod != null)
                                    {
                                        // Find PopupFlags enum in nested types (same as F9 logic)
                                        Type popupFlagsType = null;
                                        Type[] nestedTypes = toolType.GetNestedTypes();
                                        foreach (Type nested in nestedTypes)
                                        {
                                            if (nested.Name.Contains("PopupFlags"))
                                            {
                                                popupFlagsType = nested;
                                                break;
                                            }
                                        }
                                        
                                        if (popupFlagsType != null)
                                        {
                                            object itemGetFlag = Enum.Parse(popupFlagsType, "ItemGet");
                                            unlockMethod.Invoke(obj, new object[] { null, itemGetFlag });
                                        }
                                        else
                                        {
                                            unlockMethod.Invoke(obj, new object[] { null, null });
                                        }
                                        
                                        ShowToast($"Unlocked {toolDisplayName}!");
                                        found = true;
                                        return;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                MelonLogger.Msg($"Error processing ToolItemSkill {obj.name}: {e.Message}");
                            }
                        }
                    }
                }
                
                if (!found)
                {
                    ShowToast($"Could not find tool: {toolDisplayName}");
                }
            }
            catch (Exception e)
            {
                ShowToast($"Error unlocking {toolDisplayName}: {e.Message}");
            }
        }
        
        
        private void SetToolStorage(string toolDisplayName, int amount)
        {
            try
            {
                UnityEngine.Object[] allObjects = Resources.FindObjectsOfTypeAll(typeof(ToolItemBasic));
                
                foreach (UnityEngine.Object obj in allObjects)
                {
                    if (obj != null)
                    {
                        try
                        {
                            string objectDisplayName = GetToolDisplayName(obj);
                            
                            if (objectDisplayName == toolDisplayName)
                            {
                                // Find baseStorageAmount field in the inheritance hierarchy
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
                                    // Check if this tool actually has storage (> 0) before allowing modification
                                    int currentAmount = (int)baseStorageField.GetValue(obj);
                                    
                                    if (currentAmount > 0)
                                    {
                                        baseStorageField.SetValue(obj, amount);
                                        ShowToast($"Set {toolDisplayName} storage to {amount}!");
                                        return;
                                    }
                                    else
                                    {
                                        ShowToast($"{toolDisplayName} doesn't use storage (action tool)");
                                        return;
                                    }
                                }
                                else
                                {
                                    ShowToast($"{toolDisplayName} has no storage field");
                                    return;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            MelonLogger.Msg($"Error processing tool {obj.name}: {e.Message}");
                        }
                    }
                }
                
                ShowToast($"Could not find tool: {toolDisplayName}");
            }
            catch (Exception e)
            {
                ShowToast($"Error setting storage: {e.Message}");
            }
        }
        
        private void RefillToolAmmo(string toolDisplayName)
        {
            try
            {
                UnityEngine.Object[] allObjects = Resources.FindObjectsOfTypeAll(typeof(ToolItemBasic));
                
                foreach (UnityEngine.Object obj in allObjects)
                {
                    if (obj != null)
                    {
                        try
                        {
                            string objectDisplayName = GetToolDisplayName(obj);
                            
                            if (objectDisplayName == toolDisplayName)
                            {
                                // Check if this tool has storage before trying to refill
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
                                    int currentAmount = (int)baseStorageField.GetValue(obj);
                                    
                                    if (currentAmount > 0)
                                    {
                                        // Tool has storage, use CollectFree to refill ammo
                                        Type toolType = obj.GetType();
                                        MethodInfo collectFreeMethod = toolType.GetMethod("CollectFree", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                        
                                        if (collectFreeMethod != null)
                                        {
                                            collectFreeMethod.Invoke(obj, new object[] { 9999 });
                                            ShowToast($"Refilled {toolDisplayName} ammo!");
                                            return;
                                        }
                                        else
                                        {
                                            ShowToast($"CollectFree method not found for {toolDisplayName}");
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        ShowToast($"{toolDisplayName} doesn't use ammo (action tool)");
                                        return;
                                    }
                                }
                                else
                                {
                                    ShowToast($"{toolDisplayName} has no storage field");
                                    return;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            MelonLogger.Msg($"Error processing tool {obj.name}: {e.Message}");
                        }
                    }
                }
                
                ShowToast($"Could not find tool: {toolDisplayName}");
            }
            catch (Exception e)
            {
                ShowToast($"Error refilling ammo: {e.Message}");
            }
        }
        
        private void UnlockDoubleJump()
        {
            try
            {
                if (SetPlayerDataBool("hasDoubleJump", true))
                {
                    ShowToast("Double Jump unlocked!");
                    MelonLogger.Msg("Successfully unlocked double jump (playerData.hasDoubleJump = true)");
                }
                else
                {
                    ShowToast("Failed to unlock Double Jump - enter game first");
                    MelonLogger.Msg("Failed to set playerData.hasDoubleJump");
                }
            }
            catch (Exception e)
            {
                ShowToast($"Error unlocking Double Jump: {e.Message}");
                MelonLogger.Msg($"Error in UnlockDoubleJump: {e.Message}");
            }
        }
        
        private void UnlockDash()
        {
            try
            {
                if (SetPlayerDataBool("hasDash", true))
                {
                    ShowToast("Dash unlocked!");
                    MelonLogger.Msg("Successfully unlocked dash (playerData.hasDash = true)");
                }
                else
                {
                    ShowToast("Failed to unlock Dash - enter game first");
                    MelonLogger.Msg("Failed to set playerData.hasDash");
                }
            }
            catch (Exception e)
            {
                ShowToast($"Error unlocking Dash: {e.Message}");
                MelonLogger.Msg($"Error in UnlockDash: {e.Message}");
            }
        }
        
        private void UnlockWallJump()
        {
            try
            {
                if (SetPlayerDataBool("hasWalljump", true))
                {
                    ShowToast("Wall Jump unlocked!");
                    MelonLogger.Msg("Successfully unlocked wall jump (playerData.hasWalljump = true)");
                }
                else
                {
                    ShowToast("Failed to unlock Wall Jump - enter game first");
                    MelonLogger.Msg("Failed to set playerData.hasWalljump");
                }
            }
            catch (Exception e)
            {
                ShowToast($"Error unlocking Wall Jump: {e.Message}");
                MelonLogger.Msg($"Error in UnlockWallJump: {e.Message}");
            }
        }
        
        private void UnlockChargeAttack()
        {
            try
            {
                bool playerDataSet = SetPlayerDataBool("hasChargeSlash", true);
                bool configSet = SetHeroConfigBool("canNailCharge", true);
                
                if (playerDataSet && configSet)
                {
                    ShowToast("Charge Attack fully unlocked! (PlayerData + Config)");
                    MelonLogger.Msg("Successfully unlocked charge attack (playerData.hasChargeSlash = true, Config.canNailCharge = true)");
                }
                else if (playerDataSet)
                {
                    ShowToast("Charge Attack partially unlocked (PlayerData only)");
                    MelonLogger.Msg("Set playerData.hasChargeSlash = true, but failed to set Config.canNailCharge");
                }
                else
                {
                    ShowToast("Failed to unlock Charge Attack - enter game first");
                    MelonLogger.Msg("Failed to set playerData.hasChargeSlash");
                }
            }
            catch (Exception e)
            {
                ShowToast($"Error unlocking Charge Attack: {e.Message}");
                MelonLogger.Msg($"Error in UnlockChargeAttack: {e.Message}");
            }
        }
        
        private void UnlockNeedolin()
        {
            try
            {
                // Needolin requires BOTH playerData.hasNeedolin AND Config.canPlayNeedolin
                bool playerDataSet = SetPlayerDataBool("hasNeedolin", true);
                bool configSet = SetHeroConfigBool("canPlayNeedolin", true);
                
                if (playerDataSet && configSet)
                {
                    ShowToast("Needolin unlocked! 🎵");
                    MelonLogger.Msg("Successfully unlocked Needolin (playerData.hasNeedolin = true, Config.canPlayNeedolin = true)");
                }
                else if (playerDataSet)
                {
                    ShowToast("Needolin partially unlocked (PlayerData only)");
                    MelonLogger.Msg("Set playerData.hasNeedolin = true, but failed to set Config.canPlayNeedolin");
                }
                else
                {
                    ShowToast("Failed to unlock Needolin - enter game first");
                    MelonLogger.Msg("Failed to set playerData.hasNeedolin");
                }
            }
            catch (Exception e)
            {
                ShowToast($"Error unlocking Needolin: {e.Message}");
                MelonLogger.Msg($"Error in UnlockNeedolin: {e.Message}");
            }
        }
        
        private void UnlockGlide()
        {
            try
            {
                // Glide requires BOTH playerData.hasBrolly AND Config.canBrolly
                bool playerDataSet = SetPlayerDataBool("hasBrolly", true);
                bool configSet = SetHeroConfigBool("canBrolly", true);
                
                if (playerDataSet && configSet)
                {
                    ShowToast("Glide/Drifter's Cloak unlocked! ☂️");
                    MelonLogger.Msg("Successfully unlocked Glide (playerData.hasBrolly = true, Config.canBrolly = true)");
                }
                else if (playerDataSet)
                {
                    ShowToast("Glide partially unlocked (PlayerData only)");
                    MelonLogger.Msg("Set playerData.hasBrolly = true, but failed to set Config.canBrolly");
                }
                else
                {
                    ShowToast("Failed to unlock Glide - enter game first");
                    MelonLogger.Msg("Failed to set playerData.hasBrolly");
                }
            }
            catch (Exception e)
            {
                ShowToast($"Error unlocking Glide: {e.Message}");
                MelonLogger.Msg($"Error in UnlockGlide: {e.Message}");
            }
        }
        
        private void UnlockGrapplingHook()
        {
            try
            {
                // Grappling Hook requires BOTH playerData.hasHarpoonDash AND Config.canHarpoonDash
                bool playerDataSet = SetPlayerDataBool("hasHarpoonDash", true);
                bool configSet = SetHeroConfigBool("canHarpoonDash", true);
                
                if (playerDataSet && configSet)
                {
                    ShowToast("Grappling Hook unlocked! 🎣 (Clawline Ancestral Art)");
                    MelonLogger.Msg("Successfully unlocked Grappling Hook (playerData.hasHarpoonDash = true, Config.canHarpoonDash = true)");
                }
                else if (playerDataSet)
                {
                    ShowToast("Grappling Hook partially unlocked (PlayerData only)");
                    MelonLogger.Msg("Set playerData.hasHarpoonDash = true, but failed to set Config.canHarpoonDash");
                }
                else
                {
                    ShowToast("Failed to unlock Grappling Hook - enter game first");
                    MelonLogger.Msg("Failed to set playerData.hasHarpoonDash");
                }
            }
            catch (Exception e)
            {
                ShowToast($"Error unlocking Grappling Hook: {e.Message}");
                MelonLogger.Msg($"Error in UnlockGrapplingHook: {e.Message}");
            }
        }
        
        // TODO: Re-implement these after learning more about their requirements
        /*
        private void UnlockHarpoonDash()
        {
            try
            {
                if (SetPlayerDataBool("hasHarpoonDash", true))
                {
                    ShowToast("Harpoon Dash unlocked!");
                    MelonLogger.Msg("Successfully unlocked harpoon dash (playerData.hasHarpoonDash = true)");
                }
                else
                {
                    ShowToast("Failed to unlock Harpoon Dash - enter game first");
                    MelonLogger.Msg("Failed to set playerData.hasHarpoonDash");
                }
            }
            catch (Exception e)
            {
                ShowToast($"Error unlocking Harpoon Dash: {e.Message}");
                MelonLogger.Msg($"Error in UnlockHarpoonDash: {e.Message}");
            }
        }
        
        private void UnlockSuperJump()
        {
            try
            {
                // Super Jump requires both hasSuperJump AND hasHarpoonDash
                bool superJumpSet = SetPlayerDataBool("hasSuperJump", true);
                bool harpoonDashSet = SetPlayerDataBool("hasHarpoonDash", true);
                
                if (superJumpSet && harpoonDashSet)
                {
                    ShowToast("Super Jump unlocked! (includes Harpoon Dash)");
                    MelonLogger.Msg("Successfully unlocked super jump (playerData.hasSuperJump = true, playerData.hasHarpoonDash = true)");
                }
                else
                {
                    ShowToast("Failed to unlock Super Jump - enter game first");
                    MelonLogger.Msg($"Failed to set Super Jump requirements (hasSuperJump: {superJumpSet}, hasHarpoonDash: {harpoonDashSet})");
                }
            }
            catch (Exception e)
            {
                ShowToast($"Error unlocking Super Jump: {e.Message}");
                MelonLogger.Msg($"Error in UnlockSuperJump: {e.Message}");
            }
        }
        */
        
        private void ToggleInfiniteAirJump()
        {
            try
            {
                infiniteAirJumpEnabled = !infiniteAirJumpEnabled;
                
                if (SetPlayerDataBool("infiniteAirJump", infiniteAirJumpEnabled))
                {
                    string status = infiniteAirJumpEnabled ? "enabled" : "disabled";
                    ShowToast($"Infinite Air Jump {status}!");
                    MelonLogger.Msg($"Successfully {status} infinite air jump (playerData.infiniteAirJump = {infiniteAirJumpEnabled})");
                }
                else
                {
                    // Revert the toggle if setting failed
                    infiniteAirJumpEnabled = !infiniteAirJumpEnabled;
                    ShowToast("Failed to toggle Infinite Air Jump - enter game first");
                    MelonLogger.Msg("Failed to set playerData.infiniteAirJump");
                }
            }
            catch (Exception e)
            {
                // Revert the toggle if error occurred
                infiniteAirJumpEnabled = !infiniteAirJumpEnabled;
                ShowToast($"Error toggling Infinite Air Jump: {e.Message}");
                MelonLogger.Msg($"Error in ToggleInfiniteAirJump: {e.Message}");
            }
        }
        
        private bool SelectedToolUsesAmmo()
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
                
                // Check ToolItemSkill objects FIRST - they never use ammo
                UnityEngine.Object[] skillObjects = Resources.FindObjectsOfTypeAll(typeof(ToolItemSkill));
                
                foreach (UnityEngine.Object obj in skillObjects)
                {
                    if (obj != null)
                    {
                        string objectDisplayName = GetToolDisplayName(obj);
                        
                        if (objectDisplayName == selectedTool)
                        {
                            // ToolItemSkill objects don't use ammo (they're skills, not consumables)
                            lastAmmoCheckToolIndex = selectedToolIndex;
                            lastAmmoCheckResult = false;
                            return false;
                        }
                    }
                }
                
                // Then check ToolItemBasic objects
                UnityEngine.Object[] allObjects = Resources.FindObjectsOfTypeAll(typeof(ToolItemBasic));
                
                foreach (UnityEngine.Object obj in allObjects)
                {
                    if (obj != null)
                    {
                        string objectDisplayName = GetToolDisplayName(obj);
                        
                        if (objectDisplayName == selectedTool)
                        {
                            // Check if tool has baseStorageAmount > 0
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
                                int currentAmount = (int)baseStorageField.GetValue(obj);
                                bool hasAmmo = currentAmount > 0;
                                lastAmmoCheckToolIndex = selectedToolIndex;
                                lastAmmoCheckResult = hasAmmo;
                                return hasAmmo; // Tool uses ammo if storage > 0
                            }
                        }
                    }
                }
            }
            catch { }
            
            // Cache the result
            lastAmmoCheckToolIndex = selectedToolIndex;
            lastAmmoCheckResult = false;
            return false;
        }
        
        private int GetSelectedToolCurrentStorage()
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
                UnityEngine.Object[] allObjects = Resources.FindObjectsOfTypeAll(typeof(ToolItemBasic));
                
                foreach (UnityEngine.Object obj in allObjects)
                {
                    if (obj != null)
                    {
                        string objectDisplayName = GetToolDisplayName(obj);
                        
                        if (objectDisplayName == selectedTool)
                        {
                            // Get current baseStorageAmount value
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
                                return (int)baseStorageField.GetValue(obj);
                            }
                        }
                    }
                }
                
                // Also check ToolItemSkill objects
                UnityEngine.Object[] skillObjects = Resources.FindObjectsOfTypeAll(typeof(ToolItemSkill));
                
                foreach (UnityEngine.Object obj in skillObjects)
                {
                    if (obj != null)
                    {
                        string objectDisplayName = GetToolDisplayName(obj);
                        
                        if (objectDisplayName == selectedTool)
                        {
                            // Get current baseStorageAmount value
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
                                return (int)baseStorageField.GetValue(obj);
                            }
                            else
                            {
                                // ToolItemSkill without baseStorageAmount field = no storage
                                return 0;
                            }
                        }
                    }
                }
            }
            catch { }
            
            return 0;
        }
        
        private bool SetHeroConfigBool(string fieldName, bool value)
        {
            try
            {
                // Get HeroController instance
                var heroController = GameObject.FindFirstObjectByType<HeroController>();
                if (heroController == null)
                {
                    MelonLogger.Msg($"HeroController not found - cannot set {fieldName}");
                    return false;
                }

                // Get the Config property
                var configProperty = heroController.GetType().GetProperty("Config");
                if (configProperty == null)
                {
                    MelonLogger.Msg("Config property not found on HeroController");
                    return false;
                }

                var config = configProperty.GetValue(heroController);
                if (config == null)
                {
                    MelonLogger.Msg("Config object is null");
                    return false;
                }

                // Get the field using reflection (it's private, so we need NonPublic)
                var field = config.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null)
                {
                    MelonLogger.Msg($"Field '{fieldName}' not found in HeroControllerConfig");
                    return false;
                }

                // Set the field value
                field.SetValue(config, value);
                MelonLogger.Msg($"Successfully set HeroControllerConfig.{fieldName} = {value}");
                return true;
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Error setting HeroControllerConfig.{fieldName}: {e.Message}");
                return false;
            }
        }
        
        private void ScanDamageFields()
        {
            if (fieldsScanned) return;
            
            damageFields.Clear();
            damageBehaviours.Clear();
            originalValues.Clear();
            
            // Use Resources.FindObjectsOfTypeAll instead of FindObjectsByType
            // This finds ALL objects, including inactive ones and those in other scenes
            UnityEngine.Object[] allObjects = Resources.FindObjectsOfTypeAll(typeof(MonoBehaviour));
            MelonLogger.Msg($"Scanning {allObjects.Length} total objects for damage fields");
            
            foreach (UnityEngine.Object obj in allObjects)
            {
                if (obj == null) continue;
                
                MonoBehaviour behaviour = obj as MonoBehaviour;
                if (behaviour == null) continue;
                
                Type type = behaviour.GetType();
                string typeName = type.Name.ToLower();
                
                // Focus on DamageEnemies and similar combat components
                if (typeName.Contains("damage") || typeName.Contains("attack") || typeName.Contains("combat") || 
                    typeName.Contains("enemy") || typeName.Contains("weapon") || typeName.Contains("projectile"))
                {
                    FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    
                    foreach (FieldInfo field in fields)
                    {
                        string fieldName = field.Name.ToLower();
                        
                        // Look for damage-related fields
                        if ((fieldName.Contains("damage") || fieldName.Contains("multiplier") || fieldName.Contains("power") ||
                             fieldName.Contains("strength") || fieldName.Contains("force"))
                            && (field.FieldType == typeof(float) || field.FieldType == typeof(int)))
                        {
                            try
                            {
                                object currentValue = field.GetValue(behaviour);
                                if (currentValue != null)
                                {
                                    damageFields.Add(field);
                                    damageBehaviours.Add(behaviour);
                                    originalValues[field] = currentValue;
                                    MelonLogger.Msg($"Found: {type.Name}.{field.Name} = {currentValue}");
                                }
                            }
                            catch (Exception ex)
                            {
                                // Skip inaccessible fields
                                MelonLogger.Msg($"Skipped {type.Name}.{field.Name}: {ex.Message}");
                            }
                        }
                    }
                }
            }
            
            fieldsScanned = true;
            MelonLogger.Msg($"Scanned {damageFields.Count} damage-related fields");
        }
        
        private void ApplyGlobalMultiplier()
        {
            if (!float.TryParse(globalMultiplierText, out globalMultiplier))
            {
                ShowToast("Invalid multiplier value!");
                return;
            }
            
            int modifiedCount = 0;
            HashSet<string> uniqueFields = new HashSet<string>();
            
            for (int i = 0; i < damageFields.Count; i++)
            {
                FieldInfo field = damageFields[i];
                MonoBehaviour behaviour = damageBehaviours[i];
                
                if (behaviour == null || !originalValues.ContainsKey(field)) continue;
                
                try
                {
                    object originalValue = originalValues[field];
                    
                    if (field.FieldType == typeof(float))
                    {
                        float original = (float)originalValue;
                        float newValue = original * globalMultiplier;
                        field.SetValue(behaviour, newValue);
                        modifiedCount++;
                        uniqueFields.Add(field.Name); // Track unique field names
                    }
                    else if (field.FieldType == typeof(int))
                    {
                        int original = (int)originalValue;
                        int newValue = Mathf.RoundToInt(original * globalMultiplier);
                        field.SetValue(behaviour, newValue);
                        modifiedCount++;
                        uniqueFields.Add(field.Name); // Track unique field names
                    }
                }
                catch (Exception)
                {
                    // Skip read-only fields
                }
            }
            
            ShowToast($"Applied {globalMultiplier}x to {uniqueFields.Count} unique fields ({modifiedCount} total)");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            base.OnSceneWasLoaded(buildIndex, sceneName);
            MelonLogger.Msg($"Scene: {sceneName}");
            heroController = null; // Reset hero controller for new scene
        }

        public override void OnGUI()
        {
            if (showGUI)
            {
                // Set window background opacity (more opaque than default)
                Color originalBackground = GUI.backgroundColor;
                GUI.backgroundColor = new Color(originalBackground.r, originalBackground.g, originalBackground.b, 1.0f);
                
                windowRect = GUI.Window(0, windowRect, GuiWindow, "SILKSONG CHEATS");
                
                // Reset background color
                GUI.backgroundColor = originalBackground;
            }

            // Draw confirmation modal as separate window if needed (always check, regardless of showGUI)
            if (showConfirmModal)
            {
                // Create solid black overlay first
                if (solidBlackTexture == null)
                {
                    solidBlackTexture = new Texture2D(1, 1);
                    solidBlackTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.8f));
                    solidBlackTexture.Apply();
                }
                
                // Draw full screen overlay
                GUI.depth = -1000;
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), solidBlackTexture);
                
                // Center the modal on screen
                modalWindowRect.x = (Screen.width - modalWindowRect.width) / 2;
                modalWindowRect.y = (Screen.height - modalWindowRect.height) / 2;
                
                // Draw modal window with high depth priority and full opacity
                GUI.depth = -999;
                Color originalBackground = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1.0f); // Dark gray, fully opaque
                modalWindowRect = GUI.Window(54321, modalWindowRect, DrawConfirmModal, $"Confirm {confirmActionName}");
                GUI.backgroundColor = originalBackground;
                GUI.depth = 0;
            }
        }

        private void GuiWindow(int windowID)
        {
            GUILayout.BeginVertical();

            // Title
            GUILayout.Label("SILKSONG CHEATS", GUI.skin.box);
            GUILayout.Space(5);

            // Tab buttons
            GUILayout.BeginHorizontal();
            for (int i = 0; i < tabNames.Length; i++)
            {
                GUI.color = selectedTab == i ? Color.cyan : Color.white;
                if (GUILayout.Button(tabNames[i]))
                {
                    selectedTab = i;
                    if (selectedTab == 1) // Balance tab
                    {
                        ScanDamageFields();
                    }
                }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Tab content
            if (selectedTab == 0)
            {
                DrawCheatsTab();
            }
            else if (selectedTab == 1)
            {
                DrawBalanceTab();
            }

            // Toast notification area (fixed at bottom)
            if (toastTimer > 0f)
            {
                GUI.color = new Color(0.2f, 0.8f, 0.2f, toastTimer / TOAST_DURATION);
                GUILayout.Label(lastToastMessage, GUI.skin.box);
                GUI.color = Color.white;
            }

            GUILayout.Space(5);
            if (GUILayout.Button("Close"))
            {
                showGUI = false;
                if (universeLibInitialized && uiBase != null)
                {
                    UniversalUI.SetUIActive("SilkSongCheatGUI", false);
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

            GUILayout.EndVertical();
            
            
            GUI.DragWindow(new Rect(0, 0, windowRect.width, 30));
        }

        private void DrawConfirmModal(int windowID)
        {
            GUILayout.BeginVertical();
            
            // Message with better styling
            GUI.color = Color.white;
            GUIStyle messageStyle = new GUIStyle(GUI.skin.label);
            messageStyle.wordWrap = true;
            messageStyle.normal.textColor = Color.white;
            messageStyle.fontSize = 14;
            messageStyle.padding = new RectOffset(10, 10, 10, 10);
            
            GUILayout.Label(confirmMessage, messageStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(80));
            
            GUILayout.FlexibleSpace();
            
            // Buttons
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            // Yes button (positive action - green)
            GUI.color = Color.green;
            if (GUILayout.Button("Yes", GUILayout.Width(100), GUILayout.Height(30)))
            {
                pendingAction?.Invoke();
                showConfirmModal = false;
                pendingAction = null;
                modalCooldownTime = Time.time + 0.2f;
            }
            
            GUILayout.Space(20);
            
            // No button (neutral - white)
            GUI.color = Color.white;
            if (GUILayout.Button("No", GUILayout.Width(100), GUILayout.Height(30)))
            {
                showConfirmModal = false;
                pendingAction = null;
                modalCooldownTime = Time.time + 0.2f;
            }
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            GUILayout.EndVertical();
            GUI.color = Color.white;
        }
        
        private bool DrawCollapsingHeader(string title, bool isExpanded)
        {
            // Create arrow icon based on state
            string arrow = isExpanded ? "▼" : "►";
            string displayText = $"{arrow} {title}";
            
            // Use a darker color for collapsed sections
            if (!isExpanded)
            {
                GUI.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            }
            
            bool clicked = GUILayout.Button(displayText, GUILayout.Height(25));
            
            GUI.color = Color.white; // Reset color
            
            return clicked ? !isExpanded : isExpanded;
        }

        private void DrawCheatsTab()
        {
            // Begin scroll view
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(380), GUILayout.Height(windowRect.height - 120));

            // Toggle Features section
            showToggleFeatures = DrawCollapsingHeader("Toggle Features", showToggleFeatures);
            if (showToggleFeatures)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                
            bool newAutoSilk = GUILayout.Toggle(autoRefillSilk, "Auto Silk Refill (every 2 seconds)");
            if (newAutoSilk != autoRefillSilk)
            {
                ToggleAutoSilkRefill();
                ShowToast($"Auto Silk Refill: {(autoRefillSilk ? "Enabled" : "Disabled")}");
            }
            
            bool newOneHitKill = GUILayout.Toggle(oneHitKillEnabled, "One Hit Kill Mode");
            if (newOneHitKill != oneHitKillEnabled)
            {
                EnableOneHitKill();
                ShowToast($"One Hit Kill: {(oneHitKillEnabled ? "Enabled" : "Disabled")}");
            }

            bool newInfiniteAirJump = GUILayout.Toggle(infiniteAirJumpEnabled, "Infinite Air Jump");
            if (newInfiniteAirJump != infiniteAirJumpEnabled)
            {
                ToggleInfiniteAirJump();
            }

                GUILayout.EndVertical();
            }

            GUILayout.Space(5);

            // Action Amounts section
            showActionAmounts = DrawCollapsingHeader("Action Amounts", showActionAmounts);
            if (showActionAmounts)
            {
                GUILayout.BeginVertical(GUI.skin.box);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Health:", GUILayout.Width(80));
            healthAmount = GUILayout.TextField(healthAmount, GUILayout.Width(60));
            if (GUILayout.Button("Add", GUILayout.Width(50)))
            {
                if (int.TryParse(healthAmount, out int health))
                {
                    AddHealth(health);
                    ShowToast($"Added {health} health!");
                }
            }
            GUILayout.EndHorizontal();

            // Set Health input and button
            GUILayout.BeginHorizontal();
            GUILayout.Label("Set Health:", GUILayout.Width(80));
            setHealthAmount = GUILayout.TextField(setHealthAmount, GUILayout.Width(60));
            if (GUILayout.Button("Set", GUILayout.Width(50)))
            {
                if (int.TryParse(setHealthAmount, out int targetHealth))
                {
                    ShowConfirmation("Set Health", 
                        $"This will set health to {targetHealth}. You must quit to main menu and restart to see the effect in game. Max amount that will show in UI is 11.", 
                        () => {
                            SetMaxHealthExact(targetHealth);
                            ShowToast($"Set max health to {targetHealth} - Save & reload to see UI!");
                        });
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Money:", GUILayout.Width(80));
            moneyAmount = GUILayout.TextField(moneyAmount, GUILayout.Width(60));
            if (GUILayout.Button("Add", GUILayout.Width(50)))
            {
                if (int.TryParse(moneyAmount, out int money))
                {
                    AddMoney(money);
                    ShowToast($"Added {money} money!");
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Shards:", GUILayout.Width(80));
            shardAmount = GUILayout.TextField(shardAmount, GUILayout.Width(60));
            if (GUILayout.Button("Add", GUILayout.Width(50)))
            {
                if (int.TryParse(shardAmount, out int shards))
                {
                    AddShards(shards);
                    ShowToast($"Added {shards} shards!");
                }
            }
            GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }

            GUILayout.Space(5);

            // Collectible Items section
            showCollectibleItems = DrawCollapsingHeader("Collectible Items", showCollectibleItems);
            if (showCollectibleItems)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                
                // Scan collectables on first access
                if (!collectablesScanned)
                {
                    ScanCollectables();
                }
                
                if (collectableNames.Length > 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Item:", GUILayout.Width(80));
                    
                    // Dropdown button
                    string currentSelection = selectedCollectableIndex < collectableNames.Length ? collectableNames[selectedCollectableIndex] : "Select Item";
                    
                    if (GUILayout.Button($"{currentSelection} ▼", GUILayout.Width(180)))
                    {
                        showCollectableDropdown = !showCollectableDropdown;
                        if (showCollectableDropdown)
                        {
                            collectableSearchFilter = ""; // Reset search when opening
                            FilterCollectables();
                        }
                    }
                    GUILayout.EndHorizontal();
                    
                    // Dropdown with search
                    if (showCollectableDropdown)
                    {
                        GUILayout.BeginVertical(GUI.skin.box);
                        
                        // Search box
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Search:", GUILayout.Width(50));
                        string newFilter = GUILayout.TextField(collectableSearchFilter, GUILayout.Width(130));
                        if (newFilter != collectableSearchFilter)
                        {
                            collectableSearchFilter = newFilter;
                            FilterCollectables();
                            collectableDropdownScroll = Vector2.zero; // Reset scroll when filtering
                        }
                        GUILayout.EndHorizontal();
                        
                        // Clear search button
                        if (!string.IsNullOrEmpty(collectableSearchFilter))
                        {
                            if (GUILayout.Button("Clear", GUILayout.Width(60)))
                            {
                                collectableSearchFilter = "";
                                FilterCollectables();
                            }
                        }
                        
                        // Scrollable filtered list
                        collectableDropdownScroll = GUILayout.BeginScrollView(collectableDropdownScroll, GUILayout.Height(150));
                        
                        if (filteredCollectableNames.Length > 0)
                        {
                            for (int i = 0; i < filteredCollectableNames.Length; i++)
                            {
                                string itemName = filteredCollectableNames[i];
                                
                                // Find the index in the original array
                                int originalIndex = Array.IndexOf(collectableNames, itemName);
                                
                                if (GUILayout.Button(itemName, GUI.skin.label))
                                {
                                    selectedCollectableIndex = originalIndex;
                                    showCollectableDropdown = false;
                                    collectableSearchFilter = ""; // Clear search after selection
                                }
                            }
                        }
                        else
                        {
                            GUILayout.Label("No items match search", GUI.skin.label);
                        }
                        
                        GUILayout.EndScrollView();
                        GUILayout.EndVertical();
                    }
                
                GUILayout.BeginHorizontal();
                GUILayout.Label("Amount:", GUILayout.Width(80));
                collectableAmount = GUILayout.TextField(collectableAmount, GUILayout.Width(60));
                if (GUILayout.Button("Set", GUILayout.Width(50)))
                {
                    if (int.TryParse(collectableAmount, out int amount) && selectedCollectableIndex < collectableNames.Length)
                    {
                        string selectedCollectable = collectableNames[selectedCollectableIndex];
                        SetCollectableAmount(selectedCollectable, amount);
                        showCollectableDropdown = false; // Close dropdown after action
                    }
                    else
                    {
                        ShowToast("Invalid amount or no item selected!");
                    }
                }
                GUILayout.EndHorizontal();
            }
                else
                {
                    GUILayout.Label("No collectables found - enter game first", GUI.skin.label);
                    if (GUILayout.Button("Refresh List", GUILayout.Width(100)))
                    {
                        collectablesScanned = false; // Force rescan
                        ScanCollectables();
                    }
                }
                
                GUILayout.EndVertical();
            }

            GUILayout.Space(5);

            // Crest Tools section
            showCrestTools = DrawCollapsingHeader("Crest Tools", showCrestTools);
            if (showCrestTools)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                
                // Skills-only filter checkbox
                GUILayout.BeginHorizontal();
                bool newShowSkillsOnly = GUILayout.Toggle(showSkillsOnly, "Skills Only", GUILayout.Width(100));
                if (newShowSkillsOnly != showSkillsOnly)
                {
                    showSkillsOnly = newShowSkillsOnly;
                    selectedToolIndex = 0; // Reset selection
                    FilterTools(); // Update filtered list
                    
                    // Reset cache since tool selection changed
                    lastAmmoCheckToolIndex = -1;
                    lastStorageCheckToolIndex = -1;
                }
                GUILayout.EndHorizontal();
                
                // Scan tools on first access
                if (!toolsScanned)
                {
                    ScanTools();
                }
                
                if (toolNames.Length > 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Tool:", GUILayout.Width(80));
                    
                    // Dropdown button
                    string currentSelection = selectedToolIndex < toolNames.Length ? toolNames[selectedToolIndex] : "Select Tool";
                    
                    if (GUILayout.Button($"{currentSelection} ▼", GUILayout.Width(180)))
                    {
                        showToolDropdown = !showToolDropdown;
                        if (showToolDropdown)
                        {
                            toolSearchFilter = ""; // Reset search when opening
                            FilterTools();
                        }
                    }
                    GUILayout.EndHorizontal();
                    
                    // Dropdown with search
                    if (showToolDropdown)
                    {
                        GUILayout.BeginVertical(GUI.skin.box);
                        
                        // Search box
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Search:", GUILayout.Width(50));
                        string newFilter = GUILayout.TextField(toolSearchFilter, GUILayout.Width(130));
                        if (newFilter != toolSearchFilter)
                        {
                            toolSearchFilter = newFilter;
                            FilterTools();
                            toolDropdownScroll = Vector2.zero; // Reset scroll when filtering
                        }
                        GUILayout.EndHorizontal();
                        
                        // Clear search button
                        if (!string.IsNullOrEmpty(toolSearchFilter))
                        {
                            if (GUILayout.Button("Clear", GUILayout.Width(60)))
                            {
                                toolSearchFilter = "";
                                FilterTools();
                            }
                        }
                        
                        // Scrollable filtered list
                        toolDropdownScroll = GUILayout.BeginScrollView(toolDropdownScroll, GUILayout.Height(150));
                        
                        if (filteredToolNames.Length > 0)
                        {
                            for (int i = 0; i < filteredToolNames.Length; i++)
                            {
                                string toolName = filteredToolNames[i];
                                
                                // Find the index in the original array
                                int originalIndex = Array.IndexOf(toolNames, toolName);
                                
                                if (GUILayout.Button(toolName, GUI.skin.label))
                                {
                                    selectedToolIndex = originalIndex;
                                    showToolDropdown = false;
                                    toolSearchFilter = ""; // Clear search after selection
                                }
                            }
                        }
                        else
                        {
                            GUILayout.Label("No tools match search", GUI.skin.label);
                        }
                        
                        GUILayout.EndScrollView();
                        GUILayout.EndVertical();
                    }
                
                    // Unlock control
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Unlock Selected Tool", GUILayout.Width(150)))
                    {
                        if (selectedToolIndex < toolNames.Length)
                        {
                            string selectedTool = toolNames[selectedToolIndex];
                            UnlockSpecificTool(selectedTool);
                            showToolDropdown = false; // Close dropdown after action
                        }
                        else
                        {
                            ShowToast("No tool selected!");
                        }
                    }
                    GUILayout.EndHorizontal();
                    
                    // Only show ammo-related controls if selected tool uses ammo
                    if (SelectedToolUsesAmmo())
                    {
                        // Get current storage amount once
                        int currentStorage = GetSelectedToolCurrentStorage();
                        
                        // Update storage amount field when tool selection changes
                        if (lastSelectedToolIndex != selectedToolIndex)
                        {
                            if (currentStorage > 0)
                            {
                                toolStorageAmount = currentStorage.ToString();
                            }
                            lastSelectedToolIndex = selectedToolIndex;
                        }
                        
                        // Storage amount control with current value display
                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"Base Storage Amount (Current: {currentStorage}):", GUILayout.Width(200));
                        toolStorageAmount = GUILayout.TextField(toolStorageAmount, GUILayout.Width(60));
                        if (GUILayout.Button("Set", GUILayout.Width(50)))
                        {
                            if (int.TryParse(toolStorageAmount, out int amount) && selectedToolIndex < toolNames.Length)
                            {
                                string selectedTool = toolNames[selectedToolIndex];
                                SetToolStorage(selectedTool, amount);
                                showToolDropdown = false; // Close dropdown after action
                            }
                            else
                            {
                                ShowToast("Invalid amount or no tool selected!");
                            }
                        }
                        GUILayout.EndHorizontal();
                        
                        // Refill ammo control
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Refill Ammo", GUILayout.Width(100)))
                        {
                            if (selectedToolIndex < toolNames.Length)
                            {
                                string selectedTool = toolNames[selectedToolIndex];
                                RefillToolAmmo(selectedTool);
                                showToolDropdown = false; // Close dropdown after action
                            }
                            else
                            {
                                ShowToast("No tool selected!");
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                else
                {
                    GUILayout.Label("No tools found - enter game first", GUI.skin.label);
                    if (GUILayout.Button("Refresh List", GUILayout.Width(100)))
                    {
                        toolsScanned = false; // Force rescan
                        ScanTools();
                    }
                }
                
                GUILayout.EndVertical();
            }

            GUILayout.Space(5);

            // Player Skills section
            showPlayerSkills = DrawCollapsingHeader("Player Skills", showPlayerSkills);
            if (showPlayerSkills)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                
                // Two-column layout for better organization
                GUILayout.BeginHorizontal();
                
                // Left Column (Movement Abilities)
                GUILayout.BeginVertical();
                if (GUILayout.Button("Unlock Double Jump", GUILayout.Width(170)))
                {
                    UnlockDoubleJump();
                }
                if (GUILayout.Button("Unlock Dash", GUILayout.Width(170)))
                {
                    UnlockDash();
                }
                if (GUILayout.Button("Unlock Wall Jump", GUILayout.Width(170)))
                {
                    UnlockWallJump();
                }
                if (GUILayout.Button("Unlock Glide", GUILayout.Width(170)))
                {
                    UnlockGlide();
                }
                GUILayout.EndVertical();
                
                GUILayout.Space(10);
                
                // Right Column (Special Abilities)
                GUILayout.BeginVertical();
                if (GUILayout.Button("Unlock Charge Attack", GUILayout.Width(170)))
                {
                    UnlockChargeAttack();
                }
                if (GUILayout.Button("Unlock Needolin", GUILayout.Width(170)))
                {
                    UnlockNeedolin();
                }
                if (GUILayout.Button("Unlock Grappling Hook", GUILayout.Width(170)))
                {
                    UnlockGrapplingHook();
                }
                GUILayout.EndVertical();
                
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            GUILayout.Space(5);

            // Quick Actions section
            showQuickActions = DrawCollapsingHeader("Quick Actions", showQuickActions);
            if (showQuickActions)
            {
                GUILayout.BeginVertical(GUI.skin.box);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refill Health"))
            {
                RefillHealth();
                ShowToast("Health refilled to max!");
            }
            string ohkButtonText = oneHitKillEnabled ? "Disable One Hit Kill" : "Enable One Hit Kill";
            GUI.color = oneHitKillEnabled ? Color.green : Color.white;
            if (GUILayout.Button(ohkButtonText))
            {
                EnableOneHitKill();
                ShowToast($"One Hit Kill {(oneHitKillEnabled ? "enabled" : "disabled")}!");
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Unlock Crests"))
            {
                ShowConfirmation("Unlock All Crests", 
                    "This will unlock all crests. This action cannot be undone.", 
                    () => {
                        UnlockAllCrests();
                        ShowToast("All crests unlocked!");
                    });
            }
            if (GUILayout.Button("Unlock Crest Skills"))
            {
                ShowConfirmation("Unlock All Crest Skills", 
                    "This will unlock all crest skills. This action cannot be undone.", 
                    () => {
                        UnlockAllTools();
                        ShowToast("All crest skills unlocked!");
                    });
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Unlock Crest Tools"))
            {
                ShowConfirmation("Unlock All Crest Tools", 
                    "This will unlock all crest tools. This action cannot be undone.", 
                    () => {
                        UnlockAllItems();
                        ShowToast("All crest tools unlocked!");
                    });
            }
            if (GUILayout.Button("Max Collectables"))
            {
                ShowConfirmation("Max All Collectables", 
                    "This will maximize all collectible items. This action cannot be undone.", 
                    () => {
                        MaxAllCollectables();
                        ShowToast("All collectables maxed!");
                    });
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Unlock All Fast Travel Locations"))
            {
                ShowConfirmation("Unlock All Fast Travel", 
                    "This will unlock all fast travel locations. This action cannot be undone.", 
                    () => {
                        UnlockAllFastTravel();
                        ShowToast("All fast travel unlocked!");
                    });
            }
            GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }

            GUILayout.Space(5);

            // Keybind Settings section
            string keybindTitle = isSettingKeybind ? "Press any key (ESC to cancel)" : "Keybind Settings";
            showKeybindSettings = DrawCollapsingHeader(keybindTitle, showKeybindSettings);
            if (showKeybindSettings)
            {
                GUILayout.BeginVertical(GUI.skin.box);
            
            if (!isSettingKeybind)
            {
                // Enable/Disable All buttons
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Enable All Defaults", GUILayout.Width(120)))
                {
                    SetDefaultKeybinds();
                    ShowToast("All keybinds restored to defaults");
                }
                if (GUILayout.Button("Disable All", GUILayout.Width(80)))
                {
                    DisableAllKeybinds();
                    ShowToast("All keybinds disabled");
                }
                GUILayout.EndHorizontal();
                
                GUILayout.Space(5);
                
                for (int i = 0; i < keybindNames.Length; i++)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{keybindNames[i]}:", GUILayout.Width(100));
                    GUILayout.Label($"{currentKeybinds[i]}", GUILayout.Width(70));
                    if (GUILayout.Button("Set", GUILayout.Width(35)))
                    {
                        isSettingKeybind = true;
                        keybindToSet = i;
                        ShowToast($"Press key for {keybindNames[i]}");
                    }
                    if (GUILayout.Button("Clear", GUILayout.Width(45)))
                    {
                        currentKeybinds[i] = KeyCode.None;
                        ShowToast($"Cleared {keybindNames[i]} keybind");
                    }
                    GUILayout.EndHorizontal();
                }
                }
                
                GUILayout.EndVertical();
            }

            GUILayout.EndScrollView();
        }

        private void DrawBalanceTab()
        {
            // Begin scroll view
            balanceScrollPosition = GUILayout.BeginScrollView(balanceScrollPosition, GUILayout.Width(380), GUILayout.Height(windowRect.height - 120));

            GUILayout.Label("Damage Balance System", GUI.skin.box);
            GUILayout.Label("Adjust damage multiplier for easier gameplay without cheats feel", GUI.skin.label);
            GUILayout.Space(10);

            // Global multiplier controls
            GUILayout.BeginHorizontal();
            GUILayout.Label("Global Multiplier:", GUILayout.Width(120));
            globalMultiplierText = GUILayout.TextField(globalMultiplierText, GUILayout.Width(60));
            if (GUILayout.Button("Apply", GUILayout.Width(60)))
            {
                ApplyGlobalMultiplier();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(15);

            // Show current status (what users care about)
            if (fieldsScanned && damageFields.Count > 0)
            {
                if (Math.Abs(globalMultiplier - 1.0f) > 0.01f)
                {
                    GUI.color = Color.yellow;
                    GUILayout.Label($"Currently applied: {globalMultiplier:F2}x damage multiplier", GUI.skin.box);
                    GUI.color = Color.white;
                }
                else
                {
                    GUILayout.Label("No multiplier currently applied (normal damage)", GUI.skin.label);
                }
            }
            else if (fieldsScanned)
            {
                GUILayout.Label("No damage fields found - try changing scenes", GUI.skin.label);
            }
            else
            {
                GUILayout.Label("Click 'Refresh' to scan for damage fields", GUI.skin.label);
            }

            GUILayout.Space(15);

            // Optional detailed view toggle
            showDetails = GUILayout.Toggle(showDetails, "Show Technical Details");
            
            if (showDetails)
            {
                GUILayout.Space(5);
                
                // Examples and target info
                GUILayout.Label("Examples: 1.0 = Normal, 1.5 = +50% damage, 2.0 = Double damage", GUI.skin.label);
                GUILayout.Space(5);
                
                if (fieldsScanned && damageFields.Count > 0)
                {
                    GUILayout.Label("Target: DamageEnemies Components", GUI.skin.box);
                    GUILayout.Label($"Found {damageFields.Count} damage-related fields", GUI.skin.label);
                    GUILayout.Label("Multiplier will modify all damage values proportionally", GUI.skin.label);
                    GUILayout.Space(5);
                }
                
                GUILayout.Label($"All Fields ({damageFields.Count} found)", GUI.skin.box);
                
                // Show simplified technical list
                var uniqueFieldNames = new HashSet<string>();
                for (int i = 0; i < damageFields.Count; i++)
                {
                    FieldInfo field = damageFields[i];
                    MonoBehaviour behaviour = damageBehaviours[i];
                    
                    if (behaviour == null) continue;
                    
                    string fieldName = field.Name;
                    if (uniqueFieldNames.Contains(fieldName)) continue;
                    uniqueFieldNames.Add(fieldName);
                    
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(fieldName, GUILayout.Width(200));
                    
                    // Count how many instances
                    int instanceCount = 0;
                    for (int j = 0; j < damageFields.Count; j++)
                    {
                        if (damageBehaviours[j] != null && damageFields[j].Name == fieldName)
                            instanceCount++;
                    }
                    
                    GUILayout.Label($"({instanceCount} instances)", GUILayout.Width(100));
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndScrollView();
        }





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
            catch (Exception ex)
            {
                MelonLogger.Msg($"Error in FindTypeInAssemblies: {ex.Message}");
                return null;
            }
        }

        private Canvas GetUIManagerCanvas()
        {
            try
            {
                // Find UIManager
                Type uiManagerType = FindTypeInAssemblies("UIManager");
                if (uiManagerType == null) return null;

                PropertyInfo instanceProperty = uiManagerType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
                object uiManagerInstance = instanceProperty?.GetValue(null);
                if (uiManagerInstance == null) return null;

                // Get UICanvas field
                FieldInfo canvasField = uiManagerType.GetField("UICanvas", BindingFlags.Public | BindingFlags.Instance);
                Canvas uiCanvas = canvasField?.GetValue(uiManagerInstance) as Canvas;
                
                if (uiCanvas != null)
                {
                    MelonLogger.Msg($"✓ Found UIManager Canvas: {uiCanvas.name}");
                }

                return uiCanvas;
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Error getting UIManager Canvas: {e.Message}");
                return null;
            }
        }

        private GameObject CreateMenuGameObject(string name, Canvas parentCanvas)
        {
            try
            {
                // Create the main GameObject
                GameObject menuGO = new GameObject(name);

                // Add essential UI components like real menus have
                RectTransform rectTransform = menuGO.AddComponent<RectTransform>();
                CanvasRenderer canvasRenderer = menuGO.AddComponent<CanvasRenderer>();
                CanvasGroup canvasGroup = menuGO.AddComponent<CanvasGroup>();

                // Setup CanvasGroup properties (like real menus)
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;

                // Parent to UI Canvas if available
                if (parentCanvas != null)
                {
                    menuGO.transform.SetParent(parentCanvas.transform, false);
                    menuGO.layer = parentCanvas.gameObject.layer; // Set to UI layer
                    
                    // Setup RectTransform like real menus (full screen)
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                    
                    MelonLogger.Msg($"✓ Created menu GameObject parented to {parentCanvas.name}");
                }
                else
                {
                    // Fallback: set layer manually
                    menuGO.layer = LayerMask.NameToLayer("UI");
                    MelonLogger.Msg("✓ Created standalone menu GameObject");
                }

                return menuGO;
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Error creating menu GameObject: {e.Message}");
                return new GameObject(name); // Fallback to basic GameObject
            }
        }

        private void CreateMenuChildren(GameObject parentMenu)
        {
            try
            {
                MelonLogger.Msg("Creating menu children structure...");

                // Create Title child (like ControllerMenuScreen has)
                GameObject titleGO = new GameObject("Title");
                titleGO.transform.SetParent(parentMenu.transform, false);
                titleGO.AddComponent<RectTransform>();
                
                // Add TextMeshProUGUI for title
                Type textMeshType = FindTypeInAssemblies("TextMeshProUGUI");
                if (textMeshType != null)
                {
                    Component titleText = titleGO.AddComponent(textMeshType);
                    PropertyInfo textProperty = textMeshType.GetProperty("text");
                    textProperty?.SetValue(titleText, "DEBUG MENU");
                    MelonLogger.Msg("✓ Created Title child with text");
                }

                // Create Content child (container for menu items)
                GameObject contentGO = new GameObject("Content");
                contentGO.transform.SetParent(parentMenu.transform, false);
                RectTransform contentRect = contentGO.AddComponent<RectTransform>();
                contentGO.AddComponent<CanvasRenderer>();
                
                // Position content below title
                contentRect.anchorMin = new Vector2(0, 0);
                contentRect.anchorMax = new Vector2(1, 0.8f);
                contentRect.offsetMin = Vector2.zero;
                contentRect.offsetMax = Vector2.zero;
                
                MelonLogger.Msg("✓ Created Content child container");

                // Create Controls child (for button container)
                GameObject controlsGO = new GameObject("Controls");
                controlsGO.transform.SetParent(parentMenu.transform, false);
                RectTransform controlsRect = controlsGO.AddComponent<RectTransform>();
                controlsGO.AddComponent<CanvasRenderer>();

                // Position controls at bottom
                controlsRect.anchorMin = new Vector2(0, 0);
                controlsRect.anchorMax = new Vector2(1, 0.2f);
                controlsRect.offsetMin = Vector2.zero;
                controlsRect.offsetMax = Vector2.zero;

                MelonLogger.Msg("✓ Created Controls child container");

                // Try to add MenuScreen component if it exists (like real menus)
                Type menuScreenType = FindTypeInAssemblies("MenuScreen");
                if (menuScreenType != null)
                {
                    parentMenu.AddComponent(menuScreenType);
                    MelonLogger.Msg("✓ Added MenuScreen component");
                }

                MelonLogger.Msg($"✓ Created menu structure with {parentMenu.transform.childCount} children");
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Error creating menu children: {e.Message}");
            }
        }



        private void AnalyzeExistingMenus()
        {
            try
            {
                MelonLogger.Msg("=== ANALYZING EXISTING MENU STRUCTURES ===");

                // Find all GameObjects with MenuScreen components
                Type menuScreenType = FindTypeInAssemblies("MenuScreen");
                if (menuScreenType != null)
                {
                    UnityEngine.Object[] menuScreens = Resources.FindObjectsOfTypeAll(menuScreenType);
                    MelonLogger.Msg($"Found {menuScreens.Length} MenuScreen instances:");

                    foreach (UnityEngine.Object screen in menuScreens)
                    {
                        if (screen != null)
                        {
                            Component comp = screen as Component;
                            GameObject go = comp.gameObject;
                            MelonLogger.Msg($"\n--- {go.name} ---");
                            MelonLogger.Msg($"Active: {go.activeInHierarchy}");
                            MelonLogger.Msg($"Layer: {LayerMask.LayerToName(go.layer)}");
                            MelonLogger.Msg($"Components:");

                            Component[] components = go.GetComponents<Component>();
                            foreach (Component component in components)
                            {
                                MelonLogger.Msg($"  - {component.GetType().Name}");
                            }

                            MelonLogger.Msg($"Children ({go.transform.childCount}):");
                            for (int i = 0; i < go.transform.childCount; i++)
                            {
                                Transform child = go.transform.GetChild(i);
                                MelonLogger.Msg($"  [{i}] {child.name}");
                                
                                // Show child components too
                                Component[] childComponents = child.GetComponents<Component>();
                                foreach (Component childComp in childComponents)
                                {
                                    MelonLogger.Msg($"      - {childComp.GetType().Name}");
                                }
                            }
                        }
                    }
                }

                // Also analyze ControllerMenuScreen specifically since we saw it in inspector
                GameObject[] allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                GameObject controllerMenu = null;
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name == "ControllerMenuScreen")
                    {
                        controllerMenu = obj;
                        break;
                    }
                }

                if (controllerMenu != null)
                {
                    MelonLogger.Msg("\n=== DETAILED ANALYSIS: ControllerMenuScreen ===");
                    MelonLogger.Msg($"Parent: {(controllerMenu.transform.parent ? controllerMenu.transform.parent.name : "None")}");
                    MelonLogger.Msg($"Layer: {LayerMask.LayerToName(controllerMenu.layer)}");
                    MelonLogger.Msg($"Scale: {controllerMenu.transform.localScale}");
                    
                    RectTransform rect = controllerMenu.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        MelonLogger.Msg($"RectTransform - AnchorMin: {rect.anchorMin}, AnchorMax: {rect.anchorMax}");
                        MelonLogger.Msg($"OffsetMin: {rect.offsetMin}, OffsetMax: {rect.offsetMax}");
                    }
                }

                ShowToast("Menu analysis completed - check console");
            }
            catch (Exception e)
            {
                ShowToast($"Error analyzing menus: {e.Message}");
                MelonLogger.Msg($"Error analyzing existing menus: {e.Message}");
            }
        }

        
        private Dictionary<string, float> GetFieldOverview()
        {
            var result = new Dictionary<string, float>();
            var processedCombinations = new HashSet<string>();
            
            for (int i = 0; i < damageFields.Count; i++)
            {
                FieldInfo field = damageFields[i];
                MonoBehaviour behaviour = damageBehaviours[i];
                
                if (behaviour == null || !originalValues.ContainsKey(field)) continue;
                
                // Create proper Component.Property format
                string componentProperty = $"{behaviour.GetType().Name}.{field.Name}";
                if (processedCombinations.Contains(componentProperty)) continue;
                processedCombinations.Add(componentProperty);
                
                try
                {
                    object originalValue = originalValues[field];
                    object currentValue = field.GetValue(behaviour);
                    
                    if (originalValue != null && currentValue != null)
                    {
                        float multiplier = 1.0f;
                        
                        if (field.FieldType == typeof(float))
                        {
                            float original = (float)originalValue;
                            float current = (float)currentValue;
                            multiplier = original != 0 ? current / original : 1.0f;
                        }
                        else if (field.FieldType == typeof(int))
                        {
                            int original = (int)originalValue;
                            int current = (int)currentValue;
                            multiplier = original != 0 ? (float)current / original : 1.0f;
                        }
                        
                        // Include ALL fields (both modified and unmodified)
                        // This shows defaults at 1.0x and modified values
                        result[componentProperty] = multiplier;
                    }
                }
                catch (Exception)
                {
                    // Skip errored fields, but don't add to result
                }
            }
            
            return result;
        }
        
        private void SetDefaultKeybinds()
        {
            // Restore original default keybinds
            currentKeybinds[0] = KeyCode.F1;   // Add Health
            currentKeybinds[1] = KeyCode.F2;   // Set Health
            currentKeybinds[2] = KeyCode.F3;   // Refill Health
            currentKeybinds[3] = KeyCode.F4;   // One Hit Kill
            currentKeybinds[4] = KeyCode.F5;   // Add Money
            currentKeybinds[5] = KeyCode.F6;   // Add Shards
            currentKeybinds[6] = KeyCode.F8;   // Unlock Crests
            currentKeybinds[7] = KeyCode.F9;   // Unlock Crest Skills
            currentKeybinds[8] = KeyCode.F10;  // Unlock Crest Tools
            currentKeybinds[9] = KeyCode.F11;  // Max Collectables
            currentKeybinds[10] = KeyCode.F12; // Auto Silk
        }
        
        private void DisableAllKeybinds()
        {
            // Set all keybinds to None (disabled)
            for (int i = 0; i < currentKeybinds.Length; i++)
            {
                currentKeybinds[i] = KeyCode.None;
            }
        }

        private void AddHealth(int amount)
        {
            try
            {
                Type type = heroController.GetType();
                MethodInfo addHealthMethod = type.GetMethod("AddHealth");
                
                if (addHealthMethod != null)
                {
                    addHealthMethod.Invoke(heroController, new object[] { amount });
                    MelonLogger.Msg($"Added {amount} health");
                }
                else
                {
                    MelonLogger.Msg("AddHealth method not found");
                }
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Error adding health: {e.Message}");
            }
        }


        private void SetMaxHealthExact(int targetMaxHealth)
        {
            try
            {
                // Validate minimum health
                if (targetMaxHealth < 1)
                {
                    ShowToast("Warning: Minimum health is 1!");
                    MelonLogger.Msg("Attempted to set max health below 1 - setting to 1 instead");
                    targetMaxHealth = 1;
                }

                // Validate maximum to prevent crashes (reasonable upper limit)
                if (targetMaxHealth > 9999)
                {
                    ShowToast("Warning: Max health capped at 9999!");
                    MelonLogger.Msg("Attempted to set max health above 9999 - capping at 9999");
                    targetMaxHealth = 9999;
                }

                // Get current max health via playerData
                Type heroType = heroController.GetType();
                FieldInfo playerDataField = heroType.GetField("playerData", BindingFlags.Public | BindingFlags.Instance);
                
                if (playerDataField != null)
                {
                    object playerData = playerDataField.GetValue(heroController);
                    Type playerDataType = playerData.GetType();
                    
                    // Get CurrentMaxHealth property
                    PropertyInfo currentMaxHealthProp = playerDataType.GetProperty("CurrentMaxHealth");
                    if (currentMaxHealthProp != null)
                    {
                        int currentMaxHealth = (int)currentMaxHealthProp.GetValue(playerData);
                        int difference = targetMaxHealth - currentMaxHealth;
                        
                        MelonLogger.Msg($"Current max health: {currentMaxHealth}, Target: {targetMaxHealth}, Difference: {difference}");
                        
                        // Use AddToMaxHealth to reach the target
                        MethodInfo addMaxHealthMethod = heroType.GetMethod("AddToMaxHealth");
                if (addMaxHealthMethod != null)
                {
                            addMaxHealthMethod.Invoke(heroController, new object[] { difference });
                            MelonLogger.Msg($"Set max health to {targetMaxHealth}");
                            
                            // Automatically refill health to new max
                            RefillHealthAfterMaxChange();
                            
                            ShowToast($"Max health set to {targetMaxHealth} - Save & reload to see UI update!");
                }
                else
                {
                    MelonLogger.Msg("AddToMaxHealth method not found");
                        }
                    }
                    else
                    {
                        MelonLogger.Msg("CurrentMaxHealth property not found");
                    }
                }
                else
                {
                    MelonLogger.Msg("playerData field not found");
                }
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Error setting exact max health: {e.Message}");
                ShowToast($"Error setting max health: {e.Message}");
            }
        }

        private void RefillHealthAfterMaxChange()
        {
            try
            {
                // Get the current health and max health
                Type heroType = heroController.GetType();
                FieldInfo playerDataField = heroType.GetField("playerData", BindingFlags.Public | BindingFlags.Instance);
                
                if (playerDataField != null)
                {
                    object playerData = playerDataField.GetValue(heroController);
                    Type playerDataType = playerData.GetType();
                    
                    PropertyInfo currentMaxHealthProp = playerDataType.GetProperty("CurrentMaxHealth");
                    PropertyInfo healthProp = playerDataType.GetProperty("health");
                    
                    if (currentMaxHealthProp != null && healthProp != null)
                    {
                        int currentMaxHealth = (int)currentMaxHealthProp.GetValue(playerData);
                        int currentHealth = (int)healthProp.GetValue(playerData);
                        
                        // Calculate how much health to add to reach max
                        int healthToAdd = currentMaxHealth - currentHealth;
                        
                        if (healthToAdd > 0)
                        {
                            // Use the AddHealth method to properly refill
                            MethodInfo addHealthMethod = heroType.GetMethod("AddHealth");
                            if (addHealthMethod != null)
                            {
                                addHealthMethod.Invoke(heroController, new object[] { healthToAdd });
                                MelonLogger.Msg($"Refilled {healthToAdd} health to reach new max of {currentMaxHealth}");
                            }
                        }
                        else
                        {
                            MelonLogger.Msg("Health already at or above max");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Error refilling health after max change: {e.Message}");
            }
        }

        private void RefillHealth()
        {
            try
            {
                Type type = heroController.GetType();
                MethodInfo refillMethod = type.GetMethod("RefillHealthToMax");
                
                if (refillMethod != null)
                {
                    refillMethod.Invoke(heroController, null);
                    MelonLogger.Msg("Health refilled to max");
                }
                else
                {
                    MelonLogger.Msg("RefillHealthToMax method not found");
                }
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Error refilling health: {e.Message}");
            }
        }

        private void AddMoney(int amount)
        {
            try
            {
                if (heroController != null)
                {
                    Type heroControllerType = heroController.GetType();
                    
                    // Find CurrencyType enum from the same assembly as HeroController
                    Assembly gameAssembly = heroControllerType.Assembly;
                    Type currencyType = gameAssembly.GetTypes().FirstOrDefault(t => t.Name == "CurrencyType");

                    if (currencyType != null)
                    {
                        object moneyEnum = Enum.Parse(currencyType, "Money");
                        
                        // Explicitly specify BindingFlags for public instance methods
                        MethodInfo addCurrencyMethod = heroControllerType.GetMethod("AddCurrency", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(int), currencyType, typeof(bool) }, null);
                        
                        if (addCurrencyMethod != null)
                        {
                            addCurrencyMethod.Invoke(heroController, new object[] { amount, moneyEnum, false });
                            MelonLogger.Msg($"Added {amount} money");
                        }
                        else
                        {
                            MelonLogger.Msg("HeroController.AddCurrency method not found");
                        }
                    }
                    else
                    {
                        MelonLogger.Msg("CurrencyType enum not found");
                    }
                }
                else
                {
                    MelonLogger.Msg("Hero controller not found");
                }
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Error adding money: {e.Message}");
            }
        }

        private void AddShards(int amount)
        {
            try
            {
                if (heroController != null)
                {
                    Type heroControllerType = heroController.GetType();
                    
                    // Find CurrencyType enum from the same assembly as HeroController
                    Assembly gameAssembly = heroControllerType.Assembly;
                    Type currencyType = gameAssembly.GetTypes().FirstOrDefault(t => t.Name == "CurrencyType");

                    if (currencyType != null)
                    {
                        object shardEnum = Enum.Parse(currencyType, "Shard");
                        
                        // Explicitly specify BindingFlags for public instance methods
                        MethodInfo addCurrencyMethod = heroControllerType.GetMethod("AddCurrency", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(int), currencyType, typeof(bool) }, null);
                        
                        if (addCurrencyMethod != null)
                        {
                            addCurrencyMethod.Invoke(heroController, new object[] { amount, shardEnum, false });
                            MelonLogger.Msg($"Added {amount} shards");
                        }
                        else
                        {
                            MelonLogger.Msg("HeroController.AddCurrency method not found");
                        }
                    }
                    else
                    {
                        MelonLogger.Msg("CurrencyType enum not found");
                    }
                }
                else
                {
                    MelonLogger.Msg("Hero controller not found");
                }
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Error adding shards: {e.Message}");
            }
        }

        private void UnlockAllCrests()
        {
            try
            {
                MelonLogger.Msg("=== F8: CALLING MASTER CREST UNLOCK ===");
                
                // Find all ToolCrestList objects using Resources.FindObjectsOfTypeAll
                UnityEngine.Object[] allObjects = Resources.FindObjectsOfTypeAll(typeof(ToolCrestList));
                MelonLogger.Msg($"Found {allObjects.Length} ToolCrestList objects");
                
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
                            MelonLogger.Msg($"Called ToolCrestList.UnlockAll() on {obj.name} - All crests unlocked!");
                            return;
                        }
                        else
                        {
                            MelonLogger.Msg("ToolCrestList found but UnlockAll method not found");
                        }
                    }
                }

                MelonLogger.Msg("ToolCrestList component not found");
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Error calling master crest unlock: {e.Message}");
            }
        }

        private void UnlockAllTools()
        {
            try
            {
                MelonLogger.Msg("=== F9: UNLOCKING ALL TOOLS ===");
                
                // Find all ToolItemSkill objects
                UnityEngine.Object[] allObjects = Resources.FindObjectsOfTypeAll(typeof(ToolItemSkill));
                MelonLogger.Msg($"Found {allObjects.Length} ToolItemSkill objects");
                
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
                                Type popupFlagsType = null;
                                Type[] nestedTypes = toolType.GetNestedTypes();
                                foreach (Type nested in nestedTypes)
                                {
                                    if (nested.Name.Contains("PopupFlags"))
                                    {
                                        popupFlagsType = nested;
                                        break;
                                    }
                                }
                                
                                if (popupFlagsType != null)
                                {
                                    object itemGetFlag = Enum.Parse(popupFlagsType, "ItemGet");
                                    unlockMethod.Invoke(obj, new object[] { null, itemGetFlag });
                                }
                                else
                                {
                                    unlockMethod.Invoke(obj, new object[] { null, null });
                                }
                                
                                MelonLogger.Msg($"Unlocked tool: {obj.name}");
                                unlockedCount++;
                            }
                            else
                            {
                                MelonLogger.Msg($"No Unlock method found on {obj.name}");
                            }
                        }
                        catch (Exception e)
                        {
                            MelonLogger.Msg($"Error unlocking {obj.name}: {e.Message}");
                        }
                    }
                }
                
                MelonLogger.Msg($"Tool Unlock: Unlocked {unlockedCount} tools");
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Error unlocking tools: {e.Message}");
            }
        }

        private void UnlockAllItems()
        {
            try
            {
                MelonLogger.Msg("=== F10: UNLOCKING ALL ITEMS ===");
                
                // Find all ToolItemBasic objects
                UnityEngine.Object[] allObjects = Resources.FindObjectsOfTypeAll(typeof(ToolItemBasic));
                MelonLogger.Msg($"Found {allObjects.Length} ToolItemBasic objects");
                
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
                                Type popupFlagsType = null;
                                Type[] nestedTypes = toolType.GetNestedTypes();
                                foreach (Type nested in nestedTypes)
                                {
                                    if (nested.Name.Contains("PopupFlags"))
                                    {
                                        popupFlagsType = nested;
                                        break;
                                    }
                                }
                                
                                if (popupFlagsType != null)
                                {
                                    object itemGetFlag = Enum.Parse(popupFlagsType, "ItemGet");
                                    unlockMethod.Invoke(obj, new object[] { null, itemGetFlag });
                                }
                                else
                                {
                                    unlockMethod.Invoke(obj, new object[] { null, null });
                                }
                                
                                MelonLogger.Msg($"Unlocked item: {obj.name}");
                                unlockedCount++;
                            }
                            else
                            {
                                MelonLogger.Msg($"No Unlock method found on {obj.name}");
                            }
                        }
                        catch (Exception e)
                        {
                            MelonLogger.Msg($"Error unlocking {obj.name}: {e.Message}");
                        }
                    }
                }
                
                MelonLogger.Msg($"Item Unlock: Unlocked {unlockedCount} items");
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Error unlocking items: {e.Message}");
            }
        }

        private void SetCollectableAmount(string collectableDisplayName, int amount)
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
                                                
                                                ShowToast($"Set {collectableDisplayName} to {amount}");
                                                return;
                                            }
                                            else
                                            {
                                                // Fallback to public AddItem method (will add to current)
                                                MethodInfo publicAddItemMethod = managerType.GetMethod("AddItem", BindingFlags.Public | BindingFlags.Static);
                                                if (publicAddItemMethod != null)
                                                {
                                                    publicAddItemMethod.Invoke(null, new object[] { obj, amount });
                                                    ShowToast($"Added {amount} {collectableDisplayName} (note: adds to current amount)");
                                                    return;
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
                                ShowToast($"Set {collectableDisplayName} to {amount} (direct field access)");
                                return;
                            }
                            
                            // Try property
                            PropertyInfo amountProperty = itemType.GetProperty("amount", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (amountProperty != null && amountProperty.PropertyType == typeof(int) && amountProperty.CanWrite)
                            {
                                amountProperty.SetValue(obj, amount);
                                ShowToast($"Set {collectableDisplayName} to {amount} (direct property access)");
                                return;
                            }
                        }
                        catch (Exception e)
                        {
                            MelonLogger.Msg($"Error setting amount for {obj.name}: {e.Message}");
                        }
                    }
                }
                
                ShowToast($"Could not find or set amount for {collectableDisplayName}");
            }
            catch (Exception e)
            {
                ShowToast($"Error setting {collectableDisplayName}: {e.Message}");
                MelonLogger.Msg($"Error setting collectable amount: {e.Message}");
            }
        }

        private void MaxAllCollectables()
        {
            try
            {
                MelonLogger.Msg("=== F11: MAXING ALL COLLECTABLES ===");
                
                // Find all CollectableItem objects directly (base class with AddAmount method)
                UnityEngine.Object[] allObjects = Resources.FindObjectsOfTypeAll(typeof(CollectableItem));
                MelonLogger.Msg($"Found {allObjects.Length} CollectableItem objects");
                
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
                                MelonLogger.Msg($"Added 99x {obj.name}");
                                maxedCount++;
                            }
                            else
                            {
                                MelonLogger.Msg($"No AddAmount method found on {obj.name} (type: {itemType.Name})");
                            }
                        }
                        catch (Exception e)
                        {
                            MelonLogger.Msg($"Error maxing {obj.name}: {e.Message}");
                        }
                    
                }
                
                MelonLogger.Msg($"Collectable Max: Added 99x to {maxedCount} collectables");
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Error maxing collectables: {e.Message}");
            }
        }

        private void EnableOneHitKill()
        {
            try
            {
                if (oneHitKillEnabled)
                {
                    // Disable one hit kill - restore original values
                    DisableOneHitKill();
                }
                else
                {
                    // Enable one hit kill - save originals and set high values
                    MelonLogger.Msg("=== ENABLING ONE HIT KILL MODE ===");
                    
                    int modifiedCount = 0;
                    oneHitKillOriginalValues.Clear(); // Clear any previous values

                    // Search for DamageEnemies components only
                    MonoBehaviour[] allBehaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

                    foreach (MonoBehaviour behaviour in allBehaviours)
                    {
                        if (behaviour == null) continue;

                        Type type = behaviour.GetType();
                        string typeName = type.Name.ToLower();

                        // Only target DamageEnemies components
                        if (typeName.Contains("damageenemies"))
                        {
                            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            
                            foreach (FieldInfo field in fields)
                            {
                                string fieldName = field.Name.ToLower();
                                
                                // Look for damage-related fields
                                if ((fieldName.Contains("damage") || fieldName.Contains("multiplier")) 
                                    && (field.FieldType == typeof(float) || field.FieldType == typeof(int)))
                                {
                                    try
                                    {
                                        // Create unique key for this field instance
                                        string key = $"{behaviour.GetInstanceID()}_{type.Name}_{field.Name}";
                                        
                                        // Save original value
                                        object originalValue = field.GetValue(behaviour);
                                        oneHitKillOriginalValues[key] = originalValue;
                                        
                                        // Set high damage value
                                        if (field.FieldType == typeof(float))
                                        {
                                            field.SetValue(behaviour, 100.0f);
                                            MelonLogger.Msg($"Set {type.Name}.{field.Name} = 100.0f (was {originalValue})");
                                            modifiedCount++;
                                        }
                                        else if (field.FieldType == typeof(int))
                                        {
                                            field.SetValue(behaviour, 100);
                                            MelonLogger.Msg($"Set {type.Name}.{field.Name} = 100 (was {originalValue})");
                                            modifiedCount++;
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        // Ignore read-only or protected fields
                                    }
                                }
                            }
                        }
                    }

                    oneHitKillEnabled = true;
                    MelonLogger.Msg($"One Hit Kill ENABLED: Modified {modifiedCount} DamageEnemies values");
                }
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Error toggling one hit kill: {e.Message}");
            }
        }

        private void DisableOneHitKill()
        {
            try
            {
                MelonLogger.Msg("=== DISABLING ONE HIT KILL MODE ===");
                
                int restoredCount = 0;

                // Search for DamageEnemies components to restore values
                MonoBehaviour[] allBehaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

                foreach (MonoBehaviour behaviour in allBehaviours)
                {
                    if (behaviour == null) continue;

                    Type type = behaviour.GetType();
                    string typeName = type.Name.ToLower();

                    // Only target DamageEnemies components
                    if (typeName.Contains("damageenemies"))
                    {
                        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        
                        foreach (FieldInfo field in fields)
                        {
                            string fieldName = field.Name.ToLower();
                            
                            // Look for damage-related fields
                            if ((fieldName.Contains("damage") || fieldName.Contains("multiplier")) 
                                && (field.FieldType == typeof(float) || field.FieldType == typeof(int)))
                            {
                                try
                                {
                                    // Create unique key for this field instance
                                    string key = $"{behaviour.GetInstanceID()}_{type.Name}_{field.Name}";
                                    
                                    // Restore original value if we have it
                                    if (oneHitKillOriginalValues.ContainsKey(key))
                                    {
                                        object originalValue = oneHitKillOriginalValues[key];
                                        field.SetValue(behaviour, originalValue);
                                        MelonLogger.Msg($"Restored {type.Name}.{field.Name} = {originalValue}");
                                        restoredCount++;
                                    }
                                }
                                catch (Exception)
                                {
                                    // Ignore read-only or protected fields
                                }
                            }
                        }
                    }
                }

                oneHitKillEnabled = false;
                oneHitKillOriginalValues.Clear(); // Clear stored values
                MelonLogger.Msg($"One Hit Kill DISABLED: Restored {restoredCount} DamageEnemies values");
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Error disabling one hit kill: {e.Message}");
            }
        }

        private void ToggleAutoSilkRefill()
        {
            autoRefillSilk = !autoRefillSilk;
            silkRefillTimer = 0f; // Reset timer
            
            if (autoRefillSilk)
            {
                MelonLogger.Msg("Auto Silk Refill: ENABLED (every 2 seconds)");
            }
            else
            {
                MelonLogger.Msg("Auto Silk Refill: DISABLED");
            }
        }

        private void RefillSilk()
        {
            try
            {
                Type type = heroController.GetType();
                MethodInfo refillSilkMethod = type.GetMethod("RefillSilkToMax");
                
                if (refillSilkMethod != null)
                {
                    refillSilkMethod.Invoke(heroController, null);
                    // Only log occasionally to avoid spam
                    if (silkRefillTimer == 0f) // Only on manual calls or first auto call
                    {
                        MelonLogger.Msg("Silk refilled to max");
                    }
                }
                else
                {
                    MelonLogger.Msg("RefillSilkToMax method not found");
                }
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Error refilling silk: {e.Message}");
            }
        }


        private void UnlockAllFastTravel()
        {
            try
            {
                MelonLogger.Msg("=== UNLOCKING ALL FAST TRAVEL ===");

                // List of all fast travel station PlayerData booleans (from discovery)
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
                    // Note: Bone and Bonetown have empty PlayerData bools (already unlocked by default)
                };

                int unlockedCount = 0;
                foreach (string boolName in fastTravelBools)
                {
                    if (SetPlayerDataBool(boolName, true))
                    {
                        MelonLogger.Msg($"Unlocked: {boolName}");
                        unlockedCount++;
                    }
                    else
                    {
                        MelonLogger.Msg($"Failed to unlock: {boolName}");
                    }
                }

                MelonLogger.Msg($"Successfully unlocked {unlockedCount} fast travel locations");
                MelonLogger.Msg("Fast travel stations should now be available!");
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Error unlocking fast travel: {e.Message}");
            }
        }


        private bool SetPlayerDataBool(string boolName, bool value)
        {
            try
            {
                Type playerDataType = FindTypeInAssemblies("PlayerData");
                if (playerDataType == null) return false;

                PropertyInfo instanceProperty = playerDataType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceProperty == null) return false;

                object playerDataInstance = instanceProperty.GetValue(null);
                if (playerDataInstance == null) return false;

                // Try to set the field directly
                FieldInfo field = playerDataType.GetField(boolName, BindingFlags.Public | BindingFlags.Instance);
                if (field != null && field.FieldType == typeof(bool))
                {
                    field.SetValue(playerDataInstance, value);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }


    }
}
    