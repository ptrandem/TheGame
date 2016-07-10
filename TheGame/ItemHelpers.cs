using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TheGame
{
    public static class ItemHelpers
    {
        public const string BonusItemFinder = @"<([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})> \| <([^>]*)>";

        public static List<ItemFields> FindBonusItems(string input)
        {
            var result = new List<ItemFields>();
            var regex = new Regex(BonusItemFinder);
            var matches = regex.Matches(input);
            foreach (Match match in matches)
            {
                if (match.Groups.Count == 3)
                {
                    var item = new ItemFields
                    {
                        Id = match.Groups[1].Value,
                        Name = match.Groups[2].Value
                    };

                    result.Add(item);
                }
            }

            return result;
        }
    }
}
