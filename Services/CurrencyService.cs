using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using SilkSong.Interfaces;

namespace SilkSong.Services
{
    /// <summary>
    /// Service for managing player currency operations.
    /// Handles adding money, shards, and other currency types.
    /// </summary>
    public class CurrencyService
    {
        private readonly IModLogger logger;
        private Component heroController;

        public CurrencyService(IModLogger logger)
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
        /// Adds the specified amount of money to the player.
        /// </summary>
        /// <param name="amount">Amount of money to add (can be negative to remove)</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool AddMoney(int amount)
        {
            return AddCurrency(amount, "Money");
        }

        /// <summary>
        /// Adds the specified amount of shards to the player.
        /// </summary>
        /// <param name="amount">Amount of shards to add (can be negative to remove)</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool AddShards(int amount)
        {
            return AddCurrency(amount, "Shard");
        }

        /// <summary>
        /// Generic method to add any currency type.
        /// </summary>
        /// <param name="amount">Amount to add</param>
        /// <param name="currencyTypeName">Name of the currency type enum value (e.g., "Money", "Shard")</param>
        /// <returns>True if successful, false otherwise</returns>
        private bool AddCurrency(int amount, string currencyTypeName)
        {
            if (heroController == null)
            {
                logger.Log($"Cannot add {currencyTypeName.ToLower()}: Hero controller not available");
                return false;
            }

            try
            {
                Type heroControllerType = heroController.GetType();

                // Find CurrencyType enum from the same assembly as HeroController
                Assembly gameAssembly = heroControllerType.Assembly;
                Type currencyType = gameAssembly.GetTypes().FirstOrDefault(t => t.Name == "CurrencyType");

                if (currencyType != null)
                {
                    object currencyEnum = Enum.Parse(currencyType, currencyTypeName);

                    // Explicitly specify BindingFlags for public instance methods
                    MethodInfo addCurrencyMethod = heroControllerType.GetMethod("AddCurrency", 
                        BindingFlags.Public | BindingFlags.Instance, 
                        null, 
                        new Type[] { typeof(int), currencyType, typeof(bool) }, 
                        null);

                    if (addCurrencyMethod != null)
                    {
                        addCurrencyMethod.Invoke(heroController, new object[] { amount, currencyEnum, false });
                        logger.Log($"Added {amount} {currencyTypeName.ToLower()}");
                        return true;
                    }
                    else
                    {
                        logger.Log("HeroController.AddCurrency method not found");
                        return false;
                    }
                }
                else
                {
                    logger.Log("CurrencyType enum not found");
                    return false;
                }
            }
            catch (Exception e)
            {
                logger.Log($"Error adding {currencyTypeName.ToLower()}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if the currency service is ready to perform operations.
        /// </summary>
        /// <returns>True if hero controller is available, false otherwise</returns>
        public bool IsReady()
        {
            return heroController != null;
        }
    }
}
