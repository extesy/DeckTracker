using System;
using System.Linq;
using System.Threading;
using System.Windows;
using DeckTracker.Domain;
using DeckTracker.LowLevel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
#if !DEBUG
using Squirrel;
#endif

namespace DeckTracker
{
    public partial class App
    {
        private Mutex mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            if (e.Args.Contains("--debug"))
                Logger.DebugMode = true;
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

            mutex = new Mutex(true, "Local\\DeckTracker", out bool isNew);
            if (!isNew) {
                MessageBox.Show("Universal Deck Tracker is already running...");
                Shutdown();
            }

            JsonConvert.DefaultSettings = () => {
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new StringEnumConverter());
                return settings;
            };

            Logger.Initialize();
            ConfigManager.Initialize();
            ReplayUploader.Initialize();
            ArchetypeManager.Initialize();
            try {
                DeckClassifier.Initialize();
            } catch (Exception exception) {
                MessageBox.Show(exception.Message);
                Shutdown();
            }
            GameMessageDispatcher.Start();
            ProcessMonitor.Start();
#if !DEBUG
            Update();
#endif
        }

        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;
            Logger.LogError(ex.ToString());
            MessageBox.Show($"Something went wrong...\n{ex.Message}", "Error");
            Environment.Exit(1);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ArchetypeManager.Stop();
            ReplayUploader.Stop();
            ProcessMonitor.Stop();
            GameMessageDispatcher.Stop();
            mutex.Dispose();
            base.OnExit(e);
        }

#if !DEBUG
        private static async void Update()
        {
            try {
                using (var updateManager = await UpdateManager.GitHubUpdateManager("https://github.com/extesy/DeckTracker"))
                    await updateManager.UpdateApp();
            } catch {
            }
        }
#endif
    }
}
