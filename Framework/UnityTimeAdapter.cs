using UnityEngine;
using SilkSong.Interfaces;

namespace SilkSong.Framework
{
    /// <summary>
    /// Unity implementation of ITimeProvider.
    /// Works with both MelonLoader and BepInEx since both use Unity's Time system.
    /// </summary>
    public class UnityTimeAdapter : ITimeProvider
    {
        public float DeltaTime => Time.deltaTime;
        
        public float CurrentTime => Time.time;
        
        public float TimeScale 
        { 
            get => Time.timeScale;
            set => Time.timeScale = value;
        }
    }
}
