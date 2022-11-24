using System;
using System.Linq;
using System.Collections.Generic;
using NFox.Cad;
using Linq2Acad;
using DotNetARX;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThPlatform3D.StructPlane.Service;
using ThPlatform3D.Model.Printer;
using ThPlatform3D.Common;

namespace ThPlatform3D.StructPlane.Print
{
    internal class ThSlabPatternTableBuilder
    {
        /*
         *   ------------            ------------
         *   |          |            |          |
         *   |          | ABCDEFG    |          |
         *   ------------            ------------
         *   
         *   ------------            ------------
         *   |          |            |          |
         *   |          | HIJKLMN    |          |
         *   ------------            ------------
         */
        private SlabPatternTableParameter TblParameter { get; set; }
        private ObjectId _thStyle3Id = ObjectId.Null;
        public ThSlabPatternTableBuilder(SlabPatternTableParameter parameter)
        {
            TblParameter = parameter;
            _thStyle3Id = DbHelper.GetTextStyleId(ThPrintStyleManager.THSTYLE3);
        }
        public DBObjectCollection Build(AcadDatabase acadDb)
        {
            var results = CreateTable(acadDb);
            MoveTo(acadDb,results);
            return results;
        }

        private void MoveTo(AcadDatabase acadDb,DBObjectCollection objs)
        {
            if (objs.Count == 0)
            {
                return;
            }
            var maxX = objs.OfType<Entity>()
                .Select(o => o.GeometricExtents.MaxPoint.X)
                .OrderByDescending(o => o).First();
            var maxY = objs.OfType<Entity>()
                .Select(o => o.GeometricExtents.MaxPoint.Y)
                .OrderByDescending(o => o).First();
            var basePt = new Point3d(maxX, maxY, 0);
            var mt = Matrix3d.Displacement(TblParameter.RightUpbasePt - basePt);
            objs.OfType<Entity>().ForEach(e =>
            {
                var entity = acadDb.Element<Entity>(e.ObjectId, true);
                entity.TransformBy(mt);
            });
        }

        private DBObjectCollection CreateTable(AcadDatabase acadDb)
        {
            //在原点处创建
            var results = new DBObjectCollection();
            var dbTexts = TblParameter.HatchConfigs.Select(o =>
            {
                var bgElevation = GetBGElevation(o.Key);
                var text = CreateDBtext(GetSlabShowText(bgElevation));
                if (bgElevation < 0)
                {
                    text.ColorIndex = 3;
                }
                return text;
            }).ToCollection();
            if (dbTexts.Count == 0)
            {
                return results;
            }
            var textMaxWidth = dbTexts.OfType<DBText>().Select(o =>
            o.GeometricExtents.MaxPoint.X - o.GeometricExtents.MinPoint.X)
                .OrderByDescending(o => o).First();
            var hatchPairs = new List<Tuple<HatchPrintConfig, Polyline>>();
            for (int i = 0; i < TblParameter.Rows; i++)
            {
                var rowBasePt = new Point3d(0, i * (TblParameter.RectHeight +
                    TblParameter.RrInterval), 0);
                for (int j = 0; j < TblParameter.Columns; j++)
                {
                    var index = i * TblParameter.Columns + j;
                    if (index >= TblParameter.HatchConfigs.Count)
                    {
                        i = TblParameter.Rows;
                        break;
                    }
                    var rowNextPt = new Point3d(j * (TblParameter.RectWidth + textMaxWidth +
                        TblParameter.RtPrevInterval + TblParameter.RtBackInterval)
                        , rowBasePt.Y, 0);
                    var rectangle = CreateRectangle();
                    var mt1 = Matrix3d.Displacement(rowNextPt - Point3d.Origin);
                    rectangle.TransformBy(mt1);
                    var text = dbTexts[index] as DBText; // 在原点的文字
                    var textPos = new Point3d(rowNextPt.X + TblParameter.RectWidth +
                        TblParameter.RtPrevInterval, rowNextPt.Y - TblParameter.RectHeight / 2.0, 0);
                    var mt2 = Matrix3d.Displacement(textPos - Point3d.Origin);
                    text.TransformBy(mt2);
                    hatchPairs.Add(Tuple.Create(GetHatchConfig(TblParameter.HatchConfigs, index), rectangle));
                }
            }

            // 插入洞口
            var holeBasePt = new Point3d(0, -1 * TblParameter.Rows * (TblParameter.RectHeight
                + TblParameter.RrInterval * 2), 0);
            var holeId = acadDb.ModelSpace.ObjectId.InsertBlockReference(
                ThPrintLayerManager.SlabPatternTableTextLayerName,
                ThPrintBlockManager.SDemoH2BlkName, holeBasePt, new Scale3d(0.83, 0.83, 0.83), 0.0);
            var hole = acadDb.Element<BlockReference>(holeId, true);
            var holeHeight = hole.GeometricExtents.MaxPoint.Y - hole.GeometricExtents.MinPoint.Y;
            var holeMt = Matrix3d.Displacement(new Vector3d(
                0.0, holeBasePt.Y - hole.GeometricExtents.MaxPoint.Y, 0.0));
            hole.TransformBy(holeMt);

            // 洞口标注
            var holeText = CreateDBtext("楼板预留永久洞口，板筋截断，并按结构设计总说明的要求进行洞口补强；");
            var holeTextPos = new Point3d(
                hole.GeometricExtents.MaxPoint.X + TblParameter.RtPrevInterval,
                holeBasePt.Y, 0);
            var mt3 = Matrix3d.Displacement(holeTextPos - Point3d.Origin);
            holeText.TransformBy(mt3);
            // 让洞口标注可洞口块中心对齐
            var holeY = (hole.GeometricExtents.MinPoint.Y + hole.GeometricExtents.MaxPoint.Y) / 2.0;
            var holeTextY = (holeText.GeometricExtents.MinPoint.Y + holeText.GeometricExtents.MaxPoint.Y) / 2.0;
            var centerAlignMt = Matrix3d.Displacement(new Vector3d(0, holeY - holeTextY, 0));
            holeText.TransformBy(centerAlignMt);

            results.Add(hole);
            results.Add(holeText);
            acadDb.ModelSpace.Add(holeText);

            // 打印
            dbTexts.OfType<DBText>().ForEach(text =>
            {
                results.Add(text);
                acadDb.ModelSpace.Add(text);
            });
            hatchPairs.ForEach(h =>
            {
                results.Add(h.Item2);
                h.Item2.Layer = h.Item1.LayerName;
                var rectId = acadDb.ModelSpace.Add(h.Item2);
                var objIds = new ObjectIdCollection { rectId };
                var hatchId = objIds.Print(acadDb.Database, h.Item1);
                results.Add(acadDb.Element<Hatch>(hatchId));
            });
            return results;
        }

        private string GetSlabShowText(double bgElevation)
        {
            if (bgElevation >= 0)
            {
                return "板面标高BG+" + (bgElevation / 1000.0).ToString("0.000") + ";";
            }
            else
            {
                return "板面标高BG" + (bgElevation / 1000.0).ToString("0.000") + ";";
            }
        }

        private double GetBGElevation(string elevation)
        {
            double eleValue = double.Parse(elevation);
            double height = TblParameter.FlrHeight;
            return Math.Round(eleValue - height, 2);
        }
    
        private HatchPrintConfig GetHatchConfig(
            Dictionary<string, HatchPrintConfig> hatchConfigs,int index)
        {
            int i = 0;
            foreach(var item in hatchConfigs)
            {
                if(i++ == index)
                {
                    return item.Value;
                }
            }
            return null;
        }

        private DBText CreateDBtext(string content)
        {
            var dbText = new DBText();
            dbText.TextString = content;
            dbText.Position = Point3d.Origin;
            dbText.Height = 400;
            dbText.WidthFactor = 0.7;
            dbText.TextStyleId = _thStyle3Id;
            dbText.Layer = ThPrintLayerManager.SlabPatternTableTextLayerName;
            dbText.ColorIndex = (int)ColorIndex.BYLAYER;
            return dbText;
        }

        private Polyline CreateRectangle()
        {
            var pts = new Point3dCollection();
            pts.Add(new Point3d(0, TblParameter.RectHeight / 2.0,0.0));
            pts.Add(new Point3d(0, -TblParameter.RectHeight / 2.0, 0.0));
            pts.Add(new Point3d(TblParameter.RectWidth, -TblParameter.RectHeight / 2.0, 0.0));
            pts.Add(new Point3d(TblParameter.RectWidth, TblParameter.RectHeight / 2.0, 0.0));
            return pts.CreatePolyline();
        }
    }
    internal class SlabPatternTableParameter
    {
        public int Columns { get; set; } = 3;
        public double RectWidth { get; set; } = 800; // 长方形长度
        public double RectHeight { get; set; } = 500; // 长方形宽度
        public double RtPrevInterval { get; set; } = 150; //  文字与框的间距
        public double RtBackInterval { get; set; } = 200; //  文字与框的间距
        public double RrInterval { get; set; } = 100;
        public Point3d RightUpbasePt { get; set; }
        public double FlrBottomEle { get; set; }
        public double FlrHeight { get; set; }
        public Dictionary<string, HatchPrintConfig> HatchConfigs { get; set; }
        public int Rows
        {
            get
            {
                return GetRows();
            }
        }
        private int GetRows()
        {
            int rows = 1;
            if(HatchConfigs!=null)
            {
                int count = HatchConfigs.Count;
                while (true)
                {
                    if (rows * Columns >= count)
                    {
                        break;
                    }
                    else
                    {
                        rows++;
                    }
                }
            }
            return rows;
        }
    }
}
