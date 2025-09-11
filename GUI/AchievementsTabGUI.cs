using UnityEngine;
using System;

namespace SilkSong.UserInterface
{
    /// <summary>
    /// GUI component for the Achievements tab.
    /// Handles achievement browsing, searching, and awarding functionality.
    /// </summary>
    public class AchievementsTabGUI
    {
        // State variables
        private int selectedAchievementIndex = 0;
        private bool showAchievementDropdown = false;
        private Vector2 achievementDropdownScroll = Vector2.zero;
        private string achievementSearchFilter = "";
        private string[] filteredAchievementNames = new string[0];

        /// <summary>
        /// Renders the Achievements tab GUI.
        /// </summary>
        /// <param name="context">GUI context containing all services and shared state</param>
        /// <param name="achievementScrollPosition">Current scroll position (passed by reference)</param>
        /// <param name="windowRect">Current window rectangle for sizing</param>
        /// <param name="onToast">Toast notification callback</param>
        public void Render(GuiContext context, ref Vector2 achievementScrollPosition, Rect windowRect, System.Action<string> onToast)
        {
            // Begin scroll view
            achievementScrollPosition = GUILayout.BeginScrollView(achievementScrollPosition, GUILayout.Width(380), GUILayout.Height(windowRect.height - 120));

            GUILayout.Label("Achievement System", GUI.skin.box);
            GUILayout.Label("Award achievements instantly to unlock Steam/platform rewards", GUI.skin.label);
            GUILayout.Space(10);

            // Auto-scan achievements on first access
            if (!context.AchievementService.AreAchievementsScanned)
            {
                context.AchievementService.ScanAchievements(onToast, onToast);
            }

            // Achievement selection and awarding
            if (context.AchievementService.AreAchievementsScanned)
            {
                if (context.AchievementService.AchievementCount > 0)
                {
                    var achievementNames = context.AchievementService.GetAchievementNames();
                    if (achievementNames.Length > 0)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Achievement:", GUILayout.Width(80));

                        // Dropdown button
                        string currentSelection = selectedAchievementIndex < achievementNames.Length ? achievementNames[selectedAchievementIndex] : "Select Achievement";

                        if (GUILayout.Button($"{currentSelection} ▼", GUILayout.Width(180)))
                        {
                            showAchievementDropdown = !showAchievementDropdown;
                            if (showAchievementDropdown)
                            {
                                achievementSearchFilter = ""; // Reset search when opening
                                filteredAchievementNames = context.AchievementService.FilterAchievements(achievementSearchFilter);
                            }
                        }
                        GUILayout.EndHorizontal();

                        // Dropdown with search
                        if (showAchievementDropdown)
                        {
                            GUILayout.BeginVertical(GUI.skin.box);

                            // Search box
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("Search:", GUILayout.Width(50));
                            string newFilter = GUILayout.TextField(achievementSearchFilter, GUILayout.Width(130));
                            if (newFilter != achievementSearchFilter)
                            {
                                achievementSearchFilter = newFilter;
                                filteredAchievementNames = context.AchievementService.FilterAchievements(achievementSearchFilter);
                                achievementDropdownScroll = Vector2.zero; // Reset scroll when filtering
                            }
                            GUILayout.EndHorizontal();

                            // Clear search button
                            if (!string.IsNullOrEmpty(achievementSearchFilter))
                            {
                                if (GUILayout.Button("Clear", GUILayout.Width(60)))
                                {
                                    achievementSearchFilter = "";
                                    filteredAchievementNames = context.AchievementService.FilterAchievements(achievementSearchFilter);
                                }
                            }

                            // Scrollable filtered list
                            achievementDropdownScroll = GUILayout.BeginScrollView(achievementDropdownScroll, GUILayout.Height(150));

                            if (filteredAchievementNames.Length > 0)
                            {
                                for (int i = 0; i < filteredAchievementNames.Length; i++)
                                {
                                    string achievementName = filteredAchievementNames[i];

                                    // Find the index in the original array
                                    int originalIndex = Array.IndexOf(achievementNames, achievementName);

                                    if (GUILayout.Button(achievementName, GUI.skin.label))
                                    {
                                        selectedAchievementIndex = originalIndex;
                                        showAchievementDropdown = false;
                                        achievementSearchFilter = ""; // Clear search after selection
                                    }
                                }
                            }
                            else
                            {
                                GUILayout.Label("No achievements found", GUI.skin.label);
                            }

                            GUILayout.EndScrollView();
                            GUILayout.EndVertical();
                        }
                    }
                    else
                    {
                        GUILayout.Label("No achievements available", GUI.skin.label);
                    }

                    GUILayout.Space(10);

                    // Award button
                    if (achievementNames.Length > 0 && selectedAchievementIndex < achievementNames.Length)
                    {
                        if (GUILayout.Button($"Award Achievement: {achievementNames[selectedAchievementIndex]}", GUILayout.Height(30)))
                        {
                            string selectedAchievementName = achievementNames[selectedAchievementIndex];
                            context.AchievementService.AwardAchievementByDisplayName(selectedAchievementName, onToast, onToast);
                        }
                    }

                    GUILayout.Space(10);

                    // Quick award all button with confirmation
                    GUI.color = Color.yellow;
                    if (GUILayout.Button("Award ALL Achievements", GUILayout.Height(30)))
                    {
                        context.ConfirmationSystem.ShowConfirmation("Award ALL Achievements", "This will award ALL achievements and cannot be undone!", () =>
                        {
                            context.AchievementService.AwardAllAchievements(onToast, onToast);
                        });
                    }
                    GUI.color = Color.white;
                }
                else
                {
                    GUILayout.Label("No achievements found - try changing scenes or restarting the game", GUI.skin.label);
                }
            }

            GUILayout.EndScrollView();
        }
    }
}
