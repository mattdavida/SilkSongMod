namespace SilkSong.Interfaces
{
    /// <summary>
    /// Abstraction for time access.
    /// Allows core logic to access time without depending on Unity directly.
    /// </summary>
    public interface ITimeProvider
    {
        /// <summary>
        /// Gets the time since the last frame in seconds.
        /// </summary>
        float DeltaTime { get; }
        
        /// <summary>
        /// Gets the current time since the game started.
        /// </summary>
        float CurrentTime { get; }
        
        /// <summary>
        /// Gets or sets the time scale (game speed multiplier).
        /// </summary>
        float TimeScale { get; set; }
    }
}
