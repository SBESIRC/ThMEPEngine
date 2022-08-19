using System;
using NFox.Cad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using Dreambuild.AutoCAD;
using ThMEPEngineCore.CAD;
using ThMEPLighting.Common;
using ThMEPLighting.Garage.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPLighting.Garage.Service
{
    /// <summary>
    /// 找到灯块、编号文字、连接灯块的线
    /// </summary>
    public class ThQueryLightWireService
    {
        #region ----------外部传入----------
        private DBObjectCollection LightBlks { get; set; }
        private DBObjectCollection NumberTexts { get; set; }
        private DBObjectCollection LightWires { get; set; }
        private List<Line> LightingLines { get; set; }
        private DBObjectCollection TCHCableTrays { get; set; }
        private ThLightArrangeParameter ArrangeParameter { get; set; }
        #endregion
        #region ----------中间参数----------
        private const double EnvelopWidth = 2.0;
        private const double SearchDistanceTolerance = 5.0;
        private const int MaxFindCount = 20;
        private ThCADCoreNTSSpatialIndex NumberTextSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex LightWireSpatialIndex { get; set; }
        private ThCADCoreNTSSpatialIndex TCHCableTraysSpatialIndex { get; set; }
        // <summary>
        /// 用于查找灯块上方的文字
        /// </summary>
        private double TextQueryHeight { get; set; }
        private double WireQueryLength { get; set; }
        #endregion
        #region ----------返回结果----------
        public DBObjectCollection QualifiedBlks { get; private set; }
        public DBObjectCollection QualifiedTexts { get; private set; }
        public DBObjectCollection QualifiedTCHCableTrays { get; private set; }
        public DBObjectCollection QualifiedCurves { get; private set; }
        #endregion

        public ThQueryLightWireService(
            DBObjectCollection lightBlks,
            DBObjectCollection numberTexts,
            DBObjectCollection lightWires,
            List<Line> lightLines,
            DBObjectCollection tchCableTrays,
            ThLightArrangeParameter arrangeParameter
            )
        {
            LightBlks = lightBlks;
            NumberTexts = numberTexts;
            LightWires = lightWires;
            LightingLines = lightLines;
            TCHCableTrays = tchCableTrays;
            ArrangeParameter = arrangeParameter;
            Init();
        }
        private void Init()
        {
            QualifiedBlks = new DBObjectCollection();
            QualifiedTexts = new DBObjectCollection();
            QualifiedTCHCableTrays = new DBObjectCollection();
            QualifiedCurves = new DBObjectCollection();
            NumberTextSpatialIndex = CreateSpatialIndex(NumberTexts, true);
            LightWireSpatialIndex = CreateSpatialIndex(LightWires, true);
            TCHCableTraysSpatialIndex = CreateSpatialIndex(TCHCableTrays, true);
            TextQueryHeight = ArrangeParameter.Width / 2.0 + ArrangeParameter.LightNumberTextGap
                + ArrangeParameter.LightNumberTextHeight + SearchDistanceTolerance;
            WireQueryLength = (ArrangeParameter.LampLength + ArrangeParameter.LightWireBreakLength) / 2.0
                + SearchDistanceTolerance;
        }
        public void Query()
        {
            // 查找思路：
            // 从灯块出发，按照一定范围搜索其附近的编号文字
            // 从块出发找到其附近的编号文字和相连的线
            var lightPairs = GetLightBlockTextPairs(LightBlks, NumberTextSpatialIndex);
            var tchCableTrays = GetTCHCAbleTrays(LightingLines, TCHCableTraysSpatialIndex);

            // 收集找到的灯块，编号文字
            lightPairs.ForEach(o =>
            {
                QualifiedBlks.Add(o.Item1);
                Add(QualifiedTexts, o.Item2);
            });

            // 收集天正桥架
            Add(QualifiedTCHCableTrays, tchCableTrays);

            // 查找QualifiedBlks附近的线
            var nearbyWires = GetLightBlockNearbyWires(QualifiedBlks, LightWireSpatialIndex);
            Add(QualifiedCurves, nearbyWires);

            // 搜索:从剩余的线中找与 QualifiedCurves 相连的线

            var restWires = LightWires.Difference(QualifiedCurves);
            restWires.OfType<Curve>().ForEach(o =>
            {
                if (o is Line line)
                {
                    int findCount = 0;
                    if (TraceIsLinkWire(line, line.StartPoint, findCount))
                    {
                        QualifiedCurves.Add(o);
                    }
                    findCount = 0;
                    if (TraceIsLinkWire(line, line.EndPoint, findCount))
                    {
                        QualifiedCurves.Add(o);
                    }
                }
            });
        }

        private bool TraceIsLinkWire(Line line, Point3d pt, int findCount)
        {
            if (findCount < MaxFindCount)
            {
                findCount++;
            }
            else
            {
                return false;
            }
            var links = Query(pt);
            links.Remove(line);
            if (IsInQualifiedCurves(links))
            {
                return true;
            }
            if (links.Count == 0)
            {
                var dir = line.LineDirection();
                if (pt.DistanceTo(line.StartPoint) < pt.DistanceTo(line.EndPoint))
                {
                    dir = dir.Negate();
                }
                var extPt = GetExtendPt(pt, dir);
                var extLinkObjs = Query(extPt);
                links = FilterExtendLinks(line, extLinkObjs).ToCollection();
                if (IsInQualifiedCurves(links))
                {
                    return true;
                }
            }
            // 只支持对线的追踪
            links = links.OfType<Curve>().Where(o => o is Line).ToCollection();
            foreach (Line link in links)
            {
                var nextPt = pt.GetNextLinkPt(link.StartPoint, link.EndPoint);
                return TraceIsLinkWire(link, nextPt, findCount);
            }
            return false;
        }

        private bool IsInQualifiedCurves(DBObjectCollection objs)
        {
            return objs
                .OfType<DBObject>()
                .Where(o => QualifiedCurves.Contains(o)).Any();
        }

        private List<Line> FilterExtendLinks(Line line, DBObjectCollection extLinks)
        {
            return extLinks
                .OfType<Curve>()
                .Where(o => o is Line).Select(o => o as Line)
                .Where(o => ThGeometryTool.IsCollinearEx(line.StartPoint,
                line.EndPoint, o.StartPoint, o.EndPoint))
                .ToList();
        }

        private Point3d GetExtendPt(Point3d portPt, Vector3d vec)
        {
            return portPt + vec.GetNormal().MultiplyBy(ArrangeParameter.LightWireBreakLength);
        }

        private DBObjectCollection Query(Point3d pt)
        {
            var envelop = pt.CreateSquare(ThGarageLightCommon.RepeatedPointDistance);
            return LightWireSpatialIndex.SelectCrossingPolygon(envelop);
        }

        private List<Tuple<BlockReference, DBObjectCollection>> GetLightBlockTextPairs(
            DBObjectCollection lightBlks, ThCADCoreNTSSpatialIndex numberTextSpatialIndex)
        {
            // 从灯块出发，优先查找一定范围以内有编号文字，如果有，则视为布置的照明灯
            // 如果没有则看其连接处是否有灯连线
            var results = new List<Tuple<BlockReference, DBObjectCollection>>();
            lightBlks.OfType<BlockReference>().ForEach(b =>
            {
                var vec = Vector3d.XAxis.RotateBy(b.Rotation + Math.PI / 2.0, b.Normal).GetNormal();
                var sp = b.Position + vec.MultiplyBy(TextQueryHeight);
                var ep = b.Position - vec.MultiplyBy(TextQueryHeight);
                var envelop = ThDrawTool.ToOutline(sp, ep, EnvelopWidth);
                var objs = numberTextSpatialIndex.SelectCrossingPolygon(envelop);
                if (objs.Count > 0)
                {
                    results.Add(Tuple.Create(b, objs));
                }
            });
            return results;
        }

        private DBObjectCollection GetTCHCAbleTrays(List<Line> lightingLines, ThCADCoreNTSSpatialIndex tchCableTraySpatialIndex)
        {
            // 从灯块出发，优先查找一定范围以内有编号文字，如果有，则视为布置的照明灯
            // 如果没有则看其连接处是否有灯连线
            var results = new DBObjectCollection();
            lightingLines.ForEach(l =>
            {
                var envelop = l.BufferSquare(50.0);
                var objs = tchCableTraySpatialIndex.SelectCrossingPolygon(envelop);
                if (objs.Count > 0)
                {
                    Add(results, objs);
                }
            });
            return results;
        }

        private DBObjectCollection GetLightBlockNearbyWires(
            DBObjectCollection lightBlks, ThCADCoreNTSSpatialIndex wireSpatialIndex)
        {
            var results = new DBObjectCollection();
            // 从灯块出发，优先查找一定范围以内有编号文字，如果有，则视为布置的照明灯
            // 如果没有则看其连接处是否有灯连线
            lightBlks.OfType<BlockReference>().ForEach(b =>
            {
                var vec1 = Vector3d.XAxis.RotateBy(b.Rotation, b.Normal).GetNormal();
                var sp = b.Position + vec1.MultiplyBy(WireQueryLength);
                var ep = b.Position - vec1.MultiplyBy(WireQueryLength);
                var envelop1 = ThDrawTool.ToOutline(sp, ep, EnvelopWidth);
                results = results.Union(wireSpatialIndex.SelectCrossingPolygon(envelop1));

                var vec2 = vec1.RotateBy(Math.PI / 2.0, b.Normal).GetNormal();
                sp = b.Position + vec2.MultiplyBy(TextQueryHeight);
                ep = b.Position - vec2.MultiplyBy(TextQueryHeight);
                var envelop2 = ThDrawTool.ToOutline(sp, ep, EnvelopWidth);
                results = results.Union(wireSpatialIndex.SelectCrossingPolygon(envelop2));
            });
            return results.Distinct();
        }
        private ThCADCoreNTSSpatialIndex CreateSpatialIndex(DBObjectCollection objs, bool allowDuplicate)
        {
            return new ThCADCoreNTSSpatialIndex(objs)
            {
                AllowDuplicate = allowDuplicate,
            };
        }
        private void Add(DBObjectCollection origins, DBObjectCollection newAdds)
        {
            foreach (DBObject dbObj in newAdds)
            {
                if (!origins.Contains(dbObj))
                {
                    origins.Add(dbObj);
                }
            }
        }
    }
}
