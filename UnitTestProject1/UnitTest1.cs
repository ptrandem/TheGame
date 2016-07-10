using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TheGame;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void FindBonusItems_correctly_finds_bonus_items()
        {
            const string input = "{\"Messages\":[\"You used < Pokeball > on ptrandem; 35 points for ptrandem!\",\"You found a bonus item! <511e8bbe-5521-4417-a4d8-df4bd15c8eae> | <Red Shell> \"],\"TargetName\":\"ptrandem\",\"Points\":119181}";
            var output = ItemHelpers.FindBonusItems(input);
            Assert.AreEqual(1, output.Count);
            Assert.AreEqual("511e8bbe-5521-4417-a4d8-df4bd15c8eae", output[0].Id);
        }

        [TestMethod]
        public void FindBonusItems_correctly_finds_multiple_bonus_items()
        {
            const string input =
                "{\"Messages\":[\"You used <Treasure Chest> on ptrandem; 0 points for ptrandem!\",\"You found a bonus item! <bece4940-438a-4114-9761-add224f222f3> | <Green Shell>\",\"You found a bonus item! <288e6bdb-40d9-44f2-a4f1-5a0766c0996e> | <Green Shell>\",\"You found a bonus item! <6aaf53b8-fac2-4db8-b428-a9aee8732d3c> | <Rail Gun>\"],\"TargetName\":\"ptrandem\",\"Points\":611}";
            var output = ItemHelpers.FindBonusItems(input);
            Assert.AreEqual(3, output.Count);
            Assert.AreEqual("bece4940-438a-4114-9761-add224f222f3", output[0].Id);
            Assert.AreEqual("288e6bdb-40d9-44f2-a4f1-5a0766c0996e", output[1].Id);
            Assert.AreEqual("6aaf53b8-fac2-4db8-b428-a9aee8732d3c", output[2].Id);
        }
    }
}
