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
        private Vector2 achievementScrollPosition = Vector2.zero;

        // Tab system
        private int selectedTab = 0;
        private string[] tabNames = { "Cheats", "Balance", "Achievements" };

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

        // Invincibility toggle system
        private bool invincibilityEnabled = false;

        // Game speed control system (multiplier format: x2.0 instead of 200%)
        private string gameSpeedText = "2.0";
        private float currentGameSpeed = 2.0f;
        private bool gameSpeedEnabled = false;

        // Always Active Tools system
        private List<string> alwaysActiveTools = new List<string>();
        private bool showAlwaysActiveTools = false;

        // Achievement system
        private bool achievementsScanned = false;
        private List<string> availableAchievements = new List<string>();
        private string[] achievementNames = new string[0];
        private int selectedAchievementIndex = 0;
        private bool showAchievementDropdown = false;
        private Vector2 achievementDropdownScroll = Vector2.zero;
        private string achievementSearchFilter = "";
        private string[] filteredAchievementNames = new string[0];
        private Dictionary<string, string> achievementDisplayToKey = new Dictionary<string, string>();


        // Input field variables
        private string healthAmount = "1";
        private string moneyAmount = "1000";
        private string shardAmount = "1000";
        private string setHealthAmount = "11"; // For setting exact health
        private int selectedInvincibilityMode = 0; // 0 = FullInvincible, 1 = PreventDeath

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
        private bool showActionAmounts = false;
        private bool showCollectibleItems = false;
        private bool showCrestTools = false;
        private bool showPlayerSkills = false;
        private bool showQuickActions = true;
        private bool showKeybindSettings = false;

        // Keybind variables (disabled by default - users can enable in GUI)
        private KeyCode[] currentKeybinds = new KeyCode[]
        {
            KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None,
            KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None
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
            MelonLogger.Msg("Silksong Simple Cheats Mod v1.0 - Ready!");
            MelonLogger.Msg("Controls: INSERT/TILDE=Toggle GUI (Keybinds disabled by default - enable in GUI settings if desired)");

            MelonLogger.Msg("Silksong Simple Cheats Mod initialized successfully!");
        }

        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();
            MelonLogger.Msg("Simple Cheats Mod Initialized!");
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
                        // Initialize state from PlayerData now that we have access
                        InitializeStateFromPlayerData();
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


            // Enforce game speed setting if enabled (prevent resets from damage/pause events)
            if (gameSpeedEnabled && Time.timeScale != currentGameSpeed)
            {
                MelonLogger.Msg($"Game speed was reset to {Time.timeScale}, restoring to {currentGameSpeed}");
                Time.timeScale = currentGameSpeed;
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

        private void ToggleInvincibility()
        {
            try
            {
                invincibilityEnabled = !invincibilityEnabled;

                if (invincibilityEnabled)
                {
                    ApplyInvincibilityMode();
                }
                else
                {
                    DisableInvincibility();
                }
            }
            catch (Exception e)
            {
                // Revert the toggle if error occurred
                invincibilityEnabled = !invincibilityEnabled;
                ShowToast($"Error toggling Invincibility: {e.Message}");
                MelonLogger.Msg($"Error in ToggleInvincibility: {e.Message}");
            }
        }

        private void ApplyInvincibilityMode()
        {
            try
            {
                Type cheatManagerType = FindTypeInAssemblies("CheatManager");
                if (cheatManagerType != null)
                {
                    PropertyInfo invincibilityProp = cheatManagerType.GetProperty("Invincibility", BindingFlags.Public | BindingFlags.Static);
                    if (invincibilityProp != null)
                    {
                        Type invincibilityEnumType = cheatManagerType.GetNestedType("InvincibilityStates");
                        if (invincibilityEnumType != null)
                        {
                            // Apply selected mode
                            string[] modes = { "FullInvincible", "PreventDeath" };
                            string selectedMode = modes[selectedInvincibilityMode];
                            object newState = Enum.Parse(invincibilityEnumType, selectedMode);
                            invincibilityProp.SetValue(null, newState);
                            ShowToast($"Invincibility mode: {selectedMode}");
                            MelonLogger.Msg($"Invincibility mode applied: {selectedMode}");
                        }
                    }
                }
                else
                {
                    ShowToast("Invincibility unavailable - CheatManager not found");
                }
            }
            catch (Exception e)
            {
                ShowToast($"Error applying invincibility mode: {e.Message}");
                MelonLogger.Msg($"Error in ApplyInvincibilityMode: {e.Message}");
            }
        }

        private void DisableInvincibility()
        {
            try
            {
                Type cheatManagerType = FindTypeInAssemblies("CheatManager");
                if (cheatManagerType != null)
                {
                    PropertyInfo invincibilityProp = cheatManagerType.GetProperty("Invincibility", BindingFlags.Public | BindingFlags.Static);
                    if (invincibilityProp != null)
                    {
                        Type invincibilityEnumType = cheatManagerType.GetNestedType("InvincibilityStates");
                        if (invincibilityEnumType != null)
                        {
                            // Disable invincibility
                            object offState = Enum.Parse(invincibilityEnumType, "Off");
                            invincibilityProp.SetValue(null, offState);
                            ShowToast("Invincibility disabled!");
                            MelonLogger.Msg("Invincibility disabled");
                        }
                    }
                }
                else
                {
                    ShowToast("Invincibility unavailable - CheatManager not found");
                }
            }
            catch (Exception e)
            {
                ShowToast($"Error disabling invincibility: {e.Message}");
                MelonLogger.Msg($"Error in DisableInvincibility: {e.Message}");
            }
        }

        private void UnlockCompass()
        {
            try
            {
                // Unlock via PlayerData - compass functionality
                SetPlayerDataBool("hasCompass", true);
                MelonLogger.Msg("Compass unlocked via PlayerData");
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Error unlocking Compass: {e.Message}");
            }
        }

        private void UnlockMagnetiteBrooch()
        {
            try
            {
                // Unlock via PlayerData - magnetite brooch functionality  
                SetPlayerDataBool("hasMagnetiteBrooch", true);
                MelonLogger.Msg("Magnetite Brooch unlocked via PlayerData");
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Error unlocking Magnetite Brooch: {e.Message}");
            }
        }
        private void AddAlwaysActiveTool(string displayName)
        {
            try
            {
                if (string.IsNullOrEmpty(displayName))
                {
                    ShowToast("Invalid tool name");
                    return;
                }

                // Convert display name to object name for the game's tool system  
                string objectName = displayName;
                if (displayName == "Compass")
                {
                    objectName = "Compass";
                }
                else if (displayName == "Magnetite Brooch")
                {
                    objectName = "Rosary Magnet";
                }
                else
                {
                    objectName = displayName.Replace(" ", "");
                }

                if (alwaysActiveTools.Contains(objectName))
                {
                    ShowToast($"{displayName} is already active");
                    return;
                }

                alwaysActiveTools.Add(objectName);
                ApplyAlwaysActiveTools();
                ShowToast($"Added {displayName} to always active tools");
                MelonLogger.Msg($"Added {displayName} ({objectName}) to always active tools");

            }
            catch (Exception e)
            {
                ShowToast($"Error adding tool: {e.Message}");
                MelonLogger.Msg($"Error in AddAlwaysActiveTool: {e.Message}");
            }
        }
        private void UnlockAllMapItems()
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

                ShowToast($"Unlocked {unlockedCount} map items!");
                MelonLogger.Msg($"Unlocked {unlockedCount} map items using SetPurchased");
            }
            catch (Exception e)
            {
                ShowToast($"Error unlocking map items: {e.Message}");
                MelonLogger.Msg($"Error in UnlockAllMapItems: {e.Message}");
            }
        }
        private void ApplyAlwaysActiveTools()
        {
            try
            {
                // Find ToolItemManager type
                Type toolItemManagerType = FindTypeInAssemblies("ToolItemManager");
                if (toolItemManagerType == null)
                {
                    MelonLogger.Msg("ToolItemManager type not found for applying always active tools");
                    return;
                }

                // Get the SetExtraEquippedTool method
                MethodInfo setExtraEquippedToolMethod = toolItemManagerType.GetMethod("SetExtraEquippedTool",
                    BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string), typeof(string) }, null);

                if (setExtraEquippedToolMethod == null)
                {
                    MelonLogger.Msg("SetExtraEquippedTool method not found");
                    return;
                }

                // Clear existing extra equipped tools first
                ClearAlwaysActiveTools();

                // Apply each active tool to an extra slot
                for (int i = 0; i < alwaysActiveTools.Count; i++)
                {
                    string slotId = $"AlwaysActive_{i}";
                    string toolName = alwaysActiveTools[i];

                    setExtraEquippedToolMethod.Invoke(null, new object[] { slotId, toolName });
                    MelonLogger.Msg($"Applied always active tool: {toolName} to slot {slotId}");

                }

                MelonLogger.Msg($"Applied {alwaysActiveTools.Count} always active tools");
            }
            catch (Exception e)
            {
                ShowToast($"Error applying always active tools: {e.Message}");
                MelonLogger.Msg($"Error in ApplyAlwaysActiveTools: {e.Message}");
            }
        }

        private void ClearAlwaysActiveTools()
        {
            try
            {
                // Find ToolItemManager type
                Type toolItemManagerType = FindTypeInAssemblies("ToolItemManager");
                if (toolItemManagerType == null)
                {
                    return;
                }

                // Get the SetExtraEquippedTool method
                MethodInfo setExtraEquippedToolMethod = toolItemManagerType.GetMethod("SetExtraEquippedTool",
                    BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string), typeof(string) }, null);

                if (setExtraEquippedToolMethod == null)
                {
                    return;
                }

                // Clear all our always active slots (support up to 20 tools)
                for (int i = 0; i < 20; i++)
                {
                    string slotId = $"AlwaysActive_{i}";
                    setExtraEquippedToolMethod.Invoke(null, new object[] { slotId, string.Empty });
                }

                MelonLogger.Msg("Cleared all always active tool slots");
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Error in ClearAlwaysActiveTools: {e.Message}");
            }
        }

        private void ToggleGameSpeed()
        {
            try
            {
                gameSpeedEnabled = !gameSpeedEnabled;

                if (gameSpeedEnabled)
                {
                    // Enable and apply the stored speed value
                    Time.timeScale = currentGameSpeed;
                    ShowToast($"Game Speed Control enabled (x{currentGameSpeed:F1})");
                    MelonLogger.Msg($"Game Speed Control enabled - applied speed: x{currentGameSpeed:F1}");
                }
                else
                {
                    // Disable and reset to normal speed
                    Time.timeScale = 1.0f;
                    ShowToast("Game Speed Control disabled (100% normal speed)");
                    MelonLogger.Msg("Game Speed Control disabled - reset to normal speed");
                }
            }
            catch (Exception e)
            {
                // Revert the toggle if error occurred
                gameSpeedEnabled = !gameSpeedEnabled;
                ShowToast($"Error toggling Game Speed Control: {e.Message}");
                MelonLogger.Msg($"Error in ToggleGameSpeed: {e.Message}");
            }
        }

        private void SetGameSpeed(float speed)
        {
            try
            {
                // Apply the speed directly (clamping already done in GUI)
                Time.timeScale = speed;
                MelonLogger.Msg($"Game speed applied: x{speed:F1}");
            }
            catch (Exception e)
            {
                ShowToast($"Error setting game speed: {e.Message}");
                MelonLogger.Msg($"Error in SetGameSpeed: {e.Message}");
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

        private bool GetHeroConfigBool(string fieldName, bool defaultValue = false)
        {
            try
            {
                // Get HeroController instance
                var heroController = GameObject.FindFirstObjectByType<HeroController>();
                if (heroController == null)
                {
                    return defaultValue;
                }

                // Get the Config property
                var configProperty = heroController.GetType().GetProperty("Config");
                if (configProperty == null)
                {
                    return defaultValue;
                }

                var config = configProperty.GetValue(heroController);
                if (config == null)
                {
                    return defaultValue;
                }

                // Get the field using reflection (it's private, so we need NonPublic)
                var field = config.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null)
                {
                    return defaultValue;
                }

                // Get the field value
                if (field.FieldType == typeof(bool))
                {
                    return (bool)field.GetValue(config);
                }

                return defaultValue;
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        // Helper methods to check current skill states
        private bool IsDoubleJumpUnlocked()
        {
            return GetPlayerDataBool("hasDoubleJump", false);
        }

        private bool IsDashUnlocked()
        {
            return GetPlayerDataBool("hasDash", false);
        }

        private bool IsWallJumpUnlocked()
        {
            return GetPlayerDataBool("hasWalljump", false);
        }

        private bool IsGlideUnlocked()
        {
            // Glide requires BOTH playerData.hasBrolly AND Config.canBrolly
            bool playerDataHas = GetPlayerDataBool("hasBrolly", false);
            bool configCan = GetHeroConfigBool("canBrolly", false);
            return playerDataHas && configCan;
        }

        private bool IsChargeAttackUnlocked()
        {
            // Charge Attack requires BOTH playerData.hasChargeSlash AND Config.canNailCharge
            bool playerDataHas = GetPlayerDataBool("hasChargeSlash", false);
            bool configCan = GetHeroConfigBool("canNailCharge", false);
            return playerDataHas && configCan;
        }

        private bool IsNeedolinUnlocked()
        {
            // Needolin requires BOTH playerData.hasNeedolin AND Config.canPlayNeedolin
            bool playerDataHas = GetPlayerDataBool("hasNeedolin", false);
            bool configCan = GetHeroConfigBool("canPlayNeedolin", false);
            return playerDataHas && configCan;
        }

        private bool IsGrapplingHookUnlocked()
        {
            // Grappling Hook requires BOTH playerData.hasHarpoonDash AND Config.canHarpoonDash
            bool playerDataHas = GetPlayerDataBool("hasHarpoonDash", false);
            bool configCan = GetHeroConfigBool("canHarpoonDash", false);
            return playerDataHas && configCan;
        }

        private bool IsSuperJumpUnlocked()
        {
            // Super Jump requires both hasSuperJump AND hasHarpoonDash
            bool superJumpSet = GetPlayerDataBool("hasSuperJump", false);
            bool harpoonDashSet = GetPlayerDataBool("hasHarpoonDash", false);
            return superJumpSet && harpoonDashSet;
        }

        // Toggle methods for skills
        private void ToggleDoubleJump()
        {
            bool currentState = IsDoubleJumpUnlocked();
            bool newState = !currentState;

            if (SetPlayerDataBool("hasDoubleJump", newState))
            {
                ShowToast($"Double Jump {(newState ? "unlocked" : "locked")}!");
                MelonLogger.Msg($"Successfully {(newState ? "unlocked" : "locked")} double jump (playerData.hasDoubleJump = {newState})");
            }
        }

        private void ToggleDash()
        {
            bool currentState = IsDashUnlocked();
            bool newState = !currentState;

            if (SetPlayerDataBool("hasDash", newState))
            {
                ShowToast($"Dash {(newState ? "unlocked" : "locked")}!");
                MelonLogger.Msg($"Successfully {(newState ? "unlocked" : "locked")} dash (playerData.hasDash = {newState})");
            }
        }

        private void ToggleWallJump()
        {
            bool currentState = IsWallJumpUnlocked();
            bool newState = !currentState;

            if (SetPlayerDataBool("hasWalljump", newState))
            {
                ShowToast($"Wall Jump {(newState ? "unlocked" : "locked")}!");
                MelonLogger.Msg($"Successfully {(newState ? "unlocked" : "locked")} wall jump (playerData.hasWalljump = {newState})");
            }
        }

        private void ToggleGlide()
        {
            bool currentState = IsGlideUnlocked();
            bool newState = !currentState;

            // Glide requires BOTH playerData.hasBrolly AND Config.canBrolly
            bool playerDataSet = SetPlayerDataBool("hasBrolly", newState);
            bool configSet = SetHeroConfigBool("canBrolly", newState);

            if (playerDataSet && configSet)
            {
                ShowToast($"Glide/Drifter's Cloak {(newState ? "unlocked" : "locked")}! ☂️");
                MelonLogger.Msg($"Successfully {(newState ? "unlocked" : "locked")} glide (playerData.hasBrolly = {newState}, Config.canBrolly = {newState})");
            }
        }

        private void ToggleChargeAttack()
        {
            bool currentState = IsChargeAttackUnlocked();
            bool newState = !currentState;

            bool playerDataSet = SetPlayerDataBool("hasChargeSlash", newState);
            bool configSet = SetHeroConfigBool("canNailCharge", newState);

            if (playerDataSet && configSet)
            {
                ShowToast($"Charge Attack {(newState ? "unlocked" : "locked")}!");
                MelonLogger.Msg($"Successfully {(newState ? "unlocked" : "locked")} charge attack (playerData.hasChargeSlash = {newState}, Config.canNailCharge = {newState})");
            }
        }

        private void ToggleNeedolin()
        {
            bool currentState = IsNeedolinUnlocked();
            bool newState = !currentState;

            // Needolin requires BOTH playerData.hasNeedolin AND Config.canPlayNeedolin
            bool playerDataSet = SetPlayerDataBool("hasNeedolin", newState);
            bool configSet = SetHeroConfigBool("canPlayNeedolin", newState);

            if (playerDataSet && configSet)
            {
                ShowToast($"Needolin {(newState ? "unlocked" : "locked")}! 🎵");
                MelonLogger.Msg($"Successfully {(newState ? "unlocked" : "locked")} needolin (playerData.hasNeedolin = {newState}, Config.canPlayNeedolin = {newState})");
            }
        }

        private void ToggleGrapplingHook()
        {
            bool currentState = IsGrapplingHookUnlocked();
            bool newState = !currentState;

            // Grappling Hook requires BOTH playerData.hasHarpoonDash AND Config.canHarpoonDash
            bool playerDataSet = SetPlayerDataBool("hasHarpoonDash", newState);
            bool configSet = SetHeroConfigBool("canHarpoonDash", newState);

            if (playerDataSet && configSet)
            {
                ShowToast($"Grappling Hook {(newState ? "unlocked" : "locked")}! 🎣 (Clawline Ancestral Art)");
                MelonLogger.Msg($"Successfully {(newState ? "unlocked" : "locked")} grappling hook (playerData.hasHarpoonDash = {newState}, Config.canHarpoonDash = {newState})");
            }
        }

        private void ToggleSuperJump()
        {
            bool currentState = IsSuperJumpUnlocked();
            bool newState = !currentState;

            // Super Jump requires both hasSuperJump AND hasHarpoonDash
            bool superJumpSet = SetPlayerDataBool("hasSuperJump", newState);
            bool harpoonDashSet = SetPlayerDataBool("hasHarpoonDash", newState);

            if (superJumpSet && harpoonDashSet)
            {
                ShowToast($"Super Jump {(newState ? "unlocked" : "locked")}! (includes Harpoon Dash)");
                MelonLogger.Msg($"Successfully {(newState ? "unlocked" : "locked")} super jump (playerData.hasSuperJump = {newState}, playerData.hasHarpoonDash = {newState})");
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

            // Prevent multipliers < 1 as they can trigger invincibility bugs
            if (globalMultiplier < 1.0f)
            {
                ShowToast("Multiplier must be >= 1.0 (values < 1 can cause invincibility bugs)");
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

            // Reset damage scanning for new scene
            fieldsScanned = false;
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
            else if (selectedTab == 2)
            {
                DrawAchievementsTab();
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

                bool newInvincibility = GUILayout.Toggle(invincibilityEnabled, "Invincibility");
                if (newInvincibility != invincibilityEnabled)
                {
                    ToggleInvincibility();
                }

                bool newGameSpeed = GUILayout.Toggle(gameSpeedEnabled, $"Game Speed Control ({(currentGameSpeed * 100):F0}%)");
                if (newGameSpeed != gameSpeedEnabled)
                {
                    ToggleGameSpeed();
                }


                // Change Equipment Anywhere toggle
                try
                {
                    Type cheatManagerType = FindTypeInAssemblies("CheatManager");
                    if (cheatManagerType != null)
                    {
                        PropertyInfo canChangeEquipsProp = cheatManagerType.GetProperty("CanChangeEquipsAnywhere", BindingFlags.Public | BindingFlags.Static);
                        if (canChangeEquipsProp != null)
                        {
                            bool currentValue = (bool)canChangeEquipsProp.GetValue(null);
                            bool newValue = GUILayout.Toggle(currentValue, "Change Equipment Anywhere");
                            if (newValue != currentValue)
                            {
                                canChangeEquipsProp.SetValue(null, newValue);
                                ShowToast($"Change Equipment Anywhere: {(newValue ? "Enabled" : "Disabled")}");
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Silent fail if CheatManager unavailable
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
                            () =>
                            {
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


                // Invincibility Mode dropdown
                GUILayout.BeginHorizontal();
                GUILayout.Label("Invincibility:", GUILayout.Width(80));
                string[] invincibilityModes = { "FullInvincible", "PreventDeath" };
                int newInvincibilityMode = GUILayout.SelectionGrid(selectedInvincibilityMode, invincibilityModes, 2, GUILayout.Width(200));

                // Apply mode change immediately if invincibility is enabled
                if (newInvincibilityMode != selectedInvincibilityMode)
                {
                    selectedInvincibilityMode = newInvincibilityMode;
                    if (invincibilityEnabled)
                    {
                        ApplyInvincibilityMode();
                    }
                }
                GUILayout.EndHorizontal();

                // Game Speed Control (always visible, like health)
                GUILayout.BeginHorizontal();
                GUILayout.Label("Game Speed:", GUILayout.Width(80));
                gameSpeedText = GUILayout.TextField(gameSpeedText, GUILayout.Width(60));
                GUILayout.Label("x", GUILayout.Width(15));
                if (GUILayout.Button("Set", GUILayout.Width(50)))
                {
                    if (float.TryParse(gameSpeedText, out float speedMultiplier))
                    {
                        // Only prevent negative speeds (0 minimum, no maximum limit)
                        currentGameSpeed = Mathf.Max(speedMultiplier, 0f);
                        gameSpeedText = currentGameSpeed.ToString("F1"); // Update display to processed value

                        // Auto-enable and apply speed when user clicks "Set"
                        gameSpeedEnabled = true;
                        SetGameSpeed(currentGameSpeed);
                        ShowToast($"Game speed applied: x{currentGameSpeed:F1}");
                    }
                    else
                    {
                        ShowToast("Invalid speed value - please enter a number");
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

                // Double Jump Toggle
                bool doubleJumpUnlocked = IsDoubleJumpUnlocked();
                GUI.color = doubleJumpUnlocked ? Color.green : Color.white;
                string doubleJumpText = doubleJumpUnlocked ? "Double Jump ✓" : "Double Jump";
                if (GUILayout.Button(doubleJumpText, GUILayout.Width(170)))
                {
                    ToggleDoubleJump();
                }
                GUI.color = Color.white;

                // Dash Toggle
                bool dashUnlocked = IsDashUnlocked();
                GUI.color = dashUnlocked ? Color.green : Color.white;
                string dashText = dashUnlocked ? "Dash ✓" : "Dash";
                if (GUILayout.Button(dashText, GUILayout.Width(170)))
                {
                    ToggleDash();
                }
                GUI.color = Color.white;

                // Wall Jump Toggle
                bool wallJumpUnlocked = IsWallJumpUnlocked();
                GUI.color = wallJumpUnlocked ? Color.green : Color.white;
                string wallJumpText = wallJumpUnlocked ? "Wall Jump ✓" : "Wall Jump";
                if (GUILayout.Button(wallJumpText, GUILayout.Width(170)))
                {
                    ToggleWallJump();
                }
                GUI.color = Color.white;

                // Glide Toggle
                bool glideUnlocked = IsGlideUnlocked();
                GUI.color = glideUnlocked ? Color.green : Color.white;
                string glideText = glideUnlocked ? "Glide ✓" : "Glide";
                if (GUILayout.Button(glideText, GUILayout.Width(170)))
                {
                    ToggleGlide();
                }
                GUI.color = Color.white;
                GUILayout.EndVertical();

                GUILayout.Space(10);

                // Right Column (Special Abilities)
                GUILayout.BeginVertical();

                // Charge Attack Toggle
                bool chargeAttackUnlocked = IsChargeAttackUnlocked();
                GUI.color = chargeAttackUnlocked ? Color.green : Color.white;
                string chargeAttackText = chargeAttackUnlocked ? "Charge Attack ✓" : "Charge Attack";
                if (GUILayout.Button(chargeAttackText, GUILayout.Width(170)))
                {
                    ToggleChargeAttack();
                }
                GUI.color = Color.white;

                // Needolin Toggle
                bool needolinUnlocked = IsNeedolinUnlocked();
                GUI.color = needolinUnlocked ? Color.green : Color.white;
                string needolinText = needolinUnlocked ? "Needolin ✓" : "Needolin";
                if (GUILayout.Button(needolinText, GUILayout.Width(170)))
                {
                    ToggleNeedolin();
                }
                GUI.color = Color.white;

                // Grappling Hook Toggle
                bool grapplingHookUnlocked = IsGrapplingHookUnlocked();
                GUI.color = grapplingHookUnlocked ? Color.green : Color.white;
                string grapplingHookText = grapplingHookUnlocked ? "Grappling Hook ✓" : "Grappling Hook";
                if (GUILayout.Button(grapplingHookText, GUILayout.Width(170)))
                {
                    ToggleGrapplingHook();
                }
                GUI.color = Color.white;

                // Super Jump Toggle
                bool superJumpUnlocked = IsSuperJumpUnlocked();
                GUI.color = superJumpUnlocked ? Color.green : Color.white;
                string superJumpText = superJumpUnlocked ? "Super Jump ✓" : "Super Jump";
                if (GUILayout.Button(superJumpText, GUILayout.Width(170)))
                {
                    ToggleSuperJump();
                }
                GUI.color = Color.white;
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            GUILayout.Space(5);

            // Always Active Tools section
            showAlwaysActiveTools = DrawCollapsingHeader($"Always Active Tools ({alwaysActiveTools.Count} active)", showAlwaysActiveTools);
            if (showAlwaysActiveTools)
            {
                GUILayout.BeginVertical(GUI.skin.box);

                GUILayout.Label("Tools that are always equipped (invisible but functional):");

                // Hardcoded popular tools section
                GUILayout.Label("📍 Popular Tools (Always Active):", GUI.skin.label);
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("• Compass (navigation)");
                GUILayout.Label("• Magnetite Brooch (collection)");
                GUILayout.Label("These tools are hardcoded for convenience and cannot be removed.");
                GUILayout.EndVertical();


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
                        () =>
                        {
                            UnlockAllCrests();
                            ShowToast("All crests unlocked!");
                        });
                }
                if (GUILayout.Button("Unlock Crest Skills"))
                {
                    ShowConfirmation("Unlock All Crest Skills",
                        "This will unlock all crest skills. This action cannot be undone.",
                        () =>
                        {
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
                        () =>
                        {
                            UnlockAllItems();
                            ShowToast("All crest tools unlocked!");
                        });
                }
                if (GUILayout.Button("Max Collectables"))
                {
                    ShowConfirmation("Max All Collectables",
                        "This will maximize all collectible items. This action cannot be undone.",
                        () =>
                        {
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
                        () =>
                        {
                            UnlockAllFastTravel();
                            ShowToast("All fast travel unlocked!");
                        });
                }
                if (GUILayout.Button("Unlock All Maps"))
                {
                    ShowConfirmation("Unlock All Maps",
                        "This will unlock all map items. This action cannot be undone.",
                        () =>
                        {
                            UnlockAllMapItems();
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
            GUILayout.Label("Examples: 1.0 = Normal, 1.5 = +50% damage, 2.0 = Double damage", GUI.skin.label);
            GUILayout.Label("Note: Values < 1.0 can cause invincibility bugs", GUI.skin.label);

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
                    GUILayout.Label("Multiplier will modify all damage values proportionally (≥1.0 only)", GUI.skin.label);
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

        private void DrawAchievementsTab()
        {
            // Begin scroll view
            achievementScrollPosition = GUILayout.BeginScrollView(achievementScrollPosition, GUILayout.Width(380), GUILayout.Height(windowRect.height - 120));

            GUILayout.Label("Achievement System", GUI.skin.box);
            GUILayout.Label("Award achievements instantly to unlock Steam/platform rewards", GUI.skin.label);
            GUILayout.Space(10);

            // Auto-scan achievements on first access
            if (!achievementsScanned)
            {
                ScanAchievements();
            }

            // Achievement selection and awarding
            if (achievementsScanned)
            {
                if (availableAchievements.Count > 0)
                {
                    if (achievementNames.Length > 0)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Achievement:", GUILayout.Width(80));

                        // Dropdown button
                        string currentSelection = selectedAchievementIndex < achievementNames.Length ? achievementNames[selectedAchievementIndex] : "Select Achievement";

                        if (GUILayout.Button($"{currentSelection} ▼", GUILayout.Width(180)))
                        {
                            showAchievementDropdown = !showAchievementDropdown;
                            if (showAchievementDropdown)
                            {
                                achievementSearchFilter = ""; // Reset search when opening
                                FilterAchievements();
                            }
                        }
                        GUILayout.EndHorizontal();

                        // Dropdown with search
                        if (showAchievementDropdown)
                        {
                            GUILayout.BeginVertical(GUI.skin.box);

                            // Search box
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Search:", GUILayout.Width(50));
                            string newFilter = GUILayout.TextField(achievementSearchFilter, GUILayout.Width(130));
                            if (newFilter != achievementSearchFilter)
                            {
                                achievementSearchFilter = newFilter;
                                FilterAchievements();
                                achievementDropdownScroll = Vector2.zero; // Reset scroll when filtering
                            }
                            GUILayout.EndHorizontal();

                            // Clear search button
                            if (!string.IsNullOrEmpty(achievementSearchFilter))
                            {
                                if (GUILayout.Button("Clear", GUILayout.Width(60)))
                                {
                                    achievementSearchFilter = "";
                                    FilterAchievements();
                                }
                            }

                            // Scrollable filtered list
                            achievementDropdownScroll = GUILayout.BeginScrollView(achievementDropdownScroll, GUILayout.Height(150));

                            if (filteredAchievementNames.Length > 0)
                            {
                                for (int i = 0; i < filteredAchievementNames.Length; i++)
                                {
                                    string achievementName = filteredAchievementNames[i];

                                    // Find the index in the original array
                                    int originalIndex = Array.IndexOf(achievementNames, achievementName);

                                    if (GUILayout.Button(achievementName, GUI.skin.label))
                                    {
                                        selectedAchievementIndex = originalIndex;
                                        showAchievementDropdown = false;
                                        achievementSearchFilter = ""; // Clear search after selection
                                    }
                                }
                            }
                            else
                            {
                                GUILayout.Label("No achievements found", GUI.skin.label);
                            }

                            GUILayout.EndScrollView();
                            GUILayout.EndVertical();
                        }
                    }
                    else
                    {
                        GUILayout.Label("No achievements available", GUI.skin.label);
                    }

                    GUILayout.Space(10);

                    // Award button
                    if (achievementNames.Length > 0 && selectedAchievementIndex < achievementNames.Length)
                    {
                        if (GUILayout.Button($"Award Achievement: {achievementNames[selectedAchievementIndex]}", GUILayout.Height(30)))
                        {
                            AwardSelectedAchievement();
                        }
                    }

                    GUILayout.Space(10);

                    // Quick award all button with confirmation
                    GUI.color = Color.yellow;
                    if (GUILayout.Button("Award ALL Achievements", GUILayout.Height(30)))
                    {
                        ShowConfirmation("Award ALL Achievements", "This will award ALL achievements and cannot be undone!", () =>
                        {
                            AwardAllAchievements();
                        });
                    }
                    GUI.color = Color.white;
                }
                else
                {
                    GUILayout.Label("No achievements found - try changing scenes or restarting the game", GUI.skin.label);
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
                    // Disable one hit kill
                    DisableOneHitKill();
                }
                else
                {
                    // Try CheatManager first
                    Type cheatManagerType = FindTypeInAssemblies("CheatManager");
                    if (cheatManagerType != null)
                    {
                        PropertyInfo nailDamageProp = cheatManagerType.GetProperty("NailDamage", BindingFlags.Public | BindingFlags.Static);
                        if (nailDamageProp != null)
                        {
                            Type nailDamageEnumType = cheatManagerType.GetNestedType("NailDamageStates");
                            if (nailDamageEnumType != null)
                            {
                                object instaKillState = Enum.Parse(nailDamageEnumType, "InstaKill");
                                nailDamageProp.SetValue(null, instaKillState);
                                oneHitKillEnabled = true;
                                MelonLogger.Msg("One Hit Kill ENABLED using CheatManager.NailDamage = InstaKill");
                                return;
                            }
                        }
                    }

                    // Fallback to our existing method
                    MelonLogger.Msg("=== ENABLING ONE HIT KILL MODE (Fallback Method) ===");

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
                    MelonLogger.Msg($"One Hit Kill ENABLED (Fallback): Modified {modifiedCount} DamageEnemies values");
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
                // Try CheatManager first
                Type cheatManagerType = FindTypeInAssemblies("CheatManager");
                if (cheatManagerType != null)
                {
                    PropertyInfo nailDamageProp = cheatManagerType.GetProperty("NailDamage", BindingFlags.Public | BindingFlags.Static);
                    if (nailDamageProp != null)
                    {
                        Type nailDamageEnumType = cheatManagerType.GetNestedType("NailDamageStates");
                        if (nailDamageEnumType != null)
                        {
                            object normalState = Enum.Parse(nailDamageEnumType, "Normal");
                            nailDamageProp.SetValue(null, normalState);
                            oneHitKillEnabled = false;
                            MelonLogger.Msg("One Hit Kill DISABLED using CheatManager.NailDamage = Normal");
                            return;
                        }
                    }
                }

                // Fallback to our existing restore method
                MelonLogger.Msg("=== DISABLING ONE HIT KILL MODE (Fallback Method) ===");

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
                MelonLogger.Msg($"One Hit Kill DISABLED (Fallback): Restored {restoredCount} DamageEnemies values");
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

        private void InitializeStateFromPlayerData()
        {
            try
            {
                // Initialize infinite air jump state from PlayerData
                bool playerDataInfiniteAirJump = GetPlayerDataBool("infiniteAirJump", false);
                infiniteAirJumpEnabled = playerDataInfiniteAirJump;

                if (infiniteAirJumpEnabled)
                {
                    MelonLogger.Msg("Infinite Air Jump found enabled in PlayerData - syncing toggle state");
                }

                // Auto-equip popular tools (hardcoded - most players always want these)
                MelonLogger.Msg("Auto-unlocking and equipping popular tools (Compass, Magnetite Brooch)");
                UnlockCompass();
                UnlockMagnetiteBrooch();
                AddAlwaysActiveTool("Compass");
                AddAlwaysActiveTool("Magnetite Brooch");

                // TODO: Add other persistent PlayerData toggles here if needed in the future
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Error initializing state from PlayerData: {e.Message}");
            }
        }


        private bool GetPlayerDataBool(string boolName, bool defaultValue = false)
        {
            try
            {
                Type playerDataType = FindTypeInAssemblies("PlayerData");
                if (playerDataType == null) return defaultValue;

                PropertyInfo instanceProperty = playerDataType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceProperty == null) return defaultValue;

                object playerDataInstance = instanceProperty.GetValue(null);
                if (playerDataInstance == null) return defaultValue;

                // Try to get the field value
                FieldInfo field = playerDataType.GetField(boolName, BindingFlags.Public | BindingFlags.Instance);
                if (field != null && field.FieldType == typeof(bool))
                {
                    return (bool)field.GetValue(playerDataInstance);
                }

                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        private void ScanAchievements()
        {
            try
            {
                // Find AchievementHandler components
                Type achievementHandlerType = FindTypeInAssemblies("AchievementHandler");
                if (achievementHandlerType == null)
                {
                    ShowToast("AchievementHandler type not found");
                    return;
                }

                UnityEngine.Object[] achievementHandlers = Resources.FindObjectsOfTypeAll(achievementHandlerType);
                if (achievementHandlers.Length == 0)
                {
                    ShowToast("No AchievementHandler components found");
                    return;
                }

                object achievementHandler = achievementHandlers[0];

                // Get achievementsList field from AchievementHandler
                FieldInfo achievementsListField = achievementHandlerType.GetField("achievementsList", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                if (achievementsListField == null)
                {
                    ShowToast("achievementsList field not found on AchievementHandler");
                    return;
                }

                object achievementsList = achievementsListField.GetValue(achievementHandler);
                if (achievementsList == null)
                {
                    ShowToast("achievementsList field is null");
                    return;
                }

                // Get the achievements field from AchievementsList
                Type achievementsListType = achievementsList.GetType();
                FieldInfo achievementsField = achievementsListType.GetField("achievements", BindingFlags.NonPublic | BindingFlags.Instance);
                object achievements = null;

                if (achievementsField != null)
                {
                    achievements = achievementsField.GetValue(achievementsList);
                }
                else
                {
                    // Fallback to public property
                    PropertyInfo achievementsProperty = achievementsListType.GetProperty("Achievements", BindingFlags.Public | BindingFlags.Instance);
                    if (achievementsProperty != null)
                    {
                        achievements = achievementsProperty.GetValue(achievementsList);
                    }
                }

                if (achievements == null)
                {
                    ShowToast("Could not access achievements field or property");
                    return;
                }

                // Clear existing data
                availableAchievements.Clear();
                achievementDisplayToKey.Clear();

                // Enumerate achievements using IEnumerable
                System.Collections.IEnumerable achievementEnumerable = achievements as System.Collections.IEnumerable;
                if (achievementEnumerable != null)
                {
                    foreach (object achievement in achievementEnumerable)
                    {
                        if (achievement == null) continue;

                        try
                        {
                            Type achievementType = achievement.GetType();
                            string platformKey = null;

                            // Try to get PlatformKey field (public field according to decompiled source)
                            FieldInfo platformKeyField = achievementType.GetField("PlatformKey", BindingFlags.Public | BindingFlags.Instance);
                            if (platformKeyField != null)
                            {
                                platformKey = platformKeyField.GetValue(achievement) as string;
                            }

                            // Fallback: try property or other field names
                            if (string.IsNullOrEmpty(platformKey))
                            {
                                PropertyInfo platformKeyProperty = achievementType.GetProperty("PlatformKey", BindingFlags.Public | BindingFlags.Instance);
                                if (platformKeyProperty != null)
                                {
                                    platformKey = platformKeyProperty.GetValue(achievement) as string;
                                }
                            }

                            if (!string.IsNullOrEmpty(platformKey))
                            {
                                string displayName = platformKey;

                                // Try to get a display name
                                PropertyInfo displayNameProperty = achievementType.GetProperty("DisplayName", BindingFlags.Public | BindingFlags.Instance);
                                if (displayNameProperty != null)
                                {
                                    string displayNameValue = displayNameProperty.GetValue(achievement) as string;
                                    if (!string.IsNullOrEmpty(displayNameValue))
                                    {
                                        displayName = displayNameValue;
                                    }
                                }

                                availableAchievements.Add(platformKey);
                                achievementDisplayToKey[displayName] = platformKey;
                            }
                        }
                        catch (Exception ex)
                        {
                            MelonLogger.Msg($"Error processing achievement: {ex.Message}");
                        }
                    }
                }

                achievementsScanned = true;
                achievementNames = achievementDisplayToKey.Keys.ToArray();
                FilterAchievements();

                ShowToast($"Found {availableAchievements.Count} achievements");
            }
            catch (Exception ex)
            {
                ShowToast($"Error scanning achievements: {ex.Message}");
                MelonLogger.Msg($"Error in ScanAchievements: {ex.Message}");
            }
        }

        private void FilterAchievements()
        {
            if (!achievementsScanned)
            {
                filteredAchievementNames = new string[0];
                return;
            }

            if (string.IsNullOrEmpty(achievementSearchFilter))
            {
                filteredAchievementNames = achievementNames;
            }
            else
            {
                filteredAchievementNames = achievementNames
                    .Where(name => name.ToLower().Contains(achievementSearchFilter.ToLower()))
                    .ToArray();
            }

            // Reset selection if current selection is invalid
            if (selectedAchievementIndex >= filteredAchievementNames.Length)
            {
                selectedAchievementIndex = 0;
            }
        }

        private void AwardSelectedAchievement()
        {
            try
            {
                if (!achievementsScanned || achievementNames.Length == 0 || selectedAchievementIndex >= achievementNames.Length)
                {
                    ShowToast("No achievements available");
                    return;
                }

                string displayName = achievementNames[selectedAchievementIndex];
                if (!achievementDisplayToKey.TryGetValue(displayName, out string platformKey))
                {
                    ShowToast("Achievement key not found");
                    return;
                }

                // Find AchievementHandler instead of GameManager
                Type achievementHandlerType = FindTypeInAssemblies("AchievementHandler");
                if (achievementHandlerType == null)
                {
                    ShowToast("AchievementHandler not found");
                    return;
                }

                UnityEngine.Object[] achievementHandlers = Resources.FindObjectsOfTypeAll(achievementHandlerType);
                if (achievementHandlers.Length == 0)
                {
                    ShowToast("AchievementHandler instance not found");
                    return;
                }

                object achievementHandler = achievementHandlers[0];

                // Look for AwardAchievementToPlayer method on AchievementHandler, or get GameManager from it
                MethodInfo awardMethod = achievementHandlerType.GetMethod("AwardAchievementToPlayer", BindingFlags.Public | BindingFlags.Instance);
                object targetInstance = achievementHandler;

                if (awardMethod == null)
                {
                    // Fallback: try to get GameManager and call method there
                    Type gameManagerType = FindTypeInAssemblies("GameManager");
                    if (gameManagerType != null)
                    {
                        PropertyInfo instanceProperty = gameManagerType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
                        if (instanceProperty != null)
                        {
                            object gameManagerInstance = instanceProperty.GetValue(null);
                            if (gameManagerInstance != null)
                            {
                                awardMethod = gameManagerType.GetMethod("AwardAchievementToPlayer", BindingFlags.Public | BindingFlags.Instance);
                                targetInstance = gameManagerInstance;
                            }
                        }
                    }
                }
                if (awardMethod == null)
                {
                    ShowToast("AwardAchievementToPlayer method not found");
                    return;
                }

                awardMethod.Invoke(targetInstance, new object[] { platformKey });
                ShowToast($"Awarded: {displayName}");
                MelonLogger.Msg($"Awarded achievement: {displayName} ({platformKey})");
            }
            catch (Exception ex)
            {
                ShowToast($"Error awarding achievement: {ex.Message}");
                MelonLogger.Msg($"Error in AwardSelectedAchievement: {ex.Message}");
            }
        }

        private void AwardAllAchievements()
        {
            try
            {
                if (!achievementsScanned || availableAchievements.Count == 0)
                {
                    ShowToast("No achievements available");
                    return;
                }

                // Find AchievementHandler instead of GameManager
                Type achievementHandlerType = FindTypeInAssemblies("AchievementHandler");
                if (achievementHandlerType == null)
                {
                    ShowToast("AchievementHandler not found");
                    return;
                }

                UnityEngine.Object[] achievementHandlers = Resources.FindObjectsOfTypeAll(achievementHandlerType);
                if (achievementHandlers.Length == 0)
                {
                    ShowToast("AchievementHandler instance not found");
                    return;
                }

                object achievementHandler = achievementHandlers[0];

                // Look for AwardAchievementToPlayer method on AchievementHandler, or get GameManager from it
                MethodInfo awardMethod = achievementHandlerType.GetMethod("AwardAchievementToPlayer", BindingFlags.Public | BindingFlags.Instance);
                object targetInstance = achievementHandler;

                if (awardMethod == null)
                {
                    // Fallback: try to get GameManager and call method there
                    Type gameManagerType = FindTypeInAssemblies("GameManager");
                    if (gameManagerType != null)
                    {
                        PropertyInfo instanceProperty = gameManagerType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
                        if (instanceProperty != null)
                        {
                            object gameManagerInstance = instanceProperty.GetValue(null);
                            if (gameManagerInstance != null)
                            {
                                awardMethod = gameManagerType.GetMethod("AwardAchievementToPlayer", BindingFlags.Public | BindingFlags.Instance);
                                targetInstance = gameManagerInstance;
                            }
                        }
                    }
                }

                if (awardMethod == null)
                {
                    ShowToast("AwardAchievementToPlayer method not found");
                    return;
                }

                int awardedCount = 0;
                foreach (string platformKey in availableAchievements)
                {
                    try
                    {
                        awardMethod.Invoke(targetInstance, new object[] { platformKey });
                        awardedCount++;
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Msg($"Error awarding {platformKey}: {ex.Message}");
                    }
                }

                ShowToast($"Awarded {awardedCount}/{availableAchievements.Count} achievements");
                MelonLogger.Msg($"Awarded all achievements: {awardedCount}/{availableAchievements.Count}");
            }
            catch (Exception ex)
            {
                ShowToast($"Error awarding all achievements: {ex.Message}");
                MelonLogger.Msg($"Error in AwardAllAchievements: {ex.Message}");
            }
        }


    }
}
