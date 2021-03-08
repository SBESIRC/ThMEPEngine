using System;
using System.Linq;
using AcHelper;
using DotNetARX;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using AcHelper.Commands;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service.Hvac;
using NFox.Cad;

namespace ThMEPHVAC.Command
{
    public class ThModelBaseExtractCmd : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
        }

        public void Execute()
        {
            using (var adb = AcadDatabase.Active())
            {
                var frame = GetEntity<Polyline>(adb, "\n请选择范围框");
                if (frame == null) return;

                var visitor = new ThModelExtractionVisitor();
                var extractor = new ThDistributionElementExtractor();
                extractor.Accept(visitor);
                extractor.Extract(adb.Database);
                if (visitor.Results.Count == 0) return;

                var spatialIndex = GetSpatialIndex(visitor);
                var fanBlks = GetFanBlocks(frame, spatialIndex);
                if (fanBlks.Count == 0) return;

                var showLabelBox = QueryYesOrNo("\n是否显示提示框");
                if (showLabelBox)
                {
                    AddAndSetDateLayer(adb.Database);
                }

                ImportLayer(adb.Database, ThHvacCommon.FOUNDATION_LAYER);
                fanBlks.Cast<Entity>().ForEach(e =>
                {
                    var entitySet = new DBObjectCollection();
                    e.Explode(entitySet);
                    var ents = entitySet.Cast<Entity>()
                         .Where(o => o.Layer.Contains(ThHvacCommon.FOUNDATION_LAYER)).ToList();
                    ents.ForEach(ent =>
                    {
                        adb.ModelSpace.Add(ent);
                        ent.SetDatabaseDefaults();
                        ent.Layer = ThHvacCommon.FOUNDATION_LAYER;
                    });
                    if (showLabelBox)
                    {
                        DrawLabelBox(adb.Database, ents);
                    }
                });
            }
        }

        public T GetEntity<T>(AcadDatabase adb, string title) where T : DBObject
        {
            var opt = new PromptEntityOptions(title);
            var ret = Active.Editor.GetEntity(opt);
            if (ret.Status != PromptStatus.OK) return null;
            return adb.ElementOrDefault<T>(ret.ObjectId);
        }

        private  ThCADCoreNTSSpatialIndex GetSpatialIndex(ThModelExtractionVisitor visitor)
        {
            var objs = visitor.Results.Select(o => o.Geometry).ToCollection();
            return new ThCADCoreNTSSpatialIndex(objs);
        }

        public  void ImportLayer(Database database, string name, bool replaceIfDuplicate = false)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.HvacModelDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(name), replaceIfDuplicate);
            }
        }

        private DBObjectCollection GetFanBlocks(Polyline frame, ThCADCoreNTSSpatialIndex spatialIndex)
        {
            return spatialIndex.SelectCrossingPolygon(frame.Vertices());
        }

        public  bool QueryYesOrNo(string text)
        {
            var separation_key = new PromptKeywordOptions(text);
            separation_key.Keywords.Add("是", "Y", "是(Y)");
            separation_key.Keywords.Add("否", "N", "否(N)");
            separation_key.Keywords.Default = "否";
            var result = Active.Editor.GetKeywords(separation_key);
            if (result.Status != PromptStatus.OK) return false;
            return result.StringResult == "是";
        }

        private  bool AddAndSetDateLayer(Database db)
        {
            //直接打开或新建，然后解冻、解锁
            var currentDateStr = DateTime.Now.ToString("yyyyMMdd");
            var targetLayerName = "风机基础" + currentDateStr;
            var targetLayer = db.AddLayer(targetLayerName);
            if (targetLayer.IsNull) return false;
            db.UnFrozenLayer(targetLayerName);
            db.UnLockLayer(targetLayerName);
            db.UnPrintLayer(targetLayerName);
            db.UnOffLayer(targetLayerName);
            db.SetCurrentLayer(targetLayerName);

            var curScore = long.Parse(currentDateStr);
            var items = (from layer in db.GetAllLayers()
                         let m = Regex.Match(layer.Name, @"^\s*风机基础(\d{8})\s*$")
                         where m.Success
                         let dtStr = m.Groups[1].Value
                         let score = long.Parse(dtStr)
                         where score < curScore
                         select new { layer, dtStr, score }).ToList();
            short targetColorIndex = 1;
            if (items.Any())
            {
                //找出最接近今天的日期
                var max = items.Max(x => x.score);
                var targetItem = items.First(x => x.score == max);
                targetColorIndex = (short)(targetItem.layer.Color.ColorIndex + 1);
            }
            if (targetColorIndex > 6) targetColorIndex = 1;

            return db.SetLayerColor(targetLayerName, targetColorIndex);
        }

        private  void DrawLabelBox(Database db, IEnumerable<Entity> ents)
        {
            using (var adb = AcadDatabase.Use(db))
            {
                var pts = new Point3dCollection();
                foreach (var ent in ents)
                {
                    if (ent?.Bounds is Extents3d bd)
                    {
                        pts.Add(bd.MaxPoint);
                        pts.Add(bd.MinPoint);
                    }
                }

                //基础搂到本地后设计师可能难以在图纸上直接找到搂到本地的基础，因此要做提醒。提醒方式：
                //形状：等边六角形
                //图元形式：pline
                //全局宽度：50
                //位置：搂到的基础的几何中心
                //半径：搂到的基础的boundingsphere的1.5倍
                if (!GetCircleBoundary(pts, out Point3d center, out double radius)) return;
                var plineId = DrawPolygon(db, center.ToPoint2D(), 6, radius * 1.5);
                var pline = adb.Element<Polyline>(plineId, true);
                pline.ConstantWidth = 50;
            }
        }

        public  bool GetCircleBoundary(Point3dCollection points, out Point3d center, out double radius)
        {
            if (points.Count == 0)
            {
                center = default;
                radius = default;
                return false;
            }
            var minX = points.Cast<Point3d>().Select(p => p.X).Min();
            var maxX = points.Cast<Point3d>().Select(p => p.X).Max();
            var minY = points.Cast<Point3d>().Select(p => p.Y).Min();
            var maxY = points.Cast<Point3d>().Select(p => p.Y).Max();
            var minZ = points.Cast<Point3d>().Select(p => p.Z).Min();
            var maxZ = points.Cast<Point3d>().Select(p => p.Z).Max();
            var pt1 = new Point3d(minX, minY, minZ);
            var pt2 = new Point3d(maxX, maxY, maxZ);
            center = GeTools.MidPoint(pt1, pt2);
            radius = (center - pt1).Length;
            return true;
        }

        public  ObjectId DrawPolygon(Database db, Point2d center, int num, double radius)
        {
            using (var adb = AcadDatabase.Use(db))
            {
                var pline = new Polyline()
                {
                    Closed = true,
                };
                pline.CreatePolygon(center, num, radius);
                adb.ModelSpace.Add(pline);
                return pline.ObjectId;
            }
        }
    }
}
