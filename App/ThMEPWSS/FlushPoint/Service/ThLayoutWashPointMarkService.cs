using Linq2Acad;
using DotNetARX;
using ThCADExtension;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.FlushPoint.Service
{
    public class ThLayoutWashPointMarkService
    {
        private WashPointLayoutData LayoutData { get; set; }
        private double textSize { get; set; }
        private double leaderXForwardLength { get; set; }
        private double leaderYForwardLength { get; set; }

        public ThLayoutWashPointMarkService(WashPointLayoutData layoutData)
        {
            LayoutData = layoutData;
            textSize = LayoutData.GetMarkTextSize();
            leaderXForwardLength = LayoutData.GetLeaderXForwardLength();
            leaderYForwardLength = LayoutData.GetLeaderYForwardLength();
        }
        public void Layout()
        {
            SetDatabaseDefaults();
            var codeDic = BuildPointCode();
            codeDic.ForEach(o => DrawMark(o.Key,o.Value));
        }
        private void DrawMark(Point3d pt,string content)
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Use(LayoutData.Db))
            {
                var cornerPt = new Point3d(pt.X + leaderXForwardLength, pt.Y + leaderYForwardLength, 0.0);
                var firstLine = new Line(pt, cornerPt);
                firstLine.Layer = LayoutData.WaterSupplyMarkLayerName;
                firstLine.ColorIndex = (int)ColorIndex.BYLAYER;
                firstLine.Linetype = "ByLayer";
                firstLine.LineWeight = LineWeight.ByLayer;


                var textWidth = GetTextWidth(content);
                var secondLine = new Line(cornerPt, new Point3d(cornerPt.X + textWidth, cornerPt.Y, 0));
                secondLine.Layer = LayoutData.WaterSupplyMarkLayerName;
                secondLine.ColorIndex = (int)ColorIndex.BYLAYER;
                secondLine.Linetype = "ByLayer";
                secondLine.LineWeight = LineWeight.ByLayer;

                var dbText = new DBText();
                dbText.Position = cornerPt;
                dbText.TextString = content;
                dbText.Height = textSize;
                dbText.Layer = LayoutData.WaterSupplyMarkLayerName;
                dbText.WidthFactor = LayoutData.WaterSupplyMarkWidthFactor;
                dbText.TextStyleId = acadDb.TextStyles.ElementOrDefault(LayoutData.WaterSupplyMarkStyle).ObjectId;
                dbText.ColorIndex = (int)ColorIndex.BYLAYER;
                dbText.Linetype = "ByLayer";
                dbText.LineWeight = LineWeight.ByLayer;

                acadDb.ModelSpace.Add(firstLine);
                acadDb.ModelSpace.Add(secondLine);
                acadDb.ModelSpace.Add(dbText);
            }
        }
        private Dictionary<Point3d, string> BuildPointCode()
        {
            var result = new Dictionary<Point3d, string>();
            for (int i = 1; i <= LayoutData.WashPoints.Count; i++)
            {
                string code = 
                    string.IsNullOrEmpty(LayoutData.FloorSign) 
                    ? "CX" + "-" + i.ToString() 
                    : LayoutData.FloorSign + "-" + "CX" + "-" + i.ToString();
                result.Add(LayoutData.WashPoints[i - 1], code);
            }
            return result;
        }
        private void SetDatabaseDefaults()
        {
            using (var currentDb = AcadDatabase.Use(LayoutData.Db))
            using (var blockDb = AcadDatabase.Open(ThCADCommon.WSSDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(LayoutData.WaterSupplyMarkLayerName), false);
                currentDb.TextStyles.Import(blockDb.TextStyles.ElementOrDefault(LayoutData.WaterSupplyMarkStyle), false);
                SetLayerDefaults(LayoutData.WaterSupplyMarkLayerName);
            }
        }
        private void SetLayerDefaults(string name)
        {
            using (var currentDb = AcadDatabase.Active())
            {
                currentDb.Database.UnOffLayer(name);
                currentDb.Database.UnLockLayer(name);
                currentDb.Database.UnPrintLayer(name);
                currentDb.Database.UnFrozenLayer(name);
            }
        }
        private double GetTextWidth(string content)
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Use(LayoutData.Db))
            {
                DBText dbText = CreateText(content, Point3d.Origin);
                acadDb.ModelSpace.Add(dbText);
                var textWidth = dbText.GeometricExtents.MaxPoint.X - dbText.GeometricExtents.MinPoint.X;
                dbText.Erase();
                return textWidth;
            }
        }
        private DBText CreateText(string content,Point3d pt)
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Use(LayoutData.Db))
            {
                var dbText = new DBText();
                dbText.Position = pt;
                dbText.TextString = content;
                dbText.Height = textSize;
                dbText.Layer = LayoutData.WaterSupplyMarkLayerName;
                dbText.TextStyleId = acadDb.TextStyles.Element(LayoutData.WaterSupplyMarkStyle).Id;
                dbText.WidthFactor = LayoutData.WaterSupplyMarkWidthFactor;
                return dbText;
            }
        }
    }
}
