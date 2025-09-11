using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using SilkSong.Interfaces;

namespace SilkSong.Services
{
    /// <summary>
    /// Service for managing achievement operations including scanning, filtering, and awarding.
    /// Handles all Steam/platform achievement functionality.
    /// </summary>
    public class AchievementService
    {
        private readonly IModLogger logger;
        
        // Achievement data
        private bool achievementsScanned = false;
        private List<string> availableAchievements = new List<string>();
        private string[] achievementNames = new string[0];
        private Dictionary<string, string> achievementDisplayToKey = new Dictionary<string, string>();

        public AchievementService(IModLogger logger)
        {
            this.logger = logger;
        }

        #region Properties

        /// <summary>
        /// Gets whether achievements have been scanned.
        /// </summary>
        public bool AreAchievementsScanned => achievementsScanned;

        /// <summary>
        /// Gets the count of available achievements.
        /// </summary>
        public int AchievementCount => availableAchievements.Count;

        /// <summary>
        /// Gets the array of achievement display names.
        /// </summary>
        public string[] GetAchievementNames() => achievementNames.ToArray();

        #endregion

        #region Core Achievement Operations

        /// <summary>
        /// Scans for available achievements in the game.
        /// </summary>
        /// <param name="onSuccess">Callback for success message</param>
        /// <param name="onError">Callback for error message</param>
        /// <returns>True if scanning was successful, false otherwise</returns>
        public bool ScanAchievements(Action<string> onSuccess = null, Action<string> onError = null)
        {
            try
            {
                // Find AchievementHandler components
                Type achievementHandlerType = FindTypeInAssemblies("AchievementHandler");
                if (achievementHandlerType == null)
                {
                    onError?.Invoke("AchievementHandler type not found");
                    return false;
                }

                UnityEngine.Object[] achievementHandlers = Resources.FindObjectsOfTypeAll(achievementHandlerType);
                if (achievementHandlers.Length == 0)
                {
                    onError?.Invoke("No AchievementHandler components found");
                    return false;
                }

                object achievementHandler = achievementHandlers[0];

                // Get achievementsList field from AchievementHandler
                FieldInfo achievementsListField = achievementHandlerType.GetField("achievementsList", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                if (achievementsListField == null)
                {
                    onError?.Invoke("achievementsList field not found on AchievementHandler");
                    return false;
                }

                object achievementsList = achievementsListField.GetValue(achievementHandler);
                if (achievementsList == null)
                {
                    onError?.Invoke("achievementsList field is null");
                    return false;
                }

                // Get the achievements field from AchievementsList
                Type achievementsListType = achievementsList.GetType();
                FieldInfo achievementsField = achievementsListType.GetField("achievements", BindingFlags.NonPublic | BindingFlags.Instance);
                object achievements = null;

                if (achievementsField != null)
                {
                    achievements = achievementsField.GetValue(achievementsList);
                }
                else
                {
                    // Fallback to public property
                    PropertyInfo achievementsProperty = achievementsListType.GetProperty("Achievements", BindingFlags.Public | BindingFlags.Instance);
                    if (achievementsProperty != null)
                    {
                        achievements = achievementsProperty.GetValue(achievementsList);
                    }
                }

                if (achievements == null)
                {
                    onError?.Invoke("Could not access achievements field or property");
                    return false;
                }

                // Clear existing data
                availableAchievements.Clear();
                achievementDisplayToKey.Clear();

                // Enumerate achievements using IEnumerable
                System.Collections.IEnumerable achievementEnumerable = achievements as System.Collections.IEnumerable;
                if (achievementEnumerable != null)
                {
                    foreach (object achievement in achievementEnumerable)
                    {
                        if (achievement == null) continue;

                        try
                        {
                            Type achievementType = achievement.GetType();
                            string platformKey = null;

                            // Try to get PlatformKey field (public field according to decompiled source)
                            FieldInfo platformKeyField = achievementType.GetField("PlatformKey", BindingFlags.Public | BindingFlags.Instance);
                            if (platformKeyField != null)
                            {
                                platformKey = platformKeyField.GetValue(achievement) as string;
                            }

                            // Fallback: try property or other field names
                            if (string.IsNullOrEmpty(platformKey))
                            {
                                PropertyInfo platformKeyProperty = achievementType.GetProperty("PlatformKey", BindingFlags.Public | BindingFlags.Instance);
                                if (platformKeyProperty != null)
                                {
                                    platformKey = platformKeyProperty.GetValue(achievement) as string;
                                }
                            }

                            if (!string.IsNullOrEmpty(platformKey))
                            {
                                string displayName = platformKey;

                                // Try to get a display name
                                PropertyInfo displayNameProperty = achievementType.GetProperty("DisplayName", BindingFlags.Public | BindingFlags.Instance);
                                if (displayNameProperty != null)
                                {
                                    string displayNameValue = displayNameProperty.GetValue(achievement) as string;
                                    if (!string.IsNullOrEmpty(displayNameValue))
                                    {
                                        displayName = displayNameValue;
                                    }
                                }

                                availableAchievements.Add(platformKey);
                                achievementDisplayToKey[displayName] = platformKey;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Log($"Error processing achievement: {ex.Message}");
                        }
                    }
                }

                achievementsScanned = true;
                achievementNames = achievementDisplayToKey.Keys.ToArray();

                onSuccess?.Invoke($"Found {availableAchievements.Count} achievements");
                logger.Log($"Achievement scan completed: {availableAchievements.Count} achievements found");
                return true;
            }
            catch (Exception ex)
            {
                onError?.Invoke($"Error scanning achievements: {ex.Message}");
                logger.Log($"Error in ScanAchievements: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Filters achievements based on search text.
        /// </summary>
        /// <param name="filter">Search filter text</param>
        /// <returns>Array of filtered achievement names</returns>
        public string[] FilterAchievements(string filter)
        {
            if (!achievementsScanned)
            {
                return new string[0];
            }

            if (string.IsNullOrEmpty(filter))
            {
                return achievementNames;
            }
            else
            {
                return achievementNames
                    .Where(name => name.ToLower().Contains(filter.ToLower()))
                    .ToArray();
            }
        }

        /// <summary>
        /// Awards a specific achievement by display name.
        /// </summary>
        /// <param name="displayName">Display name of the achievement</param>
        /// <param name="onSuccess">Callback for success message</param>
        /// <param name="onError">Callback for error message</param>
        /// <returns>True if awarding was successful, false otherwise</returns>
        public bool AwardAchievementByDisplayName(string displayName, Action<string> onSuccess = null, Action<string> onError = null)
        {
            try
            {
                if (!achievementsScanned || achievementNames.Length == 0)
                {
                    onError?.Invoke("No achievements available");
                    return false;
                }

                if (!achievementDisplayToKey.TryGetValue(displayName, out string platformKey))
                {
                    onError?.Invoke("Achievement key not found");
                    return false;
                }

                return AwardAchievementByPlatformKey(platformKey, onSuccess, onError);
            }
            catch (Exception ex)
            {
                onError?.Invoke($"Error awarding achievement: {ex.Message}");
                logger.Log($"Error in AwardAchievementByDisplayName: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Awards a specific achievement by platform key.
        /// </summary>
        /// <param name="platformKey">Platform key of the achievement</param>
        /// <param name="onSuccess">Callback for success message</param>
        /// <param name="onError">Callback for error message</param>
        /// <returns>True if awarding was successful, false otherwise</returns>
        public bool AwardAchievementByPlatformKey(string platformKey, Action<string> onSuccess = null, Action<string> onError = null)
        {
            try
            {
                // Find AchievementHandler instead of GameManager
                Type achievementHandlerType = FindTypeInAssemblies("AchievementHandler");
                if (achievementHandlerType == null)
                {
                    onError?.Invoke("AchievementHandler not found");
                    return false;
                }

                UnityEngine.Object[] achievementHandlers = Resources.FindObjectsOfTypeAll(achievementHandlerType);
                if (achievementHandlers.Length == 0)
                {
                    onError?.Invoke("AchievementHandler instance not found");
                    return false;
                }

                object achievementHandler = achievementHandlers[0];

                // Look for AwardAchievementToPlayer method on AchievementHandler, or get GameManager from it
                MethodInfo awardMethod = achievementHandlerType.GetMethod("AwardAchievementToPlayer", BindingFlags.Public | BindingFlags.Instance);
                object targetInstance = achievementHandler;

                if (awardMethod == null)
                {
                    // Fallback: try to get GameManager and call method there
                    Type gameManagerType = FindTypeInAssemblies("GameManager");
                    if (gameManagerType != null)
                    {
                        PropertyInfo instanceProperty = gameManagerType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
                        if (instanceProperty != null)
                        {
                            object gameManagerInstance = instanceProperty.GetValue(null);
                            if (gameManagerInstance != null)
                            {
                                awardMethod = gameManagerType.GetMethod("AwardAchievementToPlayer", BindingFlags.Public | BindingFlags.Instance);
                                targetInstance = gameManagerInstance;
                            }
                        }
                    }
                }

                if (awardMethod == null)
                {
                    onError?.Invoke("AwardAchievementToPlayer method not found");
                    return false;
                }

                awardMethod.Invoke(targetInstance, new object[] { platformKey });
                
                // Find display name for success message
                string displayName = achievementDisplayToKey.FirstOrDefault(kvp => kvp.Value == platformKey).Key ?? platformKey;
                onSuccess?.Invoke($"Awarded: {displayName}");
                logger.Log($"Awarded achievement: {displayName} ({platformKey})");
                return true;
            }
            catch (Exception ex)
            {
                onError?.Invoke($"Error awarding achievement: {ex.Message}");
                logger.Log($"Error in AwardAchievementByPlatformKey: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Awards all available achievements.
        /// </summary>
        /// <param name="onSuccess">Callback for success message</param>
        /// <param name="onError">Callback for error message</param>
        /// <returns>Number of achievements successfully awarded</returns>
        public int AwardAllAchievements(Action<string> onSuccess = null, Action<string> onError = null)
        {
            try
            {
                if (!achievementsScanned || availableAchievements.Count == 0)
                {
                    onError?.Invoke("No achievements available");
                    return 0;
                }

                // Find AchievementHandler instead of GameManager
                Type achievementHandlerType = FindTypeInAssemblies("AchievementHandler");
                if (achievementHandlerType == null)
                {
                    onError?.Invoke("AchievementHandler not found");
                    return 0;
                }

                UnityEngine.Object[] achievementHandlers = Resources.FindObjectsOfTypeAll(achievementHandlerType);
                if (achievementHandlers.Length == 0)
                {
                    onError?.Invoke("AchievementHandler instance not found");
                    return 0;
                }

                object achievementHandler = achievementHandlers[0];

                // Look for AwardAchievementToPlayer method on AchievementHandler, or get GameManager from it
                MethodInfo awardMethod = achievementHandlerType.GetMethod("AwardAchievementToPlayer", BindingFlags.Public | BindingFlags.Instance);
                object targetInstance = achievementHandler;

                if (awardMethod == null)
                {
                    // Fallback: try to get GameManager and call method there
                    Type gameManagerType = FindTypeInAssemblies("GameManager");
                    if (gameManagerType != null)
                    {
                        PropertyInfo instanceProperty = gameManagerType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
                        if (instanceProperty != null)
                        {
                            object gameManagerInstance = instanceProperty.GetValue(null);
                            if (gameManagerInstance != null)
                            {
                                awardMethod = gameManagerType.GetMethod("AwardAchievementToPlayer", BindingFlags.Public | BindingFlags.Instance);
                                targetInstance = gameManagerInstance;
                            }
                        }
                    }
                }

                if (awardMethod == null)
                {
                    onError?.Invoke("AwardAchievementToPlayer method not found");
                    return 0;
                }

                int awardedCount = 0;
                foreach (string platformKey in availableAchievements)
                {
                    try
                    {
                        awardMethod.Invoke(targetInstance, new object[] { platformKey });
                        awardedCount++;
                    }
                    catch (Exception ex)
                    {
                        logger.Log($"Error awarding {platformKey}: {ex.Message}");
                    }
                }

                onSuccess?.Invoke($"Awarded {awardedCount}/{availableAchievements.Count} achievements");
                logger.Log($"Awarded all achievements: {awardedCount}/{availableAchievements.Count}");
                return awardedCount;
            }
            catch (Exception ex)
            {
                onError?.Invoke($"Error awarding all achievements: {ex.Message}");
                logger.Log($"Error in AwardAllAchievements: {ex.Message}");
                return 0;
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
