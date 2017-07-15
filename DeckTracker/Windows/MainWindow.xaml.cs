using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using DeckTracker.Domain;
using DeckTracker.Domain.Eternal.Extensions;
using DeckTracker.Domain.TheElderScrollsLegends.Extensions;
using DeckTracker.LowLevel;
using JetBrains.Annotations;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet;

namespace DeckTracker.Windows
{
    public class LevelConverter : DependencyObject, IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var level = (int)values[0];
            var indent = (double)values[1];
            return indent * level;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class MainWindow
    {
        private static readonly Dictionary<InjectionState, Color> InjectionStateColors = new Dictionary<InjectionState, Color> {
            {InjectionState.Idle, Colors.Black},
            {InjectionState.Injecting, Colors.Blue},
            {InjectionState.Injected, Colors.Green},
            {InjectionState.Disconnected, Colors.Yellow},
            {InjectionState.Failed, Colors.Red}
        };

        private class MainWindowViewModel : INotifyPropertyChanged
        {
            public ObservableCollection<Game> Games { get; } = new ObservableCollection<Game>();
            public ObservableCollection<GameMode> GameModes { get; } = new ObservableCollection<GameMode>();
            public ObservableSortedList<Deck> Decks { get; } = new ObservableSortedList<Deck>(Deck.OrderByLastPlayed);
            public ObservableSortedList<Deck> FilteredDecks { get; } = new ObservableSortedList<Deck>(Deck.OrderByLastPlayed);
            public Deck SelectedDeck { get; set; }
            public bool HasGameModes => !GameModes.IsEmpty();

            public string WinRate {
                get {
                    if (FilteredDecks.Count == 0) return "";
                    int played = FilteredDecks.Select(deck => deck.Stats[0].GamesPlayed).Sum();
                    int won = FilteredDecks.Select(deck => deck.Stats[0].GamesWon).Sum();
                    return "Total Win Rate: " + Math.Round((double)won / played * 100) + "% of " + played;
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            [NotifyPropertyChangedInvocator]
            public void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private readonly MainWindowViewModel model = new MainWindowViewModel();

        private void OnGameMessage(GameMessage gameMessage)
        {
            try {
                switch (gameMessage.MessageType) {
                    case MessageType.Game:
                        OnGameCompleted(gameMessage);
                        break;
                    case MessageType.Decks:
                        OnDecksPlayed(gameMessage);
                        break;
                    default:
                        return;
                }
            } catch (Exception e) {
                Logger.LogError(e.ToString());
            }
        }

        private static Deck UnpackDeck(JToken deckData, GameMessage gameMessage)
        {
            return new Deck {
                GameType = gameMessage.GameType,
                Id = deckData["id"]?.Value<string>(),
                Name = deckData["name"]?.Value<string>(),
                Tags = deckData["tags"]?.Values<string>().ToArray(),
                Cards = deckData["cards"]?.Values<string>().ToArray(),
                Colors = deckData["colors"]?.Values<string>().ToArray(),
                LastPlayed = gameMessage.Timestamp
            };
        }

        private void OnGameCompleted(GameMessage gameMessage)
        {
            var data = JsonConvert.DeserializeObject<JObject>(gameMessage.Message);
            var game = new Game {
                GameType = gameMessage.GameType,
                Id = data["id"].Value<string>(),
                Type = data["type"].Value<string>(),
                Mode = data["mode"]?.Value<string>(),
                Win = data["result"].Value<string>() == "Win",
                PlayerDeck = UnpackDeck(data["player"]["deck"], gameMessage),
                OpponentDeck = UnpackDeck(data["opponent"]["deck"], gameMessage),
                Start = data["startTime"] != null ? DateTimeOffset.FromUnixTimeMilliseconds(data["startTime"].Value<long>()).DateTime : gameMessage.Timestamp,
                End = data["endTime"] != null ? DateTimeOffset.FromUnixTimeMilliseconds(data["endTime"].Value<long>()).DateTime : DateTime.MaxValue
            };
            game.OpponentDeck.Name = data["opponent"]["name"]?.Value<string>();
            model.Games.Add(game);

            InferDeckMode(game.GameType, game.PlayerDeck, ref game.Mode, game.Type);
            if (game.Mode == null || game.PlayerDeck.Id == null)
                return;

            if (model.GameModes.All(mode => mode.Name != game.Mode))
                model.GameModes.Add(new GameMode {IsEnabled = true, Name = game.Mode});

            var deck = model.Decks.FirstOrDefault(d => d.Id == game.PlayerDeck.Id);
            if (deck == null) {
                deck = game.PlayerDeck;
                model.Decks.Add(deck);
                if (model.GameModes.Any(m => m.Name == game.Mode && m.IsEnabled)) {
                    model.FilteredDecks.Add(deck);
                    model.OnPropertyChanged(nameof(model.WinRate));
                }
            } else {
                deck.Name = game.PlayerDeck.Name;
                deck.Tags = game.PlayerDeck.Tags;
                deck.Cards = game.PlayerDeck.Cards;
                deck.Colors = game.PlayerDeck.Colors;
                deck.LastPlayed = game.PlayerDeck.LastPlayed;
            }

            deck.Classification = deck.Id == "Draft" || deck.Id == "Forge" ? deck.Id : ClassifyDeck(deck).Name;
            var opponentDeck = ClassifyDeck(game.OpponentDeck);
//            var stack = new Stack<AgainstDeck>();
            while (opponentDeck != null) {
                if (!opponentDeck.Name.StartsWith("$")/* && opponentDeck.Level <= 2*/) {
                    var againstDeck = deck.Stats.FirstOrDefault(d => d.Classification == opponentDeck.Name);
                    if (againstDeck == null) {
                        againstDeck = new AgainstDeck {Classification = opponentDeck.Name};
                        deck.Stats.Add(againstDeck);
                    }
                    againstDeck.Games.Add(game);
                    if (!againstDeck.GamesPlayedByMode.ContainsKey(game.Mode)) {
                        againstDeck.GamesPlayedByMode[game.Mode] = 0;
                        againstDeck.GamesWonByMode[game.Mode] = 0;
                    }
                    againstDeck.GamesPlayedByMode[game.Mode]++;
                    if (game.Win) againstDeck.GamesWonByMode[game.Mode]++;
                    if (model.GameModes.Any(m => m.Name == game.Mode && m.IsEnabled)) {
                        againstDeck.GamesPlayed++;
                        if (game.Win) againstDeck.GamesWon++;
                    }
//                    if (!stack.IsEmpty())
//                        stack.Peek().Parent = againstDeck;
//                    stack.Push(againstDeck);
                }
                opponentDeck = opponentDeck.Parent;
            }
//            while (!stack.IsEmpty()) {
//                var currentDeck = stack.Pop();
//                var againstDeck = deck.Stats.FirstOrDefault(d => d.Classification == currentDeck.Classification);
//                againstDeck?.Parent?.Children.Add(currentDeck);
//            }
            deck.OnPropertyChanged(nameof(deck.Stats));
        }

        private static void InferDeckMode(GameType gameType, Deck deck, ref string mode, string type)
        {
            if (mode == null) {
                if (type == "Versus") mode = "Versus";
                if (type == "Solitaire") mode = gameType == GameType.Eternal ? "Survival" : "HydraSolitaire";
                if (deck.Tags?.Contains("LastDraftDeck") == true || deck.Tags?.Contains("Draft") == true) mode = "Draft";
                if (deck.Tags?.Contains("LastForgeDeck") == true || deck.Tags?.Contains("Forge") == true) mode = "Forge";
                if (deck.Tags?.Contains("SoloArena") == true) mode = "HydraDraft";
                if (deck.Tags?.Contains("VersusArena") == true) mode = "HydraConquest";
                if (deck.Tags?.Contains("ChaosArena") == true) mode = "HydraChaosHandler";
                if (mode == null) return;
            }

            string friendlyName = gameType == GameType.Eternal
                ? Domain.Eternal.Helpers.GameModeFromType(mode).FriendlyName()
                : Domain.TheElderScrollsLegends.Helpers.GameModeFromType(mode).FriendlyName();

            if (mode == "Draft" || mode == "Forge" || mode == "HydraDraft" || mode == "HydraConquest" || mode == "HydraChaosHandler")
                deck.Id = friendlyName;
            if (deck.Id == null) return;

            mode = friendlyName;
            mode = $"{gameType} - {mode}";

            if (string.IsNullOrWhiteSpace(deck.Name))
                deck.Name = $"{deck.Id} Deck";
        }

        private void OnDecksPlayed(GameMessage gameMessage)
        {
            var data = JsonConvert.DeserializeObject<JObject>(gameMessage.Message);
            var playerDeck = UnpackDeck(data["player"], gameMessage);
            var opponentDeck = UnpackDeck(data["opponent"], gameMessage);
            var mode = data["mode"]?.Value<string>();
            InferDeckMode(gameMessage.GameType, playerDeck, ref mode, null);
            var deck = model.Decks.FirstOrDefault(d => d.Id == playerDeck.Id);
            if (deck == null) return;
            var result = new Dictionary<string, object> {
                {"totalWinRate", deck.Stats[0].WinRateByMode(mode)}
            };
            var classification = ClassifyDeck(opponentDeck);
            if (classification.Parent != null) {
                result["againstClass"] = classification.Name;
                var againstDeck = deck.Stats.FirstOrDefault(stat => stat.Classification == classification.Name);
                if (againstDeck != null)
                    result["againstWinRate"] = againstDeck.WinRateByMode(mode);
            }
            ProcessMonitor.SendCommand(gameMessage.GameType, CommandType.DeckStats, JsonConvert.SerializeObject(result));
        }

        private static DeckClassifier.DeckDefinition ClassifyDeck(Deck deck)
        {
            var attrs = new DeckClassifier.DeckAttributes {
                GameType = deck.GameType.ToString(),
                Colors = deck.Colors?.GroupBy(c => c).ToDictionary(g => g.Key, g => g.Count()),
                Cards = deck.Cards.GroupBy(c => c).ToDictionary(g => ArchetypeManager.IdToName.ContainsKey(g.Key) ? ArchetypeManager.IdToName[g.Key] : g.Key, g => g.Count()),
                Words = deck.Cards.SelectMany(c => ArchetypeManager.IdToWords.ContainsKey(c) ? ArchetypeManager.IdToWords[c] : Enumerable.Empty<string>()).GroupBy(c => c).ToDictionary(g => g.Key, g => g.Count())
            };
            return DeckClassifier.ClassifyDeck(attrs);
        }

        private void LoadSavedGames()
        {
            if (!File.Exists(Logger.GamesFile)) return;
            using (var streamReader = new StreamReader(Logger.GamesFile)) {
                string line;
                while ((line = streamReader.ReadLine()) != null) {
                    var parts = line.Split(new[] {'|'}, 3);
                    OnGameMessage(new GameMessage {
                        Timestamp = DateTime.Parse(parts[0]),
                        GameType = (GameType)Enum.Parse(typeof(GameType), parts[1]),
                        MessageType = MessageType.Game,
                        Message = parts[2]
                    });
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            Title += " v" + Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
            DataContext = model;
            GameModeFilter.DataContext = model;
            DecksListView.DataContext = model;
            model.PropertyChanged += (sender, args) => {
                if (args.PropertyName == nameof(model.SelectedDeck))
                    AgainstListView.DataContext = model.SelectedDeck?.Stats;
            };
            LoadSavedGames();
            model.SelectedDeck = model.FilteredDecks.FirstOrDefault();
            //GameModeFilter.Visibility = model.GameModes.IsEmpty() ? Visibility.Hidden : Visibility.Visible;

            GameMessageDispatcher.OnGameMessage += gameMessage => Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => OnGameMessage(gameMessage)));

            ProcessMonitor.OnGameInjectionStateChange += (gameType, injectionState) => {
                InjectionStateLabel.Content = "Running game detected: " + (injectionState == InjectionState.Idle ? "None" : gameType.ToString());
                var color = InjectionStateColors[injectionState];
                if ((InjectionStateLabel.Foreground as SolidColorBrush)?.Color != color)
                    InjectionStateLabel.Foreground = new SolidColorBrush(color);
                ExportCollectionButton.IsEnabled = injectionState == InjectionState.Injected;
                ImportDeckButton.IsEnabled = injectionState == InjectionState.Injected;
            };
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            var presentationsource = PresentationSource.FromVisual(this);
            WindowsHelper.DpiScalingX = presentationsource?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            WindowsHelper.DpiScalingY = presentationsource?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
        }

        private void DecksListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var listView = sender as ListView;
            var gridView = listView.View as GridView;
            var actualWidth = listView.ActualWidth - SystemParameters.VerticalScrollBarWidth;
            for (var i = 1; i < gridView.Columns.Count; i++)
                actualWidth = actualWidth - gridView.Columns[i].ActualWidth;
            gridView.Columns[0].Width = Math.Max(actualWidth, 150);
        }

        private void GameModeFilter_Checked(object sender, RoutedEventArgs e)
        {
            var mode = (GameMode)((CheckBox)e.Source).DataContext;
            if (mode.IsEnabled) {
                var newDecks = model.Decks.Where(deck => !model.FilteredDecks.Contains(deck) && deck.Stats.Any(againstDeck => againstDeck.GamesPlayedByMode.ContainsKey(mode.Name)));
                model.FilteredDecks.AddRange(newDecks);
                foreach (var deck in model.FilteredDecks)
                foreach (var againstDeck in deck.Stats.ToArray()) {
                    if (!againstDeck.GamesPlayedByMode.ContainsKey(mode.Name)) continue;
                    againstDeck.GamesPlayed += againstDeck.GamesPlayedByMode[mode.Name];
                    againstDeck.GamesWon += againstDeck.GamesWonByMode[mode.Name];
                }
                if (DecksListView.SelectedItem == null)
                    DecksListView.SelectedItem = DecksListView.Items[0];
            } else {
                foreach (var deck in model.FilteredDecks)
                foreach (var againstDeck in deck.Stats.ToArray()) {
                    if (!againstDeck.GamesPlayedByMode.ContainsKey(mode.Name)) continue;
                    againstDeck.GamesPlayed -= againstDeck.GamesPlayedByMode[mode.Name];
                    againstDeck.GamesWon -= againstDeck.GamesWonByMode[mode.Name];
                }
                var currentDeck = model.SelectedDeck;
                model.FilteredDecks.RemoveAll(deck => deck.Stats.All(againstDeck => againstDeck.GamesPlayed == 0));
                if (!model.FilteredDecks.Contains(currentDeck))
                    DecksListView.SelectedItem = DecksListView.Items.IsEmpty ? null : DecksListView.Items[0];
            }
            model.OnPropertyChanged(nameof(model.WinRate));
            e.Handled = true;
        }

        private void ExportCollectionButton_OnClick(object sender, RoutedEventArgs e)
        {
            try {
                var game = ProcessMonitor.RunningGameType;
                if (!game.HasValue) return;
                var collection = ArchetypeManager.GetCollection(game.Value);
                if (collection != null) {
                    string export = ArchetypeManager.GetExportedDeck(game.Value, collection);
                    if (WindowsHelper.TryCopyToClipboard(export, out string blockingWindowText))
                        MessageBox.Show("Collection exported to clipboard", "Success");
                    else
                        MessageBox.Show(blockingWindowText != null ? $"Unable to access clipboard.\nPlease close this window: {blockingWindowText}" : "Unable to access clipboard.", "Error");
                } else
                    MessageBox.Show("Something went wrong...", "Error");
            } catch (Exception ex) {
                MessageBox.Show($"Something went wrong...\n\n{ex.Message}", "Error");
            }
        }

        private void DeleteDeckMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var deck = (((sender as MenuItem)?.CommandParameter as ContextMenu)?.PlacementTarget as ListViewItem)?.DataContext as Deck;
            if (deck == null) return;
            var response = MessageBox.Show("Are you sure you want to delete this deck?", "Delete deck", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (response != MessageBoxResult.Yes) return;
            model.Decks.Remove(deck);
            model.FilteredDecks.Remove(deck);
            model.OnPropertyChanged(nameof(model.WinRate));
            DecksListView.SelectedItem = DecksListView.Items.IsEmpty ? null : DecksListView.Items[0];
            using (var streamReader = new StreamReader(Logger.GamesFile))
            using (var streamWriter = new StreamWriter(Logger.GamesFile + ".new")) {
                string line;
                while ((line = streamReader.ReadLine()) != null) {
                    if (!line.Contains(deck.Id))
                        streamWriter.WriteLine(line);
                }
            }
            if (File.Exists(Logger.GamesFile + ".bak"))
                File.Delete(Logger.GamesFile + ".bak");
            File.Move(Logger.GamesFile, Logger.GamesFile + ".bak");
            File.Move(Logger.GamesFile + ".new", Logger.GamesFile);
            File.Delete(Logger.GamesFile + ".bak");
        }

        private void ExportPlayerDeckMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            try {
                var deck = (((sender as MenuItem)?.CommandParameter as ContextMenu)?.PlacementTarget as ListViewItem)?.DataContext as Deck;
                if (deck == null) return;
                var export = new StringBuilder();
                export.AppendLine($"### {deck.Name} ###");
                export.Append(ArchetypeManager.GetExportedDeck(deck.GameType, deck.Cards));
                if (WindowsHelper.TryCopyToClipboard(export.ToString(), out string blockingWindowText))
                    MessageBox.Show("Last played deck has been exported to clipboard", "Success");
                else
                    MessageBox.Show(blockingWindowText != null ? $"Unable to access clipboard.\nPlease close this window: {blockingWindowText}" : "Unable to access clipboard.", "Error");
            } catch (Exception ex) {
                MessageBox.Show($"Something went wrong...\n{ex.Message}", "Error");
            }
        }

        private void ExportOpponentDecksMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            try {
                var deck = DecksListView.SelectedItem as Deck;
                var againstDeck = (((sender as MenuItem)?.CommandParameter as ContextMenu)?.PlacementTarget as ListViewItem)?.DataContext as AgainstDeck;
                if (deck == null || againstDeck == null) return;
                var export = new StringBuilder();
                int count = 0;
                foreach (var game in againstDeck.Games) {
                    if (!model.GameModes.First(mode => mode.Name == game.Mode).IsEnabled) continue;
                    if (export.Length > 0) export.AppendLine();
                    export.AppendLine($"### {game.OpponentDeck.Name} ### {game.Start:F} ###");
                    export.Append(ArchetypeManager.GetExportedDeck(game.GameType, game.OpponentDeck.Cards));
                    count++;
                }
                if (WindowsHelper.TryCopyToClipboard(export.ToString(), out string blockingWindowText))
                    MessageBox.Show($"{count} opponent decks have been exported to clipboard", "Success");
                else
                    MessageBox.Show(blockingWindowText != null ? $"Unable to access clipboard.\nPlease close this window: {blockingWindowText}" : "Unable to access clipboard.", "Error");
            } catch (Exception ex) {
                MessageBox.Show($"Something went wrong...\n{ex.Message}", "Error");
            }
        }

        private async void ImportDeckButton_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new ImportDeckDialog();
            await this.ShowMetroDialogAsync(dialog);
            string deckList = await dialog.WaitForButtonPressAsync();
            await this.HideMetroDialogAsync(dialog);
            var game = ProcessMonitor.RunningGameType;
            if (deckList == null || !game.HasValue) return;
            try {
                bool isUrl = Uri.TryCreate(deckList, UriKind.Absolute, out Uri uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                if (isUrl) {
                    deckList = ArchetypeManager.DownloadDeck(deckList);
                    if (deckList == null) {
                        MessageBox.Show("Unable to download deck list", "Error");
                        return;
                    }
                }
                if (ArchetypeManager.ImportDeck(game.Value, deckList))
                    MessageBox.Show("Deck has been imported", "Success");
                else
                    MessageBox.Show("Something went wrong... Deck has not been imported", "Error");
            } catch (Exception ex) {
                MessageBox.Show($"Something went wrong...\n{ex.Message}", "Error");
            }
        }

        private void ReportBugs_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/extesy/decktracker/issues"));
        }
    }
}
