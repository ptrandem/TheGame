using System.IO;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using TheGame.Enums;

namespace TheGame
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly object LogfileLock = new object();
        private static readonly object LeaderboardLock = new object();

        public string ApiKey = "4a0da0c3-f9c3-4ebf-95ae-4905f5fb564a";
        public string AssistUser = "ptrandem";
        public string Self = "ptrandem";



        public string ItemPath
        {
            get
            {
                return $"c:\\temp\\{Self}_items.txt";
            }
        }

        public string LogPath
        {
            get
            {
                return $"c:\\temp\\{DateTime.Now.ToString("yyyyMMdd")}_{Self}_log.txt";
            }
        }

        public string ErrorLogPath
        {
            get
            {
                return $"c:\\temp\\{DateTime.Now.ToString("yyyyMMdd")}_{Self}_errors.txt";
            }
        }

        private readonly RestClient _client = new RestClient("http://thegame.nerderylabs.com:1337");
        private readonly System.Timers.Timer _pointsTimer = new System.Timers.Timer(1001);
        private readonly System.Timers.Timer _itemsTimer = new System.Timers.Timer(61000);
        private readonly System.Timers.Timer _intermediateTimer = new System.Timers.Timer(10100);
        private readonly List<PlayerInfo> _players = new List<PlayerInfo>();
        private readonly PriorityQueue<ItemUsage> _itemQueue = new PriorityQueue<ItemUsage>();

        protected ObservableCollection<ItemFields> Items { get; set; }
        protected ObservableCollection<string> SelfEffects { get; set; }
        protected ObservableCollection<string> AssistUserEffects { get; set; }
        protected ObservableCollection<PlayerInfo> Players { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

            _pointsTimer.Elapsed += _timer_Elapsed;
            _itemsTimer.Elapsed += _itemsTimer_Elapsed;
            _intermediateTimer.Elapsed += _intermediateTimer_Elapsed;

            Items = new ObservableCollection<ItemFields>();
            Players = new ObservableCollection<PlayerInfo>();
            SelfEffects = new ObservableCollection<string>();
            AssistUserEffects = new ObservableCollection<string>();

            LeaderboardDatagrid.AutoGeneratingColumn += LeaderboardDatagrid_AutoGeneratingColumn;
            ItemsGrid.ItemsSource = Items;
            LeaderboardDatagrid.ItemsSource = Players;

            GetLeaderboard();

            LoadItemCollection();

            //_itemsTimer.Start();
            //_intermediateTimer.Start();

        }

        void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            File.AppendAllText(ErrorLogPath, $"{e.Exception.Message}\n");
            File.AppendAllText(ErrorLogPath, $"{e.Exception.StackTrace}\n\n");

        }

        private void LeaderboardDatagrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "Effects")
            {
                e.Cancel = true;
            }

            if (e.PropertyName == "EffectsString")
            {
                e.Column.Header = "Effects";
            }
        }

        private void _intermediateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _intermediateTimer.Stop();

            GetLeaderboard();
            ApplyQueueRules();

            _intermediateTimer.Start();
        }


        private void _itemsTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _itemsTimer.Stop();
            UseNextItem();
            _itemsTimer.Start();
        }

        private void ApplyQueueRules()
        {
            var twinkee = GetPlayerByType(PlayerType.Twinkee);
            //var self = GetPlayerByType(PlayerType.Self);

            if (SelfEffects.Contains("Vampirism"))
            {
                _itemsTimer.Interval = 5000;
                //_intermediateTimer.Interval = 10000;
                if (DateTime.Now.Hour <= 6 || DateTime.Now.Hour >= 21)
                {
                    return; // Use no items during the Forbidden Time
                }
            }
            else
            {
                _itemsTimer.Interval = 60001;
                //_intermediateTimer.Interval = 20000;
            }
            if (SelfEffects.Contains("Slow"))
            {
                EnqueueEffectIfMissing("Mushroom", PlayerType.Self, 2);
            }
            else
            {
                // Speeds
                EnqueueEffectIfNoneEquipped(new[] { "7777", "Warthog", "Moogle" }, PlayerType.Twinkee, 0);


                if (twinkee != null)
                {
                    if (twinkee.Effects.Contains("Warthog"))
                    {
                        EnqueueFirstAvailable("Moogle", PlayerType.Twinkee, 0);
                    }

                    if (twinkee.Effects.Contains("Moogle"))
                    {
                        EnqueueFirstAvailable("Warthog", PlayerType.Twinkee, 0);
                    }

                    // Protections
                    if (twinkee.Effects.Contains("Carbuncle") && Items.Any(x => x.Name == "Carbuncle"))
                    {
                        EnqueueFirstAvailable("Carbuncle", PlayerType.Twinkee, -2);
                        EnqueueFirstAvailable("Master Sword", PlayerType.Twinkee);

                    }
                    else
                    {
                        EnqueueEffectIfNoneEquipped(new[] { "Varia Suit", "Tanooki Suit", "Gold Ring", "Star" },
                            PlayerType.Twinkee, 0);
                    }
                }
            }

            // Positions
            EnqueueFirstAvailable("Cardboard Box", PlayerType.Twinkee, 0);
            EnqueueFirstAvailable("Morph Ball", PlayerType.Twinkee, 0);
            EnqueueFirstAvailable("Bullet Bill", PlayerType.Twinkee, 0);

            // Points
            EnqueueAllAvailable("Buffalo", PlayerType.Twinkee);
            EnqueueAllAvailable("Biggs", PlayerType.Twinkee);
            EnqueueAllAvailable("Pizza", PlayerType.Twinkee);
            EnqueueAllAvailable("Wedge", PlayerType.Twinkee);
            EnqueueFirstAvailable("Pokeball", PlayerType.Self, 1);
            EnqueueFirstAvailable("Da Da Da Da Daaa Da DAA da da", PlayerType.Twinkee, 1);
            EnqueueFirstAvailable("Treasure Chest", PlayerType.Self, 1);
            EnqueueFirstAvailable("Bo Jackson", PlayerType.Twinkee, 4);
            EnqueueFirstAvailable("UUDDLRLRBA", PlayerType.Twinkee, 3);

            // Attacks
            EnqueueFirstAvailable("Banana Peel", PlayerType.Behind, 3);

            EnqueueFirstAvailable("Charizard", PlayerType.Ahead, 1);
            EnqueueFirstAvailable("Hard Knuckle", PlayerType.Ahead, 3);
            EnqueueFirstAvailable("Fire Flower", PlayerType.Ahead, 3);
            EnqueueFirstAvailable("Hadouken", PlayerType.Ahead, 3);
            EnqueueFirstAvailable("Green Shell", PlayerType.Ahead, 3);
            EnqueueFirstAvailable("Holy Water", PlayerType.Ahead, 3);
            EnqueueFirstAvailable("Buster Sword", PlayerType.Ahead, 3);
            EnqueueFirstAvailable("Knuckle Punch", PlayerType.Ahead, 3);
            EnqueueFirstAvailable("SPNKR", PlayerType.Ahead, 3);
            EnqueueFirstAvailable("Portal Nun", PlayerType.Ahead, 3);
            EnqueueFirstAvailable("Hadouken", PlayerType.Ahead, 3);
            EnqueueFirstAvailable("Box of Bees", PlayerType.Ahead, 3);


            EnqueueAllAvailable("Master Sword", PlayerType.Leader, 1);
            EnqueueAllAvailable("Blue Shell", PlayerType.Leader, 1);
            EnqueueAllAvailable("Golden Gun", PlayerType.Leader, 1);
            EnqueueAllAvailable("Red Shell", PlayerType.Leader, 3);
            EnqueueAllAvailable("Crowbar", PlayerType.Leader, 3);
        }

        private PlayerInfo GetPlayerByType(PlayerType playerType)
        {
            //[lock (LeaderboardLock)
            {
                if (!_players.Any())
                {
                    return null;
                }
                var twinkIndex = _players.FindIndex(x => x.PlayerName == AssistUser);
                var selfIndex = _players.FindIndex(x => x.PlayerName == Self);

                switch (playerType)
                {
                    case PlayerType.Undefined:
                        return null;

                    case PlayerType.Self:
                        if (selfIndex < 0) return null;
                        return _players[selfIndex];

                    case PlayerType.Behind:
                        if (twinkIndex < 0)
                        {
                            return null;
                        }
                        if (twinkIndex < _players.Count - 1)
                        {
                            return _players[twinkIndex + 1];
                        }
                        return null;

                    case PlayerType.Ahead:
                        if (twinkIndex <= 0) return null;
                        return _players[twinkIndex - 1];

                    case PlayerType.Twinkee:
                        if (twinkIndex < 0) return null;
                        return _players[twinkIndex];

                    case PlayerType.Leader:
                        return _players.FirstOrDefault();

                    default:
                        return null;
                }
            }
        }

        private bool IsPlayerInvincible(PlayerInfo player)
        {
            if (player.Effects.Contains("Varia Suit")) { return true; }
            if (player.Effects.Contains("Tanooki Suit")) { return true; }
            if (player.Effects.Contains("Gold Ring")) { return true; }
            if (player.Effects.Contains("Carbuncle")) { return true; }
            if (player.Effects.Contains("Star")) { return true; }

            return false;
        }

        private void EnqueueEffectIfNoneEquipped(string[] effects, PlayerType playerType, int priority = 2)
        {
            // TODO: Get effects by playerType
            var i = effects.Intersect(SelfEffects);
            if (i.Any())
            {
                return;
            }

            foreach (var e in effects)
            {
                var item = Items
                    .Where(x => x.Name == e && !_itemQueue.IsEnqueued(q => q.ItemId == x.Id) && !x.Errored)
                    .OrderBy(x => x.Acquired)
                    .FirstOrDefault();
                if (item != null)
                {
                    EnqueueItemUsage(item.Id, playerType, priority);
                    return;
                }
            }
        }

        private void EnqueueEffectIfMissing(string effectName, PlayerType playerType, int priority = 2)
        {
            var player = GetPlayerByType(playerType);
            if (player == null) return;

            if (!player.Effects.Contains(effectName))
            {
                EnqueueFirstAvailable(effectName, playerType, priority);
            }
        }

        private void EnqueueFirstAvailable(string name, PlayerType playerType, int priority = 2)
        {
            var item = Items
                    .Where(x => x.Name == name
                        && !_itemQueue.IsEnqueued(q => q.ItemId == x.Id)
                        && !x.Errored)
                    .OrderBy(x => x.Acquired)
                    .FirstOrDefault();
            if (item != null)
            {
                EnqueueItemUsage(item.Id, playerType, priority);
            }
        }

        private void EnqueueAllAvailable(string name, PlayerType target, int priority = 2)
        {
            foreach (var item in Items.Where(x => x.Name == name
                                && !_itemQueue.IsEnqueued(q => q.ItemId == x.Id)
                                && !x.Errored))
            {
                EnqueueItemUsage(item.Id, target, priority);
            }
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _pointsTimer.Stop();
            GetPoint();
            _pointsTimer.Start();
        }

        private void OneHitButton_Click(object sender, RoutedEventArgs e)
        {
            GetPoint();
        }

        private void GetPoint()
        {
            var request = new RestRequest("points", Method.POST);
            request.AddHeader("apikey", ApiKey);
            var response = _client.Execute<PointResponse>(request);

            if (response.Data == null)
            {
                return;
            }

            foreach (var message in response.Data.Messages)
            {
                if (message.StartsWith("ptrandem gained") || message.StartsWith("Thy hero ptrandem hath gained"))
                {
                    var resultString = Regex.Match(message, @"\d+").Value;
                    if (resultString.Any())
                    {
                        WriteLog(resultString.FirstOrDefault() + " ", true);
                    }
                }
                else
                {
                    WriteLog(message);
                }
                var bonusItems = ItemHelpers.FindBonusItems(message);
                if (bonusItems.Count > 0)
                {
                    foreach (var item in bonusItems)
                    {
                        Items.Add(item);
                    }
                }
            }

            if (response.Data.Item != null)
            {
                //this.Dispatcher.Invoke((Action)(() =>
                //{
                //    Log.Text += response.Data.ItemString;
                //}));

                AddItem(response.Data.Item);
            }

            Dispatcher.Invoke(() =>
            {
                PointsLabel.Content = response.Data.Points;
                BadgesLog.Text = string.Join("\n", response.Data.Badges);
            });

            SyncEffects(SelfEffects, response.Data.Effects);

        }

        private void GetLeaderboard()
        {
            var players = new List<PlayerInfo>();
            {
                var top10Request = new RestRequest("/", Method.GET);
                //            top10Request.AddHeader("apikey", APIKey);
                top10Request.AddHeader("Accept", "application/json");
                var response1 = _client.Execute<List<PlayerInfo>>(top10Request);
                if (response1?.Data != null)
                {

                    players.AddRange(response1.Data);


                }

                for (var i = 1; i < 8; i++)
                {
                    //Thread.Sleep(10);
                    var nextFew = new RestRequest($"/?page={i}", Method.GET);
                    //            nextFew.AddHeader("apikey", APIKey);
                    nextFew.AddHeader("Accept", "application/json");

                    var response2 = _client.Execute<List<PlayerInfo>>(nextFew);
                    if (response2?.Data != null)
                    {
                        players.AddRange(response2.Data);
                    }
                }
                _players.Clear();
                _players.AddRange(players);
            }
            SyncPlayers();
        }

        private void AddItem(Item item)
        {
            this.Dispatcher.Invoke(() =>
            {

                //ItemsLog.Text += item.ToString();
                foreach (var f in item.Fields)
                {
                    Items.Add(f);
                }
                //ScrollToEnd(ItemsLog);
            });

            PersistItemCollection();
        }

        private void ScrollToEnd(TextBox textBox)
        {
            //textBox.Focus();
            textBox.CaretIndex = textBox.Text.Length;
            textBox.ScrollToEnd();
            if (textBox.LineCount > 200)
            {
                textBox.Clear();
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            _pointsTimer.Start();
            _itemsTimer.Start();
            _intermediateTimer.Start();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _pointsTimer.Stop();
        }

        private void UseOnSelfButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItemId = GetActiveItemIdFromGui();
            EnqueueItemUsage(selectedItemId, AssistUser, -1);
        }

        private void UseOnTargetButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItemId = GetActiveItemIdFromGui();
            EnqueueItemUsage(selectedItemId, ItemTarget.Text, -1);
        }

        private void UseOnLeaderButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItemId = GetActiveItemIdFromGui();
            EnqueueItemUsage(selectedItemId, PlayerType.Leader, -1);
        }


        private void WriteLog(string text, bool suppressNewline = false)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            Dispatcher.Invoke(() =>
            {

                if (!suppressNewline)
                {
                    text = $"\n{text}\n";
                }

                Log.Text += OutputFilter(text);

                ScrollToEnd(Log);
            });

            if (text.StartsWith("\n"))
            {
                text = $"{DateTime.Now.ToShortTimeString()} \t {text}";
            }
            lock (LogfileLock)
            {
                File.AppendAllText(LogPath, text);
            }
        }

        private string OutputFilter(string text)
        {
            if (text.StartsWith("\nKupo")
                || text.StartsWith("\nThe nun following you")
                || text.StartsWith("\nThusly the holy woman")
                )
            {
                return "";
            }

            return text;
        }

        private void EnqueueItemUsage(string itemId, PlayerType playerType, int priority = 2)
        {
            var item = Items.Where(x => x.Id == itemId).OrderBy(x => x.Acquired).FirstOrDefault();
            if (item != null)
            {
                item.Queued = true;
            }

            var usage = new ItemUsage { ItemId = itemId, PlayerType = playerType };
            _itemQueue.Enqueue(usage, priority);
            this.Dispatcher.Invoke(() =>
            {
                QueueCountLabel.Content = _itemQueue.Count;
            });

        }

        private void EnqueueItemUsage(string itemId, string target, int priority = 2)
        {
            if (string.IsNullOrWhiteSpace(itemId) || string.IsNullOrWhiteSpace(target))
            {
                WriteLog("Cannot enqueue invalid item params");
            }

            var item = Items.FirstOrDefault(x => x.Id == itemId);

            if (item != null)
            {
                item.Queued = true;
            }

            var usage = new ItemUsage { ItemId = itemId, Target = target };
            _itemQueue.Enqueue(usage, priority);
            this.Dispatcher.Invoke((Action)(() =>
            {
                QueueCountLabel.Content = _itemQueue.Count;
            }));

        }

        private void UseNextItem()
        {
            if (!_itemQueue.Any()) return;

            var usage = _itemQueue.Dequeue();
            Dispatcher.Invoke(() =>
            {
                QueueCountLabel.Content = _itemQueue.Count;
            });

            if (string.IsNullOrWhiteSpace(usage.Target))
            {
                var player = GetPlayerByType(usage.PlayerType);
                if (player == null)
                {
                    WriteLog($"ERROR: Could not find player type of '{Enum.GetName(typeof(PlayerType), usage.PlayerType)}'. Item not played.");
                    return;
                }

                usage.Target = player.PlayerName;
            }

            var request = new RestRequest($"items/use/{usage.ItemId}?target={usage.Target}", Method.POST);
            request.AddHeader("apikey", ApiKey);
            var response = _client.Execute(request);
            WriteLog(response.Content + "\n");
            var item = Items.FirstOrDefault(x => x.Id == usage.ItemId);

            var bonusItems = ItemHelpers.FindBonusItems(response.Content);
            if (bonusItems.Count > 0)
            {
                foreach (var bonus in bonusItems)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Items.Add(bonus);
                    });
                }
            }

            if (response.Content.StartsWith("No such item found")
                || response.Content.StartsWith("{\"Message\":\"Invalid item GUID\"}"))
            {
                if (item != null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Items.Remove(item);
                    });
                }
            }
            else if (response.StatusCode != HttpStatusCode.OK)
            {
                if (item != null)
                {
                    item.Errored = true;
                    item.LastError = response.StatusDescription;
                    WriteLog($"Error using {item.Name} ({item.Id}): {response.StatusDescription}");
                }
                else
                {
                    WriteLog($"Error deploying usage with id {usage.ItemId}");
                }
            }
            else
            {
                if (response.Content.IndexOf("<", StringComparison.CurrentCultureIgnoreCase) >= 0) // TODO: is this a good indicator?
                {
                    // Seems like we've used it?

                    if (item != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Items.Remove(item);
                        });
                    }
                }
                else if (item != null)
                {
                    item.Errored = true;
                    WriteLog($"Item may have been used, but was not removed.");
                }
            }


            PersistItemCollection();
        }

        private string GetActiveItemIdFromGui()
        {
            if (!string.IsNullOrWhiteSpace(ItemIdOverride.Text))
            {
                return ItemIdOverride.Text;
            }
            var item = ItemsGrid.SelectedItem as ItemFields;
            return item?.Id;
        }

        private void SyncEffects(ObservableCollection<string> collection, List<string> effects)
        {
            collection.Clear();
            foreach (var e in effects)
            {
                collection.Add(e);
            }

            if (collection == SelfEffects)
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    EffectsLog.Text = string.Join("\n", effects);
                }));
            }
        }

        private void SyncPlayers()
        {

            this.Dispatcher.Invoke((Action)(() =>
            {
                Players.Clear();
                foreach (var e in _players)
                {
                    Players.Add(e);
                }

                var leader = GetPlayerByType(PlayerType.Leader);
                if (leader != null)
                {
                    CurrentLeaderLabel.Content = leader.PlayerName;
                }

                var self = Players.FirstOrDefault(x => x.PlayerName == Self);
                if (self != null)
                {
                    CurrentRankLabel.Content = Players.IndexOf(self) + 1;
                    LeaderboardDatagrid.ScrollIntoView(self);
                }

                var assistingUser = Players.FirstOrDefault(x => x.PlayerName == AssistUser);
                if (assistingUser != null)
                {
                    SyncEffects(AssistUserEffects, assistingUser.Effects);
                }

            }));
        }

        private void ImportItemsButton_Click(object sender, RoutedEventArgs e)
        {
            var items = ItemImporter.GetItemsFromClipboard();
            foreach (var item in items)
            {
                Dispatcher.Invoke(() =>
                {
                    Items.Add(item);

                });
            }
        }

        private void SaveToDiskButton_OnClick(object sender, RoutedEventArgs e)
        {
            PersistItemCollection();
        }

        private void ReadFromDiskButton_OnClick(object sender, RoutedEventArgs e)
        {
            LoadItemCollection();
        }

        private void LoadItemCollection()
        {
            var items = ItemImporter.ImportItemsFromDisk(ItemPath);
            foreach (var item in items)
            {
                ItemFields item1 = item;
                Dispatcher.Invoke(() =>
                {
                    if (Items.FirstOrDefault(x => x.Id == item1.Id) == null)
                    {
                        Items.Add(item1);
                    }
                });
            }
        }

        private void PersistItemCollection()
        {
            Dispatcher.Invoke(() => ItemImporter.ExportAllItemsToDisk(Items, ItemPath));
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            Log.Clear();
        }
    }
}
