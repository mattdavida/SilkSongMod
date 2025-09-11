using System;
using System.Reflection;
using UnityEngine;
using SilkSong.Interfaces;

namespace SilkSong.Services
{
    /// <summary>
    /// Service for managing player health operations.
    /// Handles adding health, setting max health, and health refills.
    /// </summary>
    public class HealthService
    {
        private readonly IModLogger logger;
        private Component heroController;

        public HealthService(IModLogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Sets the hero controller reference. Must be called when hero is available.
        /// </summary>
        /// <param name="heroController">The hero controller component</param>
        public void SetHeroController(Component heroController)
        {
            this.heroController = heroController;
        }

        /// <summary>
        /// Adds the specified amount of health to the player.
        /// </summary>
        /// <param name="amount">Amount of health to add (can be negative to remove)</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool AddHealth(int amount)
        {
            if (heroController == null)
            {
                logger.Log("Cannot add health: Hero controller not available");
                return false;
            }

            try
            {
                Type type = heroController.GetType();
                MethodInfo addHealthMethod = type.GetMethod("AddHealth");

                if (addHealthMethod != null)
                {
                    addHealthMethod.Invoke(heroController, new object[] { amount });
                    logger.Log($"Added {amount} health");
                    return true;
                }
                else
                {
                    logger.Log("AddHealth method not found");
                    return false;
                }
            }
            catch (Exception e)
            {
                logger.Log($"Error adding health: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sets the exact maximum health for the player.
        /// </summary>
        /// <param name="targetMaxHealth">Target max health value (1-9999)</param>
        /// <param name="onSuccess">Callback called on success with success message</param>
        /// <param name="onError">Callback called on error with error message</param>
        /// <returns>True if operation started successfully, false otherwise</returns>
        public bool SetMaxHealthExact(int targetMaxHealth, Action<string> onSuccess = null, Action<string> onError = null)
        {
            if (heroController == null)
            {
                string error = "Cannot set max health: Hero controller not available";
                logger.Log(error);
                onError?.Invoke(error);
                return false;
            }

            try
            {
                // Validate minimum health
                if (targetMaxHealth < 1)
                {
                    string warning = "Warning: Minimum health is 1!";
                    logger.Log("Attempted to set max health below 1 - setting to 1 instead");
                    onError?.Invoke(warning);
                    targetMaxHealth = 1;
                }

                // Validate maximum to prevent crashes (reasonable upper limit)
                if (targetMaxHealth > 9999)
                {
                    string warning = "Warning: Max health capped at 9999!";
                    logger.Log("Attempted to set max health above 9999 - capping at 9999");
                    onError?.Invoke(warning);
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

                        logger.Log($"Current max health: {currentMaxHealth}, Target: {targetMaxHealth}, Difference: {difference}");

                        // Use AddToMaxHealth to reach the target
                        MethodInfo addMaxHealthMethod = heroType.GetMethod("AddToMaxHealth");
                        if (addMaxHealthMethod != null)
                        {
                            addMaxHealthMethod.Invoke(heroController, new object[] { difference });
                            logger.Log($"Set max health to {targetMaxHealth}");

                            // Automatically refill health to new max
                            RefillHealthAfterMaxChange();

                            string successMsg = $"Max health set to {targetMaxHealth} - Save & reload to see UI update!";
                            onSuccess?.Invoke(successMsg);
                            return true;
                        }
                        else
                        {
                            string error = "AddToMaxHealth method not found";
                            logger.Log(error);
                            onError?.Invoke(error);
                            return false;
                        }
                    }
                    else
                    {
                        string error = "CurrentMaxHealth property not found";
                        logger.Log(error);
                        onError?.Invoke(error);
                        return false;
                    }
                }
                else
                {
                    string error = "playerData field not found";
                    logger.Log(error);
                    onError?.Invoke(error);
                    return false;
                }
            }
            catch (Exception e)
            {
                string error = $"Error setting max health: {e.Message}";
                logger.Log($"Error setting exact max health: {e.Message}");
                onError?.Invoke(error);
                return false;
            }
        }

        /// <summary>
        /// Refills player health to maximum.
        /// </summary>
        /// <returns>True if successful, false otherwise</returns>
        public bool RefillHealth()
        {
            if (heroController == null)
            {
                logger.Log("Cannot refill health: Hero controller not available");
                return false;
            }

            try
            {
                Type type = heroController.GetType();
                MethodInfo refillMethod = type.GetMethod("RefillHealthToMax");

                if (refillMethod != null)
                {
                    refillMethod.Invoke(heroController, null);
                    logger.Log("Health refilled to max");
                    return true;
                }
                else
                {
                    logger.Log("RefillHealthToMax method not found");
                    return false;
                }
            }
            catch (Exception e)
            {
                logger.Log($"Error refilling health: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Internal method to refill health after max health changes.
        /// </summary>
        private void RefillHealthAfterMaxChange()
        {
            if (heroController == null) return;

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
                                logger.Log($"Refilled {healthToAdd} health to reach new max of {currentMaxHealth}");
                            }
                        }
                        else
                        {
                            logger.Log("Health already at or above max");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.Log($"Error refilling health after max change: {e.Message}");
            }
        }

        /// <summary>
        /// Checks if the health service is ready to perform operations.
        /// </summary>
        /// <returns>True if hero controller is available, false otherwise</returns>
        public bool IsReady()
        {
            return heroController != null;
        }
    }
}
