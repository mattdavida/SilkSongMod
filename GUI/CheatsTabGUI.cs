using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SilkSong.UserInterface
{
    /// <summary>
    /// GUI component for the Cheats tab.
    /// Handles all cheat-related GUI rendering and interactions including:
    /// - Toggle Features (auto silk, one-hit kill, invincibility, game speed, equipment changes)
    /// - Action Amounts (health, currency, invincibility modes, game speed control)
    /// - Collectible Items (scanning, selection, amount setting)
    /// - Crest Tools (scanning, unlocking, ammo management)
    /// - Player Skills (all 8 movement/combat abilities)
    /// - Always Active Tools (18 Yellow Tools system)
    /// - Quick Actions (refill health, unlock systems, map/achievements)
    /// - Keybind Settings (customization, defaults, clearing)
    /// </summary>
    public class CheatsTabGUI
    {
        // Keybind management component
        private KeybindSettingsGUI keybindSettingsGUI = new KeybindSettingsGUI();
        // Collapsible section states
        private bool showToggleFeatures = true;
        private bool showActionAmounts = false;
        private bool showCollectibleItems = false;
        private bool showCrestTools = false;
        private bool showPlayerSkills = false;
        private bool showAlwaysActiveTools = false;
        private bool showQuickActions = true;
        // Input field variables
        private string healthAmount = "1";
        private string moneyAmount = "1000";
        private string shardAmount = "1000";
        private string setHealthAmount = "11"; // For setting exact health
        private string gameSpeedText = "2.0";

        // Set Collectable Amount variables
        private string collectableAmount = "1";
        private string[] collectableNames = new string[0];
        private int selectedCollectableIndex = 0;
        private bool showCollectableDropdown = false;
        private Vector2 collectableDropdownScroll = Vector2.zero;
        private string collectableSearchFilter = "";
        private string[] filteredCollectableNames = new string[0];

        // Crest Tools variables
        // Crest Tools variables (now using ToolService)
        private int selectedToolIndex = 0;
        private bool showToolDropdown = false;
        private Vector2 toolDropdownScroll = Vector2.zero;
        private string toolSearchFilter = "";
        private string[] filteredToolNames = new string[0];
        private string toolStorageAmount = "100";
        private bool showSkillsOnly = false;


        // State for infinite air jump (managed by PlayerSkillService)
        private bool infiniteAirJumpEnabled = false;

        // Auto silk refill state
        private bool autoRefillSilk = false;

        /// <summary>
        /// Renders the Cheats tab GUI.
        /// </summary>
        /// <param name="context">GUI context containing all services and shared state</param>
        /// <param name="scrollPosition">Current scroll position (passed by reference)</param>
        /// <param name="windowRect">Current window rectangle for sizing</param>
        /// <param name="autoRefillSilkState">Current auto silk refill state</param>
        /// <param name="infiniteAirJumpState">Current infinite air jump state</param>
        /// <param name="isSettingKeybindState">Current keybind setting state</param>
        /// <param name="keybindToSetState">Current keybind being set</param>
        /// <param name="currentKeybindsState">Current keybind array</param>
        public void Render(GuiContext context, ref Vector2 scrollPosition, Rect windowRect, 
                          ref bool autoRefillSilkState, ref bool infiniteAirJumpState,
                          ref bool isSettingKeybindState, ref int keybindToSetState, 
                          ref KeyCode[] currentKeybindsState, System.Action<string> onToast)
        {
            // Sync state from main class
            autoRefillSilk = autoRefillSilkState;
            infiniteAirJumpEnabled = infiniteAirJumpState;

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
                    autoRefillSilk = newAutoSilk;
                    autoRefillSilkState = autoRefillSilk; // Sync back to main class
                    onToast($"Auto Silk Refill: {(autoRefillSilk ? "Enabled" : "Disabled")}");
                }

                bool newOneHitKill = GUILayout.Toggle(context.GameStateService.IsOneHitKillEnabled, "One Hit Kill Mode");
                if (newOneHitKill != context.GameStateService.IsOneHitKillEnabled)
                {
                    context.GameStateService.ToggleOneHitKill(onToast, onToast);
                }

                bool newInfiniteAirJump = GUILayout.Toggle(infiniteAirJumpEnabled, "Infinite Air Jump");
                if (newInfiniteAirJump != infiniteAirJumpEnabled)
                {
                    context.PlayerSkillService.ToggleInfiniteAirJump(onToast, onToast);
                    infiniteAirJumpEnabled = context.PlayerSkillService.IsInfiniteAirJumpUnlocked();
                    infiniteAirJumpState = infiniteAirJumpEnabled; // Sync back to main class
                }

                bool newInvincibility = GUILayout.Toggle(context.GameStateService.IsInvincibilityEnabled, "Invincibility");
                if (newInvincibility != context.GameStateService.IsInvincibilityEnabled)
                {
                    context.GameStateService.ToggleInvincibility(onToast, onToast);
                }

                bool newGameSpeed = GUILayout.Toggle(context.GameStateService.IsGameSpeedEnabled, $"Game Speed Control ({(context.GameStateService.CurrentGameSpeed * 100):F0}%)");
                if (newGameSpeed != context.GameStateService.IsGameSpeedEnabled)
                {
                    context.GameStateService.ToggleGameSpeed(onToast, onToast);
                }

                // Change Equipment Anywhere toggle
                try
                {
                    Type cheatManagerType = context.ToolService.FindTypeInAssemblies("CheatManager");
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
                                onToast($"Change Equipment Anywhere: {(newValue ? "Enabled" : "Disabled")}");
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
                        if (context.HealthService.AddHealth(health))
                            onToast($"Added {health} health!");
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
                        context.ConfirmationSystem.ShowConfirmation("Set Health",
                            $"This will set health to {targetHealth}. You must quit to main menu and restart to see the effect in game. Max amount that will show in UI is 11.",
                            () =>
                            {
                                context.HealthService.SetMaxHealthExact(targetHealth, onToast, onToast);
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
                        if (context.CurrencyService.AddMoney(money))
                            onToast($"Added {money} money!");
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
                        if (context.CurrencyService.AddShards(shards))
                            onToast($"Added {shards} shards!");
                    }
                }
                GUILayout.EndHorizontal();

                // Invincibility Mode dropdown
                GUILayout.BeginHorizontal();
                GUILayout.Label("Invincibility:", GUILayout.Width(80));
                string[] invincibilityModes = { "FullInvincible", "PreventDeath" };
                int newInvincibilityMode = GUILayout.SelectionGrid(context.GameStateService.SelectedInvincibilityMode, invincibilityModes, 2, GUILayout.Width(200));

                // Apply mode change immediately if invincibility is enabled
                if (newInvincibilityMode != context.GameStateService.SelectedInvincibilityMode)
                {
                    context.GameStateService.SetInvincibilityMode(newInvincibilityMode, onToast, onToast);
                }
                GUILayout.EndHorizontal();

                // Game Speed Control (always visible, like health)
                GUILayout.BeginHorizontal();
                GUILayout.Label("Game Speed:", GUILayout.Width(80));
                gameSpeedText = GUILayout.TextField(gameSpeedText, GUILayout.Width(60));
                GUILayout.Label("x", GUILayout.Width(15));
                if (GUILayout.Button("Set", GUILayout.Width(50)))
                {
                    context.GameStateService.UpdateGameSpeedFromText(gameSpeedText, onToast, onToast);
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
                if (collectableNames.Length == 0)
                {
                    collectableNames = context.CollectableService.GetAvailableCollectables().ToArray();
                    FilterCollectables(context); // Initialize filtered list
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
                            FilterCollectables(context);
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
                            FilterCollectables(context);
                            collectableDropdownScroll = Vector2.zero; // Reset scroll when filtering
                        }
                        GUILayout.EndHorizontal();

                        // Clear search button
                        if (!string.IsNullOrEmpty(collectableSearchFilter))
                        {
                            if (GUILayout.Button("Clear", GUILayout.Width(60)))
                            {
                                collectableSearchFilter = "";
                                FilterCollectables(context);
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
                            bool success = context.CollectableService.SetCollectableAmount(selectedCollectable, amount, onToast, onToast);
                            if (success)
                            {
                                showCollectableDropdown = false; // Close dropdown after action
                            }
                        }
                        else
                        {
                            onToast("Invalid amount or no item selected!");
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Label("No collectables found - enter game first", GUI.skin.label);
                    if (GUILayout.Button("Refresh List", GUILayout.Width(100)))
                    {
                        context.CollectableService.ForceScan();
                        collectableNames = context.CollectableService.GetAvailableCollectables().ToArray();
                        FilterCollectables(context);
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
                    filteredToolNames = context.ToolService.FilterTools(toolSearchFilter, showSkillsOnly); // Update filtered list
                }
                GUILayout.EndHorizontal();

                // Scan tools on first access
                if (!context.ToolService.ToolsScanned)
                {
                    context.ToolService.ScanTools();
                }

                if (context.ToolService.ToolNames.Length > 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Tool:", GUILayout.Width(80));

                    // Dropdown button
                    string currentSelection = selectedToolIndex < context.ToolService.ToolNames.Length ? context.ToolService.ToolNames[selectedToolIndex] : "Select Tool";

                    if (GUILayout.Button($"{currentSelection} ▼", GUILayout.Width(180)))
                    {
                        showToolDropdown = !showToolDropdown;
                        if (showToolDropdown)
                        {
                            toolSearchFilter = ""; // Reset search when opening
                            filteredToolNames = context.ToolService.FilterTools(toolSearchFilter, showSkillsOnly);
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
                            filteredToolNames = context.ToolService.FilterTools(toolSearchFilter, showSkillsOnly);
                            toolDropdownScroll = Vector2.zero; // Reset scroll when filtering
                        }
                        GUILayout.EndHorizontal();

                        // Clear search button
                        if (!string.IsNullOrEmpty(toolSearchFilter))
                        {
                            if (GUILayout.Button("Clear", GUILayout.Width(60)))
                            {
                                toolSearchFilter = "";
                                filteredToolNames = context.ToolService.FilterTools(toolSearchFilter, showSkillsOnly);
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
                                    int originalIndex = Array.IndexOf(context.ToolService.ToolNames, toolName);

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
                        if (selectedToolIndex < context.ToolService.ToolNames.Length)
                        {
                            string selectedTool = context.ToolService.ToolNames[selectedToolIndex];
                            if (context.ToolService.UnlockSpecificTool(selectedTool))
                                onToast($"Unlocked {selectedTool}!");
                            else
                                onToast($"Failed to unlock {selectedTool}");
                            showToolDropdown = false; // Close dropdown after action
                        }
                        else
                        {
                            onToast("No tool selected!");
                        }
                    }
                    GUILayout.EndHorizontal();

                    // Only show ammo-related controls if selected tool uses ammo
                    if (context.ToolService.SelectedToolUsesAmmo(selectedToolIndex))
                    {
                        // Get current storage amount once
                        int currentStorage = context.ToolService.GetSelectedToolCurrentStorage(selectedToolIndex);

                        // Update storage amount field based on current storage
                        if (currentStorage > 0)
                        {
                            // Only update if the field is empty or default
                            if (string.IsNullOrEmpty(toolStorageAmount) || toolStorageAmount == "100")
                            {
                                toolStorageAmount = currentStorage.ToString();
                            }
                        }

                        // Storage amount control with current value display
                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"Base Storage Amount (Current: {currentStorage}):", GUILayout.Width(200));
                        toolStorageAmount = GUILayout.TextField(toolStorageAmount, GUILayout.Width(60));
                        if (GUILayout.Button("Set", GUILayout.Width(50)))
                        {
                            if (int.TryParse(toolStorageAmount, out int amount) && selectedToolIndex < context.ToolService.ToolNames.Length)
                            {
                                string selectedTool = context.ToolService.ToolNames[selectedToolIndex];
                                if (context.ToolService.SetToolStorage(selectedTool, amount))
                                    onToast($"Set {selectedTool} storage to {amount}!");
                                else
                                    onToast($"Failed to set {selectedTool} storage");
                                showToolDropdown = false; // Close dropdown after action
                            }
                            else
                            {
                                onToast("Invalid amount or no tool selected!");
                            }
                        }
                        GUILayout.EndHorizontal();

                        // Refill ammo control
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Refill Ammo", GUILayout.Width(100)))
                        {
                            if (selectedToolIndex < context.ToolService.ToolNames.Length)
                            {
                                string selectedTool = context.ToolService.ToolNames[selectedToolIndex];
                                if (context.ToolService.RefillToolAmmo(selectedTool))
                                    onToast($"Refilled {selectedTool} ammo!");
                                else
                                    onToast($"Failed to refill {selectedTool} ammo");
                                showToolDropdown = false; // Close dropdown after action
                            }
                            else
                            {
                                onToast("No tool selected!");
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
                        context.ToolService.ScanTools(); // Force rescan
                        filteredToolNames = context.ToolService.FilterTools(toolSearchFilter, showSkillsOnly);
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
                bool doubleJumpUnlocked = context.PlayerSkillService.IsDoubleJumpUnlocked();
                GUI.color = doubleJumpUnlocked ? Color.green : Color.white;
                string doubleJumpText = doubleJumpUnlocked ? "Double Jump ✓" : "Double Jump";
                if (GUILayout.Button(doubleJumpText, GUILayout.Width(170)))
                {
                    context.PlayerSkillService.ToggleDoubleJump(onToast, onToast);
                }
                GUI.color = Color.white;

                // Dash Toggle
                bool dashUnlocked = context.PlayerSkillService.IsDashUnlocked();
                GUI.color = dashUnlocked ? Color.green : Color.white;
                string dashText = dashUnlocked ? "Dash ✓" : "Dash";
                if (GUILayout.Button(dashText, GUILayout.Width(170)))
                {
                    context.PlayerSkillService.ToggleDash(onToast, onToast);
                }
                GUI.color = Color.white;

                // Wall Jump Toggle
                bool wallJumpUnlocked = context.PlayerSkillService.IsWallJumpUnlocked();
                GUI.color = wallJumpUnlocked ? Color.green : Color.white;
                string wallJumpText = wallJumpUnlocked ? "Wall Jump ✓" : "Wall Jump";
                if (GUILayout.Button(wallJumpText, GUILayout.Width(170)))
                {
                    context.PlayerSkillService.ToggleWallJump(onToast, onToast);
                }
                GUI.color = Color.white;

                // Glide Toggle
                bool glideUnlocked = context.PlayerSkillService.IsGlideUnlocked();
                GUI.color = glideUnlocked ? Color.green : Color.white;
                string glideText = glideUnlocked ? "Glide ✓" : "Glide";
                if (GUILayout.Button(glideText, GUILayout.Width(170)))
                {
                    context.PlayerSkillService.ToggleGlide(onToast, onToast);
                }
                GUI.color = Color.white;
                GUILayout.EndVertical();

                GUILayout.Space(10);

                // Right Column (Special Abilities)
                GUILayout.BeginVertical();

                // Charge Attack Toggle
                bool chargeAttackUnlocked = context.PlayerSkillService.IsChargeAttackUnlocked();
                GUI.color = chargeAttackUnlocked ? Color.green : Color.white;
                string chargeAttackText = chargeAttackUnlocked ? "Charge Attack ✓" : "Charge Attack";
                if (GUILayout.Button(chargeAttackText, GUILayout.Width(170)))
                {
                    context.PlayerSkillService.ToggleChargeAttack(onToast, onToast);
                }
                GUI.color = Color.white;

                // Needolin Toggle
                bool needolinUnlocked = context.PlayerSkillService.IsNeedolinUnlocked();
                GUI.color = needolinUnlocked ? Color.green : Color.white;
                string needolinText = needolinUnlocked ? "Needolin ✓" : "Needolin";
                if (GUILayout.Button(needolinText, GUILayout.Width(170)))
                {
                    context.PlayerSkillService.ToggleNeedolin(onToast, onToast);
                }
                GUI.color = Color.white;

                // Grappling Hook Toggle
                bool grapplingHookUnlocked = context.PlayerSkillService.IsGrapplingHookUnlocked();
                GUI.color = grapplingHookUnlocked ? Color.green : Color.white;
                string grapplingHookText = grapplingHookUnlocked ? "Grappling Hook ✓" : "Grappling Hook";
                if (GUILayout.Button(grapplingHookText, GUILayout.Width(170)))
                {
                    context.PlayerSkillService.ToggleGrapplingHook(onToast, onToast);
                }
                GUI.color = Color.white;

                // Super Jump Toggle
                bool superJumpUnlocked = context.PlayerSkillService.IsSuperJumpUnlocked();
                GUI.color = superJumpUnlocked ? Color.green : Color.white;
                string superJumpText = superJumpUnlocked ? "Super Jump ✓" : "Super Jump";
                if (GUILayout.Button(superJumpText, GUILayout.Width(170)))
                {
                    context.PlayerSkillService.ToggleSuperJump(onToast, onToast);
                }
                GUI.color = Color.white;
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            GUILayout.Space(5);

            // Always Active Tools section
            showAlwaysActiveTools = DrawCollapsingHeader("Always Active Tools", showAlwaysActiveTools);
            if (showAlwaysActiveTools)
            {
                GUILayout.BeginVertical(GUI.skin.box);

                // Compass and Magnetite Brooch (top priority tools)
                GUILayout.BeginHorizontal();
                GUI.backgroundColor = context.AlwaysActiveToolsService.IsCompassActive ? Color.green : Color.white;
                if (GUILayout.Button("Compass"))
                {
                    context.AlwaysActiveToolsService.ToggleCompass(onToast, onToast);
                }
                GUI.backgroundColor = context.AlwaysActiveToolsService.IsMagnetiteBroochActive ? Color.green : Color.white;
                if (GUILayout.Button("Magnetite Brooch"))
                {
                    context.AlwaysActiveToolsService.ToggleMagnetiteBrooch(onToast, onToast);
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();

                // Shard Pendant and Ascendant's Grip
                GUILayout.BeginHorizontal();
                GUI.backgroundColor = context.AlwaysActiveToolsService.IsShardPendantActive ? Color.green : Color.white;
                if (GUILayout.Button("Shard Pendant"))
                {
                    context.AlwaysActiveToolsService.ToggleShardPendant(onToast, onToast);
                }
                GUI.backgroundColor = context.AlwaysActiveToolsService.IsAscendantsGripActive ? Color.green : Color.white;
                if (GUILayout.Button("Ascendant's Grip"))
                {
                    context.AlwaysActiveToolsService.ToggleAscendantsGrip(onToast, onToast);
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();

                // Dead Bug's Purse and Shell Satchel
                GUILayout.BeginHorizontal();
                GUI.backgroundColor = context.AlwaysActiveToolsService.IsDeadBugsPurseActive ? Color.green : Color.white;
                if (GUILayout.Button("Dead Bug's Purse"))
                {
                    context.AlwaysActiveToolsService.ToggleDeadBugsPurse(onToast, onToast);
                }
                GUI.backgroundColor = context.AlwaysActiveToolsService.IsShellSatchelActive ? Color.green : Color.white;
                if (GUILayout.Button("Shell Satchel"))
                {
                    context.AlwaysActiveToolsService.ToggleShellSatchel(onToast, onToast);
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();

                // Weighted Belt and Silkspeed Anklets
                GUILayout.BeginHorizontal();
                GUI.backgroundColor = context.AlwaysActiveToolsService.IsWeightedBeltActive ? Color.green : Color.white;
                if (GUILayout.Button("Weighted Belt"))
                {
                    context.AlwaysActiveToolsService.ToggleWeightedBelt(onToast, onToast);
                }
                GUI.backgroundColor = context.AlwaysActiveToolsService.IsSilkspeedAnkletsActive ? Color.green : Color.white;
                if (GUILayout.Button("Silkspeed Anklets"))
                {
                    context.AlwaysActiveToolsService.ToggleSilkspeedAnklets(onToast, onToast);
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();

                // Spider Strings and Scuttlebrace
                GUILayout.BeginHorizontal();
                GUI.backgroundColor = context.AlwaysActiveToolsService.IsSpiderStringsActive ? Color.green : Color.white;
                if (GUILayout.Button("Spider Strings"))
                {
                    context.AlwaysActiveToolsService.ToggleSpiderStrings(onToast, onToast);
                }
                GUI.backgroundColor = context.AlwaysActiveToolsService.IsScuttlebraceActive ? Color.green : Color.white;
                if (GUILayout.Button("Scuttlebrace"))
                {
                    context.AlwaysActiveToolsService.ToggleScuttlebrace(onToast, onToast);
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();

                // Barbed Bracelet and Thief's Mark
                GUILayout.BeginHorizontal();
                GUI.backgroundColor = context.AlwaysActiveToolsService.IsBarbedBraceletActive ? Color.green : Color.white;
                if (GUILayout.Button("Barbed Bracelet"))
                {
                    context.AlwaysActiveToolsService.ToggleBarbedBracelet(onToast, onToast);
                }
                GUI.backgroundColor = context.AlwaysActiveToolsService.IsThiefsMarkActive ? Color.green : Color.white;
                if (GUILayout.Button("Thief's Mark"))
                {
                    context.AlwaysActiveToolsService.ToggleThiefsMark(onToast, onToast);
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();

                // Magnetite Dice and Longclaw
                GUILayout.BeginHorizontal();
                GUI.backgroundColor = context.AlwaysActiveToolsService.IsMagnetiteDiceActive ? Color.green : Color.white;
                if (GUILayout.Button("Magnetite Dice"))
                {
                    context.AlwaysActiveToolsService.ToggleMagnetiteDice(onToast, onToast);
                }
                GUI.backgroundColor = context.AlwaysActiveToolsService.IsLongclawActive ? Color.green : Color.white;
                if (GUILayout.Button("Longclaw"))
                {
                    context.AlwaysActiveToolsService.ToggleLongclaw(onToast, onToast);
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();

                // Pin Badge and Quick Sling
                GUILayout.BeginHorizontal();
                GUI.backgroundColor = context.AlwaysActiveToolsService.IsPinBadgeActive ? Color.green : Color.white;
                if (GUILayout.Button("Pin Badge"))
                {
                    context.AlwaysActiveToolsService.TogglePinBadge(onToast, onToast);
                }
                GUI.backgroundColor = context.AlwaysActiveToolsService.IsQuickSlingActive ? Color.green : Color.white;
                if (GUILayout.Button("Quick Sling"))
                {
                    context.AlwaysActiveToolsService.ToggleQuickSling(onToast, onToast);
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();

                // Weavelight and Warding Bell
                GUILayout.BeginHorizontal();
                GUI.backgroundColor = context.AlwaysActiveToolsService.IsWeavelightActive ? Color.green : Color.white;
                if (GUILayout.Button("Weavelight"))
                {
                    context.AlwaysActiveToolsService.ToggleWeavelight(onToast, onToast);
                }
                GUI.backgroundColor = context.AlwaysActiveToolsService.IsWardingBellActive ? Color.green : Color.white;
                if (GUILayout.Button("Warding Bell"))
                {
                    context.AlwaysActiveToolsService.ToggleWardingBell(onToast, onToast);
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();

                // Clear All button (centered)
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Clear All"))
                {
                    context.ConfirmationSystem.ShowConfirmation("Clear All Tools",
                        "This will disable all always-active tools. Are you sure?",
                        () =>
                        {
                            context.AlwaysActiveToolsService.ClearAllTools(onToast, onToast);
                        });
                }
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
                    if (context.HealthService.RefillHealth())
                        onToast("Health refilled to max!");
                }
                string ohkButtonText = context.GameStateService.IsOneHitKillEnabled ? "Disable One Hit Kill" : "Enable One Hit Kill";
                GUI.color = context.GameStateService.IsOneHitKillEnabled ? Color.green : Color.white;
                if (GUILayout.Button(ohkButtonText))
                {
                    context.GameStateService.ToggleOneHitKill(onToast, onToast);
                }
                GUI.color = Color.white;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Unlock Crests"))
                {
                    context.ConfirmationSystem.ShowConfirmation("Unlock All Crests",
                        "This will unlock all crests. This action cannot be undone.",
                        () =>
                        {
                            context.ToolService.UnlockAllCrests();
                        });
                }
                if (GUILayout.Button("Unlock Crest Skills"))
                {
                    context.ConfirmationSystem.ShowConfirmation("Unlock All Crest Skills",
                        "This will unlock all crest skills. This action cannot be undone.",
                        () =>
                        {
                            context.ToolService.UnlockAllTools();
                        });
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Unlock Crest Tools"))
                {
                    context.ConfirmationSystem.ShowConfirmation("Unlock All Crest Tools",
                        "This will unlock all crest tools. This action cannot be undone.",
                        () =>
                        {
                            context.ToolService.UnlockAllItems();
                        });
                }
                if (GUILayout.Button("Max Collectables"))
                {
                    context.ConfirmationSystem.ShowConfirmation("Max All Collectables",
                        "This will maximize all collectible items. This action cannot be undone.",
                        () =>
                        {
                            int maxedCount = context.CollectableService.MaxAllCollectables();
                            onToast($"Maxed {maxedCount} collectables!");
                        });
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Unlock All Fast Travel Locations"))
                {
                    context.ConfirmationSystem.ShowConfirmation("Unlock All Fast Travel",
                        "This will unlock all fast travel locations. This action cannot be undone.",
                        () =>
                        {
                            if (context.ToolService.UnlockAllFastTravel())
                                onToast("All fast travel unlocked!");
                        });
                }
                if (GUILayout.Button("Unlock All Maps"))
                {
                    context.ConfirmationSystem.ShowConfirmation("Unlock All Maps",
                        "This will unlock all map items. This action cannot be undone.",
                        () =>
                        {
                            int unlockedCount = context.ToolService.UnlockAllMapItems();
                            onToast($"Unlocked {unlockedCount} map items!");
                        });
                }
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }

            // Keybind Settings section
            keybindSettingsGUI.RenderSection(ref currentKeybindsState, ref isSettingKeybindState, ref keybindToSetState, onToast, DrawCollapsingHeader);

            GUILayout.EndScrollView();

            // Sync state back to main class
            autoRefillSilkState = autoRefillSilk;
            infiniteAirJumpState = infiniteAirJumpEnabled;
        }

        #region Helper Methods

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

        private void FilterCollectables(GuiContext context)
        {
            filteredCollectableNames = context.CollectableService.FilterCollectables(collectableSearchFilter);
        }


        #endregion
    }
}