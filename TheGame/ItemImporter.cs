using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TheGame
{
    public class ItemImporter
    {
        /// <summary>
        /// Expects items in tab-delimited format, requiring Name\tID and having optional \tRarity\tDescription
        /// </summary>
        /// <returns></returns>
        public static List<ItemFields> GetItemsFromClipboard()
        {
            var result = new List<ItemFields>();

            var itemString = Clipboard.GetText();
            if(!string.IsNullOrWhiteSpace(itemString))
            {
                var lines = itemString.Split('\n');
                foreach(var line in lines)
                {
                    var modLine = line.Replace("\r", "").Trim();
                    var tokens = modLine.Split('\t');
                    if(tokens.Length < 2)
                    {
                        continue;
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

                    result.Add(item);
                }
            }
            return result;
        }
    }
}
