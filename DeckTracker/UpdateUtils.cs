using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Squirrel;
using System.Reflection;
using DeckTracker.LowLevel;

namespace DeckTracker
{
    class UpdateUtils
    {
        private static readonly string UpdateUrl = "https://github.com/extesy/DeckTracker";        

        public delegate void OnNewVersionHandler (string newVersion);
        public static event OnNewVersionHandler OnNewVersion;   

        private static async void CheckForUpdate()
        {
            try
            {
                UpdateInfo updInfo = null;
                string currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
                string newVersion = currentVersion;
                using (var updateManager = await UpdateManager.GitHubUpdateManager(UpdateUrl))
                {
                    updInfo = await updateManager.CheckForUpdate();
                    newVersion = updInfo.FutureReleaseEntry.Version.ToString();
                    if (!newVersion.Equals(currentVersion))
                    {
                        Logger.LogDebug(Domain.GameType.Eternal, "started update to version " + newVersion);                        
                        await updateManager.UpdateApp();
                        Logger.LogDebug(Domain.GameType.Eternal, "updated to version " + newVersion);
                        OnNewVersion?.Invoke(newVersion);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }
        }

        public static async void StartUpdateCheck()
        {
            while (true)
            {
                CheckForUpdate();
                await Task.Delay(TimeSpan.FromHours(1.0));                
            }
        }
    }
}
