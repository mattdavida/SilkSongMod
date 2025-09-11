using System;
using System.Reflection;
using System.Linq;
using SilkSong.Interfaces;

namespace SilkSong.Services
{
    /// <summary>
    /// Service for managing PlayerData operations and type lookups.
    /// Handles reflection-based access to game's PlayerData system.
    /// </summary>
    public class PlayerDataService
    {
        private readonly IModLogger logger;

        public PlayerDataService(IModLogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region PlayerData Management

        /// <summary>
        /// Sets a boolean value in PlayerData
        /// </summary>
        public bool SetPlayerDataBool(string boolName, bool value)
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
        /// Gets a boolean value from PlayerData
        /// </summary>
        public bool GetPlayerDataBool(string boolName, bool defaultValue = false)
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

        #endregion

        #region Type Discovery

        /// <summary>
        /// Finds a type by name in loaded assemblies
        /// </summary>
        public Type FindTypeInAssemblies(string typeName)
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
                logger.Log($"Error in FindTypeInAssemblies: {ex.Message}");
                return null;
            }
        }

        #endregion
    }
}
