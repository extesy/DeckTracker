using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using DeckTracker.LowLevel;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeckTracker.Domain
{
    internal static class ArchetypeManager
    {
        private static readonly string ArchetypesFile = Path.Combine(Logger.GameDataDirectory, "archetypes.json");
        private static Dictionary<GameType, Dictionary<string, Dictionary<int, object>>> allArchetypes = new Dictionary<GameType, Dictionary<string, Dictionary<int, object>>>();
        private static Thread thread;

        public static Dictionary<string, string> IdToName = new Dictionary<string, string>();
        public static Dictionary<string, string[]> IdToWords = new Dictionary<string, string[]>();
//        public static Dictionary<string, string[]> NameToIds = new Dictionary<string, string[]>();
//        public static Dictionary<string, string[]> WordToIds = new Dictionary<string, string[]>();

        public static void Initialize()
        {
            LoadArchetypes();
            ProcessMonitor.OnGameInjectionStateChange += (gameType, injectionState) => {
                if (injectionState == InjectionState.Injected && thread == null) {
                    thread = new Thread(RefreshArchetypes) {Name = "ArchetypeManager", Priority = ThreadPriority.Lowest};
                    thread.Start(gameType);
                }
            };
        }

        public static void Stop()
        {
            if (thread != null) {
                thread.Abort();
                while (thread?.IsAlive == true) Thread.Sleep(10);
                thread = null;
            }
        }

        private static int? GetCardNumber(GameType gameType, IReadOnlyDictionary<int, object> attrs)
        {
            switch (gameType) {
                case GameType.Eternal:
                    return Eternal.Helpers.GetCardNumber(attrs);
                case GameType.TheElderScrollsLegends:
                    return TheElderScrollsLegends.Helpers.GetCardNumber(attrs);
            }
            return null;
        }

        private static string GetBaseArchetypeId(GameType gameType, string archetypeId)
        {
            var card = allArchetypes[gameType][archetypeId];
            object isPremium;
            switch (gameType) {
                case GameType.Eternal:
                    return card.TryGetValue((int)Eternal.Attribute.IsPremium, out isPremium) && (bool)isPremium ? (string)card[(int)Eternal.Attribute.ArchID] : archetypeId;
                case GameType.TheElderScrollsLegends:
                    return card.TryGetValue((int)TheElderScrollsLegends.Attribute.IsPremium, out isPremium) && (bool)isPremium ? (string)card[(int)TheElderScrollsLegends.Attribute.Parent] : archetypeId;
            }
            return null;
        }

        public static string GetExportedDeck(GameType gameType, IEnumerable<string> cardIds) => GetExportedDeck(gameType, cardIds.GroupBy(id => id).ToDictionary(key => key.Key, value => value.Count()));

        public static string GetExportedDeck(GameType gameType, Dictionary<string, int> cardCounts)
        {
            if (!allArchetypes.ContainsKey(gameType)) return null;
            var totalCardCounts = cardCounts.GroupBy(kv => GetBaseArchetypeId(gameType, kv.Key)).ToDictionary(g => g.Key, g => g.Select(kv => kv.Value).Sum());
            var export = new StringBuilder();
            foreach (var entry in totalCardCounts) {
                var attrs = allArchetypes[gameType][entry.Key];
                int count = entry.Value;
                switch (gameType) {
                    case GameType.Eternal:
                        export.AppendLine($"{count} {attrs[(int)Eternal.Attribute.Name]} (Set{attrs[(int)Eternal.Attribute.SetNumber]} #{GetCardNumber(gameType, attrs)})");
                        break;
                    case GameType.TheElderScrollsLegends:
                        export.AppendLine($"{count} {attrs[(int)TheElderScrollsLegends.Attribute.Name]} (Set{attrs[(int)TheElderScrollsLegends.Attribute.SetNumber]} #{GetCardNumber(gameType, attrs)})");
                        break;
                }
            }
            return export.ToString();
        }

        public static string DownloadDeck(string url)
        {
            if (url.Contains("legends-decks.com")) {
                var page = new HtmlWeb().Load(url).DocumentNode;
                return page.SelectSingleNode("//div[@id='deckModal']//div[@class='well_full']").InnerHtml.Replace("<br>", "\r\n").Trim();
            }
            if (url.Contains("eternalwarcry.com")) {
                var page = new HtmlWeb().Load(url).DocumentNode;
                var name = page.SelectSingleNode("//h1").InnerText.Trim();
                var cards = page.SelectSingleNode("//textarea[@id='export-deck-text']").InnerHtml.Trim();
                return $"### {name} ###\r\n{cards}";
            }
            if (url.Contains("teslegends.pro")) {
                var slug = url.Substring(url.TrimEnd('/').LastIndexOf('/')).Trim('/');
                var response = new WebClient().UploadValues("https://teslegends.pro/dc/do.php", "POST", new NameValueCollection {{"exportdeck", slug}});
                return Encoding.UTF8.GetString(response);
            }
            return null;
        }

        public static Dictionary<string, int> GetCollection(GameType gameType)
        {
            string collectionJson = ProcessMonitor.SendCommand(gameType, CommandType.Collection, "Json", 5000);
            if (collectionJson != null && !collectionJson.StartsWith("{"))
                throw new Exception(collectionJson);
            return collectionJson == null ? null : JsonConvert.DeserializeObject<Dictionary<string, int>>(collectionJson);
        }

        public static bool ImportDeck(GameType gameType, string deck)
        {
            var collection = GetCollection(gameType);
            var lines = deck.Split('\n').Select(line => line.Trim().Replace("&#39;", "'").Replace('`', '\''));
            string deckName = null;
            var cardIds = new List<string>();
            var colors = new HashSet<string>();
            foreach (string line in lines) {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.StartsWith("#")) {
                    deckName = line.Trim('#').Trim();
                    continue;
                }
                int firstSpace = line.IndexOf(' ');
                int paren1 = line.IndexOf('(');
                int hash = line.IndexOf('#');
                int paren2 = line.IndexOf(')');
                if (firstSpace < 0 || (paren1 >= 0 && (hash < 0 || paren2 < 0))) throw new Exception($"Invalid line format: {line}");

                int count = int.Parse(line.Substring(0, firstSpace));
                string cardName = line.Substring(firstSpace, (paren1 < 0 ? line.Length : paren1) - firstSpace).Trim();
                int? cardSet = null, cardNumber = null;
                if (hash > 0 && paren2 > 0) {
                    if (!int.TryParse(line.Substring(paren1 + 4, hash - paren1 - 5).Trim(), out int parsedCardSet)) throw new Exception($"Invalid line format: {line}");
                    cardSet = parsedCardSet;
                    if (!int.TryParse(line.Substring(hash + 1, paren2 - hash - 1).Trim(), out int parsedCardNumber)) throw new Exception($"Invalid line format: {line}");
                    cardNumber = parsedCardNumber;
                }

                int nameAttributeId = gameType == GameType.Eternal ? (int)Eternal.Attribute.Name : (int)TheElderScrollsLegends.Attribute.Name;
                int isPremiumAttributeId = gameType == GameType.Eternal ? (int)Eternal.Attribute.IsPremium : (int)TheElderScrollsLegends.Attribute.IsPremium;
                int rarityAttributeId = gameType == GameType.Eternal ? (int)Eternal.Attribute.Rarity : (int)TheElderScrollsLegends.Attribute.HydraRarity;
                int cardTypeAttributeId = gameType == GameType.Eternal ? (int)Eternal.Attribute.CardType : (int)TheElderScrollsLegends.Attribute.HydraCardType;

                bool CheckCardNumber(IReadOnlyDictionary<int, object> attrs) => cardNumber != null && cardNumber == GetCardNumber(gameType, attrs) &&
                    cardSet == int.Parse(attrs[gameType == GameType.Eternal ? (int)Eternal.Attribute.SetNumber : (int)TheElderScrollsLegends.Attribute.SetNumber].ToString());

                bool CheckCardType(IReadOnlyDictionary<int, object> attrs) =>
                    (attrs.TryGetValue(rarityAttributeId, out var rarity) && (string)rarity != "Special" || attrs.TryGetValue(cardTypeAttributeId, out var cardType) && (string)cardType == "Power") &&
                    (gameType != GameType.TheElderScrollsLegends || !(bool)attrs[(int)TheElderScrollsLegends.Attribute.HydraHiddenFromDeckbuilder]);

                var matchingNames = allArchetypes[gameType].Where(a => CheckCardNumber(a.Value) || a.Value.ContainsKey(nameAttributeId) && a.Value[nameAttributeId].ToString() == cardName).ToList();
                string baseArchetypeId = matchingNames.Where(a => CheckCardType(a.Value) && !(a.Value.ContainsKey(isPremiumAttributeId) && (bool)a.Value[isPremiumAttributeId])).Select(entry => entry.Key).FirstOrDefault();
                string premiumArchetypeId = matchingNames.Where(a => CheckCardType(a.Value) && a.Value.ContainsKey(isPremiumAttributeId) && (bool)a.Value[isPremiumAttributeId]).Select(entry => entry.Key).FirstOrDefault();
                if (baseArchetypeId == null) throw new Exception($"Unknown card name: {cardName}");

                if (gameType == GameType.TheElderScrollsLegends && allArchetypes[gameType][baseArchetypeId].ContainsKey((int)TheElderScrollsLegends.Attribute.HydraColorList))
                    foreach (string color in (JArray)allArchetypes[gameType][baseArchetypeId][(int)TheElderScrollsLegends.Attribute.HydraColorList])
                        colors.Add(color);
                if (premiumArchetypeId != null && collection.ContainsKey(premiumArchetypeId)) {
                    for (var i = 0; i < collection[premiumArchetypeId] && count > 0; i++, count--)
                        cardIds.Add(premiumArchetypeId);
                }
                if (collection.ContainsKey(baseArchetypeId)) {
                    for (var i = 0; i < collection[baseArchetypeId] && count > 0; i++, count--)
                        cardIds.Add(baseArchetypeId);
                }
            }
            colors.Remove("Neutral");
            if (colors.Count > 2) throw new Exception($"Too many card colors in deck: {colors.Count}");
            string cardsList = string.Join(",", cardIds);
            string message = deckName != null ? $"{deckName}|{cardsList}" : cardsList;
            return ProcessMonitor.SendCommand(gameType, CommandType.ImportDeck, message, 1000) == "Done";
        }

        private static void LoadArchetypes()
        {
            if (!File.Exists(ArchetypesFile)) return;
            allArchetypes = JsonConvert.DeserializeObject<Dictionary<GameType, Dictionary<string, Dictionary<int, object>>>>(File.ReadAllText(ArchetypesFile));
            var names = new Dictionary<string, string>();
            foreach (var archetypes in allArchetypes.Values) {
                foreach (var archetype in archetypes) {
                    if (!archetype.Value.ContainsKey((int)Eternal.Attribute.Name)) continue;
                    names[archetype.Key] = RemoveHtmlTags((string)archetype.Value[(int)Eternal.Attribute.Name]);
                }
            }
            IdToName = names;
            IdToWords = names.ToDictionary(a => a.Key, a => Wordize(a.Value));
//            NameToIds = IdToName.ToLookup(a => a.Value, a => a.Key).ToDictionary(a => a.Key, a => a.ToArray());
//            WordToIds = IdToName.SelectMany(a => Wordize(a.Value).Select(w => new KeyValuePair<string, string>(a.Key, w))).ToLookup(a => a.Value, a => a.Key).ToDictionary(a => a.Key, a => a.ToArray());
        }

        private static void RefreshArchetypes(object context)
        {
            var gameType = (GameType)context;
            try {
                Dictionary<string, Dictionary<int, object>> archetypes = null;
                while (archetypes == null) {
                    Thread.Sleep(30000);
                    string response = ProcessMonitor.SendCommand(gameType, CommandType.Archetypes, 10000);
                    if (response == null) continue;
                    archetypes = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<int, object>>>(response);
                }
                if (!allArchetypes.ContainsKey(gameType))
                    allArchetypes[gameType] = new Dictionary<string, Dictionary<int, object>>();
                foreach (var archetype in archetypes) {
                    if (!allArchetypes[gameType].ContainsKey(archetype.Key)) {
                        allArchetypes[gameType][archetype.Key] = archetype.Value;
                    } else {
                        foreach (var attribute in archetype.Value)
                            allArchetypes[gameType][archetype.Key][attribute.Key] = attribute.Value;
                    }
                }
                File.WriteAllText(ArchetypesFile, JsonConvert.SerializeObject(allArchetypes));
                LoadArchetypes();
            } catch (ThreadAbortException) {
            } catch (Exception e) {
                Logger.LogError(e.ToString());
            } finally {
                thread = null;
            }
        }

        private static string RemoveHtmlTags(string line)
        {
            return Regex.Replace(line.Replace("\n", " "), "<.*?>", string.Empty);
        }

        private static string[] Wordize(string line)
        {
            return RemoveHtmlTags(line.Replace(",", "").Replace("-", "").Replace("'", "")).Split(' ');
        }
    }
}
