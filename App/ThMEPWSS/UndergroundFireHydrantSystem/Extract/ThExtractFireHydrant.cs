using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.UndergroundFireHydrantSystem.Model;
using ThMEPWSS.UndergroundFireHydrantSystem.Service;

namespace ThMEPWSS.UndergroundFireHydrantSystem.Extract
{
    public class ThExtractFireHydrant//室内消火栓平面
    {
        public DBObjectCollection DBobjs { get; private set; }
        public bool Extract(Database database, Point3dCollection polygon)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                DBobjs = ExtractBlocks(acadDatabase.Database, "室内消火栓平面", out bool hydrantWithReel);
                return hydrantWithReel;
            }
        }

        public void CreateVerticalHydrantDic(List<Point3dEx> verticals, FireHydrantSystemIn fireHydrantSysIn)
        {
            var verticalSpatialIndex = new ThCADCoreNTSSpatialIndex(CreateRect(verticals));
            var dbObjs = DBobjs.ToArray().ToList();
            for (int i = dbObjs.Count - 1; i >= 0; i--)
            {
                try
                {
                    var obj = dbObjs[i];
                    var pt = GetCenter((obj as BlockReference).GeometricExtents);

                    var pline = CreatePolyline(pt, 1000);
                    var res = verticalSpatialIndex.SelectCrossingPolygon(pline).ToArray();
                    if (res.Count() == 0)
                    {
                        continue;
                    }
                    var closedObj = res.OrderBy(e => (e as Polyline).GetCentroidPoint().DistanceTo(pt)).First();
                    var closedPt = (closedObj as Polyline).GetCentroidPoint();
                    fireHydrantSysIn.VerticalHasHydrant.Add(new Point3dEx(closedPt));
                }
                catch (Exception ex)
                {

                }
            }
        }

        private Point3d GetCenter(Extents3d extent3d)
        {
            var pt1 = extent3d.MaxPoint;
            var pt2 = extent3d.MinPoint;
            return new Point3d((pt1.X + pt2.X) / 2, (pt1.Y + pt2.Y) / 2, 0);
        }

        private DBObjectCollection CreateRect(List<Point3dEx> verticals)
        {
            var dbObjs = new DBObjectCollection();
            foreach (var pt in verticals)
            {
                var pline = CreatePolyline(pt);
                dbObjs.Add(pline);
            }
            return dbObjs;
        }

        private static Polyline CreatePolyline(Point3dEx c, int tolerance = 50)
        {
            var pl = new Polyline();
            var pts = new Point2dCollection();
            pts.Add(new Point2d(c._pt.X - tolerance, c._pt.Y - tolerance)); // low left
            pts.Add(new Point2d(c._pt.X - tolerance, c._pt.Y + tolerance)); // high left
            pts.Add(new Point2d(c._pt.X + tolerance, c._pt.Y + tolerance)); // high right
            pts.Add(new Point2d(c._pt.X + tolerance, c._pt.Y - tolerance)); // low right
            pts.Add(new Point2d(c._pt.X - tolerance, c._pt.Y - tolerance)); // low left
            pl.CreatePolyline(pts);
            return pl;
        }

        private static Polyline CreatePolyline(Point3d c, int tolerance = 50)
        {
            var pl = new Polyline();
            var pts = new Point2dCollection();
            pts.Add(new Point2d(c.X - tolerance, c.Y - tolerance)); // low left
            pts.Add(new Point2d(c.X - tolerance, c.Y + tolerance)); // high left
            pts.Add(new Point2d(c.X + tolerance, c.Y + tolerance)); // high right
            pts.Add(new Point2d(c.X + tolerance, c.Y - tolerance)); // low right
            pts.Add(new Point2d(c.X - tolerance, c.Y - tolerance)); // low left
            pl.CreatePolyline(pts);
            return pl;
        }

        private DBObjectCollection ExtractBlocks(Database db, string blockName, out bool hydrantWithReel)
        {
            Func<Entity, bool> IsBlkNameQualified = (e) =>
            {
                if (e is BlockReference br)
                {
                    return br.GetEffectiveName().ToUpper().Contains(blockName.ToUpper());
                }
                return false;
            };
            var blkVisitor = new ThBlockReferenceExtractionVisitor();
            blkVisitor.CheckQualifiedLayer = (e) => true;
            blkVisitor.CheckQualifiedBlockName = IsBlkNameQualified;

            var extractor = new ThDistributionElementExtractor();
            extractor.Accept(blkVisitor);
            extractor.Extract(db); // 提取块中块(包括外参)
            extractor.ExtractFromMS(db); // 提取本地块

            bool _hydrantWithReel = false;
            bool hasCheckedHydrant = false;
            using (var acadDb = AcadDatabase.Use(db))
            {
                blkVisitor.Results.ForEach(e =>
                {
                    var attribute = e.Data;
                    if(!(attribute is null) && !hasCheckedHydrant)
                    {
                        _hydrantWithReel = attribute.ToString().Contains("卷盘");
                        hasCheckedHydrant = true;
                    }
                });
            }
            hydrantWithReel = _hydrantWithReel;
            return blkVisitor.Results.Select(o => o.Geometry).ToCollection();
        }

        private void DrawFireHydrant(Database database)
        {
            string layerName = "消火栓圆圈图层";
            try
            {
                ThMEPEngineCoreLayerUtils.CreateAILayer(database, layerName, 30);
            }
            catch { }

            foreach (var db in DBobjs)
            {
                var br = db as BlockReference;
                using (AcadDatabase currentDb = AcadDatabase.Active())
                {
                    var rect = br.GetRect();
                    rect.LayerId = DbHelper.GetLayerId(layerName);
                    currentDb.CurrentSpace.Add(rect);
                }
            }
        }
    }

    public class ThBlockReferenceExtractionVisitor : ThDistributionElementExtractionVisitor
    {
        public Func<Entity, bool> CheckQualifiedLayer { get; set; }
        public Func<Entity, bool> CheckQualifiedBlockName { get; set; }
        
        public ThBlockReferenceExtractionVisitor()
        {
            CheckQualifiedLayer = base.CheckLayerValid;
            CheckQualifiedBlockName = (Entity entity) => true;
        }
        
        public override void DoExtract(List<ThRawIfcDistributionElementData> elements, Entity dbObj, Matrix3d matrix)
        {
            if (dbObj is BlockReference br)
            {
                elements.AddRange(Handle(br, matrix));
            }
        }

        public override void DoXClip(List<ThRawIfcDistributionElementData> elements,
            BlockReference blockReference, Matrix3d matrix)
        {
            var xclip = blockReference.XClipInfo();
            if (xclip.IsValid)
            {
                xclip.TransformBy(matrix);
                elements.RemoveAll(o => !IsContain(xclip, o.Geometry));
            }
        }

        private List<ThRawIfcDistributionElementData> Handle(BlockReference br, Matrix3d matrix)
        {
            var results = new List<ThRawIfcDistributionElementData>();
            if (IsDistributionElement(br) && CheckLayerValid(br))
            {
                var clone = br.Clone() as BlockReference;
                if (clone != null)
                {
                    clone.TransformBy(matrix);
                    results.Add(new ThRawIfcDistributionElementData()
                    {
                        Geometry = clone,
                        Data = br.ObjectId.GetDynBlockValue("可见性"),
                    });
                }
            }
            return results;
        }

        private bool IsContain(ThMEPXClipInfo xclip, Entity ent)
        {
            if (ent is BlockReference br)
            {
                return xclip.Contains(br.GeometricExtents.ToRectangle());
            }
            else
            {
                return false;
            }
        }

        public override bool IsDistributionElement(Entity entity)
        {
            return CheckQualifiedBlockName(entity);
        }

        public override bool CheckLayerValid(Entity curve)
        {
            return true;
        }

        public override bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
        {
            // 忽略图纸空间和匿名块
            if (blockTableRecord.IsLayout)
            {
                return false;
            }
            // 忽略不可“炸开”的块
            if (!blockTableRecord.Explodable)
            {
                return false;
            }
            return true;
        }
    }
}
