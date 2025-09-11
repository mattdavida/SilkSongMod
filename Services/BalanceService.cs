using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using SilkSong.Interfaces;

namespace SilkSong.Services
{
    /// <summary>
    /// Service for managing damage balance and multiplier systems.
    /// Handles dynamic damage field scanning and modification.
    /// </summary>
    public class BalanceService
    {
        private readonly IModLogger logger;
        
        // Balance state
        private List<FieldInfo> damageFields = new List<FieldInfo>();
        private List<MonoBehaviour> damageBehaviours = new List<MonoBehaviour>();
        private Dictionary<FieldInfo, object> originalValues = new Dictionary<FieldInfo, object>();
        private bool fieldsScanned = false;

        public BalanceService(IModLogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Properties

        /// <summary>
        /// Whether damage fields have been scanned
        /// </summary>
        public bool FieldsScanned => fieldsScanned;

        /// <summary>
        /// Number of damage fields found
        /// </summary>
        public int DamageFieldCount => damageFields.Count;

        /// <summary>
        /// List of damage fields for display purposes
        /// </summary>
        public IReadOnlyList<FieldInfo> DamageFields => damageFields.AsReadOnly();

        /// <summary>
        /// List of damage behaviours for display purposes
        /// </summary>
        public IReadOnlyList<MonoBehaviour> DamageBehaviours => damageBehaviours.AsReadOnly();

        #endregion

        #region Damage Field Scanning

        /// <summary>
        /// Scans all objects for damage-related fields
        /// </summary>
        public void ScanDamageFields()
        {
            if (fieldsScanned) return;

            damageFields.Clear();
            damageBehaviours.Clear();
            originalValues.Clear();

            // Use Resources.FindObjectsOfTypeAll instead of FindObjectsByType
            // This finds ALL objects, including inactive ones and those in other scenes
            UnityEngine.Object[] allObjects = Resources.FindObjectsOfTypeAll(typeof(MonoBehaviour));
            logger.Log($"Scanning {allObjects.Length} total objects for damage fields");

            foreach (UnityEngine.Object obj in allObjects)
            {
                if (obj == null) continue;

                MonoBehaviour behaviour = obj as MonoBehaviour;
                if (behaviour == null) continue;

                Type type = behaviour.GetType();
                string typeName = type.Name.ToLower();

                // Focus on DamageEnemies and similar combat components
                if (typeName.Contains("damage") || typeName.Contains("attack") || typeName.Contains("combat") ||
                    typeName.Contains("enemy") || typeName.Contains("weapon") || typeName.Contains("projectile"))
                {
                    FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    foreach (FieldInfo field in fields)
                    {
                        string fieldName = field.Name.ToLower();

                        // Look for damage-related fields
                        if ((fieldName.Contains("damage") || fieldName.Contains("multiplier") || fieldName.Contains("power") ||
                             fieldName.Contains("strength") || fieldName.Contains("force"))
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
                                    logger.Log($"Found: {type.Name}.{field.Name} = {currentValue}");
                                }
                            }
                            catch (Exception ex)
                            {
                                // Skip inaccessible fields
                                logger.Log($"Skipped {type.Name}.{field.Name}: {ex.Message}");
                            }
                        }
                    }
                }
            }

            fieldsScanned = true;
            logger.Log($"Scanned {damageFields.Count} damage-related fields");
        }

        #endregion

        #region Multiplier Application

        /// <summary>
        /// Applies a global damage multiplier to all scanned fields
        /// </summary>
        public bool ApplyGlobalMultiplier(float multiplier, System.Action<string> onSuccess, System.Action<string> onError)
        {
            // Prevent multipliers < 1 as they can trigger invincibility bugs
            if (multiplier < 1.0f)
            {
                onError?.Invoke("Multiplier must be >= 1.0 (values < 1 can cause invincibility bugs)");
                return false;
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
                        float newValue = original * multiplier;
                        field.SetValue(behaviour, newValue);
                        modifiedCount++;
                        uniqueFields.Add(field.Name); // Track unique field names
                    }
                    else if (field.FieldType == typeof(int))
                    {
                        int original = (int)originalValue;
                        int newValue = Mathf.RoundToInt(original * multiplier);
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

            onSuccess?.Invoke($"Applied {multiplier}x to {uniqueFields.Count} unique fields ({modifiedCount} total)");
            return true;
        }

        /// <summary>
        /// Resets fieldsScanned flag to force rescan (useful for scene changes)
        /// </summary>
        public void ResetScanState()
        {
            fieldsScanned = false;
        }

        #endregion
    }
}
