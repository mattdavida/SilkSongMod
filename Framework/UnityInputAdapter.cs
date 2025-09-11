using UnityEngine;
using SilkSong.Interfaces;

namespace SilkSong.Framework
{
    /// <summary>
    /// Unity implementation of IInputHandler.
    /// Works with both MelonLoader and BepInEx since both use Unity's Input system.
    /// </summary>
    public class UnityInputAdapter : IInputHandler
    {
        public bool GetKeyDown(KeyCode key)
        {
            return Input.GetKeyDown(key);
        }
    }
}
