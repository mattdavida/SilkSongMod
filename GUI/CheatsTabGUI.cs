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
        private bool showBaseColors = false;
        private bool showActionAmounts = false;
        
        // Base Colors GUI state tracking
        private bool redActive = false;
        private bool blueActive = false;
        private bool greenActive = false;
        private bool purpleActive = false;
        private bool goldActive = false;
        private bool fireActive = false;
        private bool iceActive = false;
        private bool shadowActive = false;
        private bool rainbow1Active = false;
        private bool rainbow2Active = false;
        private bool goldShadowActive = false;
        private bool redShadowActive = false;
        private bool includeAllSceneComponents = false;
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

                bool newInfiniteToolUse = GUILayout.Toggle(context.GameStateService.IsInfiniteToolUseEnabled, "Infinite Tool Use");
                if (newInfiniteToolUse != context.GameStateService.IsInfiniteToolUseEnabled)
                {
                    context.GameStateService.ToggleInfiniteToolUse(onToast, onToast);
                }

                bool newQuickAttacks = GUILayout.Toggle(context.GameStateService.IsQuickAttacksEnabled, "Quick Attacks");
                if (newQuickAttacks != context.GameStateService.IsQuickAttacksEnabled)
                {
                    context.GameStateService.ToggleQuickAttacks(onToast, onToast);
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

            // Base Colors section
            showBaseColors = DrawCollapsingHeader("Base Colors", showBaseColors);
            if (showBaseColors)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                
                // Scene-wide color toggle
                GUILayout.BeginHorizontal();
                bool newIncludeAllScene = GUILayout.Toggle(includeAllSceneComponents, "Include All Scene Components");
                if (newIncludeAllScene != includeAllSceneComponents)
                {
                    includeAllSceneComponents = newIncludeAllScene;
                    
                    // Immediately apply/remove scene colors based on toggle state
                    bool hasActiveColor = context.HeroInspectorService.CurrentDressColor.HasValue || 
                                        fireActive || iceActive || shadowActive || rainbow1Active || rainbow2Active ||
                                        goldShadowActive || redShadowActive || redActive || blueActive || 
                                        greenActive || purpleActive || goldActive;
                    
                    if (hasActiveColor)
                    {
                        if (includeAllSceneComponents)
                        {
                            // Toggle turned ON - apply appropriate scene color
                            Color sceneColor = GetCurrentSceneColor(context);
                            context.HeroInspectorService.ChangeAllSceneSpriteColors(sceneColor, 
                                success => { /* Silent success */ },
                                error => onToast($"Scene color error: {error}"));
                            onToast($"Scene-wide coloring: Enabled (applied current effect)");
                        }
                        else
                        {
                            // Toggle turned OFF - reset scene to white/default
                            context.HeroInspectorService.ChangeAllSceneSpriteColors(Color.white, 
                                success => { /* Silent success */ },
                                error => onToast($"Scene reset error: {error}"));
                            onToast($"Scene-wide coloring: Disabled (scene reset to default)");
                        }
                    }
                    else
                    {
                        // No active color
                        string status = includeAllSceneComponents ? "Enabled" : "Disabled";
                        onToast($"Scene-wide coloring: {status}");
                    }
                }
                GUILayout.EndHorizontal();
                
                GUILayout.Space(5);
                
                if (context.HeroInspectorService != null)
                {
                    // Red and Blue
                    GUILayout.BeginHorizontal();
                    GUI.backgroundColor = redActive ? Color.green : Color.white;
                    if (GUILayout.Button("Red"))
                    {
                        SetActiveColor("red");
                        ApplyColorWithOptionalSceneWide(context, new Color(1f, 0.2f, 0.2f, 1f), onToast);
                    }
                    GUI.backgroundColor = blueActive ? Color.green : Color.white;
                    if (GUILayout.Button("Blue"))
                    {
                        SetActiveColor("blue");
                        ApplyColorWithOptionalSceneWide(context, new Color(0.2f, 0.2f, 1f, 1f), onToast);
                    }
                    GUI.backgroundColor = Color.white;
                    GUILayout.EndHorizontal();

                    // Green and Purple
                    GUILayout.BeginHorizontal();
                    GUI.backgroundColor = greenActive ? Color.green : Color.white;
                    if (GUILayout.Button("Green"))
                    {
                        SetActiveColor("green");
                        ApplyColorWithOptionalSceneWide(context, new Color(0.2f, 1f, 0.2f, 1f), onToast);
                    }
                    GUI.backgroundColor = purpleActive ? Color.green : Color.white;
                    if (GUILayout.Button("Purple"))
                    {
                        SetActiveColor("purple");
                        ApplyColorWithOptionalSceneWide(context, new Color(1f, 0.2f, 1f, 1f), onToast);
                    }
                    GUI.backgroundColor = Color.white;
                    GUILayout.EndHorizontal();

                    // Gold and Fire
                    GUILayout.BeginHorizontal();
                    GUI.backgroundColor = goldActive ? Color.green : Color.white;
                    if (GUILayout.Button("Gold"))
                    {
                        SetActiveColor("gold");
                        ApplyColorWithOptionalSceneWide(context, new Color(1f, 0.8f, 0.2f, 1f), onToast);
                    }
                    GUI.backgroundColor = fireActive ? Color.green : Color.white;
                    if (GUILayout.Button("Fire"))
                    {
                        SetActiveColor("fire");
                        ApplyComplexEffectWithOptionalSceneWide(context, () => {
                            // Set fire base color + movement effects
                            context.HeroInspectorService.ChangeDressColor(Color.red, null, null); // Establish base color
                            context.HeroInspectorService.ChangeVertexColor(2, 0, Color.red, null, null);
                            context.HeroInspectorService.ChangeVertexColor(2, 1, new Color(1f, 0.5f, 0f, 1f), null, null); // Orange
                            context.HeroInspectorService.ChangeVertexColor(2, 2, Color.yellow, null, null);
                            context.HeroInspectorService.ChangeVertexColor(2, 3, new Color(1f, 0.8f, 0f, 1f), null, null); // Gold
                        }, Color.red, onToast);
                        onToast("Fire transformation!");
                    }
                    GUI.backgroundColor = Color.white;
                    GUILayout.EndHorizontal();

                    // Ice and Shadow
                    GUILayout.BeginHorizontal();
                    GUI.backgroundColor = iceActive ? Color.green : Color.white;
                    if (GUILayout.Button("Ice"))
                    {
                        SetActiveColor("ice");
                        ApplyComplexEffectWithOptionalSceneWide(context, () => {
                            // Set ice base color + movement effects
                            context.HeroInspectorService.ChangeDressColor(Color.blue, null, null); // Establish base color
                            context.HeroInspectorService.ChangeVertexColor(2, 0, Color.blue, null, null);
                            context.HeroInspectorService.ChangeVertexColor(2, 1, Color.cyan, null, null);
                            context.HeroInspectorService.ChangeVertexColor(2, 2, new Color(0.7f, 0.9f, 1f, 1f), null, null); // Light blue
                            context.HeroInspectorService.ChangeVertexColor(2, 3, Color.white, null, null);
                        }, Color.blue, onToast);
                        onToast("Ice transformation!");
                    }
                    GUI.backgroundColor = shadowActive ? Color.green : Color.white;
                    if (GUILayout.Button("Shadow"))
                    {
                        SetActiveColor("shadow");
                        ApplyComplexEffectWithOptionalSceneWide(context, () => {
                            // Set shadow base color + movement effects
                            context.HeroInspectorService.ChangeDressColor(new Color(0.2f, 0.2f, 0.2f, 1f), null, null); // Establish base color
                            context.HeroInspectorService.ChangeVertexColor(3, 0, new Color(0.2f, 0.2f, 0.2f, 1f), null, null);
                            context.HeroInspectorService.ChangeVertexColor(3, 1, new Color(0.4f, 0.2f, 0.4f, 1f), null, null); // Dark purple
                            context.HeroInspectorService.ChangeVertexColor(3, 2, new Color(0.2f, 0.2f, 0.4f, 1f), null, null); // Dark blue
                            context.HeroInspectorService.ChangeVertexColor(3, 3, new Color(0.1f, 0.1f, 0.1f, 1f), null, null); // Almost black
                        }, new Color(0.2f, 0.2f, 0.2f, 1f), onToast);
                        onToast("Shadow transformation!");
                    }
                    GUI.backgroundColor = Color.white;
                    GUILayout.EndHorizontal();

                    // Rainbow 1 and Rainbow 2
                    GUILayout.BeginHorizontal();
                    GUI.backgroundColor = rainbow1Active ? Color.green : Color.white;
                    if (GUILayout.Button("Rainbow 1"))
                    {
                        SetActiveColor("rainbow1");
                        ApplyComplexEffectWithOptionalSceneWide(context, () => {
                            // Set bright base color + electric movement effects
                            context.HeroInspectorService.ChangeDressColor(new Color(1f, 1f, 0.7f, 1f), null, null); // Bright yellow base
                            context.HeroInspectorService.ChangeVertexColor(2, 0, new Color(1f, 1f, 0f, 1f), null, null); // Bright yellow
                            context.HeroInspectorService.ChangeVertexColor(2, 1, new Color(0f, 1f, 1f, 1f), null, null); // Bright cyan
                            context.HeroInspectorService.ChangeVertexColor(2, 2, new Color(1f, 0f, 1f, 1f), null, null); // Bright magenta
                            context.HeroInspectorService.ChangeVertexColor(2, 3, new Color(1.2f, 1.2f, 1.2f, 1f), null, null); // Super bright white
                        }, new Color(0f, 1f, 1f, 1f), onToast); // Bright cyan for scene
                        onToast("Rainbow 1 transformation!");
                    }
                    GUI.backgroundColor = rainbow2Active ? Color.green : Color.white;
                    if (GUILayout.Button("Rainbow 2"))
                    {
                        SetActiveColor("rainbow2");
                        ApplyComplexEffectWithOptionalSceneWide(context, () => {
                            // Set colorful base + rainbow movement effects
                            context.HeroInspectorService.ChangeDressColor(new Color(1f, 0.8f, 1f, 1f), null, null); // Light pink base
                            context.HeroInspectorService.ChangeVertexColor(3, 0, Color.red, null, null);
                            context.HeroInspectorService.ChangeVertexColor(3, 1, Color.green, null, null);
                            context.HeroInspectorService.ChangeVertexColor(3, 2, Color.blue, null, null);
                            context.HeroInspectorService.ChangeVertexColor(3, 3, Color.yellow, null, null);
                        }, new Color(1f, 0f, 0.5f, 1f), onToast); // Hot pink for scene
                        onToast("Rainbow 2 transformation!");
                    }
                    GUI.backgroundColor = Color.white;
                    GUILayout.EndHorizontal();

                    // Gold Shadow and Red Shadow
                    GUILayout.BeginHorizontal();
                    GUI.backgroundColor = goldShadowActive ? Color.green : Color.white;
                    if (GUILayout.Button("Gold Shadow"))
                    {
                        SetActiveColor("goldshadow");
                        ApplyComplexEffectWithOptionalSceneWide(context, () => {
                            // Set yellow base color + black shadow pattern
                            context.HeroInspectorService.ChangeDressColor(new Color(1f, 0.9f, 0.2f, 1f), null, null); // Bright yellow base
                            context.HeroInspectorService.ChangeVertexColor(2, 0, new Color(1f, 0.8f, 0f, 1f), null, null); // Golden yellow
                            context.HeroInspectorService.ChangeVertexColor(2, 1, new Color(0.2f, 0.2f, 0.1f, 1f), null, null); // Dark brown/black
                            context.HeroInspectorService.ChangeVertexColor(2, 2, new Color(0.8f, 0.7f, 0.1f, 1f), null, null); // Darker yellow
                            context.HeroInspectorService.ChangeVertexColor(2, 3, new Color(0.1f, 0.1f, 0.05f, 1f), null, null); // Black shadows
                        }, new Color(1f, 0.9f, 0.2f, 1f), onToast);
                        onToast("Gold Shadow transformation!");
                    }
                    GUI.backgroundColor = redShadowActive ? Color.green : Color.white;
                    if (GUILayout.Button("Red Shadow"))
                    {
                        SetActiveColor("redshadow");
                        ApplyComplexEffectWithOptionalSceneWide(context, () => {
                            // Set orange base color + black shadow pattern
                            context.HeroInspectorService.ChangeDressColor(new Color(1f, 0.5f, 0.1f, 1f), null, null); // Bright orange base
                            context.HeroInspectorService.ChangeVertexColor(3, 0, new Color(1f, 0.4f, 0f, 1f), null, null); // Deep orange
                            context.HeroInspectorService.ChangeVertexColor(3, 1, new Color(0.1f, 0.1f, 0.1f, 1f), null, null); // Black shadows
                            context.HeroInspectorService.ChangeVertexColor(3, 2, new Color(0.8f, 0.3f, 0f, 1f), null, null); // Darker orange
                            context.HeroInspectorService.ChangeVertexColor(3, 3, new Color(0.05f, 0.05f, 0.05f, 1f), null, null); // Deep black shadows
                        }, new Color(1f, 0.5f, 0.1f, 1f), onToast);
                        onToast("Red Shadow transformation!");
                    }
                    GUI.backgroundColor = Color.white;
                    GUILayout.EndHorizontal();

                    // Reset button (matching Always Active Tools style)
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Reset"))
                    {
                        ClearAllActiveColors();
                        // Clear the color system entirely - this stops all persistence
                        context.HeroInspectorService.ClearDressColor();
                        
                        // If scene-wide toggle is enabled, also reset all scene sprites to white/default
                        if (includeAllSceneComponents)
                        {
                            context.HeroInspectorService.ChangeAllSceneSpriteColors(Color.white, 
                                success => { /* Silent success for scene reset */ },
                                error => onToast($"Scene reset error: {error}"));
                            onToast("All effects reset to default (including scene sprites)");
                        }
                        else
                        {
                            onToast("All effects reset to default");
                        }
                    }
                    
                    // Experimental developer tool for testing current color on all scene sprites
                    
                    // if (GUILayout.Button("🧪 Test All Scene Sprites"))
                    // {
                    //     // Use currently active color, or white if no color is set
                    //     Color testColor = context.HeroInspectorService.CurrentDressColor ?? Color.white;
                    //     string colorName = context.HeroInspectorService.CurrentDressColor.HasValue ? 
                    //         $"current color ({testColor})" : "white (no active color)";
                            
                    //     context.HeroInspectorService.ChangeAllSceneSpriteColors(testColor, 
                    //         success => onToast($"Scene Test with {colorName}: {success}"),
                    //         error => onToast($"Scene Test Failed: {error}"));
                    // }
                    
                    
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Label("Hero Inspector Service not available");
                }
                
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
                    if (GUILayout.Button("Add", GUILayout.Width(50)))
                    {
                        if (int.TryParse(collectableAmount, out int amount) && selectedCollectableIndex < collectableNames.Length)
                        {
                            string selectedCollectable = collectableNames[selectedCollectableIndex];
                            bool success = context.CollectableService.AddCollectableAmount(selectedCollectable, amount, onToast, onToast);
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
                    if (GUILayout.Button("Take", GUILayout.Width(50)))
                    {
                        if (int.TryParse(collectableAmount, out int amount) && selectedCollectableIndex < collectableNames.Length)
                        {
                            string selectedCollectable = collectableNames[selectedCollectableIndex];
                            bool success = context.CollectableService.TakeCollectableAmount(selectedCollectable, amount, onToast, onToast);
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

        private void SetActiveColor(string colorName)
        {
            // Reset all colors to inactive
            redActive = false;
            blueActive = false;
            greenActive = false;
            purpleActive = false;
            goldActive = false;
            fireActive = false;
            iceActive = false;
            shadowActive = false;
            rainbow1Active = false;
            rainbow2Active = false;
            goldShadowActive = false;
            redShadowActive = false;
            
            // Set the selected color to active
            switch (colorName.ToLower())
            {
                case "red": redActive = true; break;
                case "blue": blueActive = true; break;
                case "green": greenActive = true; break;
                case "purple": purpleActive = true; break;
                case "gold": goldActive = true; break;
                case "fire": fireActive = true; break;
                case "ice": iceActive = true; break;
                case "shadow": shadowActive = true; break;
                case "rainbow1": rainbow1Active = true; break;
                case "rainbow2": rainbow2Active = true; break;
                case "goldshadow": goldShadowActive = true; break;
                case "redshadow": redShadowActive = true; break;
            }
        }

        private void ClearAllActiveColors()
        {
            redActive = false;
            blueActive = false;
            greenActive = false;
            purpleActive = false;
            goldActive = false;
            fireActive = false;
            iceActive = false;
            shadowActive = false;
            rainbow1Active = false;
            rainbow2Active = false;
            goldShadowActive = false;
            redShadowActive = false;
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

        private void FilterCollectables(GuiContext context)
        {
            filteredCollectableNames = context.CollectableService.FilterCollectables(collectableSearchFilter);
        }

        /// <summary>
        /// Applies color change and optionally includes all scene components based on toggle state.
        /// </summary>
        private void ApplyColorWithOptionalSceneWide(GuiContext context, Color color, System.Action<string> onToast)
        {
            // Always apply to hero first
            context.HeroInspectorService.ChangeDressColor(color, onToast, onToast);
            context.HeroInspectorService.CaptureCurrentColorState();
            
            // If toggle is enabled, also apply to all scene sprites
            if (includeAllSceneComponents)
            {
                context.HeroInspectorService.ChangeAllSceneSpriteColors(color, 
                    success => { /* Silent success for scene-wide */ },
                    error => onToast($"Scene-wide error: {error}"));
            }
        }

        /// <summary>
        /// Applies complex vertex color effects and optionally includes all scene components.
        /// </summary>
        private void ApplyComplexEffectWithOptionalSceneWide(GuiContext context, System.Action applyVertexColors, Color baseColor, System.Action<string> onToast)
        {
            // Always apply vertex colors to hero first
            applyVertexColors();
            context.HeroInspectorService.CaptureCurrentColorState();
            
            // If toggle is enabled, also apply base color to all scene sprites
            if (includeAllSceneComponents)
            {
                context.HeroInspectorService.ChangeAllSceneSpriteColors(baseColor, 
                    success => { /* Silent success for scene-wide */ },
                    error => onToast($"Scene-wide error: {error}"));
            }
        }

        /// <summary>
        /// Gets the appropriate scene color for the currently active color effect.
        /// </summary>
        private Color GetCurrentSceneColor(GuiContext context)
        {
            // Map active color effects to their scene colors
            if (fireActive) return Color.red;
            if (iceActive) return Color.blue;
            if (shadowActive) return new Color(0.2f, 0.2f, 0.2f, 1f);
            if (rainbow1Active) return new Color(0f, 1f, 1f, 1f); // Bright cyan
            if (rainbow2Active) return new Color(1f, 0f, 0.5f, 1f); // Hot pink
            if (goldShadowActive) return new Color(1f, 0.9f, 0.2f, 1f);
            if (redShadowActive) return new Color(1f, 0.5f, 0.1f, 1f);
            
            // For simple colors or if no flags are set, use the stored hero color
            return context.HeroInspectorService.CurrentDressColor ?? Color.white;
        }

        #endregion
    }
}