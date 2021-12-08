using Autodesk.AutoCAD.DatabaseServices;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;

namespace ThMEPElectrical.SystemDiagram.Extension
{
    public class ThQuantityMarkExtension
    {
        private static ThCADCoreNTSSpatialIndex Lines { get; set; }
        private static ThCADCoreNTSSpatialIndex Texts { get; set; }
        private static ThCADCoreNTSSpatialIndex BlockIO { get; set; }
        public static void ReSet()
        {
            Lines = new ThCADCoreNTSSpatialIndex(new DBObjectCollection());
            Texts = new ThCADCoreNTSSpatialIndex(new DBObjectCollection());
            BlockIO = new ThCADCoreNTSSpatialIndex(new DBObjectCollection());
        }
        public static void SetGlobalLineData(List<Line> lines)
        {
            Lines = new ThCADCoreNTSSpatialIndex(lines.ToCollection());
        }

        public static void SetGlobalMarkData(DBObjectCollection texts)
        {
            Texts = new ThCADCoreNTSSpatialIndex(texts);
        }
        
        public static void SetGlobalBlockIOData(DBObjectCollection blockBoundary)
        {
            BlockIO = new ThCADCoreNTSSpatialIndex(blockBoundary);
        }

        public static int GetQuantity(Polyline boundary)
        {
            var BufferPL = boundary.BufferPL(25)[0] as Polyline;
            var lineCollection = Lines.SelectFence(BufferPL);//块OBB buffer25找起点
            if (lineCollection.Count > 0)
            {
                Line line = lineCollection.Cast<Line>().OrderByDescending(o => o.Length).First();
                var bfLine = line.ExtendLine(10).Buffer(10);
                lineCollection = Lines.SelectCrossingPolygon(bfLine);
                lineCollection.Remove(line);
                if (lineCollection.Count > 0)
                {
                    line = lineCollection.Cast<Line>().OrderByDescending(o => o.Length).First();
                }
                bfLine = line.ExtendLine(200).Buffer(200);
                var TextCollection = Texts.SelectCrossingPolygon(bfLine);//（Buffer200）+文字
                if(TextCollection.Count==1)
                {
                    DBText dBText = TextCollection[0] as DBText;
                    string multipleValue = dBText.TextString;
                    string result = multipleValue.Replace("x","").Replace("X", "").Replace("*", "");
                    if (int.TryParse(result, out int quantity))
                        return quantity;
                }
            }
            else
            {
                var IOCollection= BlockIO.SelectFence(BufferPL);
                if(IOCollection.Count == 1)
                {
                    Polyline IOpolyline = IOCollection[0] as Polyline;
                    if (BufferPL.Contains(IOpolyline))
                        return 1;
                    else
                        return GetQuantity(IOpolyline);
                }
            }
            return 1;
        }
        public static int GetQuantity(BlockReference blkref)
        {
            return 1;
        }
    }
}
