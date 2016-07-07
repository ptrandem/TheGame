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
        public const string APIKey = "4a0da0c3-f9c3-4ebf-95ae-4905f5fb564a";
        public const string Self = "ptrandem";

        private RestClient _client = new RestClient("http://thegame.nerderylabs.com");
        private System.Timers.Timer _pointsTimer = new System.Timers.Timer();
        private System.Timers.Timer _itemsTimer = new System.Timers.Timer(61000);
        private System.Timers.Timer _intermediateTimer = new System.Timers.Timer(20000);
        private List<PlayerInfo> _players = new List<PlayerInfo>();
        
        private PriorityQueue<ItemUsage> _itemQueue = new PriorityQueue<ItemUsage>();

        protected ObservableCollection<ItemFields> Items { get; set; }
        protected ObservableCollection<string> Effects { get; set; }
        protected ObservableCollection<PlayerInfo> Players { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            _pointsTimer.Interval = 2000;
            _pointsTimer.Elapsed += _timer_Elapsed;
            _itemsTimer.Elapsed += _itemsTimer_Elapsed;
            _intermediateTimer.Elapsed += _intermediateTimer_Elapsed;

            
            Items = new ObservableCollection<ItemFields>();
            Players = new ObservableCollection<PlayerInfo>();
            Effects = new ObservableCollection<string>();

            LeaderboardDatagrid.AutoGeneratingColumn += LeaderboardDatagrid_AutoGeneratingColumn;
            ItemsGrid.ItemsSource = Items;
            LeaderboardDatagrid.ItemsSource = Players;

            GetLeaderboard();

            _itemsTimer.Start();
            _intermediateTimer.Start();

        }

        private void LeaderboardDatagrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if(e.PropertyName == "Effects")
            {
                e.Cancel = true;
            }

            if(e.PropertyName == "EffectsString")
            {
                e.Column.Header = "Effects";
            }
        }

        private void _intermediateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            GetLeaderboard();
            ApplyQueueRules();
            
            
        }

        
        private void _itemsTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _itemsTimer.Stop();
            UseNextItem();
            _itemsTimer.Start();
        }

        private void ApplyQueueRules()
        {
            if(Effects.Contains("Slow"))
            {
                EnqueueEffectIfMissing("Mushroom", 0);
            }
            else
            {
                // Speeds
                EnqueueEffectIfMissing("Warthog", 0);
                EnqueueEffectIfMissing("Moogle", 0);
                EnqueueEffectIfMissing("7777", 0);
            }


            // Protections
            EnqueueEffectIfNoneEquipped(new[] { "Varia Suit", "Tanooki Suit", "Gold Ring", "Carbuncle", "Star" }, Self, 0);
            
            // Points
            //EnqueueFirstAvailable("Morph Ball", priority: 1); // Let's save these for final game time
            //EnqueueFirstAvailable("Bullet Bill", priority: 1);
            EnqueueAllAvailable("Buffalo");
            EnqueueAllAvailable("Biggs");
            EnqueueAllAvailable("Pizza");
            EnqueueAllAvailable("Wedge");
            EnqueueFirstAvailable("Pokeball", priority: 1);
            EnqueueFirstAvailable("Da Da Da Da Daaa Da DAA da da", priority: 1);
            EnqueueFirstAvailable("Bo Jackson", priority: 3);
            EnqueueFirstAvailable("UUDDLRLRBA", priority: 3);

            // Attacks
            if (_players.Any())
            {
                PlayerInfo playerAhead = null, playerBehind = null;

                var selfIndex = _players.FindIndex(x => x.PlayerName == Self);
                if (selfIndex > 0)
                {
                    playerAhead = _players[selfIndex - 1];
                }

                if(selfIndex < _players.Count)
                {
                    playerBehind = _players[selfIndex + 1];
                }

                var leader = _players.FirstOrDefault();

                if(playerBehind != null && !IsPlayerInvincible(playerBehind))
                {
                    EnqueueFirstAvailable("Banana Peel", playerBehind.PlayerName, 3);
                }

                if(playerAhead != null && !IsPlayerInvincible(playerAhead))
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
                    EnqueueFirstAvailable("Golden Gun", playerAhead.PlayerName, 3);
                    EnqueueFirstAvailable("Portal Nun", playerAhead.PlayerName, 3);
                    EnqueueFirstAvailable("Hadouken", playerAhead.PlayerName, 3);
                    EnqueueFirstAvailable("Box of Bees", playerAhead.PlayerName, 3);
                }

                if (leader != null && leader.PlayerName != Self && !IsPlayerInvincible(leader))
                {
                    EnqueueFirstAvailable("Master Sword", leader.PlayerName, 3);
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
            if (player.Effects.Contains("Tanooki Suit")) { return true;}
            if (player.Effects.Contains("Gold Ring")) { return true; }
            if (player.Effects.Contains("Carbuncle")) { return true; }
            if (player.Effects.Contains("Star")) { return true; }

            return false;
        }

        

        private void EnqueueEffectIfNoneEquipped(string[] effects, string target = Self, int priority = 2)
        {
            var i = effects.Intersect(Effects);
            if(i.Any())
            {
                return;
            }

            foreach(var e in effects)
            {
                var item = Items.FirstOrDefault(x => x.Name == e && !_itemQueue.IsEnqueued(q => q.ItemId == x.Id));
                if (item != null)
                {
                    EnqueueItemUsage(item.Id, target, priority);
                    return;
                }
            }
        }

        private void EnqueueEffectIfMissing(string effectName, int priority = 2)
        {
            if(!Effects.Contains(effectName))
            {
                EnqueueFirstAvailable(effectName, priority: priority);
            }
        }

        private void EnqueueFirstAvailable(string name, string target = Self, int priority = 2)
        {
            var item = Items.FirstOrDefault(x => x.Name == name && !_itemQueue.IsEnqueued(q => q.ItemId == x.Id));
            if(item != null)
            {
                EnqueueItemUsage(item.Id, target, priority);
            }
        }

        private void EnqueueAllAvailable(string name, string target = Self, int priority = 2)
        {
            foreach(var item in Items.Where(x => x.Name == name && !_itemQueue.IsEnqueued(q => q.ItemId == x.Id)))
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

            if(response.Data == null)
            {
                return;
            }

            foreach (var message in response.Data.Messages)
            {
                if(message.StartsWith("ptrandem gained"))
                {
                    var resultString = Regex.Match(message, @"\d+").Value;
                    if(resultString.Any())
                    {
                        WriteLog(resultString.FirstOrDefault().ToString() + " ", true);
                    }
                }
                else
                {
                    WriteLog(message);
                }
                var bonusItems = ItemHelpers.FindBonusItems(message);
                if(bonusItems.Count > 0)
                {
                    foreach(var item in bonusItems)
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

            SyncEffects(response.Data.Effects);

        }

        private async void GetLeaderboard()
        {
            var top10Request = new RestRequest("/", Method.GET);
//            top10Request.AddHeader("apikey", APIKey);
            top10Request.AddHeader("Accept", "application/json");
            var response1 = _client.Execute<List<PlayerInfo>>(top10Request);
            if (response1 != null && response1.Data != null)
            {
                _players.Clear();
                _players.AddRange(response1.Data);

                this.Dispatcher.Invoke((Action)(() =>
                {
                    CurrentLeaderLabel.Content = _players.FirstOrDefault().PlayerName;
                }));
            }

            Thread.Sleep(300);

            var nextFew = new RestRequest("/?page=1", Method.GET);
//            nextFew.AddHeader("apikey", APIKey);
            nextFew.AddHeader("Accept", "application/json");

            var response2 = _client.Execute<List<PlayerInfo>>(nextFew);
            if (response2 != null && response2.Data != null)
            {
                _players.AddRange(response2.Data);
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
            EnqueueItemUsage(selectedItemId, Self, 0);
        }

        private void UseOnTargetButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItemId = GetActiveItemIdFromGUI();
            EnqueueItemUsage(selectedItemId, ItemTarget.Text, 0);
        }

        private void UseOnLeaderButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItemId = GetActiveItemIdFromGUI();
            var leader = _players.FirstOrDefault();
            if (leader != null)
            {
                EnqueueItemUsage(selectedItemId, leader.PlayerName, 0);
            }
        }

        private void WriteLog(string text, bool suppressNewline = false)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                if (!suppressNewline)
                {
                    Log.Text += "\n";
                }
                Log.Text += text;
                if (!suppressNewline)
                {
                    Log.Text += "\n";
                }
                ScrollToEnd(Log);
            }));
        }

        private void EnqueueItemUsage(string itemId, string target, int priority = 2)
        {
            if (string.IsNullOrWhiteSpace(itemId) || string.IsNullOrWhiteSpace(target))
            {
                WriteLog("Item request not queued.\n");
                return;
            }

            var item = Items.FirstOrDefault(x => x.Id == itemId);
            if(item != null)
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

            if (response.Content.StartsWith("No such item found"))
            {
                if(item != null)
                {
                    WriteLog($"Error using {item.Name} ({item.Id})");
                }
                else
                {
                    WriteLog($"Error deploying usage with id {usage.ItemId}");
                }
                
            }
            
            if (item != null)
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    Items.Remove(item);
                }));
            }
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

        private void SyncEffects(List<string> effects)
        {
            Effects.Clear();
            foreach(var e in effects)
            {
                Effects.Add(e);
            }

            this.Dispatcher.Invoke((Action)(() =>
            {
                EffectsLog.Text = string.Join("\n", effects);
            }));
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
                
            }));
        }

        private void ImportItemsButton_Click(object sender, RoutedEventArgs e)
        {
            var items = ItemImporter.GetItemsFromClipboard();
            foreach (var item in items)
            {
                Items.Add(item);
            }
        }
    }
}
