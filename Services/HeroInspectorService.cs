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

        public HeroInspectorService(IModLogger logger)
        {
            this.logger = logger;
        }

        #region Properties

        /// <summary>
        /// Gets whether colors have been modified.
        /// </summary>
        public bool ColorsModified => colorsModified;

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the hero controller and performs component analysis.
        /// </summary>
        public void SetHeroController(Component heroController)
        {
            this.heroController = heroController;
            
            if (heroController != null)
            {
                heroGameObject = heroController.gameObject;
                AnalyzeHeroComponents();
                logger.Log("HeroInspectorService: Hero controller set, analyzing components");
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

                logger.Log($"=== UNITY EXPLORER COMPONENT ANALYSIS ===");
                logger.Log($"Hero analysis complete: {allComponents.Count} total, {spriteComponents.Count} SpriteRenderer, {tk2dComponents.Count} tk2dSprite");
                
                // List all tk2dSprite components like Unity Explorer does
                for (int i = 0; i < tk2dComponents.Count; i++)
                {
                    var comp = tk2dComponents[i];
                    logger.Log($"tk2dSprite #{i}: {comp.Component.GetType().FullName}");
                    logger.Log($"  GameObject: {comp.GameObject.name}");
                    logger.Log($"  Component Name: {comp.Component.name}");
                    logger.Log($"  Instance ID: {comp.Component.GetInstanceID()}");
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
