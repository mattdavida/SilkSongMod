using UnityEngine;
using System;

namespace SilkSong.Core
{
    /// <summary>
    /// Handles confirmation modal dialogs for destructive actions.
    /// Provides a clean interface for showing confirmation dialogs with Yes/No options.
    /// </summary>
    public class ConfirmationSystem
    {
        private bool showConfirmModal = false;
        private string confirmMessage = "";
        private string confirmActionName = "";
        private System.Action pendingAction = null;
        private static Texture2D solidBlackTexture = null;
        private float modalCooldownTime = 0f;
        private Rect modalWindowRect = new Rect(0, 0, 400, 200);

        /// <summary>
        /// Shows a confirmation dialog with the specified action and message.
        /// </summary>
        /// <param name="actionName">Name of the action (e.g., "Delete All")</param>
        /// <param name="message">Confirmation message to display</param>
        /// <param name="action">Action to execute if user confirms</param>
        public void ShowConfirmation(string actionName, string message, System.Action action)
        {
            // Prevent rapid-fire modal creation
            if (Time.time < modalCooldownTime)
            {
                return;
            }

            confirmActionName = actionName;
            confirmMessage = message;
            pendingAction = action;
            showConfirmModal = true;
        }

        /// <summary>
        /// Updates the confirmation system. Call this every frame from your main Update loop.
        /// </summary>
        /// <param name="currentTime">Current game time (typically Time.time)</param>
        public void Update(float currentTime)
        {
            // Update cooldown timer - this is handled internally
            // No external update needed for this system
        }

        /// <summary>
        /// Renders the confirmation modal if one is active.
        /// Call this from your OnGUI method AFTER the main UI.
        /// </summary>
        public void RenderModal()
        {
            if (!showConfirmModal) return;

            // Create solid black overlay first
            if (solidBlackTexture == null)
            {
                solidBlackTexture = new Texture2D(1, 1);
                solidBlackTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.8f));
                solidBlackTexture.Apply();
            }

            // Draw full screen overlay
            GUI.depth = -1000;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), solidBlackTexture);

            // Center the modal on screen
            modalWindowRect.x = (Screen.width - modalWindowRect.width) / 2;
            modalWindowRect.y = (Screen.height - modalWindowRect.height) / 2;

            // Draw modal window with high depth priority and full opacity
            GUI.depth = -999;
            Color originalBackground = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1.0f); // Dark gray, fully opaque
            modalWindowRect = GUI.Window(54321, modalWindowRect, DrawConfirmModal, $"Confirm {confirmActionName}");
            GUI.backgroundColor = originalBackground;
            GUI.depth = 0;
        }

        /// <summary>
        /// Checks if a confirmation modal is currently being displayed.
        /// </summary>
        /// <returns>True if a modal is active, false otherwise</returns>
        public bool IsModalActive()
        {
            return showConfirmModal;
        }

        /// <summary>
        /// Immediately closes any active confirmation modal without executing the action.
        /// </summary>
        public void CloseModal()
        {
            showConfirmModal = false;
            pendingAction = null;
            modalCooldownTime = Time.time + 0.2f;
        }

        /// <summary>
        /// Executes the pending action and closes the modal.
        /// </summary>
        public void ConfirmAction()
        {
            pendingAction?.Invoke();
            showConfirmModal = false;
            pendingAction = null;
            modalCooldownTime = Time.time + 0.2f;
        }

        /// <summary>
        /// Internal method to draw the confirmation modal content.
        /// </summary>
        /// <param name="windowID">GUI window ID</param>
        private void DrawConfirmModal(int windowID)
        {
            GUILayout.BeginVertical();

            // Message with better styling
            GUI.color = Color.white;
            GUIStyle messageStyle = new GUIStyle(GUI.skin.label);
            messageStyle.wordWrap = true;
            messageStyle.normal.textColor = Color.white;
            messageStyle.fontSize = 14;
            messageStyle.padding = new RectOffset(10, 10, 10, 10);

            GUILayout.Label(confirmMessage, messageStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(80));

            GUILayout.FlexibleSpace();

            // Buttons
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // Yes button (positive action - green)
            GUI.color = Color.green;
            if (GUILayout.Button("Yes", GUILayout.Width(100), GUILayout.Height(30)))
            {
                ConfirmAction();
            }

            GUILayout.Space(20);

            // No button (neutral - white)
            GUI.color = Color.white;
            if (GUILayout.Button("No", GUILayout.Width(100), GUILayout.Height(30)))
            {
                CloseModal();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.EndVertical();
            GUI.color = Color.white;
        }
    }
}
