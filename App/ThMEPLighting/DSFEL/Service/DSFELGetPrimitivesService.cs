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
using ThMEPEngineCore.Algorithm;
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
        /// 获取需要布置疏散路径的灯
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
                    var isUsefel = roomTexts.Any(z => DSFELConfigCommon.LayoutRoomText.Where(y => y.Contains(z.TextString)).Count() > 0);
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
        public List<List<Line>> GetCentterLines(Polyline frame, List<Polyline> polylines)
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

            return parkingLines;
        }
    }
}
