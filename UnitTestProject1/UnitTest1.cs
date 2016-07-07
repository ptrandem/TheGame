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
    }
}
