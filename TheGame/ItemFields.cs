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
        public string Name { get; set; }
        public string Id { get; set; }
        public int Rarity { get; set; }
        public string Description { get; set; }

        private bool _queued = false;
        public bool Queued {
            get { return _queued; }
            set
            {
                _queued = value;
                NotifyPropertyChanged(nameof(Queued));
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
            return $"{Name}\t{Id}\t{Rarity}\t{Description}\n";
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

            return item;
        }
    }
}
