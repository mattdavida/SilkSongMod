using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using SilkSong.Interfaces;

namespace SilkSong.Services
{
    /// <summary>
    /// Service for managing player skill operations including toggles and state checking.
    /// Handles all player abilities like double jump, dash, wall jump, glide, charge attack, etc.
    /// </summary>
    public class PlayerSkillService
    {
        private readonly IModLogger logger;

        public PlayerSkillService(IModLogger logger)
        {
            this.logger = logger;
        }

        #region Skill State Checking Methods

        /// <summary>
        /// Checks if Double Jump is unlocked.
        /// </summary>
        public bool IsDoubleJumpUnlocked()
        {
            return GetPlayerDataBool("hasDoubleJump", false);
        }

        /// <summary>
        /// Checks if Dash is unlocked.
        /// </summary>
        public bool IsDashUnlocked()
        {
            return GetPlayerDataBool("hasDash", false);
        }

        /// <summary>
        /// Checks if Wall Jump is unlocked.
        /// </summary>
        public bool IsWallJumpUnlocked()
        {
            return GetPlayerDataBool("hasWalljump", false);
        }

        /// <summary>
        /// Checks if Glide is unlocked (requires both PlayerData and Config).
        /// </summary>
        public bool IsGlideUnlocked()
        {
            // Glide requires BOTH playerData.hasBrolly AND Config.canBrolly
            bool playerDataHas = GetPlayerDataBool("hasBrolly", false);
            bool configCan = GetHeroConfigBool("canBrolly", false);
            return playerDataHas && configCan;
        }

        /// <summary>
        /// Checks if Charge Attack is unlocked (requires both PlayerData and Config).
        /// </summary>
        public bool IsChargeAttackUnlocked()
        {
            // Charge Attack requires BOTH playerData.hasChargeSlash AND Config.canNailCharge
            bool playerDataHas = GetPlayerDataBool("hasChargeSlash", false);
            bool configCan = GetHeroConfigBool("canNailCharge", false);
            return playerDataHas && configCan;
        }

        /// <summary>
        /// Checks if Needolin is unlocked (requires both PlayerData and Config).
        /// </summary>
        public bool IsNeedolinUnlocked()
        {
            // Needolin requires BOTH playerData.hasNeedolin AND Config.canPlayNeedolin
            bool playerDataHas = GetPlayerDataBool("hasNeedolin", false);
            bool configCan = GetHeroConfigBool("canPlayNeedolin", false);
            return playerDataHas && configCan;
        }

        /// <summary>
        /// Checks if Grappling Hook is unlocked (requires both PlayerData and Config).
        /// </summary>
        public bool IsGrapplingHookUnlocked()
        {
            // Grappling Hook requires BOTH playerData.hasHarpoonDash AND Config.canHarpoonDash
            bool playerDataHas = GetPlayerDataBool("hasHarpoonDash", false);
            bool configCan = GetHeroConfigBool("canHarpoonDash", false);
            return playerDataHas && configCan;
        }

        /// <summary>
        /// Checks if Super Jump is unlocked (requires both skill flags).
        /// </summary>
        public bool IsSuperJumpUnlocked()
        {
            // Super Jump requires both hasSuperJump AND hasHarpoonDash
            bool superJumpSet = GetPlayerDataBool("hasSuperJump", false);
            bool harpoonDashSet = GetPlayerDataBool("hasHarpoonDash", false);
            return superJumpSet && harpoonDashSet;
        }

        /// <summary>
        /// Checks if Infinite Air Jump is unlocked.
        /// </summary>
        public bool IsInfiniteAirJumpUnlocked()
        {
            return GetPlayerDataBool("infiniteAirJump", false);
        }

        #endregion

        #region Skill Toggle Methods

        /// <summary>
        /// Toggles Double Jump skill.
        /// </summary>
        /// <param name="onSuccess">Callback for success message</param>
        /// <param name="onError">Callback for error message</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool ToggleDoubleJump(Action<string> onSuccess = null, Action<string> onError = null)
        {
            bool currentState = IsDoubleJumpUnlocked();
            bool newState = !currentState;

            if (SetPlayerDataBool("hasDoubleJump", newState))
            {
                onSuccess?.Invoke($"Double Jump {(newState ? "unlocked" : "locked")}!");
                logger.Log($"Successfully {(newState ? "unlocked" : "locked")} double jump (playerData.hasDoubleJump = {newState})");
                return true;
            }
            
            onError?.Invoke("Failed to toggle Double Jump");
            return false;
        }

        /// <summary>
        /// Toggles Dash skill.
        /// </summary>
        /// <param name="onSuccess">Callback for success message</param>
        /// <param name="onError">Callback for error message</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool ToggleDash(Action<string> onSuccess = null, Action<string> onError = null)
        {
            bool currentState = IsDashUnlocked();
            bool newState = !currentState;

            if (SetPlayerDataBool("hasDash", newState))
            {
                onSuccess?.Invoke($"Dash {(newState ? "unlocked" : "locked")}!");
                logger.Log($"Successfully {(newState ? "unlocked" : "locked")} dash (playerData.hasDash = {newState})");
                return true;
            }
            
            onError?.Invoke("Failed to toggle Dash");
            return false;
        }

        /// <summary>
        /// Toggles Wall Jump skill.
        /// </summary>
        /// <param name="onSuccess">Callback for success message</param>
        /// <param name="onError">Callback for error message</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool ToggleWallJump(Action<string> onSuccess = null, Action<string> onError = null)
        {
            bool currentState = IsWallJumpUnlocked();
            bool newState = !currentState;

            if (SetPlayerDataBool("hasWalljump", newState))
            {
                onSuccess?.Invoke($"Wall Jump {(newState ? "unlocked" : "locked")}!");
                logger.Log($"Successfully {(newState ? "unlocked" : "locked")} wall jump (playerData.hasWalljump = {newState})");
                return true;
            }
            
            onError?.Invoke("Failed to toggle Wall Jump");
            return false;
        }

        /// <summary>
        /// Toggles Glide skill (requires both PlayerData and Config updates).
        /// </summary>
        /// <param name="onSuccess">Callback for success message</param>
        /// <param name="onError">Callback for error message</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool ToggleGlide(Action<string> onSuccess = null, Action<string> onError = null)
        {
            bool currentState = IsGlideUnlocked();
            bool newState = !currentState;

            // Glide requires BOTH playerData.hasBrolly AND Config.canBrolly
            bool playerDataSet = SetPlayerDataBool("hasBrolly", newState);
            bool configSet = SetHeroConfigBool("canBrolly", newState);

            if (playerDataSet && configSet)
            {
                onSuccess?.Invoke($"Glide/Drifter's Cloak {(newState ? "unlocked" : "locked")}! ☂️");
                logger.Log($"Successfully {(newState ? "unlocked" : "locked")} glide (playerData.hasBrolly = {newState}, Config.canBrolly = {newState})");
                return true;
            }
            
            onError?.Invoke("Failed to toggle Glide");
            return false;
        }

        /// <summary>
        /// Toggles Charge Attack skill (requires both PlayerData and Config updates).
        /// </summary>
        /// <param name="onSuccess">Callback for success message</param>
        /// <param name="onError">Callback for error message</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool ToggleChargeAttack(Action<string> onSuccess = null, Action<string> onError = null)
        {
            bool currentState = IsChargeAttackUnlocked();
            bool newState = !currentState;

            bool playerDataSet = SetPlayerDataBool("hasChargeSlash", newState);
            bool configSet = SetHeroConfigBool("canNailCharge", newState);

            if (playerDataSet && configSet)
            {
                onSuccess?.Invoke($"Charge Attack {(newState ? "unlocked" : "locked")}!");
                logger.Log($"Successfully {(newState ? "unlocked" : "locked")} charge attack (playerData.hasChargeSlash = {newState}, Config.canNailCharge = {newState})");
                return true;
            }
            
            onError?.Invoke("Failed to toggle Charge Attack");
            return false;
        }

        /// <summary>
        /// Toggles Needolin skill (requires both PlayerData and Config updates).
        /// </summary>
        /// <param name="onSuccess">Callback for success message</param>
        /// <param name="onError">Callback for error message</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool ToggleNeedolin(Action<string> onSuccess = null, Action<string> onError = null)
        {
            bool currentState = IsNeedolinUnlocked();
            bool newState = !currentState;

            // Needolin requires BOTH playerData.hasNeedolin AND Config.canPlayNeedolin
            bool playerDataSet = SetPlayerDataBool("hasNeedolin", newState);
            bool configSet = SetHeroConfigBool("canPlayNeedolin", newState);

            if (playerDataSet && configSet)
            {
                onSuccess?.Invoke($"Needolin {(newState ? "unlocked" : "locked")}! 🎵");
                logger.Log($"Successfully {(newState ? "unlocked" : "locked")} needolin (playerData.hasNeedolin = {newState}, Config.canPlayNeedolin = {newState})");
                return true;
            }
            
            onError?.Invoke("Failed to toggle Needolin");
            return false;
        }

        /// <summary>
        /// Toggles Grappling Hook skill (requires both PlayerData and Config updates).
        /// </summary>
        /// <param name="onSuccess">Callback for success message</param>
        /// <param name="onError">Callback for error message</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool ToggleGrapplingHook(Action<string> onSuccess = null, Action<string> onError = null)
        {
            bool currentState = IsGrapplingHookUnlocked();
            bool newState = !currentState;

            // Grappling Hook requires BOTH playerData.hasHarpoonDash AND Config.canHarpoonDash
            bool playerDataSet = SetPlayerDataBool("hasHarpoonDash", newState);
            bool configSet = SetHeroConfigBool("canHarpoonDash", newState);

            if (playerDataSet && configSet)
            {
                onSuccess?.Invoke($"Grappling Hook {(newState ? "unlocked" : "locked")}! 🎣 (Clawline Ancestral Art)");
                logger.Log($"Successfully {(newState ? "unlocked" : "locked")} grappling hook (playerData.hasHarpoonDash = {newState}, Config.canHarpoonDash = {newState})");
                return true;
            }
            
            onError?.Invoke("Failed to toggle Grappling Hook");
            return false;
        }

        /// <summary>
        /// Toggles Super Jump skill (requires both skill flags).
        /// </summary>
        /// <param name="onSuccess">Callback for success message</param>
        /// <param name="onError">Callback for error message</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool ToggleSuperJump(Action<string> onSuccess = null, Action<string> onError = null)
        {
            bool currentState = IsSuperJumpUnlocked();
            bool newState = !currentState;

            // Super Jump requires both hasSuperJump AND hasHarpoonDash
            bool superJumpSet = SetPlayerDataBool("hasSuperJump", newState);
            bool harpoonDashSet = SetPlayerDataBool("hasHarpoonDash", newState);

            if (superJumpSet && harpoonDashSet)
            {
                onSuccess?.Invoke($"Super Jump {(newState ? "unlocked" : "locked")}! (includes Harpoon Dash)");
                logger.Log($"Successfully {(newState ? "unlocked" : "locked")} super jump (playerData.hasSuperJump = {newState}, playerData.hasHarpoonDash = {newState})");
                return true;
            }
            
            onError?.Invoke("Failed to toggle Super Jump");
            return false;
        }

        /// <summary>
        /// Toggles Infinite Air Jump skill.
        /// </summary>
        /// <param name="onSuccess">Callback for success message</param>
        /// <param name="onError">Callback for error message</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool ToggleInfiniteAirJump(Action<string> onSuccess = null, Action<string> onError = null)
        {
            bool currentState = IsInfiniteAirJumpUnlocked();
            bool newState = !currentState;

            if (SetPlayerDataBool("infiniteAirJump", newState))
            {
                string status = newState ? "enabled" : "disabled";
                onSuccess?.Invoke($"Infinite Air Jump {status}!");
                logger.Log($"Successfully {status} infinite air jump (playerData.infiniteAirJump = {newState})");
                return true;
            }
            
            onError?.Invoke("Failed to toggle Infinite Air Jump - enter game first");
            return false;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Sets a boolean value in PlayerData.
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
        /// Gets a boolean value from PlayerData.
        /// </summary>
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

        /// <summary>
        /// Sets a boolean value in HeroController Config.
        /// </summary>
        private bool SetHeroConfigBool(string fieldName, bool value)
        {
            try
            {
                // Get HeroController instance
                var heroController = GameObject.FindFirstObjectByType<HeroController>();
                if (heroController == null)
                {
                    logger.Log($"HeroController not found - cannot set {fieldName}");
                    return false;
                }

                // Get the Config property
                var configProperty = heroController.GetType().GetProperty("Config");
                if (configProperty == null)
                {
                    logger.Log("Config property not found on HeroController");
                    return false;
                }

                var config = configProperty.GetValue(heroController);
                if (config == null)
                {
                    logger.Log("Config object is null");
                    return false;
                }

                // Get the field using reflection (it's private, so we need NonPublic)
                var field = config.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null)
                {
                    logger.Log($"Field '{fieldName}' not found in HeroControllerConfig");
                    return false;
                }

                // Set the field value
                field.SetValue(config, value);
                logger.Log($"Successfully set HeroControllerConfig.{fieldName} = {value}");
                return true;
            }
            catch (Exception e)
            {
                logger.Log($"Error setting HeroControllerConfig.{fieldName}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets a boolean value from HeroController Config.
        /// </summary>
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
