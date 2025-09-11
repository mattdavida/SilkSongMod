using UnityEngine;

namespace SilkSong.Interfaces
{
    /// <summary>
    /// Abstraction for input handling.
    /// Allows core logic to check input without depending on specific framework.
    /// </summary>
    public interface IInputHandler
    {
        /// <summary>
        /// Checks if a key was pressed down this frame.
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>True if key was pressed down this frame</returns>
        bool GetKeyDown(KeyCode key);
    }
}
