using UnityEngine;
using SilkSong.Core;
using SilkSong.Interfaces;
using SilkSong.Services;
using System.Collections.Generic;
using System.Reflection;

namespace SilkSong.UserInterface
{
    /// <summary>
    /// Shared context object passed to GUI tabs.
    /// Contains all shared dependencies and state needed by multiple tabs.
    /// </summary>
    public class GuiContext
    {
        // Shared systems
        public ToastSystem ToastSystem { get; set; }
        public ConfirmationSystem ConfirmationSystem { get; set; }
        public IModLogger Logger { get; set; }
        
        // Services (for CheatsTab and other GUI components)
        public HealthService HealthService { get; set; }
        public CurrencyService CurrencyService { get; set; }
        public ToolService ToolService { get; set; }
        public CollectableService CollectableService { get; set; }
        public PlayerSkillService PlayerSkillService { get; set; }
        public AchievementService AchievementService { get; set; }
        public GameStateService GameStateService { get; set; }
        public AlwaysActiveToolsService AlwaysActiveToolsService { get; set; }
        public PlayerDataService PlayerDataService { get; set; }
        public BalanceService BalanceService { get; set; }
        
        // Framework interfaces (for CheatsTab)
        public IInputHandler InputHandler { get; set; }
        public ITimeProvider TimeProvider { get; set; }
        
        // GUI state
        public Rect WindowRect { get; set; }
        
        // Balance system state (for Balance tab)
        // Balance system state (simplified - most moved to BalanceService)
        public string GlobalMultiplierText { get; set; }
        public float GlobalMultiplier { get; set; }
        public bool ShowDetails { get; set; }
    }
}
