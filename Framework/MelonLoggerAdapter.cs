using MelonLoader;
using SilkSong.Interfaces;

namespace SilkSong.Framework
{
    /// <summary>
    /// MelonLoader implementation of IModLogger.
    /// Forwards logging calls to MelonLoader's logging system.
    /// </summary>
    public class MelonLoggerAdapter : IModLogger
    {
        public void Log(string message)
        {
            MelonLogger.Msg(message);
        }
    }
}
