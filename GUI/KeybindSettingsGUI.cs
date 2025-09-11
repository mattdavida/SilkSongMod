using UnityEngine;

namespace SilkSong.UserInterface
{
    /// <summary>
    /// GUI component for keybind management.
    /// Handles setting, clearing, enabling/disabling keybinds with real-time feedback.
    /// </summary>
    public class KeybindSettingsGUI
    {
        private bool showKeybindSettings = false;

        // Keybind configuration
        private readonly string[] keybindNames = new string[]
        {
            "Add Health", "Set Health", "Refill Health", "Toggle One Hit Kill", "Add Money",
            "Add Shards", "Unlock Crests", "Unlock Crest Skills", "Unlock Crest Tools", "Max Collectables", "Auto Silk"
        };

        /// <summary>
        /// Renders the keybind settings section within a parent GUI.
        /// </summary>
        /// <param name="currentKeybinds">The current keybind array (modified by reference)</param>
        /// <param name="isSettingKeybind">Whether currently setting a keybind (modified by reference)</param>
        /// <param name="keybindToSet">Index of keybind being set (modified by reference)</param>
        /// <param name="onToast">Toast notification callback</param>
        /// <param name="drawCollapsingHeader">Function to draw collapsing headers consistently</param>
        public void RenderSection(ref KeyCode[] currentKeybinds, ref bool isSettingKeybind, ref int keybindToSet, 
                                 System.Action<string> onToast, System.Func<string, bool, bool> drawCollapsingHeader)
        {
            GUILayout.Space(5);

            // Keybind Settings section
            string keybindTitle = isSettingKeybind ? "Press any key (ESC to cancel)" : "Keybind Settings";
            showKeybindSettings = drawCollapsingHeader(keybindTitle, showKeybindSettings);
            
            if (showKeybindSettings)
            {
                GUILayout.BeginVertical(GUI.skin.box);

                if (!isSettingKeybind)
                {
                    RenderManagementButtons(ref currentKeybinds, onToast);
                    GUILayout.Space(5);
                    RenderKeybindList(ref currentKeybinds, ref isSettingKeybind, ref keybindToSet, onToast);
                }
                else
                {
                    RenderWaitingMessage(keybindToSet);
                }

                GUILayout.EndVertical();
            }
        }

        /// <summary>
        /// Renders the enable/disable all buttons.
        /// </summary>
        private void RenderManagementButtons(ref KeyCode[] currentKeybinds, System.Action<string> onToast)
        {
            // Enable/Disable All buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Enable All Defaults", GUILayout.Width(120)))
            {
                SetDefaultKeybinds(ref currentKeybinds);
                onToast("All keybinds restored to defaults");
            }
            if (GUILayout.Button("Disable All", GUILayout.Width(80)))
            {
                DisableAllKeybinds(ref currentKeybinds);
                onToast("All keybinds disabled");
            }
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Renders the list of individual keybind settings.
        /// </summary>
        private void RenderKeybindList(ref KeyCode[] currentKeybinds, ref bool isSettingKeybind, ref int keybindToSet, 
                                      System.Action<string> onToast)
        {
            for (int i = 0; i < keybindNames.Length; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{keybindNames[i]}:", GUILayout.Width(100));
                GUILayout.Label($"{currentKeybinds[i]}", GUILayout.Width(70));
                
                if (GUILayout.Button("Set", GUILayout.Width(35)))
                {
                    isSettingKeybind = true;
                    keybindToSet = i;
                    onToast($"Press key for {keybindNames[i]}");
                }
                
                if (GUILayout.Button("Clear", GUILayout.Width(45)))
                {
                    currentKeybinds[i] = KeyCode.None;
                    onToast($"Cleared {keybindNames[i]} keybind");
                }
                
                GUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// Renders the waiting message when setting a keybind.
        /// </summary>
        private void RenderWaitingMessage(int keybindToSet)
        {
            string keybindName = keybindToSet >= 0 && keybindToSet < keybindNames.Length 
                                ? keybindNames[keybindToSet] 
                                : "Unknown";
            
            GUILayout.Space(10);
            GUI.color = Color.yellow;
            GUILayout.Label($"Setting keybind for: {keybindName}", GUI.skin.box);
            GUILayout.Label("Press any key or ESC to cancel", GUI.skin.label);
            GUI.color = Color.white;
            GUILayout.Space(10);
        }

        /// <summary>
        /// Sets all keybinds to their default values.
        /// </summary>
        private void SetDefaultKeybinds(ref KeyCode[] currentKeybinds)
        {
            currentKeybinds[0] = KeyCode.F1;   // Add Health
            currentKeybinds[1] = KeyCode.F2;   // Set Health
            currentKeybinds[2] = KeyCode.F3;   // Refill Health
            currentKeybinds[3] = KeyCode.F4;   // One Hit Kill
            currentKeybinds[4] = KeyCode.F5;   // Add Money
            currentKeybinds[5] = KeyCode.F6;   // Add Shards
            currentKeybinds[6] = KeyCode.F8;   // Unlock Crests
            currentKeybinds[7] = KeyCode.F9;   // Unlock Crest Skills
            currentKeybinds[8] = KeyCode.F10;  // Unlock Crest Tools
            currentKeybinds[9] = KeyCode.F11;  // Max Collectables
            currentKeybinds[10] = KeyCode.F12; // Auto Silk
        }

        /// <summary>
        /// Disables all keybinds by setting them to None.
        /// </summary>
        private void DisableAllKeybinds(ref KeyCode[] currentKeybinds)
        {
            for (int i = 0; i < currentKeybinds.Length; i++)
            {
                currentKeybinds[i] = KeyCode.None;
            }
        }

        /// <summary>
        /// Gets the keybind names array for external use.
        /// </summary>
        public string[] GetKeybindNames()
        {
            return keybindNames;
        }

        /// <summary>
        /// Resets the GUI state (useful when switching tabs or initializing).
        /// </summary>
        public void ResetState()
        {
            showKeybindSettings = false;
        }
    }
}
