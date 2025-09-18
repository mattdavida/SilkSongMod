using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using SilkSong.Interfaces;

namespace SilkSong.Services
{
    /// <summary>
    /// Service for inspecting and modifying hero components.
    /// Provides targeted sprite modifications for color effects.
    /// </summary>
    public class HeroInspectorService
    {
        private readonly IModLogger logger;
        
        // Hero inspection system
        private Component heroController;
        private GameObject heroGameObject;
        private readonly List<ComponentInfo> allComponents = new List<ComponentInfo>();
        private readonly List<ComponentInfo> spriteComponents = new List<ComponentInfo>();
        private readonly List<ComponentInfo> tk2dComponents = new List<ComponentInfo>();
        
        // Color management
        private bool colorsModified = false;
        private Color? currentDressColor = null;
        private readonly List<object> hookedSprites = new List<object>();
        
        // Complete color state storage for complex effects like rainbow
        private readonly Dictionary<int, Color[]> storedVertexColors = new Dictionary<int, Color[]>();
        
        // Timer-based reapplication system
        private MonoBehaviour coroutineRunner = null;
        private UnityEngine.Coroutine reapplicationCoroutine = null;
        private int reapplicationCounter = 0;
        
        // Crest-aware color system
        private bool isHookedToCrestChanges = false;

        public HeroInspectorService(IModLogger logger)
        {
            this.logger = logger;
        }

        #region Properties

        /// <summary>
        /// Gets whether colors have been modified.
        /// </summary>
        public bool ColorsModified => colorsModified;

        /// <summary>
        /// Gets the current dress color being applied.
        /// </summary>
        public Color? CurrentDressColor => currentDressColor;

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the hero controller and performs component analysis.
        /// </summary>
        public void SetHeroController(Component heroController)
        {
            // Cleanup previous systems
            StopTimerReapplication();
            UnhookSpriteChangeEvents();
            UnhookFromCrestChanges();
            
            this.heroController = heroController;
            
            if (heroController != null)
            {
                heroGameObject = heroController.gameObject;
                AnalyzeHeroComponents();
                logger.Log("HeroInspectorService: Hero controller set, analyzing components");
                
                // Hook into crest change system
                HookIntoCrestChanges();
                
                // Restart timer if we have a current dress color
                if (currentDressColor.HasValue)
                {
                    StartTimerReapplication();
                }
            }
        }

        /// <summary>
        /// Changes dress color using Unity Mesh.colors (vertex colors).
        /// </summary>
        public bool ChangeDressColor(Color newColor, Action<string> onSuccess = null, Action<string> onError = null)
        {
            try
            {
                if (heroGameObject == null)
                {
                    onError?.Invoke("Hero GameObject not found - enter game first");
                    return false;
                }

                int modifiedCount = 0;
                
                logger.Log($"=== DRESS COLOR DEBUG (Unity Mesh.colors) ===");
                logger.Log($"Found {tk2dComponents.Count} tk2dSprite components");
                
                // Find tk2dSprite components and access their Unity Mesh.colors
                for (int compIndex = 0; compIndex < tk2dComponents.Count; compIndex++)
                {
                    var comp = tk2dComponents[compIndex];
                    try
                    {
                        logger.Log($"=== tk2dSprite #{compIndex} on: {comp.GameObject.name} ===");
                        
                        // Try different ways to access the mesh
                        object mesh = null;
                        
                        // Try "mesh" property
                        PropertyInfo meshProperty = comp.Component.GetType().GetProperty("mesh");
                        if (meshProperty != null)
                        {
                            mesh = meshProperty.GetValue(comp.Component);
                            logger.Log($"Found 'mesh' property on {comp.GameObject.name}");
                        }
                        
                        // Try "Mesh" property (capital M)
                        if (mesh == null)
                        {
                            PropertyInfo MeshProperty = comp.Component.GetType().GetProperty("Mesh");
                            if (MeshProperty != null)
                            {
                                mesh = MeshProperty.GetValue(comp.Component);
                                logger.Log($"Found 'Mesh' property on {comp.GameObject.name}");
                            }
                        }
                        
                        // Try accessing through renderer
                        if (mesh == null)
                        {
                            var renderer = comp.Component.GetComponent<MeshRenderer>();
                            if (renderer != null)
                            {
                                var meshFilter = comp.Component.GetComponent<MeshFilter>();
                                if (meshFilter != null)
                                {
                                    mesh = meshFilter.sharedMesh;
                                    logger.Log($"Found mesh through MeshFilter on {comp.GameObject.name}");
                                }
                            }
                        }
                        
                        if (mesh != null)
                        {
                            logger.Log($"Found Unity Mesh on {comp.GameObject.name}: {mesh.GetType().Name}");
                            
                            // Get the Unity Mesh type and look for SetColors method
                            var meshType = mesh.GetType();
                            
                            // Try to get current colors first
                            var colorsProperty = meshType.GetProperty("colors");
                            if (colorsProperty != null)
                            {
                                var colors = colorsProperty.GetValue(mesh) as Array;
                                if (colors != null && colors.Length > 0)
                                {
                                    logger.Log($"Found Unity Mesh.colors with {colors.Length} elements");
                                    
                                    // Try using SetColors method instead of setting property directly
                                    var setColorsMethod = meshType.GetMethod("SetColors", new Type[] { typeof(Color[]) });
                                    if (setColorsMethod != null)
                                    {
                                        // Create new color array with our new color
                                        Color[] newColors = new Color[colors.Length];
                                        for (int i = 0; i < colors.Length; i++)
                                        {
                                            newColors[i] = newColor;
                                        }
                                        
                                        // Use SetColors method (Unity Explorer's approach)
                                        setColorsMethod.Invoke(mesh, new object[] { newColors });
                                        modifiedCount++;
                                        
                                        logger.Log($"Successfully used SetColors method on {comp.GameObject.name} - {colors.Length} vertices set to {newColor}");
                                    }
                                    else
                                    {
                                        logger.Log($"No SetColors method found on Unity Mesh for {comp.GameObject.name}");
                                        
                                        // Fallback: try setting property directly
                                        var newColors = Array.CreateInstance(colors.GetType().GetElementType(), colors.Length);
                                        for (int i = 0; i < colors.Length; i++)
                                        {
                                            newColors.SetValue(newColor, i);
                                        }
                                        
                                        colorsProperty.SetValue(mesh, newColors);
                                        modifiedCount++;
                                        
                                        logger.Log($"Used property fallback on {comp.GameObject.name} - {colors.Length} vertices set to {newColor}");
                                    }
                                }
                                else
                                {
                                    logger.Log($"Unity Mesh.colors is null or empty on {comp.GameObject.name}");
                                }
                            }
                            else
                            {
                                logger.Log($"No colors property found on Unity Mesh for {comp.GameObject.name}");
                            }
                        }
                        else
                        {
                            logger.Log($"Could not find mesh on {comp.GameObject.name}");
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Log($"Error examining {comp.GameObject.name}: {e.Message}");
                    }
                }

                if (modifiedCount > 0)
                {
                    colorsModified = true;
                    currentDressColor = newColor;
                    
                    // Hook into crest change system for proper color persistence
                    HookIntoCrestChanges();
                    
                    // Start timer-based reapplication for testing
                    StartTimerReapplication();
                    
                    onSuccess?.Invoke($"Dress color changed! Modified {modifiedCount} Unity Mesh.colors");
                    logger.Log($"Dress color changed to {newColor} - modified {modifiedCount} Unity Mesh.colors");
                    return true;
                }
                else
                {
                    onError?.Invoke("No Unity Mesh.colors found to modify");
                    logger.Log("No Unity Mesh.colors found to modify");
                    return false;
                }
            }
            catch (Exception e)
            {
                onError?.Invoke($"Error changing dress color: {e.Message}");
                logger.Log($"Error in ChangeDressColor: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Changes a specific vertex color on a specific tk2dSprite component (Unity Explorer approach).
        /// </summary>
        public bool ChangeVertexColor(int componentIndex, int vertexIndex, Color newColor, Action<string> onSuccess = null, Action<string> onError = null)
        {
            try
            {
                if (heroGameObject == null)
                {
                    onError?.Invoke("Hero GameObject not found - enter game first");
                    return false;
                }

                if (componentIndex < 0 || componentIndex >= tk2dComponents.Count)
                {
                    onError?.Invoke($"Invalid component index {componentIndex}. Available: 0-{tk2dComponents.Count - 1}");
                    return false;
                }

                var comp = tk2dComponents[componentIndex];
                logger.Log($"=== VERTEX COLOR TEST - tk2dSprite #{componentIndex}, Vertex #{vertexIndex} ===");
                
                // Find the mesh
                object mesh = null;
                var renderer = comp.Component.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    var meshFilter = comp.Component.GetComponent<MeshFilter>();
                    if (meshFilter != null)
                    {
                        mesh = meshFilter.sharedMesh;
                    }
                }

                if (mesh != null)
                {
                    var meshType = mesh.GetType();
                    var colorsProperty = meshType.GetProperty("colors");
                    if (colorsProperty != null)
                    {
                        var colors = colorsProperty.GetValue(mesh) as Array;
                        if (colors != null && colors.Length > vertexIndex)
                        {
                            // Get current colors
                            Color[] currentColors = new Color[colors.Length];
                            for (int i = 0; i < colors.Length; i++)
                            {
                                currentColors[i] = (Color)colors.GetValue(i);
                            }
                            
                            logger.Log($"Before: Vertex {vertexIndex} = {currentColors[vertexIndex]}");
                            
                            // Only modify the specific vertex
                            currentColors[vertexIndex] = newColor;
                            
                            // Use SetColors method
                            var setColorsMethod = meshType.GetMethod("SetColors", new Type[] { typeof(Color[]) });
                            if (setColorsMethod != null)
                            {
                                setColorsMethod.Invoke(mesh, new object[] { currentColors });
                                
                                colorsModified = true;
                                onSuccess?.Invoke($"Set tk2dSprite #{componentIndex} vertex #{vertexIndex} to {newColor}");
                                logger.Log($"After: Vertex {vertexIndex} = {newColor}");
                                return true;
                            }
                        }
                        else
                        {
                            onError?.Invoke($"Invalid vertex index {vertexIndex} for component {componentIndex}");
                            return false;
                        }
                    }
                }

                onError?.Invoke($"Could not modify vertex color");
                return false;
            }
            catch (Exception e)
            {
                onError?.Invoke($"Error changing vertex color: {e.Message}");
                logger.Log($"Error in ChangeVertexColor: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Applies dress color to ALL tk2dSprite instances in the scene (test method).
        /// </summary>
        public bool ChangeAllSceneSpriteColors(Color newColor, Action<string> onSuccess = null, Action<string> onError = null)
        {
            try
            {
                logger.Log("Testing all scene tk2dSprite instances");
                
                // Find ALL tk2dSprite objects in the scene using Resources.FindObjectsOfTypeAll
                UnityEngine.Object[] allObjects = Resources.FindObjectsOfTypeAll(typeof(Component));
                
                var tk2dSpriteObjects = new List<Component>();
                
                foreach (var obj in allObjects)
                {
                    if (obj != null && obj.GetType().Name.Contains("tk2dSprite"))
                    {
                        tk2dSpriteObjects.Add((Component)obj);
                    }
                }
                
                logger.Log($"Found {tk2dSpriteObjects.Count} tk2dSprite objects in entire scene");
                
                int modifiedCount = 0;
                
                // Diagnostic counters
                int heroSkippedCount = 0;
                int enemySkippedCount = 0;
                int noMeshCount = 0;
                int noColorsPropertyCount = 0;
                int noColorsArrayCount = 0;
                int noSetColorsMethodCount = 0;
                int exceptionCount = 0;
                
                foreach (var spriteComponent in tk2dSpriteObjects)
                {
                    try
                    {
                        
                        object mesh = GetMeshFromComponent(spriteComponent);
                        if (mesh == null)
                        {
                            noMeshCount++;
                            continue;
                        }
                        
                        var meshType = mesh.GetType();
                        var colorsProperty = meshType.GetProperty("colors");
                        if (colorsProperty == null)
                        {
                            noColorsPropertyCount++;
                            continue;
                        }
                        
                        var colors = colorsProperty.GetValue(mesh) as Array;
                        if (colors == null || colors.Length == 0)
                        {
                            noColorsArrayCount++;
                            continue;
                        }
                        
                        var setColorsMethod = meshType.GetMethod("SetColors", new Type[] { typeof(Color[]) });
                        if (setColorsMethod == null)
                        {

                            noSetColorsMethodCount++;
                            continue;
                        }
                        
                        Color[] newColors = new Color[colors.Length];
                        for (int i = 0; i < colors.Length; i++)
                        {
                            newColors[i] = newColor;
                        }
                        
                        setColorsMethod.Invoke(mesh, new object[] { newColors });
                        modifiedCount++;
                    }
                    catch (Exception e)
                    {
                        exceptionCount++;
                    }
                }
                
                // Log diagnostic information
                logger.Log($"Scene sprite analysis: Found {tk2dSpriteObjects.Count} sprites");
                logger.Log($"  - Enemy sprites skipped: {enemySkippedCount}");
                logger.Log($"  - No mesh: {noMeshCount}");
                logger.Log($"  - No colors property: {noColorsPropertyCount}");
                logger.Log($"  - No/empty colors array: {noColorsArrayCount}");
                logger.Log($"  - No SetColors method: {noSetColorsMethodCount}");
                logger.Log($"  - Exceptions: {exceptionCount}");
                logger.Log($"  - Successfully modified: {modifiedCount}");
                
                if (modifiedCount > 0)
                {
                    onSuccess?.Invoke($"Modified {modifiedCount} tk2dSprite objects across entire scene!");
                    logger.Log($"Scene test complete: Modified {modifiedCount}/{tk2dSpriteObjects.Count} sprites");
                    return true;
                }
                else
                {
                    onError?.Invoke($"No tk2dSprite objects were modified (found {tk2dSpriteObjects.Count} total)");
                    return false;
                }
            }
            catch (Exception e)
            {
                onError?.Invoke($"Error testing scene sprites: {e.Message}");
                logger.Log($"Error in ChangeAllSceneSpriteColors: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Captures the current complete vertex color state for all components.
        /// Call this after applying rainbow or complex color effects.
        /// </summary>
        public void CaptureCurrentColorState()
        {
            try
            {
                storedVertexColors.Clear();
                
                for (int compIndex = 0; compIndex < tk2dComponents.Count; compIndex++)
                {
                    var comp = tk2dComponents[compIndex];
                    
                    // Get the mesh and current colors
                    object mesh = GetMeshFromComponent(comp.Component);
                    if (mesh != null)
                    {
                        var meshType = mesh.GetType();
                        var colorsProperty = meshType.GetProperty("colors");
                        if (colorsProperty != null)
                        {
                            var colors = colorsProperty.GetValue(mesh) as Array;
                            if (colors != null && colors.Length > 0)
                            {
                                // Store the current color array
                                Color[] colorArray = new Color[colors.Length];
                                for (int i = 0; i < colors.Length; i++)
                                {
                                    colorArray[i] = (Color)colors.GetValue(i);
                                }
                                storedVertexColors[compIndex] = colorArray;
                                
                                logger.Log($"Captured {colors.Length} vertex colors for component {compIndex} ({comp.GameObject.name})");
                            }
                        }
                    }
                }
                
                logger.Log($"Captured color state for {storedVertexColors.Count} components");
            }
            catch (Exception e)
            {
                logger.Log($"Error capturing color state: {e.Message}");
            }
        }

        /// <summary>
        /// Clears the current dress color and unhooks sprite change events.
        /// </summary>
        public void ClearDressColor()
        {
            try
            {
                // First, apply white/default colors to current sprites to immediately reset them
                if (tk2dComponents.Count > 0)
                {
                    int resetCount = ApplyColorsDirectlyToCurrentSprites(Color.white);
                    logger.Log($"Reset {resetCount} sprites to default colors");
                }
                
                // Then stop all persistence systems
                StopTimerReapplication();
                UnhookSpriteChangeEvents();
                UnhookFromCrestChanges();
                currentDressColor = null;
                storedVertexColors.Clear();
                colorsModified = false;
                logger.Log("Dress color cleared");
            }
            catch (Exception e)
            {
                logger.Log($"Error clearing dress color: {e.Message}");
            }
        }

        /// <summary>
        /// Hooks into the crest change event system to reapply colors when crests switch.
        /// </summary>
        private void HookIntoCrestChanges()
        {
            try
            {
                if (heroGameObject == null || isHookedToCrestChanges)
                    return;

                logger.Log("Hooking into crest change system");
                
                // Find EventRegister and hook into "TOOL EQUIPS CHANGED" event
                // This is the same event the HeroController uses: EventRegister.GetRegisterGuaranteed(base.gameObject, "TOOL EQUIPS CHANGED").ReceivedEvent += ResetAllCrestState;
                
                var eventRegisterType = System.Type.GetType("EventRegister");
                if (eventRegisterType != null)
                {
                    var getRegisterMethod = eventRegisterType.GetMethod("GetRegisterGuaranteed", new Type[] { typeof(GameObject), typeof(string) });
                    if (getRegisterMethod != null)
                    {
                        var eventRegister = getRegisterMethod.Invoke(null, new object[] { heroGameObject, "TOOL EQUIPS CHANGED" });
                        if (eventRegister != null)
                        {
                            var receivedEventField = eventRegister.GetType().GetField("ReceivedEvent");
                            if (receivedEventField != null)
                            {
                                // Create our event handler
                                System.Action crestChangeHandler = OnCrestChanged;
                                
                                // Add our handler to the event
                                var currentDelegate = receivedEventField.GetValue(eventRegister) as System.Delegate;
                                var newDelegate = System.Delegate.Combine(currentDelegate, crestChangeHandler);
                                receivedEventField.SetValue(eventRegister, newDelegate);
                                
                                isHookedToCrestChanges = true;
                                logger.Log("Successfully hooked into TOOL EQUIPS CHANGED event");
                            }
                        }
                    }
                }
                
                if (!isHookedToCrestChanges)
                {
                    logger.Log("Could not hook into crest change system - EventRegister not found");
                }
            }
            catch (Exception e)
            {
                logger.Log($"Error hooking into crest changes: {e.Message}");
            }
        }

        /// <summary>
        /// Unhooks from the crest change event system.
        /// </summary>
        private void UnhookFromCrestChanges()
        {
            try
            {
                if (!isHookedToCrestChanges)
                    return;

                // Note: In practice, unhooking from EventRegister is complex and not strictly necessary
                // since the handler will be garbage collected when this service is disposed
                isHookedToCrestChanges = false;
                logger.Log("Unhooked from crest change system");
            }
            catch (Exception e)
            {
                logger.Log($"Error unhooking from crest changes: {e.Message}");
            }
        }

        /// <summary>
        /// Event handler called when crests change - reapplies colors to new active sprites.
        /// </summary>
        private void OnCrestChanged()
        {
            try
            {
                if (!currentDressColor.HasValue)
                    return;

                logger.Log("Crest changed - reapplying colors");
                
                // Small delay to ensure crest switch is complete
                if (coroutineRunner != null)
                {
                    coroutineRunner.StartCoroutine(ReapplyColorsAfterCrestChange());
                }
            }
            catch (Exception e)
            {
                logger.Log($"Error in OnCrestChanged: {e.Message}");
            }
        }

        /// <summary>
        /// Coroutine to reapply colors after crest change with proper timing.
        /// </summary>
        private System.Collections.IEnumerator ReapplyColorsAfterCrestChange()
        {
            // Wait longer for the crest switch to complete - ActiveRoot changes need time
            yield return new UnityEngine.WaitForSeconds(0.1f);
            
            if (currentDressColor.HasValue)
            {
                // Re-analyze components to find the new active sprites
                AnalyzeHeroComponents();
                
                // Force a direct color application to the new sprites
                bool hasStoredColors = storedVertexColors.Count > 0;
                
                // Use a more direct approach - apply colors immediately without going through ChangeDressColor
                // to avoid potential recursion or event conflicts
                int modifiedCount = ApplyColorsDirectlyToCurrentSprites(currentDressColor.Value);
                
                if (modifiedCount > 0)
                {
                    logger.Log($"Applied colors to {modifiedCount} new crest sprites");
                }
                else
                {
                    // Fallback: try the scene-wide approach
                    ChangeAllSceneSpriteColors(currentDressColor.Value);
                }
            }
        }

        /// <summary>
        /// Directly applies colors to currently analyzed tk2dSprite components.
        /// </summary>
        private int ApplyColorsDirectlyToCurrentSprites(Color color)
        {
            int modifiedCount = 0;
            
            try
            {
                for (int compIndex = 0; compIndex < tk2dComponents.Count; compIndex++)
                {
                    var comp = tk2dComponents[compIndex];
                    try
                    {
                        object mesh = GetMeshFromComponent(comp.Component);
                        if (mesh != null)
                        {
                            var meshType = mesh.GetType();
                            var colorsProperty = meshType.GetProperty("colors");
                            if (colorsProperty != null)
                            {
                                var colors = colorsProperty.GetValue(mesh) as Array;
                                if (colors != null && colors.Length > 0)
                                {
                                    var setColorsMethod = meshType.GetMethod("SetColors", new Type[] { typeof(Color[]) });
                                    if (setColorsMethod != null)
                                    {
                                        Color[] newColors = new Color[colors.Length];
                                        for (int i = 0; i < colors.Length; i++)
                                        {
                                            newColors[i] = color;
                                        }
                                        
                                        setColorsMethod.Invoke(mesh, new object[] { newColors });
                                        modifiedCount++;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Log($"Error processing sprite component: {e.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                logger.Log($"Error in ApplyColorsDirectlyToCurrentSprites: {e.Message}");
            }
            
            return modifiedCount;
        }

        /// <summary>
        /// Starts timer-based color reapplication every 3 seconds for testing.
        /// </summary>
        private void StartTimerReapplication()
        {
            try
            {
                if (heroGameObject == null || !currentDressColor.HasValue)
                    return;

                logger.Log("Starting color persistence system");
                
                // Stop any existing timer
                StopTimerReapplication();
                
                // Reset counter
                reapplicationCounter = 0;
                
                // Find a MonoBehaviour to run the coroutine
                if (coroutineRunner == null)
                {
                    coroutineRunner = heroGameObject.GetComponent<MonoBehaviour>();
                    if (coroutineRunner == null)
                    {
                        // Try to find any MonoBehaviour in the scene
                        coroutineRunner = UnityEngine.Object.FindObjectOfType<MonoBehaviour>();
                    }
                }
                
                if (coroutineRunner != null)
                {
                    reapplicationCoroutine = coroutineRunner.StartCoroutine(TimerReapplicationCoroutine());
                    logger.Log($"Started timer reapplication using {coroutineRunner.GetType().Name}");
                }
                else
                {
                    logger.Log("Could not find MonoBehaviour to run timer coroutine");
                }
            }
            catch (Exception e)
            {
                logger.Log($"Error in StartTimerReapplication: {e.Message}");
            }
        }

        /// <summary>
        /// Stops the timer-based color reapplication.
        /// </summary>
        private void StopTimerReapplication()
        {
            try
            {
                if (reapplicationCoroutine != null && coroutineRunner != null)
                {
                    coroutineRunner.StopCoroutine(reapplicationCoroutine);
                    reapplicationCoroutine = null;
                    logger.Log("Stopped timer-based color reapplication");
                }
            }
            catch (Exception e)
            {
                logger.Log($"Error stopping timer reapplication: {e.Message}");
            }
        }

        /// <summary>
        /// Coroutine that reapplies dress color every frame when colors are active.
        /// </summary>
        private System.Collections.IEnumerator TimerReapplicationCoroutine()
        {
            while (currentDressColor.HasValue)
            {
                yield return null; // Every frame
                
                if (currentDressColor.HasValue)
                {
                    reapplicationCounter++;
                    
                    // Only log every 300th application (roughly every 5 seconds at 60fps) to reduce spam
                    bool shouldLog = (reapplicationCounter % 300 == 0);
                    if (shouldLog)
                    {
                        logger.Log($"Color persistence active (frame #{reapplicationCounter})");
                    }
                    
                    // Reapply the dress color every frame (but don't restart the timer to avoid recursion)
                    ReapplyDressColorDirect(currentDressColor.Value, shouldLog);
                }
            }
        }

        /// <summary>
        /// Helper method to get mesh from a component using various approaches.
        /// </summary>
        private object GetMeshFromComponent(Component component)
        {
            object mesh = null;
            
            // Try "mesh" property
            PropertyInfo meshProperty = component.GetType().GetProperty("mesh");
            if (meshProperty != null)
            {
                mesh = meshProperty.GetValue(component);
            }
            
            // Try "Mesh" property (capital M)
            if (mesh == null)
            {
                PropertyInfo MeshProperty = component.GetType().GetProperty("Mesh");
                if (MeshProperty != null)
                {
                    mesh = MeshProperty.GetValue(component);
                }
            }
            
            // Try accessing through renderer
            if (mesh == null)
            {
                var renderer = component.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    var meshFilter = component.GetComponent<MeshFilter>();
                    if (meshFilter != null)
                    {
                        mesh = meshFilter.sharedMesh;
                    }
                }
            }
            
            return mesh;
        }

        /// <summary>
        /// Directly reapplies stored color state without restarting timer.
        /// </summary>
        private void ReapplyDressColorDirect(Color color, bool shouldLog = true)
        {
            try
            {
                int modifiedCount = 0;
                
                // If we have stored vertex colors, restore those instead of uniform color
                if (storedVertexColors.Count > 0)
                {
                    if (shouldLog)
                    {
                        logger.Log("Restoring stored vertex color state (rainbow/complex effects)");
                    }
                    
                    for (int compIndex = 0; compIndex < tk2dComponents.Count; compIndex++)
                    {
                        if (storedVertexColors.ContainsKey(compIndex))
                        {
                            var comp = tk2dComponents[compIndex];
                            object mesh = GetMeshFromComponent(comp.Component);
                            
                            if (mesh != null)
                            {
                                var meshType = mesh.GetType();
                                var setColorsMethod = meshType.GetMethod("SetColors", new Type[] { typeof(Color[]) });
                                if (setColorsMethod != null)
                                {
                                    setColorsMethod.Invoke(mesh, new object[] { storedVertexColors[compIndex] });
                                    modifiedCount++;
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Fallback to uniform color application
                    for (int compIndex = 0; compIndex < tk2dComponents.Count; compIndex++)
                    {
                        var comp = tk2dComponents[compIndex];
                        try
                        {
                            object mesh = GetMeshFromComponent(comp.Component);
                            
                            if (mesh != null)
                            {
                                var meshType = mesh.GetType();
                                var colorsProperty = meshType.GetProperty("colors");
                                if (colorsProperty != null)
                                {
                                    var colors = colorsProperty.GetValue(mesh) as Array;
                                    if (colors != null && colors.Length > 0)
                                    {
                                        var setColorsMethod = meshType.GetMethod("SetColors", new Type[] { typeof(Color[]) });
                                        if (setColorsMethod != null)
                                        {
                                            Color[] newColors = new Color[colors.Length];
                                            for (int i = 0; i < colors.Length; i++)
                                            {
                                                newColors[i] = color;
                                            }
                                            
                                            setColorsMethod.Invoke(mesh, new object[] { newColors });
                                            modifiedCount++;
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            logger.Log($"Error reapplying color to {comp.GameObject.name}: {e.Message}");
                        }
                    }
                }
                
                if (shouldLog)
                {
                    string method = storedVertexColors.Count > 0 ? "stored vertex colors" : $"uniform color {color}";
                    logger.Log($"Timer reapplication: Modified {modifiedCount} components with {method}");
                }
            }
            catch (Exception e)
            {
                logger.Log($"Error in ReapplyDressColorDirect: {e.Message}");
            }
        }

        /// <summary>
        /// Unhooks all sprite change events.
        /// </summary>
        private void UnhookSpriteChangeEvents()
        {
            try
            {
                foreach (var sprite in hookedSprites)
                {
                    try
                    {
                        var spriteType = sprite.GetType();
                        var spriteChangedEvent = spriteType.GetEvent("SpriteChanged");
                        if (spriteChangedEvent != null)
                        {
                            var handlerType = spriteChangedEvent.EventHandlerType;
                            var handler = Delegate.CreateDelegate(handlerType, this, nameof(OnSpriteChanged));
                            spriteChangedEvent.RemoveEventHandler(sprite, handler);
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Log($"Error unhooking sprite event: {e.Message}");
                    }
                }
                
                hookedSprites.Clear();
                logger.Log("Unhooked all sprite change events");
            }
            catch (Exception e)
            {
                logger.Log($"Error in UnhookSpriteChangeEvents: {e.Message}");
            }
        }

        /// <summary>
        /// Event handler for sprite changes - reapplies dress color.
        /// </summary>
        private void OnSpriteChanged(object sender)
        {
            try
            {
                if (!currentDressColor.HasValue)
                    return;

                logger.Log($"=== SPRITE CHANGED EVENT - Reapplying dress color {currentDressColor.Value} ===");
                
                // Small delay to ensure sprite is fully updated
                UnityEngine.Object.FindObjectOfType<MonoBehaviour>().StartCoroutine(ReapplyColorAfterDelay());
            }
            catch (Exception e)
            {
                logger.Log($"Error in OnSpriteChanged: {e.Message}");
            }
        }

        /// <summary>
        /// Coroutine to reapply color after a small delay.
        /// </summary>
        private System.Collections.IEnumerator ReapplyColorAfterDelay()
        {
            yield return new UnityEngine.WaitForEndOfFrame();
            
            if (currentDressColor.HasValue)
            {
                logger.Log("Reapplying dress color after sprite change");
                ChangeDressColor(currentDressColor.Value, 
                    success => logger.Log($"Color reapplied: {success}"),
                    error => logger.Log($"Error reapplying color: {error}"));
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Analyzes components on the main hero GameObject (Unity Explorer style).
        /// </summary>
        private void AnalyzeHeroComponents()
        {
            try
            {
                allComponents.Clear();
                spriteComponents.Clear();
                tk2dComponents.Clear();

                if (heroGameObject == null) return;

                // Get ALL components on the main hero GameObject (Unity Explorer approach)
                var components = heroGameObject.GetComponents<Component>();
                
                foreach (var comp in components)
                {
                    if (comp == null) continue;

                    var compInfo = new ComponentInfo(comp);
                    allComponents.Add(compInfo);

                    // Categorize sprite-related components
                    if (comp is SpriteRenderer)
                    {
                        spriteComponents.Add(compInfo);
                    }
                    else if (comp.GetType().Name.Contains("tk2dSprite"))
                    {
                        tk2dComponents.Add(compInfo);
                    }
                }

                // Only log component analysis during initial setup or when explicitly debugging
                if (tk2dComponents.Count == 0)
                {
                    logger.Log("Warning: No tk2dSprite components found for color application");
                }
            }
            catch (Exception e)
            {
                logger.Log($"Error analyzing hero components: {e.Message}");
            }
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Information about a component (Unity Explorer style).
        /// </summary>
        private class ComponentInfo
        {
            public Component Component { get; }
            public GameObject GameObject => Component.gameObject;
            public string TypeName { get; }
            public bool HasColorProperty { get; }

            public ComponentInfo(Component component)
            {
                Component = component;
                TypeName = component.GetType().Name;
                HasColorProperty = CheckHasColorProperty();
            }

            private bool CheckHasColorProperty()
            {
                try
                {
                    if (Component is SpriteRenderer)
                        return true;

                    // Check for tk2dSprite color property
                    var colorProperty = Component.GetType().GetProperty("color");
                    return colorProperty != null && colorProperty.PropertyType == typeof(Color);
                }
                catch
                {
                    return false;
                }
            }

            public Color GetColor()
            {
                try
                {
                    if (Component is SpriteRenderer sr)
                        return sr.color;

                    var colorProperty = Component.GetType().GetProperty("color");
                    if (colorProperty != null)
                        return (Color)colorProperty.GetValue(Component);
                }
                catch { }
                
                return Color.white;
            }

            public bool SetColor(Color color)
            {
                try
                {
                    if (Component is SpriteRenderer sr)
                    {
                        sr.color = color;
                        return true;
                    }

                    var colorProperty = Component.GetType().GetProperty("color");
                    if (colorProperty != null && colorProperty.CanWrite)
                    {
                        colorProperty.SetValue(Component, color);
                        return true;
                    }
                }
                catch { }
                
                return false;
            }
        }

        #endregion
    }
}
