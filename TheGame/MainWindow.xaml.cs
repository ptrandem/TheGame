using System.IO;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;

namespace TheGame
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string APIKey = "4a0da0c3-f9c3-4ebf-95ae-4905f5fb564a";
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
                return $"c:\\temp\\{Self}_log.txt";
            }
        }

        public string ErrorLogPath
        {
            get
            {
                return $"c:\\temp\\{Self}_errors.txt";
            }
        }

        private RestClient _client = new RestClient("http://thegame.nerderylabs.com:1337");
        private System.Timers.Timer _pointsTimer = new System.Timers.Timer();
        private System.Timers.Timer _itemsTimer = new System.Timers.Timer(61000);
        private System.Timers.Timer _intermediateTimer = new System.Timers.Timer(20000);
        private List<PlayerInfo> _players = new List<PlayerInfo>();

        private PriorityQueue<ItemUsage> _itemQueue = new PriorityQueue<ItemUsage>();

        protected ObservableCollection<ItemFields> Items { get; set; }
        protected ObservableCollection<string> SelfEffects { get; set; }
        protected ObservableCollection<string> AssistUserEffects { get; set; }
        protected ObservableCollection<PlayerInfo> Players { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

            _pointsTimer.Interval = 1500;
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

            _itemsTimer.Start();
            _intermediateTimer.Start();

        }

        void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            File.AppendAllText(ErrorLogPath, e.Exception.Message + "\n");
            File.AppendAllText(ErrorLogPath, e.Exception.StackTrace + "\n\n");

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
            if (SelfEffects.Contains("Slow"))
            {
                EnqueueEffectIfMissing("Mushroom", Self, 0);
            }
            else
            {
                // Speeds
                EnqueueEffectIfNoneEquipped(new[] { "7777", "Warthog", "Moogle" }, AssistUser, 0);

                if (SelfEffects.Contains("Warthog"))
                {
                    EnqueueFirstAvailable("Moogle", AssistUser, 0);
                }

                if (SelfEffects.Contains("Moogle"))
                {
                    EnqueueFirstAvailable("Warthog", AssistUser, 0);
                }
                //EnqueueEffectIfMissing("Warthog", 0);
                //EnqueueEffectIfMissing("Moogle", 0);
                //EnqueueEffectIfMissing("7777", 0);
            }


            // Protections
            //EnqueueEffectIfNoneEquipped(new[] { "Varia Suit", "Tanooki Suit", "Gold Ring", "Carbuncle", "Star" }, Self, 0);

            // Points
            //EnqueueFirstAvailable("Morph Ball", priority: 1); // Let's save these for final game time
            //EnqueueFirstAvailable("Bullet Bill", priority: 1);
            EnqueueAllAvailable("Buffalo", AssistUser);
            EnqueueAllAvailable("Biggs", AssistUser);
            EnqueueAllAvailable("Pizza", AssistUser);
            EnqueueAllAvailable("Wedge", AssistUser);
            EnqueueFirstAvailable("Pokeball", Self, 1);
            EnqueueFirstAvailable("Da Da Da Da Daaa Da DAA da da", AssistUser, 1);
            EnqueueFirstAvailable("Treasure Chest", Self, 1);
            EnqueueFirstAvailable("Bo Jackson", AssistUser, 3);
            EnqueueFirstAvailable("UUDDLRLRBA", AssistUser, 3);

            // Attacks
            if (_players.Any())
            {
                PlayerInfo playerAhead = null, playerBehind = null;

                var selfIndex = _players.FindIndex(x => x.PlayerName == AssistUser);
                if (selfIndex > 0)
                {
                    playerAhead = _players[selfIndex - 1];
                }

                if (selfIndex < _players.Count)
                {
                    playerBehind = _players[selfIndex + 1];
                }

                var leader = _players.FirstOrDefault();

                if (playerBehind != null && !IsPlayerInvincible(playerBehind))
                {
                    EnqueueFirstAvailable("Banana Peel", playerBehind.PlayerName, 3);
                }

                if (playerAhead != null && !IsPlayerInvincible(playerAhead))
                {
                    EnqueueFirstAvailable("Charizard", playerAhead.PlayerName, 1);
                    EnqueueFirstAvailable("Hard Knuckle", playerAhead.PlayerName, 3);
                    EnqueueFirstAvailable("Fire Flower", playerAhead.PlayerName, 3);
                    EnqueueFirstAvailable("Hadouken", playerAhead.PlayerName, 3);
                    EnqueueFirstAvailable("Green Shell", playerAhead.PlayerName, 3);
                    EnqueueFirstAvailable("Holy Water", playerAhead.PlayerName, 3);
                    EnqueueFirstAvailable("Buster Sword", playerAhead.PlayerName, 3);

                    EnqueueFirstAvailable("Knuckle Punch", playerAhead.PlayerName, 3);
                    EnqueueFirstAvailable("SPNKR", playerAhead.PlayerName, 3);
                    //EnqueueFirstAvailable("Golden Gun", playerAhead.PlayerName, 3);
                    EnqueueFirstAvailable("Portal Nun", playerAhead.PlayerName, 3);
                    EnqueueFirstAvailable("Hadouken", playerAhead.PlayerName, 3);
                    EnqueueFirstAvailable("Box of Bees", playerAhead.PlayerName, 3);
                }

                if (leader != null && leader.PlayerName != AssistUser && !IsPlayerInvincible(leader))
                {
                    //EnqueueFirstAvailable("Master Sword", leader.PlayerName, 3);
                    // Save these up for the final moments
                    //EnqueueFirstAvailable("Blue Shell", leader.PlayerName, 3);
                    EnqueueFirstAvailable("Red Shell", leader.PlayerName, 3);
                    EnqueueFirstAvailable("Crowbar", leader.PlayerName, 3);
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



        private void EnqueueEffectIfNoneEquipped(string[] effects, string target, int priority = 2)
        {
            var i = effects.Intersect(SelfEffects);
            if (i.Any())
            {
                return;
            }

            foreach (var e in effects)
            {
                var item = Items.FirstOrDefault(x => x.Name == e && !_itemQueue.IsEnqueued(q => q.ItemId == x.Id));
                if (item != null)
                {
                    EnqueueItemUsage(item.Id, target, priority);
                    return;
                }
            }
        }

        private void EnqueueEffectIfMissing(string effectName, string target, int priority = 2)
        {
            if (!SelfEffects.Contains(effectName))
            {
                EnqueueFirstAvailable(effectName, target, priority);
            }
        }

        private void EnqueueFirstAvailable(string name, string target, int priority = 2)
        {
            var item = Items.FirstOrDefault(x => x.Name == name && !_itemQueue.IsEnqueued(q => q.ItemId == x.Id));
            if (item != null)
            {
                EnqueueItemUsage(item.Id, target, priority);
            }
        }

        private void EnqueueAllAvailable(string name, string target, int priority = 2)
        {
            foreach (var item in Items.Where(x => x.Name == name && !_itemQueue.IsEnqueued(q => q.ItemId == x.Id)))
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
            request.AddHeader("apikey", APIKey);
            var response = _client.Execute<PointResponse>(request);

            if (response.Data == null)
            {
                return;
            }

            foreach (var message in response.Data.Messages)
            {
                if (message.StartsWith("ptrandem gained"))
                {
                    var resultString = Regex.Match(message, @"\d+").Value;
                    if (resultString.Any())
                    {
                        WriteLog(resultString.FirstOrDefault().ToString() + " ", true);
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

            this.Dispatcher.Invoke((Action)(() =>
            {
                PointsLabel.Content = response.Data.Points;
                BadgesLog.Text = string.Join("\n", response.Data.Badges);
            }));

            SyncEffects(SelfEffects, response.Data.Effects);

        }

        private void GetLeaderboard()
        {
            var top10Request = new RestRequest("/", Method.GET);
            //            top10Request.AddHeader("apikey", APIKey);
            top10Request.AddHeader("Accept", "application/json");
            var response1 = _client.Execute<List<PlayerInfo>>(top10Request);
            _players.Clear();
            if (response1 != null && response1.Data != null)
            {

                _players.AddRange(response1.Data);

                this.Dispatcher.Invoke((Action)(() =>
                {
                    CurrentLeaderLabel.Content = _players.FirstOrDefault().PlayerName;
                }));
            }

            for (int i = 1; i < 5; i++)
            {


                Thread.Sleep(100);

                var nextFew = new RestRequest($"/?page={i}", Method.GET);
                //            nextFew.AddHeader("apikey", APIKey);
                nextFew.AddHeader("Accept", "application/json");

                var response2 = _client.Execute<List<PlayerInfo>>(nextFew);
                if (response2 != null && response2.Data != null)
                {
                    _players.AddRange(response2.Data);
                }
            }
            SyncPlayers();


        }


        private void AddItem(Item item)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {

                //ItemsLog.Text += item.ToString();
                foreach (var f in item.Fields)
                {
                    Items.Add(f);
                }
                //ScrollToEnd(ItemsLog);
            }));

            PersistItemCollection();
        }

        private void ScrollToEnd(TextBox textBox)
        {
            //textBox.Focus();
            textBox.CaretIndex = textBox.Text.Length;
            textBox.ScrollToEnd();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            _pointsTimer.Start();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _pointsTimer.Stop();
        }

        private void UseOnSelfButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItemId = GetActiveItemIdFromGUI();
            EnqueueItemUsage(selectedItemId, AssistUser, -1);
        }

        private void UseOnTargetButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItemId = GetActiveItemIdFromGUI();
            EnqueueItemUsage(selectedItemId, ItemTarget.Text, -1);
        }

        private void UseOnLeaderButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItemId = GetActiveItemIdFromGUI();
            var leader = _players.FirstOrDefault();
            if (leader != null)
            {
                EnqueueItemUsage(selectedItemId, leader.PlayerName, -1);
            }
        }

        private void WriteLog(string text, bool suppressNewline = false)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                
                if (!suppressNewline)
                {
                    text = "\n" + text + "\n";
                }
                
                Log.Text += text;

                ScrollToEnd(Log);
            }));

            File.AppendAllText(LogPath, text);
        }

        private void EnqueueItemUsage(string itemId, string target, int priority = 2)
        {
            if (string.IsNullOrWhiteSpace(itemId) || string.IsNullOrWhiteSpace(target))
            {
                WriteLog("Item request not queued.\n");
                return;
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
            this.Dispatcher.Invoke((Action)(() =>
            {
                QueueCountLabel.Content = _itemQueue.Count;
            }));

            var request = new RestRequest($"items/use/{usage.ItemId}?target={usage.Target}", Method.POST);
            request.AddHeader("apikey", APIKey);
            var response = _client.Execute(request);
            WriteLog(response.Content + "\n");
            var item = Items.FirstOrDefault(x => x.Id == usage.ItemId);

            var bonusItems = ItemHelpers.FindBonusItems(response.Content);
            if (bonusItems.Count > 0)
            {
                foreach (var bonus in bonusItems)
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        Items.Add(bonus);
                    }));
                }
            }

            if (response.Content.StartsWith("No such item found")
                || response.Content.StartsWith("{\"Message\":\"Invalid item GUID\"}")
                || response.ResponseStatus != ResponseStatus.Completed 
                || string.IsNullOrWhiteSpace(response.Content))
            {
                if(item != null)
                {
                    WriteLog($"Error using {item.Name} ({item.Id})");
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        Items.Remove(item);
                    }));
                }
                else
                {
                    WriteLog($"Error deploying usage with id {usage.ItemId}");
                }
            }
            else
            {
                if(response.Content.IndexOf("<") >= 0) // TODO: is this a good indicator?
                {
                    if(item != null)
                    {
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            Items.Remove(item);
                        }));
                    }
                }
                else
                {
                    WriteLog($"Item may have been used, but was not removed.");
                }
            }
            

            PersistItemCollection();
        }

        private string GetActiveItemIdFromGUI()
        {
            if(!string.IsNullOrWhiteSpace(ItemIdOverride.Text))
            {
                return ItemIdOverride.Text;
            }
            var item = ItemsGrid.SelectedItem as ItemFields;
            if(item != null)
            {
                return item.Id;
            }
            return null;
        }

        private void SyncEffects(ObservableCollection<string> collection, List<string> effects)
        {
            collection.Clear();
            foreach(var e in effects)
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

                var self = Players.FirstOrDefault(x => x.PlayerName == Self);
                if(self != null)
                {
                    CurrentRankLabel.Content = Players.IndexOf(self) + 1;
                    LeaderboardDatagrid.ScrollIntoView(self);
                }

                var assistingUser = Players.FirstOrDefault(x => x.PlayerName == AssistUser);
                if(assistingUser != null)
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
    }
}
