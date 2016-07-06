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

    }
}
