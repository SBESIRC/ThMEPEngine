using System;
using NFox.Cad;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;

namespace ThMEPWSS.FlushPoint.Service
{
    public class ThObstacleExtractService
    {
        public double OffsetDis { get; set; }
        public Dictionary<string,List<Curve>> BlkEntityDic { get; set; }
        public ThObstacleExtractService()
        {
            OffsetDis = 200.0;
            Init();
        }
        private void Init()
        {
            BlkEntityDic = new Dictionary<string, List<Curve>>();
            var blkNames = new List<string>() { 
                "室内消火栓平面", "手提式灭火器","推车式灭火器","立式泵","卧式泵","生活泵组","隔膜罐",
                "膨胀罐平面","换热器平面","商用炉平面","沉砂隔油池","污水提升器","隔油处理成套装置",
                "消防稳压设备组","带定位立管","带定位立管150","水箱及基础平面"};
            blkNames.ForEach(o => BlkEntityDic.Add(o, new List<Curve>()));
        }

        public void Extract(Database db, Point3dCollection pts)
        {
            //提取块
            ExtractBlock(db);
            BlkEntityDic.Add("Unknown", ExtractCircle(db));

            //偏移
            DoOffset(OffsetDis);

            // 过滤
            DoFilter(pts);
        }

        private void DoOffset(double offsetDis)
        {
            var keys = BlkEntityDic.Keys.ToList();
            for (int i = 0; i < keys.Count; i++)
            {
                var values = BlkEntityDic[keys[i]];
                var newValus = new List<Curve>();
                values.ForEach(o =>
                {
                    var objs = o.GetOffsetCurves(offsetDis);
                    if (objs.Count > 0)
                    {
                        newValus.Add(objs.Cast<Curve>().OrderByDescending(o => o.Area).First());
                    }
                });
                BlkEntityDic[keys[i]] = newValus;
            }
        }

        private void DoFilter(Point3dCollection pts)
        {
            if (pts.Count >= 3)
            {
                var center = pts.Envelope().CenterPoint();
                var transformer = new ThMEPOriginTransformer(center);
                var newPts = transformer.Transform(pts);
                var keys = BlkEntityDic.Keys.ToList();
                for (int i = 0; i < keys.Count; i++)
                {
                    var objs = BlkEntityDic[keys[i]].ToCollection();
                    transformer.Transform(objs);
                    var spatialIndex = new ThCADCoreNTSSpatialIndex(objs)
                    {
                        AllowDuplicate = true,
                    };
                    objs = spatialIndex.SelectCrossingPolygon(newPts);
                    transformer.Reset(objs);
                    BlkEntityDic[keys[i]] = objs.Cast<Curve>().ToList();
                }
            }
        }

        private void ExtractBlock(Database db)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                var others = new List<Curve>();
                acadDb.ModelSpace.OfType<BlockReference>().ForEach(o =>
                {
                    if(IsBuildElementBlockReference(o) && IsBlockLayer(o.Layer))
                    {
                        var btr = acadDb.Element<BlockTableRecord>(o.BlockTableRecord);
                        if(IsBuildElementBlock(btr))
                        {
                            var key = GetBlockKey(GetEffectiveName(db, o));
                            if (!string.IsNullOrEmpty(key))
                            {
                                BlkEntityDic[key].Add(GetOutline(key, o));
                            }
                        }
                    }
                });
            }
        }

        private string GetEffectiveName(Database db, BlockReference bref)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                // BlockReference.IsDynamicBlock可能会抛出异常
                // 这里通过比较块名是否包含动态块的前缀（*U）来判断
                if (bref.Name.StartsWith("*U"))
                {
                    return acadDb.Element<BlockTableRecord>(bref.DynamicBlockTableRecord).Name;
                }
                else
                {
                    return bref.Name;
                }
            }
        }

        private List<Curve> ExtractCircle(Database db)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                var results = new List<Curve>();
                acadDb.ModelSpace.OfType<Entity>().ForEach(o =>
                {
                    if(IsPointedLayer(o.Layer))
                    {
                        if (o is BlockReference br && IsBuildElementBlockReference(br))
                        {
                            var btr = acadDb.Element<BlockTableRecord>(br.BlockTableRecord);
                            if (IsBuildElementBlock(btr))
                            {
                                var objs = ThDrawTool.Explode(br)
                                .Cast<Entity>()
                                .Where(e => e is Circle && e.Visible)
                                .ToList();
                                var circles = objs
                                .Cast<Circle>()
                                .Where(e => IsPointedCircle(e))
                                .ToList();
                                circles.ForEach(o =>
                                {
                                    results.Add(o.ToRectangle());
                                });
                            }
                        }
                        else if (o.IsTCHElement())
                        {
                            var objs = o.ExplodeTCHElement()
                            .Cast<Entity>()
                            .Where(e => e is Circle && e.Visible)
                            .ToList();
                            var circles = objs.Cast<Circle>()
                            .Where(e => IsPointedCircle(e))
                            .ToList();
                            circles.ForEach(o =>
                            {
                                results.Add(o.ToRectangle());
                            });
                        }
                    }
                });
                return results;
            }
        }

        private bool IsPointedCircle(Circle circle)
        {
            return circle.Radius<=200;
        }  

        private string GetBlockKey(string name)
        {
            return BlkEntityDic.Keys.Where(o => name.Contains(o)).FirstOrDefault();
        }     
        
        private Polyline ToObb(BlockReference br)
        {
            return br.ToOBB(br.BlockTransform.PreMultiplyBy(Matrix3d.Identity));
        }
        private Circle ToBoundingsphere(BlockReference br)
        {
            var objs = ThDrawTool.Explode(br);
            var curves = objs.Cast<Entity>().Where(e => e is Curve && e.Visible).Cast<Curve>().ToList();
            var pts = new List<Point3d>();
            curves.ForEach(o =>
            {
                pts.AddRange(GetPoints(o));
            });
            if(pts.Count>0)
            {
                return ThDrawTool.ToBoundingSphere(pts);
            }
            else
            {
                var obb = ToObb(br);
                var coords = obb.Vertices();
                var midPt = ThGeometryTool.GetMidPt(coords[0], coords[2]);
                var radius = 0.5* Math.Max(coords[1].DistanceTo(coords[0]), coords[1].DistanceTo(coords[2]));
                return new Circle(midPt,Vector3d.ZAxis, radius);
            }
        }
        private List<Point3d> GetPoints(Curve curve, double tesslateLength = 5)
        {
            if (curve is Line line)
            {
                return line.GetPoints();
            }
            else if (curve is Polyline polyline)
            {
                return polyline.GetPoints(tesslateLength);
            }
            else if (curve is Arc arc)
            {
                return arc.GetPoints(tesslateLength);
            }
            else if (curve is Circle circle)
            {
                return circle.GetPoints();
            }
            else if (curve is Ellipse ellipse)
            {
                return ellipse.GetPoints();
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        private Curve GetOutline(string key,BlockReference br)
        {
            if(key == "隔膜罐" || key == "膨胀罐平面")
            {
                var circle = ToBoundingsphere(br);
                return circle.ToRectangle();
            }
            else
            {
                return ToObb(br);
            }
        }

        private bool IsPointedLayer(string layer)
        {
            return layer.ToUpper().Contains("W-") &&
                (layer.ToUpper().Contains("-EQPM") ||
                layer.ToUpper().Contains("-NOTE") ||
                 layer.ToUpper().Contains("-DIMS"));
        }
        private bool IsBlockLayer(string layer)
        {
            //ToDo,后期如果图层判断
            return true;
        }
        private bool IsBuildElementBlockReference(BlockReference blockReference)
        {
            return blockReference.BlockTableRecord.IsValid;
        }
        private bool IsBuildElementBlock(BlockTableRecord blockTableRecord)
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
