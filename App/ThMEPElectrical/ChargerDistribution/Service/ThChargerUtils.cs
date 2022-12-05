using System;
using System.Linq;
using System.Collections.Generic;

using AcHelper;
using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Geom;
using ThMEPEngineCore.Algorithm;
using ThMEPElectrical.ChargerDistribution.Model;
using ThMEPElectrical.ChargerDistribution.Common;

namespace ThMEPElectrical.ChargerDistribution.Service
{
    public static class ThChargerUtils
    {
        public static string BlockDwgPath()
        {
            return ThCADCommon.ElectricalDwgPath();
        }

        public static List<Line> LaneLineRecognize(AcadDatabase acad)
        {
            // 车道中心线
            var results = new List<Line>();
            GetLaneLines(acad).Select(o => o.Clone()).ForEach(o =>
            {
                if (o is Line line)
                {
                    results.Add(line);
                }
                else if (o is Polyline polyline)
                {
                    var objs = new DBObjectCollection();
                    polyline.Explode(objs);
                    results.AddRange(objs.OfType<Line>());
                }
            });
            return results;
        }

        public static List<Polyline> GroupingPolylineRecognize(AcadDatabase acad, bool clone = true)
        {
            // 分组线
            return acad.ModelSpace.OfType<Polyline>().Where(e => e.Layer.Equals("AI-充电桩分组")).Select(e =>
            {
                if (clone)
                {
                    return e.Clone() as Polyline;
                }
                else
                {
                    return e;
                }

            }).ToList();
        }

        public static List<BlockReference> ChargerRecognize(AcadDatabase acad)
        {
            // 充电桩
            return acad.ModelSpace.OfType<BlockReference>().Where(e => ThChargerDistributionCommon.Block_Name_Filter.Contains(e.Name)).ToList();
        }

        public static List<BlockReference> DimensionRecognize(AcadDatabase acad)
        {
            // 充电桩
            return acad.ModelSpace.OfType<BlockReference>().Where(e => e.GetEffectiveName().Equals(ThChargerDistributionCommon.Block_Name_Dimension)).ToList();
        }

        public static void CleanPolyline(AcadDatabase acad, Entity frame, ObjectId layerId)
        {
            var polylines = acad.ModelSpace.OfType<Polyline>().Where(e => e.LayerId.Equals(layerId)).ToList();
            polylines.Where(e => e.Length == 0).ForEach(e =>
            {
                Clean(e);
            });
            SelectCrossingPolygon(frame, polylines.Where(e => e.Length > 0).ToList()).ForEach(e =>
            {
                Clean(e);
            });
        }

        public static void Clean(List<BlockReference> blocks)
        {
            blocks.ForEach(e => Clean(e));
        }

        public static void Clean(Entity e)
        {
            e.UpgradeOpen();
            e.Erase();
            e.DowngradeOpen();
        }

        private static List<Entity> GetLaneLines(AcadDatabase acad)
        {
            // 车道中心线
            return acad.ModelSpace.Where(e => IsCenterline(e)).ToList();
        }

        private static bool IsCenterline(Entity e)
        {
            return (e is Line || e is Polyline) && e.Layer.Equals("E-LANE-CENTER");
        }

        public static List<Line> SelectCrossingPolygon(Entity frame, List<Line> lanelines)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(lanelines.ToCollection());
            return spatialIndex.SelectCrossingPolygon(frame).OfType<Line>().ToList();
        }

        public static List<Polyline> SelectCrossingPolygon(Entity frame, List<Polyline> lanelines)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(lanelines.ToCollection());
            return spatialIndex.SelectCrossingPolygon(frame).OfType<Polyline>().ToList();
        }

        public static List<BlockReference> SelectCrossingPolygon(Entity frame, List<BlockReference> blocks)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(blocks.ToCollection());
            return spatialIndex.SelectCrossingPolygon(frame).OfType<BlockReference>().ToList();
        }

        public static List<DBPoint> SelectCrossingPolygon(Entity frame, List<DBPoint> points)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(points.ToCollection());
            return spatialIndex.SelectCrossingPolygon(frame).OfType<DBPoint>().ToList();
        }

        public static List<ThChargerData> SelectCrossingPolygon(Entity frame, List<ThChargerData> data)
        {
            var points = data.Select(p => new DBPoint(p.Position)).ToCollection();
            var spatialIndex = new ThCADCoreNTSSpatialIndex(points);
            var filter = spatialIndex.SelectCrossingPolygon(frame).OfType<DBPoint>().Select(o => o.Position).ToList();
            return data.Where(o => filter.Contains(o.Position)).ToList();
        }

        public static Point3d GetLineCenter(this Line line)
        {
            return new Point3d((line.StartPoint.X + line.EndPoint.X) / 2, (line.StartPoint.Y + line.EndPoint.Y) / 2, (line.StartPoint.Z + line.EndPoint.Z) / 2);
        }

        public static List<Line> Trim(this List<Line> lines, Entity polygon)
        {
            var results = new List<Line>();
            lines.ForEach(l =>
            {
                Polyline frame;
                if (polygon is Polyline p)
                {
                    frame = p;
                }
                else if (polygon is MPolygon mp)
                {
                    frame = mp.Shell();
                }
                else
                {
                    return;
                }
                var trim = ThCADCoreNTSGeometryClipper.Clip(frame, l);
                trim.OfType<Entity>().ForEach(o =>
                {
                    if (o is Line line)
                    {
                        results.Add(line);
                    }
                    else if (o is Polyline polyline)
                    {
                        var objs = new DBObjectCollection();
                        polyline.Explode(objs);
                        results.AddRange(objs.OfType<Line>());
                    }
                });
            });

            results.RemoveAll(o => o.Length < 10.0);
            return results;
        }

        public static void Transform(ThMEPOriginTransformer transformer, DBObjectCollection objs)
        {
            objs.OfType<Entity>().ForEach(o =>
            {
                transformer.Transform(o);
                ThMEPEntityExtension.ProjectOntoXYPlane(o);
            });
        }

        public static void Transform(ThMEPOriginTransformer transformer, List<ThChargerData> data)
        {
            data.ForEach(o =>
            {
                transformer.Transform(o.Geometry);
                o.Position = transformer.Transform(o.Position);
                ThMEPEntityExtension.ProjectOntoXYPlane(o.Geometry);
            });
        }

        public static void Reset(ThMEPOriginTransformer transformer, DBObjectCollection objs)
        {
            objs.OfType<Entity>().ForEach(o =>
            {
                transformer.Reset(o);
            });
        }

        public static void Reset(ThMEPOriginTransformer transformer, List<ThChargerData> data)
        {
            data.ForEach(o =>
            {
                transformer.Reset(o.Geometry);
                o.Position = transformer.Reset(o.Position);
            });
        }

        public static void Reset(ThMEPOriginTransformer transformer, Entity obj)
        {
            transformer.Reset(obj);
        }

        public static string NumberChange(this int num)
        {
            return num >= 10 ? num.ToString() : "0" + num;
        }

        public static void SetAttributes(this ObjectId id, string number)
        {
            var attributes = id.GetAttributesInBlockReference();
            if (attributes.ContainsKey(ThChargerDistributionCommon.Circuit_Number_1))
            {
                attributes[ThChargerDistributionCommon.Circuit_Number_1] = number;
            }
            if (attributes.ContainsKey(ThChargerDistributionCommon.Circuit_Number_2))
            {
                attributes[ThChargerDistributionCommon.Circuit_Number_2] = "";
            }
        }

        public static void SetDynamicProperty(this ObjectId id)
        {
            using (var acad = AcadDatabase.Active())
            {

                if (id.GetObject(OpenMode.ForWrite) is BlockReference br)
                {
                    var property = br.DynamicBlockReferencePropertyCollection;
                    property.SetValue("位置1 X", -1200.0);
                    property.SetValue("翻转状态1", (short)1);
                }
            }
        }

        public static int Var(this IEnumerable<double> list)
        {
            //double tt = 2;
            //double mm = tt ^ 2;
            var sqrtSum = 0;
            var sum = 0;
            list.ForEach(o =>
            {
                var data = Convert.ToInt32(o);
                var temp = data * data;
                sqrtSum += temp;
                sum += data;
            });
            return sqrtSum / list.Count() - (sum / list.Count()) * (sum / list.Count());
        }

        public static double Range(this IEnumerable<double> list)
        {
            return list.Max() - list.Min();
        }

        public static int Calculate(this Point3d point, Vector3d vector)
        {
            return Convert.ToInt32(point.ToVector3d().DotProduct(vector));
        }

        public static void BlocksClean(AcadDatabase acad, Entity frame, List<ThChargerData> blockData, List<Polyline> geometries)
        {
            var localDimensions = SelectCrossingPolygon(frame, geometries);
            localDimensions.ForEach(geometry =>
            {
                var data = blockData.Where(o => o.Geometry.Equals(geometry)).FirstOrDefault();
                var blkref = acad.Element<BlockReference>(data.ObjectId);
                ThChargerUtils.Clean(blkref);
            });
        }
    }
}
