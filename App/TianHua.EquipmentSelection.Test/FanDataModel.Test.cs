using NUnit.Framework;
using System.Collections.Generic;
using TianHua.FanSelection;
using TianHua.FanSelection.Function;

namespace TianHua.EquipmentSelection.Test
{
    [TestFixture]
    public class FanDataModelTest
    {
        [Test]
        public void FanNumberTest()
        {
            var model = new FanDataModel()
            {
                PID = "0",
                InstallSpace = "4",
                InstallFloor = "F2",
                VentNum = "2",
                Scenario = "平时送风"
            };
            Assert.AreEqual("SF-4-F2-2", model.FanNum);
        }
    }

    [TestFixture]
    public class VentSNCalculatorTest
    {
        [Test]
        public void SequenceTest()
        {
            var calculator = new VentSNCalculator("1-5,7,9");
            Assert.AreEqual(7, calculator.SerialNumbers.Count);
            Assert.AreEqual(new List<int>() { 1, 2, 3, 4, 5, 7, 9 }, calculator.SerialNumbers);
        }
    }
}
