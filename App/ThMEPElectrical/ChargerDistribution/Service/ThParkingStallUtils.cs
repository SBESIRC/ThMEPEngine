using System;
using System.Linq;
using System.Collections.Generic;

using AcHelper;
using NFox.Cad;
using DotNetARX;
using Linq2Acad;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.Algorithm;
using ThMEPElectrical.ChargerDistribution.Common;
using ThMEPElectrical.ChargerDistribution.Model;
using ThMEPEngineCore.Geom;

namespace ThMEPElectrical.ChargerDistribution.Service
{
    public static class ThParkingStallUtils
    {
        private static readonly List<string> BlockNames = new List<string> { ThChargerDistributionCommon.Block_Name_Charging_Equipment };

        public static string BlockDwgPath()
        {
            return ThCADCommon.ElectricalDwgPath();
        }

        /// <summary>
        /// 选择区域
        /// </summary>
        /// <returns></returns>
        public static List<Polyline> GetFrames(AcadDatabase acad)
        {
            using (var acadDatabase = AcadDatabase.Use(acad.Database))
            {
                var resPolys = new List<Polyline>();
                var options = new PromptSelectionOptions()
                {
                    AllowDuplicates = false,
                    MessageForAdding = "选择区域",
                    RejectObjectsOnLockedLayers = true,
                };
                var result = Active.Editor.GetSelection(options);
                if (result.Status != PromptStatus.OK)
                {
                    return resPolys;
                }

                foreach (var obj in result.Value.GetObjectIds())
                {
                    var frame = acadDatabase.Element<Polyline>(obj);
                    var frameFix = frame.Clone() as Polyline;
                    if (!frameFix.Closed && frameFix.StartPoint.DistanceTo(frameFix.EndPoint) < 1000.0)
                    {
                        frameFix.Closed = true;
                    }
                    var collection = new DBObjectCollection { frameFix };
                    var polylineArea = collection.PolygonsEx().OfType<Polyline>().OrderByDescending(o => o.Area).FirstOrDefault();
                    var mPolygonArea = collection.PolygonsEx().OfType<MPolygon>().OrderByDescending(o => o.Area).FirstOrDefault();
                    if (!polylineArea.IsNull() && !mPolygonArea.IsNull())
                    {
                        if (polylineArea.Area > mPolygonArea.Area)
                        {
                            resPolys.Add(polylineArea);
                        }
                        else
                        {
                            resPolys.Add(mPolygonArea.Shell());
                        }
                    }
                    else if (!polylineArea.IsNull())
                    {
                        resPolys.Add(polylineArea);
                    }
                    else if (!mPolygonArea.IsNull())
                    {
                        resPolys.Add(mPolygonArea.Shell());
                    }
                }
                return resPolys;
            }
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

        public static List<Polyline> GroupingPolylineRecognize(AcadDatabase acad)
        {
            // 分组线
            return acad.ModelSpace.OfType<Polyline>().Where(e => e.Layer.Equals("AI-充电桩分组")).Select(e => e.Clone() as Polyline).ToList();
        }

        public static List<BlockReference> ChargerRecognize(AcadDatabase acad)
        {
            // 充电桩
            return acad.ModelSpace.OfType<BlockReference>().Where(e => BlockNames.Contains(e.Name)).ToList();
        }

        public static List<BlockReference> DimensionRecognize(AcadDatabase acad)
        {
            // 充电桩
            return acad.ModelSpace.OfType<BlockReference>().Where(e => e.GetEffectiveName().Equals(ThChargerDistributionCommon.Block_Name_Dimension)).ToList();
        }

        public static void CleanPolyline(AcadDatabase acad, Polyline frame, ObjectId layerId)
        {
            // 充电桩
            SelectCrossingPolygon(frame, acad.ModelSpace.OfType<Polyline>().Where(e => e.LayerId.Equals(layerId)).ToList()).ForEach(e =>
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

        public static List<Line> SelectCrossingPolygon(Polyline frame, List<Line> lanelines)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(lanelines.ToCollection());
            return spatialIndex.SelectCrossingPolygon(frame).OfType<Line>().ToList();
        }

        public static List<Polyline> SelectCrossingPolygon(Polyline frame, List<Polyline> lanelines)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(lanelines.ToCollection());
            return spatialIndex.SelectCrossingPolygon(frame).OfType<Polyline>().ToList();
        }

        public static List<BlockReference> SelectCrossingPolygon(Polyline frame, List<BlockReference> blocks)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(blocks.ToCollection());
            return spatialIndex.SelectCrossingPolygon(frame).OfType<BlockReference>().ToList();
        }

        public static List<DBPoint> SelectCrossingPolygon(Polyline frame, List<DBPoint> points)
        {
            var spatialIndex = new ThCADCoreNTSSpatialIndex(points.ToCollection());
            return spatialIndex.SelectCrossingPolygon(frame).OfType<DBPoint>().ToList();
        }

        public static List<ThChargerData> SelectCrossingPolygon(Polyline frame, List<ThChargerData> data)
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

        public static List<Line> Trim(this List<Line> lines, Polyline polygon)
        {
            var results = new List<Line>();
            lines.ForEach(l =>
            {
                var trim = ThCADCoreNTSGeometryClipper.Clip(polygon, l);
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

                    //using (Transaction trans = acad.Database.TransactionManager.StartTransaction())
                    //{
                    //    // 获取块参照
                    //    // 遍历块参照的属性，并将其属性名和属性值添加到字典中
                    //    foreach (ObjectId attId in br.AttributeCollection)
                    //    {
                    //        var attRef = (AttributeReference)trans.GetObject(attId, OpenMode.ForWrite);
                    //        if (attRef.Visible)
                    //        {
                    //            //attRef.Justify = AttachmentPoint.MiddleCenter;
                    //        }
                    //    }
                    //    trans.Commit();
                    //}
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

        public static void BlocksClean(AcadDatabase acad, Polyline frame, List<ThChargerData> blockData, List<Polyline> geometries)
        {
            var localDimensions = SelectCrossingPolygon(frame, geometries);
            localDimensions.ForEach(geometry =>
            {
                var data = blockData.Where(o => o.Geometry.Equals(geometry)).FirstOrDefault();
                var blkref = acad.Element<BlockReference>(data.ObjectId);
                ThParkingStallUtils.Clean(blkref);
            });
        }
    }
}
