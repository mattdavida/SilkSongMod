using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using SilkSong.Interfaces;

namespace SilkSong.Services
{
    /// <summary>
    /// Service for managing game state operations including invincibility, game speed control, and one-hit kill functionality.
    /// Handles all global game state modifications and cheats.
    /// </summary>
    public class GameStateService
    {
        private readonly IModLogger logger;
        private readonly ITimeProvider timeProvider;
        
        // Game state
        private bool invincibilityEnabled = false;
        private bool gameSpeedEnabled = false;
        private bool oneHitKillEnabled = false;
        
        // Game speed control
        private float currentGameSpeed = 2.0f;
        
        // Invincibility mode (0 = FullInvincible, 1 = PreventDeath)
        private int selectedInvincibilityMode = 0;
        
        // One Hit Kill original values storage
        private Dictionary<string, object> oneHitKillOriginalValues = new Dictionary<string, object>();

        public GameStateService(IModLogger logger, ITimeProvider timeProvider)
        {
            this.logger = logger;
            this.timeProvider = timeProvider;
        }

        #region Properties

        /// <summary>
        /// Gets whether invincibility is currently enabled.
        /// </summary>
        public bool IsInvincibilityEnabled => invincibilityEnabled;

        /// <summary>
        /// Gets whether game speed control is currently enabled.
        /// </summary>
        public bool IsGameSpeedEnabled => gameSpeedEnabled;

        /// <summary>
        /// Gets whether one-hit kill is currently enabled.
        /// </summary>
        public bool IsOneHitKillEnabled => oneHitKillEnabled;

        /// <summary>
        /// Gets the current game speed multiplier.
        /// </summary>
        public float CurrentGameSpeed => currentGameSpeed;

        /// <summary>
        /// Gets the selected invincibility mode (0 = FullInvincible, 1 = PreventDeath).
        /// </summary>
        public int SelectedInvincibilityMode => selectedInvincibilityMode;

        #endregion

        #region Invincibility Control

        /// <summary>
        /// Toggles invincibility on/off.
        /// </summary>
        /// <param name="onSuccess">Callback for success message</param>
        /// <param name="onError">Callback for error message</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool ToggleInvincibility(Action<string> onSuccess = null, Action<string> onError = null)
        {
            try
            {
                invincibilityEnabled = !invincibilityEnabled;

                if (invincibilityEnabled)
                {
                    return ApplyInvincibilityMode(onSuccess, onError);
                }
                else
                {
                    return DisableInvincibility(onSuccess, onError);
                }
            }
            catch (Exception e)
            {
                // Revert the toggle if error occurred
                invincibilityEnabled = !invincibilityEnabled;
                onError?.Invoke($"Error toggling Invincibility: {e.Message}");
                logger.Log($"Error in ToggleInvincibility: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sets the invincibility mode.
        /// </summary>
        /// <param name="mode">Mode (0 = FullInvincible, 1 = PreventDeath)</param>
        /// <param name="onSuccess">Callback for success message</param>
        /// <param name="onError">Callback for error message</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool SetInvincibilityMode(int mode, Action<string> onSuccess = null, Action<string> onError = null)
        {
            selectedInvincibilityMode = mode;
            
            // Apply mode change immediately if invincibility is enabled
            if (invincibilityEnabled)
            {
                return ApplyInvincibilityMode(onSuccess, onError);
            }
            
            return true;
        }

        /// <summary>
        /// Applies the selected invincibility mode.
        /// </summary>
        private bool ApplyInvincibilityMode(Action<string> onSuccess = null, Action<string> onError = null)
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
                            onSuccess?.Invoke($"Invincibility mode: {selectedMode}");
                            logger.Log($"Invincibility mode applied: {selectedMode}");
                            return true;
                        }
                    }
                }
                else
                {
                    onError?.Invoke("Invincibility unavailable - CheatManager not found");
                    return false;
                }
                
                onError?.Invoke("Failed to apply invincibility mode");
                return false;
            }
            catch (Exception e)
            {
                onError?.Invoke($"Error applying invincibility mode: {e.Message}");
                logger.Log($"Error in ApplyInvincibilityMode: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Disables invincibility.
        /// </summary>
        private bool DisableInvincibility(Action<string> onSuccess = null, Action<string> onError = null)
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
                            onSuccess?.Invoke("Invincibility disabled!");
                            logger.Log("Invincibility disabled");
                            return true;
                        }
                    }
                }
                else
                {
                    onError?.Invoke("Invincibility unavailable - CheatManager not found");
                    return false;
                }
                
                onError?.Invoke("Failed to disable invincibility");
                return false;
            }
            catch (Exception e)
            {
                onError?.Invoke($"Error disabling invincibility: {e.Message}");
                logger.Log($"Error in DisableInvincibility: {e.Message}");
                return false;
            }
        }

        #endregion

        #region Game Speed Control

        /// <summary>
        /// Toggles game speed control on/off.
        /// </summary>
        /// <param name="onSuccess">Callback for success message</param>
        /// <param name="onError">Callback for error message</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool ToggleGameSpeed(Action<string> onSuccess = null, Action<string> onError = null)
        {
            try
            {
                gameSpeedEnabled = !gameSpeedEnabled;

                if (gameSpeedEnabled)
                {
                    // Enable and apply the stored speed value
                    timeProvider.TimeScale = currentGameSpeed;
                    onSuccess?.Invoke($"Game Speed Control enabled (x{currentGameSpeed:F1})");
                    logger.Log($"Game Speed Control enabled - applied speed: x{currentGameSpeed:F1}");
                }
                else
                {
                    // Disable and reset to normal speed
                    timeProvider.TimeScale = 1.0f;
                    onSuccess?.Invoke("Game Speed Control disabled (100% normal speed)");
                    logger.Log("Game Speed Control disabled - reset to normal speed");
                }
                
                return true;
            }
            catch (Exception e)
            {
                // Revert the toggle if error occurred
                gameSpeedEnabled = !gameSpeedEnabled;
                onError?.Invoke($"Error toggling Game Speed Control: {e.Message}");
                logger.Log($"Error in ToggleGameSpeed: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sets the game speed to a specific multiplier and enables speed control.
        /// </summary>
        /// <param name="speed">Speed multiplier (minimum 0.0)</param>
        /// <param name="onSuccess">Callback for success message</param>
        /// <param name="onError">Callback for error message</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool SetGameSpeed(float speed, Action<string> onSuccess = null, Action<string> onError = null)
        {
            try
            {
                // Only prevent negative speeds (0 minimum, no maximum limit)
                currentGameSpeed = Mathf.Max(speed, 0f);
                
                // Auto-enable and apply speed when setting
                gameSpeedEnabled = true;
                timeProvider.TimeScale = currentGameSpeed;
                
                onSuccess?.Invoke($"Game speed applied: x{currentGameSpeed:F1}");
                logger.Log($"Game speed applied: x{currentGameSpeed:F1}");
                return true;
            }
            catch (Exception e)
            {
                onError?.Invoke($"Error setting game speed: {e.Message}");
                logger.Log($"Error in SetGameSpeed: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the current game speed text representation.
        /// </summary>
        public string GetGameSpeedText()
        {
            return currentGameSpeed.ToString("F1");
        }

        /// <summary>
        /// Updates the game speed text and applies it if valid.
        /// </summary>
        /// <param name="speedText">Speed text to parse</param>
        /// <param name="onSuccess">Callback for success message</param>
        /// <param name="onError">Callback for error message</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool UpdateGameSpeedFromText(string speedText, Action<string> onSuccess = null, Action<string> onError = null)
        {
            if (float.TryParse(speedText, out float speedMultiplier))
            {
                return SetGameSpeed(speedMultiplier, onSuccess, onError);
            }
            else
            {
                onError?.Invoke("Invalid speed value - please enter a number");
                return false;
            }
        }

        /// <summary>
        /// Enforces game speed setting if enabled (prevents resets from damage/pause events).
        /// Should be called in Update loop.
        /// </summary>
        public void EnforceGameSpeed()
        {
            if (gameSpeedEnabled && timeProvider.TimeScale != currentGameSpeed)
            {
                logger.Log($"Game speed was reset to {timeProvider.TimeScale}, restoring to {currentGameSpeed}");
                timeProvider.TimeScale = currentGameSpeed;
            }
        }

        #endregion

        #region One Hit Kill Control

        /// <summary>
        /// Toggles one-hit kill mode on/off.
        /// </summary>
        /// <param name="onSuccess">Callback for success message</param>
        /// <param name="onError">Callback for error message</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool ToggleOneHitKill(Action<string> onSuccess = null, Action<string> onError = null)
        {
            try
            {
                if (oneHitKillEnabled)
                {
                    return DisableOneHitKill(onSuccess, onError);
                }
                else
                {
                    return EnableOneHitKill(onSuccess, onError);
                }
            }
            catch (Exception e)
            {
                onError?.Invoke($"Error toggling one hit kill: {e.Message}");
                logger.Log($"Error toggling one hit kill: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Enables one-hit kill mode.
        /// </summary>
        private bool EnableOneHitKill(Action<string> onSuccess = null, Action<string> onError = null)
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
                            object instaKillState = Enum.Parse(nailDamageEnumType, "InstaKill");
                            nailDamageProp.SetValue(null, instaKillState);
                            oneHitKillEnabled = true;
                            onSuccess?.Invoke("One Hit Kill enabled!");
                            logger.Log("One Hit Kill ENABLED using CheatManager.NailDamage = InstaKill");
                            return true;
                        }
                    }
                }

                // Fallback to our existing method
                logger.Log("=== ENABLING ONE HIT KILL MODE (Fallback Method) ===");

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
                                        logger.Log($"Set {type.Name}.{field.Name} = 100.0f (was {originalValue})");
                                        modifiedCount++;
                                    }
                                    else if (field.FieldType == typeof(int))
                                    {
                                        field.SetValue(behaviour, 100);
                                        logger.Log($"Set {type.Name}.{field.Name} = 100 (was {originalValue})");
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
                onSuccess?.Invoke("One Hit Kill enabled (fallback method)!");
                logger.Log($"One Hit Kill ENABLED (Fallback): Modified {modifiedCount} DamageEnemies values");
                return true;
            }
            catch (Exception e)
            {
                onError?.Invoke($"Error enabling one hit kill: {e.Message}");
                logger.Log($"Error enabling one hit kill: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Disables one-hit kill mode.
        /// </summary>
        private bool DisableOneHitKill(Action<string> onSuccess = null, Action<string> onError = null)
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
                            onSuccess?.Invoke("One Hit Kill disabled!");
                            logger.Log("One Hit Kill DISABLED using CheatManager.NailDamage = Normal");
                            return true;
                        }
                    }
                }

                // Fallback to our existing restore method
                logger.Log("=== DISABLING ONE HIT KILL MODE (Fallback Method) ===");

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
                                        logger.Log($"Restored {type.Name}.{field.Name} = {originalValue}");
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
                onSuccess?.Invoke("One Hit Kill disabled!");
                logger.Log($"One Hit Kill DISABLED (Fallback): Restored {restoredCount} DamageEnemies values");
                return true;
            }
            catch (Exception e)
            {
                onError?.Invoke($"Error disabling one hit kill: {e.Message}");
                logger.Log($"Error disabling one hit kill: {e.Message}");
                return false;
            }
        }

        #endregion

        #region Helper Methods

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
