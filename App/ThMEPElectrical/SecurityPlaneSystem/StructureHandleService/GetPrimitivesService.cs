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
using ThMEPElectrical.SecurityPlaneSystem.Utls;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.LaneLine;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Common;
using ThMEPEngineCore.Model.Electrical;

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
                    originTransformer.Transform(transCurve);
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
                roomEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));

                var markEngine = new ThRoomMarkExtractionEngine();
                markEngine.ExtractFromMS(acdb.Database);
                markEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));

                var boundaryEngine = new ThRoomOutlineRecognitionEngine();
                boundaryEngine.Recognize(roomEngine.Results, polyline.Vertices());
                var rooms = boundaryEngine.Elements.Cast<ThIfcRoom>().ToList();
                var markRecEngine = new ThRoomMarkRecognitionEngine();
                markRecEngine.Recognize(markEngine.Results, polyline.Vertices());
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
                doors = acdb.ModelSpace.OfType<Polyline>().Where(x => x.Layer == "AI-门").Select(x => (x.Clone() as Polyline).FlattenRectangle()).Where(o => o.Area > 25000).ToList();

                //doors.ForEach(x => originTransformer.Transform(x));
                //var doorExtractEngine = new ThDoorExtractionEngine();
                //doorExtractEngine.Extract(acdb.Database);
                //doorExtractEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));
                //var doorEngine = new ThDoorRecognitionEngine();
                //doorEngine.Recognize(doorExtractEngine.Results, polyline.Vertices());
                //doors = doorEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();

                var spatialIndex = new ThCADCoreNTSSpatialIndex(doors.ToCollection());
                var boundary = polyline.Clone() as Polyline;
                originTransformer.Reset(boundary);
                var doorRecognitionEngine = new ThDB3DoorRecognitionEngine();
                doorRecognitionEngine.Recognize(acdb.Database, boundary.Vertices());
                foreach (var door in doorRecognitionEngine.Elements)
                {
                    Polyline doorPolyline = door.Outline as Polyline;
                    var Indentationdoor = doorPolyline.Buffer(10)[0] as Polyline;
                    if (spatialIndex.SelectWindowPolygon(Indentationdoor).Count < 1)
                    {
                        doors.Add(doorPolyline);
                    }
                }
                doors.ForEach(x => originTransformer.Transform(x));
            }

            return doors;
        }

        public List<Entity> GetOldLayout(Polyline polyline,List<string> blockNames,string LineLayer)
        {
            var vmInfo = new List<Entity>();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var vmBlockInfo = acdb.ModelSpace.OfType<BlockReference>().Where(x => blockNames.Contains(x.GetEffectiveName())).ToList();
                var vmLineInfo = acdb.ModelSpace.OfType<Line>().Where(x => x.Layer == LineLayer).ToList();
                vmInfo.AddRange(vmBlockInfo);
                vmInfo.AddRange(vmLineInfo);

                var spatialIndex = new ThCADCoreNTSSpatialIndex(vmInfo.ToCollection());
                spatialIndex.AllowDuplicate = true;
                var boundary = polyline.Clone() as Polyline;
                originTransformer.Reset(boundary);
                var objs = spatialIndex.SelectWindowPolygon(boundary);
                vmInfo = objs.Cast<Entity>().ToList();
            }
            return vmInfo;
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
                var WindowExtractEngine = new ThDB3WindowExtractionEngine();
                WindowExtractEngine.Extract(acdb.Database);
                WindowExtractEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));
                var WindowEngine = new ThDB3WindowRecognitionEngine();
                WindowEngine.Recognize(WindowExtractEngine.Results, polyline.Vertices());

                var ColumnExtractEngine = new ThColumnExtractionEngine();
                ColumnExtractEngine.Extract(acdb.Database);
                ColumnExtractEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));
                var ColumnEngine = new ThColumnRecognitionEngine();
                ColumnEngine.Recognize(ColumnExtractEngine.Results, polyline.Vertices());

                // 启动墙识别引擎
                var ShearWallExtractEngine = new ThShearWallExtractionEngine();
                ShearWallExtractEngine.Extract(acdb.Database);
                ShearWallExtractEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));
                var ShearWallEngine = new ThShearWallRecognitionEngine();
                ShearWallEngine.Recognize(ShearWallExtractEngine.Results, polyline.Vertices());

                var archWallExtractEngine = new ThDB3ArchWallExtractionEngine();
                archWallExtractEngine.Extract(acdb.Database);
                archWallExtractEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));
                var archWallEngine = new ThDB3ArchWallRecognitionEngine();
                archWallEngine.Recognize(archWallExtractEngine.Results, polyline.Vertices());

                ////获取柱
                columns = new List<Polyline>();
                columns = ColumnEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
                var objs = new DBObjectCollection();
                columns.ForEach(x => objs.Add(x));
                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                columns = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();

                ////获取窗
                var windows = new List<Polyline>();
                windows = WindowEngine.Elements.Select(o => o.Outline).Cast<Polyline>().ToList();
                objs = new DBObjectCollection();
                windows.ForEach(x => objs.Add(x));
                thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                windows = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();

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
                walls.AddRange(windows);
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
        /// 获取楼层信息
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public ThEStoreys GetFloorInfo(ObjectIdCollection objectId)
        {
            var storeysRecognitionEngine = new ThEStoreysRecognitionEngine();
            using (AcadDatabase db = AcadDatabase.Active())
            {
                storeysRecognitionEngine.RecognizeMS(db.Database, objectId);
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

        /// <summary>
        /// 获取线槽
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="layerName"></param>
        /// <returns></returns>
        public List<Line> GetTrunkings(Polyline polyline, string layerName)
        {
            var objs = new DBObjectCollection();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var trunkingLines = acdb.ModelSpace
                .OfType<Curve>()
                .Where(o => o.Layer == layerName);
                trunkingLines.ForEach(x =>
                {
                    var transCurve = x.Clone() as Curve;
                    originTransformer.Transform(transCurve);
                    objs.Add(transCurve);
                });
            }
            //originTransformer.Transform(objs);
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var selectLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Curve>().ToList();
            if (selectLines.Count <= 0)
            {
                return new List<Line>();
            }
            selectLines = selectLines.SelectMany(x => polyline.Trim(x).Cast<Curve>().ToList()).ToList();
            var resLines = new List<Line>();
            foreach (var trunking in selectLines)
            {
                if (trunking is Line line)
                {
                    resLines.Add(line);
                }
                else if (trunking is Polyline poly)
                {
                    resLines.AddRange(poly.GetAllLinesInPolyline(false));
                }
            }

            return resLines;
        }
    }
}
