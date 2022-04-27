using System;
using System.Collections.Generic;

namespace ThMEPStructure.Reinforcement.Service
{
    internal class ThSteelDataManager
    {
        private static readonly ThSteelDataManager instance = new ThSteelDataManager() { };
        static ThSteelDataManager() { }
        internal ThSteelDataManager()
        {
            InitSteelSpecAreaDict();
        }
        public static ThSteelDataManager Instance { get { return instance; } }
        public Dictionary<double, double> SteelSpecAreaDict { get; private set; }
        private void InitSteelSpecAreaDict()
        {
            SteelSpecAreaDict = new Dictionary<double, double>();
            SteelSpecAreaDict.Add(6, 28.3);
            SteelSpecAreaDict.Add(8, 50.3);
            SteelSpecAreaDict.Add(10, 78.5);
            SteelSpecAreaDict.Add(12, 113.1);
            SteelSpecAreaDict.Add(14, 153.9);
            SteelSpecAreaDict.Add(16, 201.1);
            SteelSpecAreaDict.Add(18, 254.5);
            SteelSpecAreaDict.Add(20, 314.2);
            SteelSpecAreaDict.Add(22, 380.1);
            SteelSpecAreaDict.Add(25, 490.9);
            SteelSpecAreaDict.Add(28, 615.8);
            SteelSpecAreaDict.Add(32, 804.2);
            SteelSpecAreaDict.Add(36, 1017.9);
            SteelSpecAreaDict.Add(40, 1256.6);
        }
        public double GetSteelArea(double diameter,double tolerance)
        {
            foreach (var item in SteelSpecAreaDict)
            {
                if (Math.Abs(item.Key - diameter) <= tolerance)
                {
                    return item.Value;
                }
            }
            return Math.Round(Math.PI * Math.Pow(diameter / 2.0, 2), 1);
        }
        public double? FindEnhancedDiameter(double currentDiameter)
        {
            foreach (var diameter in SteelSpecAreaDict.Keys)
            {
                if (diameter > currentDiameter)
                {
                    return diameter;
                }
            }
            return null;
        }
    }
}
