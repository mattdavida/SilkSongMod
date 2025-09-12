#if BEPINEX
using BepInEx.Logging;
using SilkSong.Interfaces;

namespace SilkSong.Framework
{
    /// <summary>
    /// BepInEx implementation of IModLogger.
    /// Forwards logging calls to BepInEx's logging system.
    /// </summary>
    public class BepInExLoggerAdapter : IModLogger
    {
        private readonly ManualLogSource logger;
        
        public BepInExLoggerAdapter(ManualLogSource logger)
        {
            this.logger = logger;
        }
        
        public void Log(string message)
        {
            logger.LogInfo(message);
        }
    }
}
#endif
