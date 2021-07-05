using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Dreambuild.AutoCAD;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.LaneLine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Common;

namespace ThMEPElectrical.StructureHandleService
{
    public class GetPrimitivesService
    {
        public ThMEPOriginTransformer originTransformer;
        public GetPrimitivesService(ThMEPOriginTransformer originTransformer)
        {
            this.originTransformer = originTransformer;
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
                .Where(o => o.Layer == ThMEPCommon.LANELINE_LAYER_NAME);
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
                var roomEngine = new ThRoomOutlineExtractionEngine();
                roomEngine.ExtractFromMS(acdb.Database);
                //roomEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));

                var markEngine = new ThRoomOutlineExtractionEngine();
                markEngine.ExtractFromMS(acdb.Database);
                //markEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));

                var boundaryEngine = new ThRoomOutlineRecognitionEngine();
                boundaryEngine.Recognize(roomEngine.Results, polyline.Vertices());
                var rooms = boundaryEngine.Elements.Cast<ThIfcRoom>().ToList();
                var markRecEngine = new ThRoomMarkRecognitionEngine();
                markRecEngine.Recognize(markEngine.Results.Cast<ThRawIfcAnnotationElementData>().ToList(), polyline.Vertices());
                 var marks = markRecEngine.Elements.Cast<ThIfcTextNote>().ToList();
                var builder = new ThRoomBuilderEngine();
                builder.Build(rooms, marks);

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
                var ColumnExtractEngine = new ThColumnExtractionEngine();
                ColumnExtractEngine.Extract(acdb.Database);
                //ColumnExtractEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));
                var ColumnEngine = new ThColumnRecognitionEngine();
                ColumnEngine.Recognize(ColumnExtractEngine.Results, polyline.Vertices());

                // 启动墙识别引擎
                var ShearWallExtractEngine = new ThShearWallExtractionEngine();
                ShearWallExtractEngine.Extract(acdb.Database);
                //ShearWallExtractEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));
                var ShearWallEngine = new ThShearWallRecognitionEngine();
                ShearWallEngine.Recognize(ShearWallExtractEngine.Results, polyline.Vertices());

                var archWallExtractEngine = new ThDB3ArchWallExtractionEngine();
                archWallExtractEngine.Extract(acdb.Database);
                //archWallExtractEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));
                var archWallEngine = new ThDB3ArchWallRecognitionEngine();
                archWallEngine.Recognize(archWallExtractEngine.Results, polyline.Vertices());

                ////获取柱
                columns = new List<Polyline>();
                columns = ColumnEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
                var objs = new DBObjectCollection();
                columns.ForEach(x => objs.Add(x));
                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                columns = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();

                //获取剪力墙
                walls = new List<Polyline>();
                walls = ShearWallEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
                objs = new DBObjectCollection();
                walls.ForEach(x => objs.Add(x));
                thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                walls = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();

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
        public ThStoreys GetFloorInfo(Polyline polyline)
        {
            var bufferPoly = polyline.Buffer(100)[0] as Polyline;
            var storeysRecognitionEngine = new ThStoreysRecognitionEngine();
            using (AcadDatabase db = AcadDatabase.Active())
            {   
                storeysRecognitionEngine.Recognize(db.Database, bufferPoly.Vertices());
            }
            if (storeysRecognitionEngine.Elements.Count > 0)
            {
                return storeysRecognitionEngine.Elements[0] as ThStoreys;
            }

            return null;
        }
    }
}
