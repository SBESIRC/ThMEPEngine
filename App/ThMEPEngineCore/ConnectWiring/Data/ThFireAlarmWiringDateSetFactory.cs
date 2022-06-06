#if (ACAD2016 || ACAD2018)
using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Data;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.GeojsonExtractor;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.IO.ExcelService;
using ThMEPEngineCore.LaneLine;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.ConnectWiring.Data
{
    public class ThFireAlarmWiringDateSetFactory : ThMEPDataSetFactory
    {
        public BlockReference powerBlock;
        public List<Polyline> holes = new List<Polyline>();
        private List<ThGeometry> Geos { get; set; }
        bool hasWall;
        bool hasColumn;
        public ThFireAlarmWiringDateSetFactory(bool wall, bool column)
        {
            Geos = new List<ThGeometry>();
            hasWall = wall;
            hasColumn = column;
        }
        protected override ThMEPDataSet BuildDataSet()
        {
            return new ThMEPDataSet()
            {
                Container = Geos,
            };
        }

        protected override void GetElements(Database database, Point3dCollection collection)
        {
            UpdateTransformer(collection);
            var extractors = new List<ThExtractorBase>()
                {
                    new ThFaRoomExtractor()           //房间
                    {
                        holes = holes,
                    },
                };
            if (hasWall)
            {
                extractors.Add(new ThArchitectureExtractor()   //建筑墙
                {
                });
                extractors.Add(new ThShearwallExtractor()      //剪力墙
                {
                }); 
            }
            if (hasColumn)
            {
                extractors.Add(new ThColumnExtractor()         //柱
                {
                });
            }
            extractors.ForEach(o => o.Extract(database, collection));
            //收集数据
            extractors.ForEach(o => Geos.AddRange(o.BuildGeometries()));

            var pline = new Polyline()
            {
                Closed = true,
            };
            pline.CreatePolyline(collection);
            Geos.AddRange(CreateFireApartGeos(pline));//添加防火分区
            Geos.AddRange(GetLineGeos(pline));//添加中心线
            Geos.AddRange(CreatePowerGeos());//添加电源
            Geos.AddRange(CreateHolesGeos());//添加洞口
        }

        /// <summary>
        /// 创建防火分区
        /// </summary>
        /// <returns></returns>
        private List<ThGeometry> CreateFireApartGeos(Polyline frame)
        {
            var geos = new List<ThGeometry>();
            var geometry = new ThGeometry();
            geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.FireApart.ToString());
            geometry.Boundary = frame;
            geos.Add(geometry);

            return geos;
        }

        /// <summary>
        /// 创建洞口
        /// </summary>
        /// <returns></returns>
        private List<ThGeometry> CreateHolesGeos()
        {
            var geos = new List<ThGeometry>();
            var unionHoles = holes.ToCollection().UnionPolygons(true).Cast<Entity>().ToList();
            unionHoles.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.Hole.ToString());
                geometry.Boundary = o;
                geos.Add(geometry);
            });

            return geos;
        }

        /// <summary>
        /// 创建电源点位
        /// </summary>
        /// <returns></returns>
        private List<ThGeometry> CreatePowerGeos()
        {
            var geos = new List<ThGeometry>();
            if (powerBlock != null)
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.PowerPosition.ToString());
                geometry.Boundary = new DBPoint(powerBlock.Position);
                geos.Add(geometry);
            }

            return geos;
        }

        /// <summary>
        /// 获取车道线或中心线
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        private List<ThGeometry> GetLineGeos(Polyline frame)
        {
            List<Line> resLines = new List<Line>();
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (ThLaneLineRecognitionEngine laneLineEngine = new ThLaneLineRecognitionEngine())
            {
                var nFrame = ThMEPFrameService.NormalizeEx(frame);
                if (nFrame.Area > 1)
                {
                    var objs = new DBObjectCollection();
                    var centerLines = acadDatabase.ModelSpace
                    .OfType<Curve>()
                    .Where(o => o.Layer == ThMEPEngineCoreCommon.LANELINE_LAYER_NAME ||
                    o.Layer == ThMEPEngineCoreLayerUtils.CENTERLINE);
                    foreach (var cLine in centerLines)
                    {
                        var transCurve = cLine.Clone() as Curve;
                        objs.Add(transCurve);
                    }

                    var centerPt = nFrame.GetCentroidPoint();
                    var transformer = new ThMEPOriginTransformer(centerPt);
                    transformer.Transform(objs);
                    transformer.Transform(nFrame);
                    objs = Clip(nFrame, objs);
                    transformer.Reset(objs);

                    //处理车道线
                    resLines = ThMEPLineExtension.TransCurveToLine(objs, 500);
                }
            }
            if (resLines.Count <= 0)
            {
                //resLines = ThMEPPolygonService.CenterLine(frame);
            }

            var geos = new List<ThGeometry>();
            resLines.ForEach(o =>
            {
                var geometry = new ThGeometry();
                geometry.Properties.Add(ThExtractorPropertyNameManager.CategoryPropertyName, BuiltInCategory.CenterLine.ToString());
                geometry.Boundary = o;
                geos.Add(geometry);
            });
            return geos;
        }

        private DBObjectCollection Clip(Polyline frame, DBObjectCollection objs)
        {
            var curves = ThLaneLineSimplifier.Simplify(objs, 1500);
            var results = ThCADCoreNTSGeometryClipper.Clip(frame, curves.ToCollection());
            return results.OfType<Curve>().ToCollection();
        }
    }
}
#endif