using Autodesk.AutoCAD.DatabaseServices;
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

namespace ThMEPLighting.DSFEL.Service
{
    public class DSFELGetPrimitivesService
    {
        ThMEPOriginTransformer originTransformer;
        public DSFELGetPrimitivesService(ThMEPOriginTransformer originTransformer)
        {
            this.originTransformer = originTransformer;
        }

        /// <summary>
        /// 获取房间
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public Dictionary<ThIfcRoom, List<Polyline>> GetRoomInfo(Polyline polyline)
        {   
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var roomEngine = new ThRoomOutlineExtractionEngine();
                roomEngine.ExtractFromMS(acdb.Database);
                //roomEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));

                var markEngine = new ThRoomMarkExtractionEngine();
                markEngine.ExtractFromMS(acdb.Database);
                //markEngine.Results.ForEach(x => originTransformer.Transform(x.Geometry));

                var boundaryEngine = new ThRoomOutlineRecognitionEngine();
                boundaryEngine.Recognize(roomEngine.Results, polyline.Vertices());
                var rooms = boundaryEngine.Elements.Cast<ThIfcRoom>().ToList();
                var markRecEngine = new ThRoomMarkRecognitionEngine();
                markRecEngine.Recognize(markEngine.Results, polyline.Vertices());
                var marks = markRecEngine.Elements.Cast<ThIfcTextNote>().ToList();
                var builder = new ThRoomBuilderEngine();
                builder.Build(rooms, marks);

                var roomInfos = new Dictionary<ThIfcRoom, List<Polyline>>();
                foreach (var room in rooms)
                {
                    var holes = CalRoomBoundary(room);
                    roomInfos.Add(room, holes);
                }
                return roomInfos;
            }
        }

        /// <summary>
        /// 获取门框线
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public List<Polyline> GetDoor(Polyline polyline)
        {
            var objs = new DBObjectCollection();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var doors = acdb.ModelSpace
                .OfType<Polyline>()
                .Where(o => o.Layer == ThMEPLightingCommon.DOOR_LAYER);
                doors.ForEach(x =>
                {
                    var transCurve = x.Clone() as Curve;
                    //originTransformer.Transform(transCurve);
                    objs.Add(transCurve);
                });
            }

            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            return thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();
        }

        /// <summary>
        /// 获取中心线
        /// </summary>
        /// <returns></returns>
        public List<Line> GetCentterLines(Polyline frame, List<Polyline> polylines)
        {
            var objs = new DBObjectCollection();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var centerLines = acdb.ModelSpace
                .OfType<Curve>()
                .Where(o => o.Layer == ThMEPLightingCommon.CENTER_LINE_LAYER);
                centerLines.ForEach(x =>
                {
                    var transCurve = x.Clone() as Curve;
                    //originTransformer.Transform(transCurve);
                    objs.Add(transCurve);
                });
            }

            if (objs.Count <= 0)
            {
                return new List<Line>();
                //objs = ThMEPPolygonService.CenterLine(frame).ToCollection();
            }

            List<Curve> resLines = new List<Curve>();
            foreach (var polyline in polylines)
            {
                ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
                var centerLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Curve>().ToList();
                if (centerLines.Count <= 0)
                {
                    continue;
                }
                resLines.AddRange(centerLines.SelectMany(x => polyline.Trim(x).Cast<Curve>().ToList()).ToList());
            }
            
            //处理车道线
            var handleLines = ThMEPLineExtension.LineSimplifier(resLines.ToCollection(), 500, 20.0, 2.0, Math.PI / 180.0);
            var parkingLinesService = new ParkingLinesService();
            var parkingLines = parkingLinesService.CreateNodedParkingLines(frame, handleLines, out List<List<Line>> otherPLines);
            parkingLines.AddRange(otherPLines);

            return parkingLines.SelectMany(x => x).ToList();
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
        /// 获取区域内的主要疏散路径或辅助
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public List<Curve> GetMainEvacuate(Polyline polyline, string name)
        {
            var objs = new DBObjectCollection();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var exitLines = acdb.ModelSpace
                .OfType<Curve>()
                .Where(x => x.Layer == name);
                exitLines.ForEach(x =>
                {
                    var transCurve = x.Clone() as Curve;
                    //originTransformer.Transform(transCurve);
                    objs.Add(transCurve);
                });
            }
            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var sprayLines = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Curve>().ToList();
            return sprayLines;
        }

        /// <summary>
        /// 获取出入口图块
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public List<BlockReference> GetEvacuationExitBlock(Polyline polyline)
        {
            var objs = new DBObjectCollection();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var exitBlock = acdb.ModelSpace
                .OfType<BlockReference>()
                .Where(x => !x.BlockTableRecord.IsNull)
                .Where(x =>
                {
                    var name = x.GetEffectiveName();
                    return name == ThMEPLightingCommon.FEI_EXIT_NAME100 ||
                     name == ThMEPLightingCommon.FEI_EXIT_NAME101 ||
                     name == ThMEPLightingCommon.FEI_EXIT_NAME102 ||
                     name == ThMEPLightingCommon.FEI_EXIT_NAME103 ||
                     name == ThMEPLightingCommon.FEI_EXIT_NAME140 ||
                     name == ThMEPLightingCommon.FEI_EXIT_NAME141;

                });
                exitBlock.ForEach(x =>
                {
                    var transBlock = x.Clone() as BlockReference;
                    originTransformer.Transform(transBlock);
                    objs.Add(transBlock);
                });
            }

            List<BlockReference> blocks = new List<BlockReference>();
            foreach (BlockReference obj in objs)
            {
                if (polyline.Contains(obj.Position))
                {
                    blocks.Add(obj);
                }
            }

            return blocks;
        }

        /// <summary>
        /// 提取throom的房间框线
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        private List<Polyline> CalRoomBoundary(ThIfcRoom room)
        {
            List<Polyline> holes = new List<Polyline>();
            if (room.Boundary is Polyline polyline)
            {
                room.Boundary = polyline;
            }
            else if (room.Boundary is MPolygon mPolygon)
            {
                var mPolygonLoops = mPolygon.Loops();
                room.Boundary = mPolygonLoops[0];
                if (mPolygonLoops.Count > 1)
                {
                    for (int i = 1; i < mPolygonLoops.Count(); i++)
                    {
                        holes.Add(mPolygonLoops[0]);
                    }
                }
            }

            return holes;
        }
    }
}
