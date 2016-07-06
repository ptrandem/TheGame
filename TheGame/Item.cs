using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheGame
{
    //{"Case":"Some","Fields":[{"Name":"Rail Gun","Id":"3b5037c9-d0e1-40e7-ab47-a5c05ce8a940","Rarity":3,"Description":"Fires depleted uranium slugs at high velocity."}]}
    public class Item
    {
        public string Case { get; set; }
        
        public List<ItemFields> Fields { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach(var f in Fields)
            {
                sb.Append($"{f.Id} - (Case: {Case})\n{f.Name} ({f.Rarity}): {f.Description}\n");
            }
            return sb.ToString();
        }
    }
}
