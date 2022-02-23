using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPArchitecture.ParkingStallArrangement.Model
{
    public class GaParameter
    {
        public int LineCount { get; set; }//直线数量
        public List<int> LineNumber { get; set; }//直线序号
        public Dictionary<int, Line> SegLine { get; set; }//分割线
        public Dictionary<int, double> MinValues { get; set; }//下限值
        public Dictionary<int, double> MaxValues { get; set; }//上限值
        public Dictionary<int, double> StartValues { get; set; }//起始点另一维
        public Dictionary<int, double> EndValues { get; set; }//终止点另一维
        public GaParameter()
        {

        }

        public GaParameter(List<Line> segLine)
        {
            LineNumber = new List<int>();
            SegLine = new Dictionary<int, Line>();
            MinValues = new Dictionary<int, double>();
            MaxValues = new Dictionary<int, double>();
            StartValues = new Dictionary<int, double>();
            EndValues = new Dictionary<int, double>();
            LineCount = segLine.Count;
            for (int i = 0; i < LineCount; i++)
            {
                LineNumber.Add(i);
                SegLine.Add(i, segLine[i]);
                MinValues.Add(i, 0);
                MaxValues.Add(i, 0);
                StartValues.Add(i, 0); 
                EndValues.Add(i, 0);
            }
        }

        public void Set(List<Line> sortedSegLine, List<double> maxVals, List<double> minVals)
        {
            LineNumber = new List<int>();
            SegLine = new Dictionary<int, Line>();
            MinValues = new Dictionary<int, double>();
            MaxValues = new Dictionary<int, double>();
            StartValues = new Dictionary<int, double>();
            EndValues = new Dictionary<int, double>();
            LineCount = sortedSegLine.Count;
            for (int i = 0; i < LineCount; i++)
            {
                LineNumber.Add(i);
                SegLine.Add(i, sortedSegLine[i]);
                MinValues.Add(i, minVals[i]);
                MaxValues.Add(i, maxVals[i]);
                StartValues.Add(i, 0);
                EndValues.Add(i, 0);
            }
        }
    }
}
