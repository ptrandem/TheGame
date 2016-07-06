using RestSharp.Deserializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheGame
{
    public class PointResponse
    {
        public List<string> Messages { get; set; }

        [DeserializeAs(Name ="Item")]
        public string ItemString { get; set; }

        public Item Item { get; set; }
        public string Points { get; set; }
        public List<string> Effects { get; set; }
        public List<string> Badges { get; set; }

    }
}
