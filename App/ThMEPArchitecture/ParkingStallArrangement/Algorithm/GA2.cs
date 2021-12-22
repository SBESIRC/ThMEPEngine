using System;
using System.Collections.Generic;
using ThMEPArchitecture.ParkingStallArrangement.Method;
using ThMEPArchitecture.ParkingStallArrangement.Model;

namespace ThMEPArchitecture.ParkingStallArrangement.Algorithm
{
    public class GA2 : IDisposable
    {
        GaParameter GaPara;

        public GA2(GaParameter gaPara)
        {
            GaPara = gaPara;
        }

        public List<Gene> Run()
        {
            var genome = new List<Gene>();
            for (int i = 0; i < GaPara.LineCount; i++)
            {
                var line = GaPara.SegLine[i];
                var dir = line.GetValue(out double value, out double startVal, out double endVal);
                var valueWithIndex = value;
                Gene gene = new Gene(valueWithIndex, dir, GaPara.MinValues[i], GaPara.MaxValues[i], startVal, endVal);
                genome.Add(gene);
            }
            return genome;
        }

        public void Dispose()
        {
        }
    }
}
