using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Text;

namespace SilkSong
{
    public class SilkSongMod : MelonMod
    {
        private Component heroController;
        private bool autoRefillSilk = false;
        private float silkRefillTimer = 0f;
        private const float SILK_REFILL_INTERVAL = 3.0f;

        [System.Obsolete]
        public override void OnApplicationStart()
        {
            MelonLogger.Msg("Silksong Health Mod v1.0 - Ready!");
            MelonLogger.Msg("Controls: F1=Add Health, F2=Max Out Mask Shards, F3=Refill Health, F4=One Hit Kill Mode, F5=Add 1000 Money, F6=Add 1000 Shards, F8=Unlock All Crests, F9=Unlock All Tools, F10=Unlock All Items, F11=Max All Collectables, F12=Toggle Auto Silk Refill");
        }

        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();
            MelonLogger.Msg("Health mod initialized!");
        }

        public override void OnUpdate()
        {
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

            // Health modification controls
            if (heroController != null)
            {
                if (Input.GetKeyDown(KeyCode.F1))
                {
                    AddHealth(1);
                }

                if (Input.GetKeyDown(KeyCode.F2))
                {
                    AddMaxHealth(6); // Add 6 hearts (5 starting + 6 = 11 total)
                }

                if (Input.GetKeyDown(KeyCode.F3))
                {
                    RefillHealth();
                }

                if (Input.GetKeyDown(KeyCode.F4))
                {
                    EnableOneHitKill();
                }

                if (Input.GetKeyDown(KeyCode.F5))
                {
                    AddMoney(1000);
                }

                if (Input.GetKeyDown(KeyCode.F6))
                {
                    AddShards(1000);
                }

                if (Input.GetKeyDown(KeyCode.F8))
                {
                    UnlockAllCrests();
                }

                if (Input.GetKeyDown(KeyCode.F9))
                {
                    UnlockAllTools();
                }

                if (Input.GetKeyDown(KeyCode.F10))
                {
                    UnlockAllItems();
                }

                if (Input.GetKeyDown(KeyCode.F11))
                {
                    MaxAllCollectables();
                }

                if (Input.GetKeyDown(KeyCode.F12))
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
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            base.OnSceneWasLoaded(buildIndex, sceneName);
            MelonLogger.Msg($"Scene: {sceneName}");
            heroController = null; // Reset hero controller for new scene
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

        private void AddMaxHealth(int amount)
        {
            try
            {
                Type type = heroController.GetType();
                MethodInfo addMaxHealthMethod = type.GetMethod("AddToMaxHealth");
                
                if (addMaxHealthMethod != null)
                {
                    addMaxHealthMethod.Invoke(heroController, new object[] { amount });
                    MelonLogger.Msg($"Added {amount} max health - Save to main menu and re-enter to see UI update");
                }
                else
                {
                    MelonLogger.Msg("AddToMaxHealth method not found");
                }
            }
            catch (Exception e)
            {
                MelonLogger.Msg($"Error adding max health: {e.Message}");
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
                MonoBehaviour[] allBehaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();

                
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
                                catch (Exception e)
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
                MelonLogger.Msg("Auto Silk Refill: ENABLED (every 3 seconds)");
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


    }
}
    