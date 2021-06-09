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
        /// 获取需要布置疏散指示灯的房间
        /// </summary>
        /// <param name="polyline"></param>
        /// <returns></returns>
        public List<KeyValuePair<Polyline, string>>GetUsefulRooms(Polyline polyline)
        {
            var objs = new DBObjectCollection();
            var textObjs = new List<DBText>();
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var centerLines = acdb.ModelSpace
                .OfType<Polyline>()
                .Where(o => o.Layer == ThMEPLightingCommon.ROOM_LAYER);
                centerLines.ForEach(x =>
                {
                    var transCurve = x.Clone() as Polyline;
                    originTransformer.Transform(transCurve);
                    objs.Add(transCurve);
                });

                var roomTexts = acdb.ModelSpace
                .OfType<DBText>()
                .Where(o => o.Layer == ThMEPLightingCommon.ROOM_TEXT_NAME_LAYER);
                roomTexts.ForEach(x =>
                {
                    var isUsefel = DSFELConfigCommon.LayoutRoomText.Where(y => y.Contains(x.TextString)).Count() > 0;
                    if (isUsefel)
                    {
                        var transText = x.Clone() as DBText;
                        originTransformer.Transform(transText);
                        textObjs.Add(transText);
                    }
                });
            }

            ThCADCoreNTSSpatialIndex thCADCoreNTSSpatialIndex = new ThCADCoreNTSSpatialIndex(objs);
            var roomPolys = thCADCoreNTSSpatialIndex.SelectCrossingPolygon(polyline).Cast<Polyline>().ToList();

            List<KeyValuePair<Polyline, string>> resRooms = new List<KeyValuePair<Polyline, string>>();
            foreach (var poly in roomPolys)
            {
                foreach (var text in textObjs)
                {
                    if (poly.Contains(text.Position))
                    {
                        resRooms.Add(new KeyValuePair<Polyline, string>(poly, text.TextString));
                    }
                }
            }

            return resRooms;
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
                    originTransformer.Transform(transCurve);
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
                    originTransformer.Transform(transCurve);
                    objs.Add(transCurve);
                });
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

    }
}
