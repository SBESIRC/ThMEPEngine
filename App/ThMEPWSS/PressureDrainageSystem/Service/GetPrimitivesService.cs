using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.LaneLine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Electrical;

namespace ThMEPWSS.PressureDrainageSystem.Service
{
    public class GetPrimitivesService
    {
        protected ThMEPOriginTransformer Transformer { get; set; }
        public GetPrimitivesService(ThMEPOriginTransformer transformer)
        {
            Transformer = transformer;
        }

        /// <summary>
        /// 获取车道线
        /// </summary>
        /// <returns></returns>
        public List<List<Line>> GetLanes(Polyline polyline, out List<List<Line>> otherLanes)
        {
            otherLanes = new List<List<Line>>();
            var objs = new DBObjectCollection();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var laneLines = acdb.ModelSpace
                .OfType<Curve>()
                .Where(o => o.Layer == "E-LANE-CENTER");
                laneLines.ForEach(x =>
                {
                    var transCurve = x.Clone() as Curve;
                    //originTransformer.Transform(transCurve);
                    objs.Add(transCurve);
                });
            }

            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var sprayLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Curve>().ToList();
            if (sprayLines.Count <= 0)
            {
                return new List<List<Line>>();
            }
            sprayLines = sprayLines.SelectMany(x => polyline.Trim(x).Cast<Curve>().ToList()).ToList();

            //处理车道线
            var handleLines = ThMEPLineExtension.LineSimplifier(sprayLines.ToCollection(), 500, 100.0, 2.0, Math.PI / 180.0);
            var parkingLinesService = new ParkingLinesService();
            var parkingLines = parkingLinesService.CreateNodedParkingLines(polyline, handleLines, out List<List<Line>> otherPLines);
            otherLanes = otherPLines;

            return parkingLines;
        }

        /// <summary>
        /// 获取房间
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public List<ThIfcRoom> GetRoomInfo(Polyline polyline)
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var roomEngine = new ThAIRoomOutlineExtractionEngine();
                roomEngine.ExtractFromMS(acdb.Database);
                //roomEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));

                var markEngine = new ThAIRoomMarkExtractionEngine();
                markEngine.ExtractFromMS(acdb.Database);
                //markEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));

                var boundaryEngine = new ThAIRoomOutlineRecognitionEngine();
                boundaryEngine.Recognize(roomEngine.Results, polyline.Vertices());
                var rooms = boundaryEngine.Elements.Cast<ThIfcRoom>().ToList();
                var markRecEngine = new ThAIRoomMarkRecognitionEngine();
                markRecEngine.Recognize(markEngine.Results, polyline.Vertices());
                var marks = markRecEngine.Elements.Cast<ThIfcTextNote>().ToList();
                var builder = new ThRoomBuilderEngine();
                builder.Build(rooms, marks);

                foreach (var room in rooms)
                {
                    //if (room.Boundary is MPolygon mPolygon)
                    //{
                    //    var mPolygonLoops = mPolygon.Loops();
                    //    room.Boundary = mPolygonLoops[0];
                    //}
                }
                return rooms;
            }
        }

        /// <summary>
        /// 获取门
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="columns"></param>
        /// <param name="walls"></param>
        public List<Polyline> GetDoorInfo(Polyline polyline)
        {
            var doors = new List<Polyline>();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                doors = acdb.ModelSpace.OfType<Polyline>().Where(x => x.Layer == "AI-门").Select(x => x.Clone() as Polyline).ToList();
                //doors.ForEach(x => originTransformer.Transform(x));
                //var doorExtractEngine = new ThDoorExtractionEngine();
                //doorExtractEngine.Extract(acdb.Database);
                //doorExtractEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));
                //var doorEngine = new ThDoorRecognitionEngine();
                //doorEngine.Recognize(doorExtractEngine.Results, polyline.Vertices());
                //doors = doorEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
            }

            return doors;
        }

        /// <summary>
        /// 获取构建
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="columns"></param>
        /// <param name="walls"></param>
        public void GetStructureInfo(Polyline polyline, out List<Polyline> columns, out List<Polyline> walls)
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var pts = polyline.Vertices();
                var newPts = pts.OfType<Point3d>().Select(p => Transformer.Transform(p)).ToCollection();
                // 启动结构柱识别引擎(结构参照柱+DB3剖切生成柱)
                var columnExtractEngine = new ThColumnExtractionEngine();
                columnExtractEngine.Extract(acdb.Database);
                columnExtractEngine.Results.ForEach(x => Transformer.Transform(x.Geometry));
                var columnEngine = new ThColumnRecognitionEngine();
                columnEngine.Recognize(columnExtractEngine.Results, newPts);
                columnEngine.Elements.ForEach(e => Transformer.Reset(e.Outline));

                var db3ColumnExtractEngine = new ThDB3ColumnExtractionEngine();
                db3ColumnExtractEngine.Extract(acdb.Database);
                db3ColumnExtractEngine.Results.ForEach(x => Transformer.Transform(x.Geometry));
                var db3ColumnEngine = new ThDB3ColumnRecognitionEngine();
                db3ColumnEngine.Recognize(db3ColumnExtractEngine.Results, newPts);
                db3ColumnEngine.Elements.ForEach(e => Transformer.Reset(e.Outline));

                // 启动墙识别引擎(结构参照墙+DB3剖切生成的墙)
                var shearWallExtractEngine = new ThShearWallExtractionEngine();
                shearWallExtractEngine.Extract(acdb.Database);
                shearWallExtractEngine.Results.ForEach(x => Transformer.Transform(x.Geometry));
                var shearWallEngine = new ThShearWallRecognitionEngine();
                shearWallEngine.Recognize(shearWallExtractEngine.Results, newPts);
                shearWallEngine.Elements.ForEach(e => Transformer.Reset(e.Outline));

                var db3ShearwallExtractEngine = new ThDB3ShearWallExtractionEngine();
                db3ShearwallExtractEngine.Extract(acdb.Database);
                db3ShearwallExtractEngine.Results.ForEach(x => Transformer.Transform(x.Geometry));
                var db3ShearWallEngine = new ThShearWallRecognitionEngine();
                db3ShearWallEngine.Recognize(db3ShearwallExtractEngine.Results, newPts);
                db3ShearWallEngine.Elements.ForEach(e => Transformer.Reset(e.Outline));

                // DB3建筑墙
                var archWallExtractEngine = new ThDB3ArchWallExtractionEngine();
                archWallExtractEngine.Extract(acdb.Database);
                archWallExtractEngine.Results.ForEach(x => Transformer.Transform(x.Geometry));
                var archWallEngine = new ThDB3ArchWallRecognitionEngine();
                archWallEngine.Recognize(archWallExtractEngine.Results, newPts);
                archWallEngine.Elements.ForEach(e => Transformer.Reset(e.Outline));

                //获取柱
                columns = new List<Polyline>();
                columns = columnEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
                columns.AddRange(db3ColumnEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList());

                //获取剪力墙
                walls = new List<Polyline>();
                walls = shearWallEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
                walls.AddRange(db3ShearWallEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList());

                //获取建筑墙
                foreach (var o in archWallEngine.Elements)
                {
                    if (o.Outline is Polyline wall)
                    {
                        walls.Add(wall);
                    }
                }
            }
        }

        /// <summary>
        /// 获取楼层信息
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public ThEStoreys GetFloorInfo(Polyline polyline)
        {
            var bufferPoly = polyline.Buffer(100)[0] as Polyline;
            var storeysRecognitionEngine = new ThEStoreysRecognitionEngine();
            using (AcadDatabase db = AcadDatabase.Active())
            {   
                storeysRecognitionEngine.Recognize(db.Database, bufferPoly.Vertices());
            }
            if (storeysRecognitionEngine.Elements.Count > 0)
            {
                return storeysRecognitionEngine.Elements[0] as ThEStoreys;
            }

            return null;
        }

        /// <summary>
        /// 获取防火分区
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public List<Entity> GetFireFrame(Polyline polyline)
        {
            using (AcadDatabase db = AcadDatabase.Active())
            {
                var builder = new ThFireCompartmentBuilder()
                {
                    LayerFilter = new List<string>() { "AI-防火分区" },
                };
                var compartments = builder.BuildFromMS(db.Database, polyline.Vertices());
                return compartments.Select(x => x.Boundary).ToList();
            }
        }
    }
}
