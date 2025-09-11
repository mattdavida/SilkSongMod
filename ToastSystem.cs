using UnityEngine;

namespace SilkSong.Core
{
    /// <summary>
    /// Handles toast notification display and management.
    /// Completely self-contained system for showing temporary messages to the user.
    /// </summary>
    public class ToastSystem
    {
        private string lastToastMessage = "";
        private float toastTimer = 0f;
        private const float TOAST_DURATION = 3f;

        /// <summary>
        /// Shows a toast notification for the default duration (3 seconds).
        /// </summary>
        /// <param name="message">The message to display</param>
        public void ShowToast(string message)
        {
            lastToastMessage = message;
            toastTimer = TOAST_DURATION;
        }

        /// <summary>
        /// Updates the toast timer. Call this every frame from your main Update loop.
        /// </summary>
        /// <param name="deltaTime">Time since last frame (typically Time.deltaTime)</param>
        public void Update(float deltaTime)
        {
            if (toastTimer > 0f)
            {
                toastTimer -= deltaTime;
            }
        }

        /// <summary>
        /// Renders the toast notification if one is active.
        /// Call this from your OnGUI method.
        /// </summary>
        public void RenderToast()
        {
            if (toastTimer > 0f && !string.IsNullOrEmpty(lastToastMessage))
            {
                // Calculate fade alpha based on remaining time
                float alpha = toastTimer / TOAST_DURATION;
                
                // Apply fade effect to color
                Color originalColor = GUI.color;
                GUI.color = new Color(0.2f, 0.8f, 0.2f, alpha);
                
                // Render the toast message
                GUILayout.Label(lastToastMessage, GUI.skin.box);
                
                // Restore original color
                GUI.color = originalColor;
            }
        }

        /// <summary>
        /// Checks if a toast is currently being displayed.
        /// </summary>
        /// <returns>True if a toast is active, false otherwise</returns>
        public bool IsToastActive()
        {
            return toastTimer > 0f;
        }

        /// <summary>
        /// Immediately clears any active toast.
        /// </summary>
        public void ClearToast()
        {
            toastTimer = 0f;
            lastToastMessage = "";
        }

        /// <summary>
        /// Gets the current toast message (if any).
        /// </summary>
        /// <returns>The current toast message, or empty string if none</returns>
        public string GetCurrentMessage()
        {
            return toastTimer > 0f ? lastToastMessage : "";
        }
    }
}
