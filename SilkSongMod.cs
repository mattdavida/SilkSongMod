#if MELONLOADER
using MelonLoader;
#elif BEPINEX
using BepInEx;
using BepInEx.Logging;
#endif
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
using SilkSong.Core;
using SilkSong.Interfaces;
using SilkSong.Framework;
using SilkSong.UserInterface;
using SilkSong.Services;

namespace SilkSong
{
#if MELONLOADER
    public class SilkSongMod : MelonMod
#elif BEPINEX
    [BepInPlugin("com.silksong.cheats", "Silksong Cheats", "1.0.0")]
    public class SilkSongMod : BaseUnityPlugin
#endif
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
        private ToastSystem toastSystem = new ToastSystem();
        
        // Confirmation modal system
        private ConfirmationSystem confirmationSystem = new ConfirmationSystem();
        
        // Framework abstraction layer
#if MELONLOADER
        private IModLogger logger = new MelonLoggerAdapter();
#elif BEPINEX
        private IModLogger logger;
#endif
        private IInputHandler inputHandler = new UnityInputAdapter();
        private ITimeProvider timeProvider = new UnityTimeAdapter();
        
        // GUI system
        private GuiContext guiContext = new GuiContext();
        private BalanceTabGUI balanceTabGUI = new BalanceTabGUI();
        private CheatsTabGUI cheatsTabGUI = new CheatsTabGUI();
        private AchievementsTabGUI achievementsTabGUI = new AchievementsTabGUI();
        
        // Services
        private HealthService healthService;
        private CurrencyService currencyService;
        private ToolService toolService;
        private CollectableService collectableService;
        private PlayerSkillService playerSkillService;
        private AchievementService achievementService;
        private GameStateService gameStateService;
        private AlwaysActiveToolsService alwaysActiveToolsService;
        private PlayerDataService playerDataService;
        private BalanceService balanceService;
        private HeroInspectorService heroInspectorService;

        // Flag to track if always active tools have been initialized (prevents reset on scene load)
        private bool alwaysActiveToolsInitialized = false;

        // Scroll position for the GUI
        private Vector2 scrollPosition = Vector2.zero;
        private Vector2 achievementScrollPosition = Vector2.zero;

        // Tab system
        private int selectedTab = 0;
        private string[] tabNames = { "Cheats", "Balance", "Achievements" };

        // Balance/multiplier system (moved to BalanceService)
        private string globalMultiplierText = "1.0";
        private float globalMultiplier = 1.0f;
        private bool showDetails = false;


        // Infinite Air Jump toggle system (managed by PlayerSkillService)
        private bool infiniteAirJumpEnabled = false;


        // Input field variables (used in keybind processing)
        private string healthAmount = "1";
        private string moneyAmount = "1000";
        private string shardAmount = "1000";
        private string setHealthAmount = "11"; // For setting exact health


        // Tool management is now handled by ToolService

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

#if MELONLOADER
        [System.Obsolete]
        public override void OnApplicationStart()
        {
            logger.Log("Silksong Simple Cheats Mod v1.0 - Ready!");
            logger.Log("Controls: INSERT/TILDE=Toggle GUI (Keybinds disabled by default - enable in GUI settings if desired)");

            logger.Log("Silksong Simple Cheats Mod initialized successfully!");
        }

        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();
            logger.Log("Simple Cheats Mod Initialized!");
            
            // Initialize GUI context
            InitializeGuiContext();
        }
#elif BEPINEX
        void Awake()
        {
            // Initialize BepInEx logger
            logger = new BepInExLoggerAdapter(Logger);
            
            logger.Log("Silksong Simple Cheats Mod v1.0 - Ready!");
            logger.Log("Controls: INSERT/TILDE=Toggle GUI (Keybinds disabled by default - enable in GUI settings if desired)");
            
            // Initialize GUI context
            InitializeGuiContext();
            
            logger.Log("Silksong Simple Cheats Mod initialized successfully!");
        }
#endif

#if MELONLOADER
        public override void OnUpdate()
#elif BEPINEX
        void Update()
#endif
        {
            // GUI Toggle (Insert or Tilde)
            if (inputHandler.GetKeyDown(KeyCode.Insert) || inputHandler.GetKeyDown(KeyCode.BackQuote))
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
                        logger.Log("UniverseLib initialization started...");
                    }
                    catch (Exception e)
                    {
                        logger.Log($"Failed to initialize UniverseLib: {e.Message}");
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

                logger.Log($"GUI {(showGUI ? "Enabled" : "Disabled")}");
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
                        logger.Log("Hero found! Health controls active.");
                        
                        // Initialize services with hero controller
                        if (healthService != null)
                        {
                            healthService.SetHeroController(heroController);
                        }
                        if (currencyService != null)
                        {
                            currencyService.SetHeroController(heroController);
                        }
                        if (heroInspectorService != null)
                        {
                            heroInspectorService.SetHeroController(heroController);
                        }
                        
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
                    if (inputHandler.GetKeyDown(key) && key != KeyCode.Insert && key != KeyCode.Escape)
                    {
                        currentKeybinds[keybindToSet] = key;
                        logger.Log($"Keybind for {keybindNames[keybindToSet]} set to {key}");
                        isSettingKeybind = false;
                        keybindToSet = -1;
                        break;
                    }
                }

                if (inputHandler.GetKeyDown(KeyCode.Escape))
                {
                    isSettingKeybind = false;
                    keybindToSet = -1;
                    logger.Log("Keybind setting cancelled");
                }
            }

            // Health modification controls using dynamic keybinds
            if (heroController != null && !isSettingKeybind)
            {
                if (inputHandler.GetKeyDown(currentKeybinds[0])) // Add Health
                {
                    if (int.TryParse(healthAmount, out int health))
                    {
                        if (healthService.AddHealth(health))
                            ShowToast($"Added {health} health!");
                    }
                }

                if (inputHandler.GetKeyDown(currentKeybinds[1])) // Set Health
                {
                    if (int.TryParse(setHealthAmount, out int targetHealth))
                    {
                        healthService.SetMaxHealthExact(targetHealth, ShowToast, ShowToast);
                    }
                }

                if (inputHandler.GetKeyDown(currentKeybinds[2])) // Refill Health
                {
                    if (healthService.RefillHealth())
                        ShowToast("Health refilled to max!");
                }

                if (inputHandler.GetKeyDown(currentKeybinds[3])) // One Hit Kill
                {
                    gameStateService.ToggleOneHitKill(ShowToast, ShowToast);
                }

                if (inputHandler.GetKeyDown(currentKeybinds[4])) // Add Money
                {
                    if (int.TryParse(moneyAmount, out int money))
                    {
                        if (currencyService.AddMoney(money))
                            ShowToast($"Added {money} money!");
                    }
                }

                if (inputHandler.GetKeyDown(currentKeybinds[5])) // Add Shards
                {
                    if (int.TryParse(shardAmount, out int shards))
                    {
                        if (currencyService.AddShards(shards))
                            ShowToast($"Added {shards} shards!");
                    }
                }

                if (inputHandler.GetKeyDown(currentKeybinds[6])) // Unlock Crests
                {
                    if (toolService.UnlockAllCrests())
                        ShowToast("All crests unlocked!");
                }

                if (inputHandler.GetKeyDown(currentKeybinds[7])) // Unlock Crest Skills
                {
                    if (toolService.UnlockAllTools())
                        ShowToast("All crest skills unlocked!");
                }

                if (inputHandler.GetKeyDown(currentKeybinds[8])) // Unlock Crest Tools
                {
                    if (toolService.UnlockAllItems())
                        ShowToast("All crest tools unlocked!");
                }

                if (inputHandler.GetKeyDown(currentKeybinds[9])) // Max Collectables
                {
                    int maxedCount = collectableService.MaxAllCollectables();
                    ShowToast($"Maxed {maxedCount} collectables!");
                }

                if (inputHandler.GetKeyDown(currentKeybinds[10])) // Auto Silk
                {
                    ToggleAutoSilkRefill();
                }
            }

            // Handle auto silk refill timer
            if (autoRefillSilk && heroController != null)
            {
                silkRefillTimer += timeProvider.DeltaTime;
                if (silkRefillTimer >= SILK_REFILL_INTERVAL)
                {
                    RefillSilk();
                    silkRefillTimer = 0f;
                }
            }


            // Enforce game speed setting if enabled (prevent resets from damage/pause events)
            gameStateService.EnforceGameSpeed();

            // Update toast system
            toastSystem.Update(timeProvider.DeltaTime);
        }

        private void ShowToast(string message)
        {
            toastSystem.ShowToast(message);
        }

        private void ShowConfirmation(string actionName, string message, System.Action action)
        {
            confirmationSystem.ShowConfirmation(actionName, message, action);
        }

        private void InitializeGuiContext()
        {
            // Initialize services
            healthService = new HealthService(logger);
            currencyService = new CurrencyService(logger);
            toolService = new ToolService(logger);
            collectableService = new CollectableService(logger);
            playerSkillService = new PlayerSkillService(logger);
            achievementService = new AchievementService(logger);
            gameStateService = new GameStateService(logger, timeProvider);
            alwaysActiveToolsService = new AlwaysActiveToolsService(logger);
            playerDataService = new PlayerDataService(logger);
            balanceService = new BalanceService(logger);
            heroInspectorService = new HeroInspectorService(logger);
            
            // Set up shared systems
            guiContext.ToastSystem = toastSystem;
            guiContext.ConfirmationSystem = confirmationSystem;
            guiContext.Logger = logger;
            
            // Set up services in context
            guiContext.HealthService = healthService;
            guiContext.CurrencyService = currencyService;
            guiContext.ToolService = toolService;
            guiContext.CollectableService = collectableService;
            guiContext.PlayerSkillService = playerSkillService;
            guiContext.AchievementService = achievementService;
            guiContext.GameStateService = gameStateService;
            guiContext.AlwaysActiveToolsService = alwaysActiveToolsService;
            guiContext.PlayerDataService = playerDataService;
            guiContext.HeroInspectorService = heroInspectorService;
            
            // Set up framework interfaces
            guiContext.InputHandler = inputHandler;
            guiContext.TimeProvider = timeProvider;
            
            // Set up balance system
            guiContext.BalanceService = balanceService;
            guiContext.GlobalMultiplierText = globalMultiplierText;
            guiContext.GlobalMultiplier = globalMultiplier;
            guiContext.ShowDetails = showDetails;
        }

        private void UpdateGuiContextForBalance()
        {
            // Sync current window rect
            guiContext.WindowRect = windowRect;
            
            // Sync balance system state to context
            guiContext.GlobalMultiplierText = globalMultiplierText;
            guiContext.GlobalMultiplier = globalMultiplier;
            guiContext.ShowDetails = showDetails;
        }

        private void SyncFromGuiContextForBalance()
        {
            // Sync balance system state back from context
            globalMultiplierText = guiContext.GlobalMultiplierText;
            globalMultiplier = guiContext.GlobalMultiplier;
            showDetails = guiContext.ShowDetails;
        }

        private void OnUniverseLibInitialized()
        {
            try
            {
                uiBase = UniversalUI.RegisterUI("SilkSongCheatGUI", null);
                universeLibInitialized = true;
                logger.Log("UniverseLib initialized successfully!");
                        }
                        catch (Exception e)
                        {
                logger.Log($"Failed to register UI with UniverseLib: {e.Message}");
            }
        }

        private void LogHandler(string message, UnityEngine.LogType type)
        {
            // Forward UniverseLib logs to the framework logger
            logger.Log($"[UniverseLib] {message}");
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



#if MELONLOADER
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            base.OnSceneWasLoaded(buildIndex, sceneName);
            logger.Log($"Scene: {sceneName}");
            heroController = null; // Reset hero controller for new scene

            // Reset damage scanning for new scene
            balanceService.ResetScanState();
        }
#elif BEPINEX
        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            logger.Log($"Scene: {scene.name}");
            heroController = null; // Reset hero controller for new scene

            // Reset damage scanning for new scene
            balanceService.ResetScanState();
        }
#endif

#if MELONLOADER
        public override void OnGUI()
#elif BEPINEX
        void OnGUI()
#endif
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
            confirmationSystem.RenderModal();
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
                        balanceService.ScanDamageFields();
                    }
                }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Tab content
            if (selectedTab == 0)
            {
                // Update context with current window rect
                guiContext.WindowRect = windowRect;
                
                // Use the extracted CheatsTabGUI
                cheatsTabGUI.Render(guiContext, ref scrollPosition, windowRect, 
                                  ref autoRefillSilk, ref infiniteAirJumpEnabled,
                                  ref isSettingKeybind, ref keybindToSet, 
                                  ref currentKeybinds, ShowToast);
            }
            else if (selectedTab == 1)
            {
                // Update context with current state
                UpdateGuiContextForBalance();
                balanceTabGUI.Render(guiContext);
                // Update our state from context
                SyncFromGuiContextForBalance();
            }
            else if (selectedTab == 2)
            {
                // Update context with current window rect
                guiContext.WindowRect = windowRect;
                
                // Use the extracted AchievementsTabGUI
                achievementsTabGUI.Render(guiContext, ref achievementScrollPosition, windowRect, ShowToast);
            }

            // Toast notification area (fixed at bottom)
            toastSystem.RenderToast();

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












        private void ToggleAutoSilkRefill()
        {
            autoRefillSilk = !autoRefillSilk;
            silkRefillTimer = 0f; // Reset timer

            if (autoRefillSilk)
            {
                logger.Log("Auto Silk Refill: ENABLED (every 2 seconds)");
            }
            else
            {
                logger.Log("Auto Silk Refill: DISABLED");
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
                        logger.Log("Silk refilled to max");
                    }
                }
                else
                {
                    logger.Log("RefillSilkToMax method not found");
                }
            }
            catch (Exception e)
            {
                logger.Log($"Error refilling silk: {e.Message}");
            }
        }






        private void InitializeStateFromPlayerData()
        {
            try
            {
                // Initialize infinite air jump state from PlayerData
                infiniteAirJumpEnabled = playerSkillService.IsInfiniteAirJumpUnlocked();
                
                if (infiniteAirJumpEnabled)
                {
                    logger.Log("Infinite Air Jump found enabled in PlayerData - syncing toggle state");
                }
                
                // Only initialize default Yellow Tools on first run, otherwise reapply current state
                if (!alwaysActiveToolsInitialized)
                {
                    alwaysActiveToolsService.InitializeDefaultTools(null, logger.Log);
                    alwaysActiveToolsInitialized = true;
                }
                else
                {
                    // Reapply current tool state after scene load
                    alwaysActiveToolsService.ReapplyCurrentTools(null, logger.Log);
                }
            }
            catch (Exception e)
            {
                logger.Log($"Error initializing state from PlayerData: {e.Message}");
            }
        }






    }
}
