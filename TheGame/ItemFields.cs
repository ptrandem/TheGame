using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheGame
{
    public class ItemFields : INotifyPropertyChanged
    {
        public ItemFields()
        {
            Acquired = DateTime.Now;
        }

        public DateTime Acquired { get; set; }
        public string Name { get; set; }
        public string Id { get; set; }
        public int Rarity { get; set; }
        public string Description { get; set; }

        private bool _queued = false;
        private bool _errored;
        private string _lastError;

        public bool Queued {
            get { return _queued; }
            set
            {
                _queued = value;
                NotifyPropertyChanged(nameof(Queued));
            }
        }

        public bool Errored
        {
            get { return _errored; }
            set
            {
                _errored = value;
                NotifyPropertyChanged(nameof(Errored));
            }
        }

        public string LastError
        {
            get { return _lastError; }
            set
            {
                _lastError = value;
                NotifyPropertyChanged(nameof(LastError));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public override string ToString()
        {
            return $"{Name}\t{Id}\t{Rarity}\t{Description}\t{Acquired}\n";
        }

        public static ItemFields GetFromString(string input)
        {
            var tokens = input.Split('\t');
            if(tokens.Length < 2)
            {
                return null;
            }

            var item = new ItemFields();
            item.Name = tokens[0];
            item.Id = tokens[1];

            if(tokens.Length >= 3)
            {
                int temp;
                if(int.TryParse(tokens[2], out temp))
                {
                    item.Rarity = temp;
                }
            }

            if (tokens.Length >= 4)
            {
                item.Description = tokens[3];
            }

            if (tokens.Length >= 5)
            {
                DateTime dt;
                if(DateTime.TryParse(tokens[4], out dt))
                {
                    item.Acquired = dt;
                }
                else
                {
                    item.Acquired = DateTime.MinValue;
                }
            }

            return item;
        }
    }
}
