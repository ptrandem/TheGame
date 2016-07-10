using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using RestSharp.Extensions;

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
            if (!string.IsNullOrWhiteSpace(itemString))
            {
                var lines = itemString.Split('\n');
                foreach (var line in lines)
                {
                    var modLine = line.Replace("\r", "").Trim();

                    var item = ItemFields.GetFromString(modLine);
                    if (item == null)
                    {
                        continue;
                    }

                    result.Add(item);
                }
            }
            return result;
        }

        public static void ExportAllItemsToDisk(IEnumerable<ItemFields> items, string path)
        {
            FileStream file;
            if (!File.Exists(path))
            {
                file = File.Open(path, FileMode.OpenOrCreate);
            }
            else
            {
                file = File.Open(path, FileMode.Truncate);
            }
            foreach (var item in items)
            {
                var bytes = Encoding.Unicode.GetBytes(item.ToString());
                file.Write(bytes, 0, bytes.Length);
            }
            file.Close();
        }

        public static void AppendSingleItemToDisk(ItemFields item, string path)
        {
            File.AppendAllText(path, item.ToString());
        }

        public static IEnumerable<ItemFields> ImportItemsFromDisk(string path)
        {
            var result = new List<ItemFields>();
            if (File.Exists(path))
            {
                var lines = File.ReadAllLines(path, Encoding.Unicode);

                foreach (var line in lines)
                {
                    var item = ItemFields.GetFromString(line);
                    if (item != null)
                    {
                        result.Add(item);
                    }
                }
            }
            return result;
        }

    }
}
