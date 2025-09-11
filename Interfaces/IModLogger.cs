namespace SilkSong.Interfaces
{
    /// <summary>
    /// Abstraction for mod framework logging.
    /// Allows core logic to log without depending on specific framework.
    /// </summary>
    public interface IModLogger
    {
        /// <summary>
        /// Logs a message to the mod framework's logging system.
        /// </summary>
        /// <param name="message">The message to log</param>
        void Log(string message);
    }
}
