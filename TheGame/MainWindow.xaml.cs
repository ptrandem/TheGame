using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
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
        private Timer _pointsTimer = new Timer();
        private Timer _itemsTimer = new Timer(61000);
        private Timer _intermediateTimer = new Timer(30000);
        private List<PlayerInfo> _players = new List<PlayerInfo>();
        private PriorityQueue<ItemUsage> _itemQueue = new PriorityQueue<ItemUsage>();

        protected ObservableCollection<ItemFields> Items { get; set; }
        protected ObservableCollection<string> Effects { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            _pointsTimer.Interval = 2000;
            _pointsTimer.Elapsed += _timer_Elapsed;
            _itemsTimer.Elapsed += _itemsTimer_Elapsed;
            _intermediateTimer.Elapsed += _intermediateTimer_Elapsed;

            
            Items = new ObservableCollection<ItemFields>();
            Effects = new ObservableCollection<string>();

            ItemsGrid.ItemsSource = Items;
            GetLeaderboard();

            _itemsTimer.Start();
            _intermediateTimer.Start();

            //Items.Add(new ItemFields { Name = "whatever", Id = "4", Rarity = 3, Description = "yep, it's a thing" });
            //Items.Add(new ItemFields { Name = "whatever 2", Id = "2", Rarity = 6, Description = "yep, it's also a thing" });

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
            EnqueueEffectIfNoneEquipped(new[] { "Varia Suit", "Tanooki Suit", "Gold Ring" }, Self, 0);
            
            // Points
            EnqueueFirstAvailable("Morph Ball", priority: 1);
            EnqueueFirstAvailable("Bullet Bill", priority: 1);
            EnqueueAllAvailable("Buffalo");
            EnqueueAllAvailable("Biggs");
            EnqueueAllAvailable("Pizza");
            EnqueueAllAvailable("Wedge");
            EnqueueFirstAvailable("Pokeball");
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
            if(player.Effects.Contains("Varia Suit"))
            {
                return true;
            }

            if (player.Effects.Contains("Tanooki Suit"))
            {
                return true;
            }

            if(player.Effects.Contains("Gold Ring"))
            {
                return true;
            }

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

        private void GetLeaderboard()
        {
            var top10Request = new RestRequest("/", Method.GET);
            top10Request.AddHeader("apikey", APIKey);
            top10Request.AddHeader("Accept", "application/json");
            var response = _client.Execute<List<PlayerInfo>>(top10Request);
            if (response != null && response.Data != null)
            {
                _players.Clear();
                _players.AddRange(response.Data);

                this.Dispatcher.Invoke((Action)(() =>
                {
                    CurrentLeaderLabel.Content = _players.FirstOrDefault().PlayerName;
                }));
            }

            var next50Request = new RestRequest("/?page=1&pagesize=200", Method.GET);
            next50Request.AddHeader("apikey", APIKey);
            next50Request.AddHeader("Accept", "application/json");

            response = _client.Execute<List<PlayerInfo>>(next50Request);
            if (response != null && response.Data != null)
            {
                _players.AddRange(response.Data);
            }
        }


        private void AddItem(Item item)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                
                ItemsLog.Text += item.ToString();
                foreach (var f in item.Fields)
                {
                    Items.Add(f);
                }
                ScrollToEnd(ItemsLog);
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
            EnqueueItemUsage(selectedItemId, Self);
        }

        private void UseOnTargetButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItemId = GetActiveItemIdFromGUI();
            EnqueueItemUsage(selectedItemId, ItemTarget.Text);
        }

        private void UseOnLeaderButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItemId = GetActiveItemIdFromGUI();
            var leader = _players.FirstOrDefault();
            if (leader != null)
            {
                EnqueueItemUsage(selectedItemId, leader.PlayerName);
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
            else
            {
                if (item != null)
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        Items.Remove(item);
                    }));
                }
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
