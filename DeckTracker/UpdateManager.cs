using System;
using System.Threading.Tasks;
using Squirrel;
using System.Reflection;
using DeckTracker.LowLevel;

namespace DeckTracker
{
    public static class UpdateManager
    {
        private const string UpdateUrl = "https://github.com/extesy/DeckTracker";

        public delegate void OnNewVersionHandler(string newVersion);
        public static event OnNewVersionHandler OnNewVersion;   

        private static async void CheckForUpdate()
        {
            try {
                string currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
                using (var updateManager = await Squirrel.UpdateManager.GitHubUpdateManager(UpdateUrl)) {
                    var updateInfo = await updateManager.CheckForUpdate();
                    string newVersion = updateInfo.FutureReleaseEntry.Version.ToString();
                    if (!newVersion.Equals(currentVersion)) {
                        Logger.LogDebug(Domain.GameType.Eternal, $"Started update to version {newVersion}");                        
                        await updateManager.UpdateApp();
                        Logger.LogDebug(Domain.GameType.Eternal, $"Updated to version {newVersion}");
                        OnNewVersion?.Invoke(newVersion);
                    }
                }
            } catch (Exception e) {
                Logger.LogError(e.ToString());
            }
        }

        public static async void StartUpdateCheck()
        {
            while (true) {
                CheckForUpdate();
                await Task.Delay(TimeSpan.FromHours(1.0));                
            }
        }
    }
}
