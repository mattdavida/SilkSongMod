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
       
        private Rect windowRect = new Rect(Screen.width - 420, 20, 400, (Screen.height * 0.8f));
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
        
        // Input field variables
        private string healthAmount = "1";
        private string moneyAmount = "1000";
        private string shardAmount = "1000";
        private string setHealthAmount = "11"; // For setting exact health
        
        // Keybind variables
        private KeyCode[] currentKeybinds = new KeyCode[] 
        {
            KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4, KeyCode.F5, 
            KeyCode.F6, KeyCode.F8, KeyCode.F9, KeyCode.F10, KeyCode.F11, KeyCode.F12
        };
        private string[] keybindNames = new string[]
        {
            "Add Health", "Set Health", "Refill Health", "One Hit Kill", "Add Money",
            "Add Shards", "Unlock Crests", "Unlock Tools", "Unlock Items", "Max Collectables", "Auto Silk"
        };
        private bool isSettingKeybind = false;
        private int keybindToSet = -1;

        [System.Obsolete]
        public override void OnApplicationStart()
        {
            MelonLogger.Msg("Silksong Health Mod v1.0 - Ready!");
            MelonLogger.Msg("Controls: F1=Add Health, F2=Set Health to 11, F3=Refill Health, F4=One Hit Kill Mode, F5=Add 1000 Money, F6=Add 1000 Shards, F8=Unlock All Crests, F9=Unlock All Tools, F10=Unlock All Items, F11=Max All Collectables, F12=Toggle Auto Silk Refill, INSERT/TILDE=Toggle GUI");
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

                if (Input.GetKeyDown(currentKeybinds[7])) // Unlock Tools
                {
                    UnlockAllTools();
                }

                if (Input.GetKeyDown(currentKeybinds[8])) // Unlock Items
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
        
        private void ScanDamageFields()
        {
            if (fieldsScanned) return;
            
            damageFields.Clear();
            damageBehaviours.Clear();
            originalValues.Clear();
            
            MonoBehaviour[] allBehaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            
            foreach (MonoBehaviour behaviour in allBehaviours)
            {
                if (behaviour == null) continue;
                
                Type type = behaviour.GetType();
                string typeName = type.Name.ToLower();
                
                // Focus on DamageEnemies and similar combat components
                if (typeName.Contains("damage") || typeName.Contains("attack") || typeName.Contains("combat"))
                {
                    FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    
                    foreach (FieldInfo field in fields)
                    {
                        string fieldName = field.Name.ToLower();
                        
                        // Look for damage-related fields
                        if ((fieldName.Contains("damage") || fieldName.Contains("multiplier") || fieldName.Contains("power"))
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
                                }
                            }
                            catch (Exception)
                            {
                                // Skip inaccessible fields
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
                windowRect = GUI.Window(0, windowRect, GuiWindow, "SILKSONG CHEATS");
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

        private void DrawCheatsTab()
        {
            // Begin scroll view
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(380), GUILayout.Height(windowRect.height - 120));

            // Toggle section (only implemented features)
            GUILayout.Label("Toggle Features", GUI.skin.box);
            bool newAutoSilk = GUILayout.Toggle(autoRefillSilk, "Auto Silk Refill (every 2 seconds)");
            if (newAutoSilk != autoRefillSilk)
            {
                ToggleAutoSilkRefill();
                ShowToast($"Auto Silk Refill: {(autoRefillSilk ? "Enabled" : "Disabled")}");
            }

            GUILayout.Space(10);

            // Input fields section
            GUILayout.Label("Action Amounts", GUI.skin.box);
            
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
                    SetMaxHealthExact(targetHealth);
                    ShowToast($"Set max health to {targetHealth} - Save & reload to see UI!");
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

            GUILayout.Space(10);

            // Quick action buttons
            GUILayout.Label("Quick Actions", GUI.skin.box);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refill Health"))
            {
                RefillHealth();
                ShowToast("Health refilled to max!");
            }
            if (GUILayout.Button("One Hit Kill"))
            {
                EnableOneHitKill();
                ShowToast("One Hit Kill enabled!");
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Unlock Crests"))
            {
                UnlockAllCrests();
                ShowToast("All crests unlocked!");
            }
            if (GUILayout.Button("Unlock Tools"))
            {
                UnlockAllTools();
                ShowToast("All tools unlocked!");
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Unlock Items"))
            {
                UnlockAllItems();
                ShowToast("All items unlocked!");
            }
            if (GUILayout.Button("Max Collectables"))
            {
                MaxAllCollectables();
                ShowToast("All collectables maxed!");
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Unlock All Fast Travel Locations"))
            {
                UnlockAllFastTravel();
                ShowToast("All fast travel unlocked!");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Keybind section
            GUILayout.Label(isSettingKeybind ? "Press any key (ESC to cancel)" : "Keybind Settings", GUI.skin.box);
            
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
            currentKeybinds[7] = KeyCode.F9;   // Unlock Tools
            currentKeybinds[8] = KeyCode.F10;  // Unlock Items
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
                MelonLogger.Msg("=== F4: TARGETING ENEMY DAMAGE ONLY ===");
                
                int modifiedCount = 0;

                // Search for DamageEnemies components only
                MonoBehaviour[] allBehaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

                
                foreach (MonoBehaviour behaviour in allBehaviours)
                {
                    if (behaviour == null) continue;

                    Type type = behaviour.GetType();
                    string typeName = type.Name.ToLower();

                    if (typeName.Contains("tool")) {
                         MelonLogger.Msg($"TypeName: {typeName}");   

                    }



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
                                    if (field.FieldType == typeof(float))
                                    {
                                        field.SetValue(behaviour, 100.0f);
                                        MelonLogger.Msg($"Set {type.Name}.{field.Name} = 100.0f");
                                        modifiedCount++;
                                    }
                                    else if (field.FieldType == typeof(int))
                                    {
                                        field.SetValue(behaviour, 100);
                                        MelonLogger.Msg($"Set {type.Name}.{field.Name} = 100");
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

                MelonLogger.Msg($"Enemy Damage Boost: Modified {modifiedCount} DamageEnemies values only");
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Error targeting enemy damage: {e.Message}");
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
    