using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Operation.Overlay.Snap;
using NetTopologySuite.Operation.OverlayNG;
using NetTopologySuite.Operation.Overlay;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Diagnostics;
using ThMEPEngineCore.AreaLayout.GridLayout.Data;
using ThMEPEngineCore.AreaLayout.CenterLineLayout.Utils;
using ThMEPEngineCore.AreaLayout.CenterLineLayout.LayoutProcess;
using NetTopologySuite.Geometries;

namespace ThMEPEngineCore.AreaLayout.CenterLineLayout.Command
{
    public class FireAlarmSystemLayoutCommand
    {
        //input
        public Polyline frame { get; set; }//房间外框线
        public List<Polyline> holeList { get; set; }//洞
        public List<MPolygon> layoutList { get; set; }//可布置区域
        public List<Polyline> wallList { get; set; } //墙
        public List<Polyline> columns { get; set; }//柱子
        public List<Polyline> prioritys { get; set; }//优先级更高点位，比如要躲避已布置好的区域
        public List<Polyline> detectArea { get; set; }//探测区域
        public Dictionary<Polyline, Vector3d> ucs { get; set; }//UCS， maybe input or output
        public double radius { get; set; }//保护半径
        public BlindType equipmentType { get; set; }//盲区类型

        //output
        public List<Point3d> layoutPoints { get; set; } = new List<Point3d>();//布置点位
        public Dictionary<Point3d, Vector3d> pointsWithDirection { get; set; } = new Dictionary<Point3d, Vector3d>(); //布置点位以及其方向//------------------------------
        public List<Polyline> blinds { get; set; } = new List<Polyline>();//盲区

        public void Execute()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                ////Get MPolygon
                //var objs = new DBObjectCollection();
                //objs.Add(frame);
                //foreach (var hole in holeList)// --------------------------------------------
                //{
                //    hole.Closed = true;
                //    objs.Add(hole);
                //}
                //MPolygon roomForCenterLine = objs.BuildMPolygon();

                GetRoomHoleMPoly(out var roomForCL);
                DrawUtils.ShowGeometry(roomForCL, "l0roomForCL", 123, 30);
                if (layoutList.Count == 0)
                {
                    return;
                }

                List<Point3d> centerLinePts = CenterLineSimplify.CLSimplifyPts(roomForCL);
                centerLinePts.ForEach(x => DrawUtils.ShowGeometry(x, "l0centerline", 1, 25, 30, "X"));

                GetRoomMPoly(out var roomForBlind, out var nonDeployableArea);
                DrawUtils.ShowGeometry(roomForBlind, "l0roomForBlind", 233, 30);

                GetLayoutMPoly(out var layoutForPlace);
                layoutForPlace.ForEach(x => DrawUtils.ShowGeometry(x, "l0layoutForPlace", 190, 30));

                //计算布置点位
                var layoutServer = new LayoutOpt()
                {
                    MRoom = roomForBlind,
                    Radius = radius,
                    EquipmentType = equipmentType,
                    DetectArea = detectArea,
                    LayoutList = layoutList,
                    //nonDeployableArea = nonDeployableArea,
                    LayoutWithHole = layoutForPlace,
                    CenterLinePts = centerLinePts,
                };

                layoutPoints = layoutServer.Calculate();


                //获取布置点位方向
                pointsWithDirection = new Dictionary<Point3d, Vector3d>();
                PointDirectionService.PointsWithDirection(frame, holeList, layoutPoints, pointsWithDirection);

                //输出
                blinds = new List<Polyline>();
                var unCoverRegion = AreaCaculator.BlandArea(roomForBlind, layoutPoints, radius, equipmentType, layoutServer.DetectSpatialIdx, layoutServer.EmptyDetect);
                foreach (var ucr in unCoverRegion.ToDbCollection())
                {
                    blinds.Add((Polyline)ucr);
                }

                //善后
                //mPolygon.UpgradeOpen();
                //mPolygon.Erase();
                //mPolygon.DowngradeOpen();

#if DEBUG
                ////显示适配信息
                //ShowInfo.SafetyCaculate(roomMP, layoutPoints, radius);
                ////显示布置点及方向
                //foreach (var dir in pointsWithDirection)
                //{
                //    ShowInfo.ShowPointAsO(dir.Key, 130, 200);
                //    ShowInfo.ShowPointWithDirection(dir.Key, dir.Value, 130);
                //}
                ////显示盲区
                //ShowInfo.ShowGeometry(unCoverRegion, acdb, 130);
#endif      
            }
        }

        public void GetInputs(ref DBObjectCollection objs, List<Polyline> nonDeployableArea)
        {
            var room = objs.BuildMPolygon().ToNTSPolygon();
            foreach (var hole in holeList)
            {
                nonDeployableArea.Add(hole);
                var geo = room.Difference(hole.ToNTSPolygon());
                if (geo is Polygon polygon)
                {
                    room = polygon;
                }
                else if (geo is GeometryCollection collection)
                {
                    Polygon tmpPoly = Polygon.Empty;
                    foreach (var poly in collection)
                    {
                        if (poly is Polygon && poly.Area > tmpPoly.Area)
                            tmpPoly = poly as Polygon;
                    }
                    room = tmpPoly;
                }
            }
            foreach (var wall in wallList)
            {
                nonDeployableArea.Add(wall);
                var geo = room.Difference(wall.ToNTSPolygon());
                if (geo is Polygon polygon)
                {
                    room = polygon;
                }
                else if (geo is GeometryCollection collection)
                {
                    Polygon tmpPoly = Polygon.Empty;
                    foreach (var poly in collection)
                    {
                        if (poly is Polygon && poly.Area > tmpPoly.Area)
                            tmpPoly = poly as Polygon;
                    }
                    room = tmpPoly;
                }
            }

            foreach (var col in columns)
            {
                nonDeployableArea.Add(col);
                var geo = room.Difference(col.ToNTSPolygon());
                if (geo is Polygon polygon)
                {
                    room = polygon;
                }
                else if (geo is GeometryCollection collection)
                {
                    Polygon tmpPoly = Polygon.Empty;
                    foreach (var poly in collection)
                    {
                        if (poly is Polygon && poly.Area > tmpPoly.Area)
                            tmpPoly = poly as Polygon;
                    }
                    room = tmpPoly;
                }
            }

            foreach (var pl in prioritys)
            {
                nonDeployableArea.Add(pl);
            }

            objs = room.ToDbCollection();

            foreach (var layout in layoutList)
            {
                var layoutHole = layout.Holes();
                nonDeployableArea.AddRange(layoutHole);
                foreach (var hole in layoutHole)
                {
                    objs = SnapIfNeededOverlayOp.Difference(objs.BuildMPolygon().ToNTSPolygon(), hole.ToNTSPolygon()).ToDbCollection();
                }
            }
        }

        /// <summary>
        /// 只有洞的房间框线（Mpolygon）
        /// 用于生成中心线。
        /// 加入柱子会使中心线形状很奇怪
        /// </summary>
        /// <param name="roomForCenterLine"></param>
        private void GetRoomHoleMPoly(out MPolygon roomForCenterLine)
        {

            var room = frame.ToNTSPolygon();
            foreach (var hole in holeList)
            {
                if (hole.Area / room.Area > 0.9) continue;
                var geo = OverlayNGRobust.Overlay(room, hole.ToNTSPolygon(), SpatialFunction.Difference);
                if (geo is Polygon polygon)
                    room = polygon;
                else if (geo is GeometryCollection collection)
                {
                    Polygon tmpPoly = Polygon.Empty;
                    foreach (var poly in collection)
                    {
                        if (poly is Polygon && poly.Area > tmpPoly.Area)
                            tmpPoly = poly as Polygon;
                    }
                    room = tmpPoly;
                }
            }

            var objs = room.ToDbCollection();
            roomForCenterLine = objs.BuildMPolygon();
        }

        /// <summary>
        /// 加入洞，墙，柱子的房间框线
        /// 前序布置的外框不影响盲区不加入房间框线但是加入不可布区域
        /// 不可布区域：用于生成可布离散点
        /// </summary>
        /// <param name="roomWithAllHole"></param>
        /// <param name="nonDeployableArea"></param>
        public void GetRoomMPoly(out MPolygon roomWithAllHole, out List<Polyline> nonDeployableArea)
        {
            nonDeployableArea = new List<Polyline>();
            nonDeployableArea.AddRange(holeList);
            nonDeployableArea.AddRange(wallList);
            nonDeployableArea.AddRange(columns);

            var room = frame.ToNTSPolygon();
            foreach (var hole in nonDeployableArea)
            {
                if (hole.Area / room.Area > 0.9) continue;
                var geo = OverlayNGRobust.Overlay(room, hole.ToNTSPolygon(), SpatialFunction.Difference);
                if (geo is Polygon polygon)
                    room = polygon;
                else if (geo is GeometryCollection collection)
                {
                    Polygon tmpPoly = Polygon.Empty;
                    foreach (var poly in collection)
                    {
                        if (poly is Polygon && poly.Area > tmpPoly.Area)
                            tmpPoly = poly as Polygon;
                    }
                    room = tmpPoly;
                }
            }

            var objs = room.ToDbCollection();

            foreach (var layout in layoutList)
            {
                var layoutHole = layout.Holes();
                nonDeployableArea.AddRange(layoutHole);
                //foreach (var hole in layoutHole)
                //{
                //    objs = SnapIfNeededOverlayOp.Difference(objs.BuildMPolygon().ToNTSPolygon(), hole.ToNTSPolygon()).ToDbCollection();
                //}
            }

            nonDeployableArea.AddRange(prioritys);//不可提前，之前布的块对盲区没有影响，不能扣掉
            roomWithAllHole = objs.BuildMPolygon();


        }

        public void GetLayoutMPoly(out List<MPolygon> layoutWithAllHole)
        {
            layoutWithAllHole = new List<MPolygon>();

            var holeObj = new DBObjectCollection();
            holeList.ForEach(x => holeObj.Add(x));
            wallList.ForEach(x => holeObj.Add(x));
            columns.ForEach(x => holeObj.Add(x));
            prioritys.ForEach(x => holeObj.Add(x));

            var holeGeom = holeObj.UnionGeometries();
            for (int i = 0; i < layoutList.Count; i++)
            {
                var layout = layoutList[i];
                var layoutWithHole = layout.DifferenceMP(holeObj);

                for (int j = 0; j < layoutWithHole.Count; j++)
                {
                    var diff = layoutWithHole[j];
                    if (diff is Polyline pl)
                    {
                        layoutWithAllHole.Add(ThMPolygonTool.CreateMPolygon(pl));
                    }
                    else if (diff is MPolygon mp)
                    {
                        layoutWithAllHole.Add(mp);
                    }
                }
            }
        }
    }
}