using System.Collections.Generic;
using System.IO;
using DeckTracker.LowLevel;
using Newtonsoft.Json;

namespace DeckTracker.Domain
{
    internal static class ConfigManager
    {
        private static readonly string ConfigFile = Path.Combine(Logger.GameDataDirectory, "config.json");
        private static Dictionary<string, Dictionary<string, object>> config = new Dictionary<string, Dictionary<string, object>>();

        public static void Initialize()
        {
            GameMessageDispatcher.OnGameMessage += OnGameMessage;
            ProcessMonitor.OnGameInjectionStateChange += OnGameInjectionStateChange;
            if (File.Exists(ConfigFile))
                config = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(File.ReadAllText(ConfigFile));
        }

        private static void OnGameInjectionStateChange(GameType gameType, InjectionState injectionState)
        {
            if (injectionState != InjectionState.Injected) return;
            if (!config.TryGetValue(gameType.ToString(), out var subConfig))
                subConfig = new Dictionary<string, object>();
#if DEBUG
            subConfig["debug"] = true;
#else
            subConfig.Remove("debug");
#endif
            string jsonConfig = JsonConvert.SerializeObject(subConfig);
            if (ProcessMonitor.SendCommand(gameType, CommandType.Config, jsonConfig, 10000) != "Done")
            if (ProcessMonitor.SendCommand(gameType, CommandType.Config, jsonConfig, 5000) != "Done")
                Logger.LogError("Unable to send config to in-game UI");
        }

        private static void OnGameMessage(GameMessage gameMessage)
        {
            if (gameMessage.MessageType != MessageType.Config) return;
            var subConfig = JsonConvert.DeserializeObject<Dictionary<string, object>>(gameMessage.Message);
            subConfig.Remove("debug");
            config[gameMessage.GameType.ToString()] = subConfig;
            File.WriteAllText(ConfigFile, JsonConvert.SerializeObject(config));
        }
    }
}
