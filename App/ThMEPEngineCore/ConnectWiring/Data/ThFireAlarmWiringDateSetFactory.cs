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
        public ThFireAlarmWiringDateSetFactory()
        {
            Geos = new List<ThGeometry>();
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
                    new ThArchitectureExtractor()   //建筑墙
                    {
                    },
                    new ThShearwallExtractor()      //剪力墙
                    {
                    },
                    new ThColumnExtractor()         //柱
                    {
                    },
                    new ThFaRoomExtractor()           //房间
                    {
                        holes = holes,
                    },
                    new ThBlockPointsExtractor()      //连线布置点位
                    {
                    },
                };
            extractors.ForEach(o => o.Extract(database, collection));
            //收集数据
            extractors.ForEach(o => Geos.AddRange(o.BuildGeometries()));

            var pline = new Polyline()
            {
                Closed = true,
            };
            pline.CreatePolyline(collection);
            Geos.AddRange(GetLineGeos(pline));//添加中心线
            Geos.AddRange(CreatePowerGeos());//添加电源
            Geos.AddRange(CreateHolesGeos());//添加洞口
        }

        /// <summary>
        /// 创建洞口
        /// </summary>
        /// <returns></returns>
        private List<ThGeometry> CreateHolesGeos()
        {
            var geos = new List<ThGeometry>();
            holes.ForEach(o =>
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
                    var bFrame = ThMEPFrameService.Buffer(nFrame, 100000.0);
                    laneLineEngine.Recognize(acadDatabase.Database, nFrame.Vertices());
                    var lines = laneLineEngine.Spaces.Select(o => o.Boundary).ToCollection();
                    var centerPt = nFrame.GetCentroidPoint();
                    var transformer = new ThMEPOriginTransformer(centerPt);
                    transformer.Transform(lines);
                    transformer.Transform(nFrame);

                    var curves = ThLaneLineSimplifier.Simplify(lines, 1500);
                    lines = ThCADCoreNTSGeometryClipper.Clip(nFrame, curves.ToCollection());
                    transformer.Reset(lines);

                    //处理车道线
                    var handleLines = ThMEPLineExtension.LineSimplifier(lines, 500, 100.0, 2.0, Math.PI / 180.0);
                    var parkingLinesService = new ParkingLinesService();
                    var parkingLines = parkingLinesService.CreateNodedParkingLines(frame, handleLines, out List<List<Line>> otherPLines);
                    parkingLines.AddRange(otherPLines);
                    resLines = parkingLines.SelectMany(x => x).ToList();
                }
            }
            if (resLines.Count <= 0)
            {
                resLines = ThMEPPolygonService.CenterLine(frame);
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
    }
}
