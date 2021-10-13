﻿using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using NetTopologySuite.Operation.Overlay.Snap;
using Linq2Acad;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.AreaLayout.GridLayout.Data;
using ThMEPEngineCore.AreaLayout.CenterLineLayout.Utils;
using ThMEPEngineCore.AreaLayout.CenterLineLayout.LayoutProcess;

namespace ThMEPEngineCore.AreaLayout.CenterLineLayout.Command
{
    public class FireAlarmSystemLayoutCommand : ThMEPBaseCommand
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
        public List<Point3d> layoutPoints { get; set; }//布置点位
        public Dictionary<Point3d, Vector3d> pointsWithDirection { get; set; } //布置点位以及其方向//------------------------------
        public List<Polyline> blinds { get; set; }//盲区

        public override void SubExecute()
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                //Get MPolygon
                var objs = new DBObjectCollection();
                objs.Add(frame);
                foreach (var hole in holeList)// --------------------------------------------
                {
                    hole.Closed = true;
                    objs.Add(hole);
                }
                MPolygon mPolygonShell = objs.BuildMPolygon();
                //Get Can not Layout Area List
                List<Polyline> nonDeployableArea = new List<Polyline>();
                if (holeList.Count != 0)
                {
                    foreach (var pl in holeList)
                    {
                        nonDeployableArea.Add(pl);
                    }
                }
                if (layoutList.Count != 0)
                {
                    foreach (var layout in layoutList)
                    {
                        var layoutHole = layout.Holes();
                        nonDeployableArea.AddRange(layoutHole);
                        layoutHole.ForEach(x => objs = SnapIfNeededOverlayOp.Difference(objs.BuildMPolygon().ToNTSPolygon(), x.ToNTSPolygon()).ToDbCollection());
                    }
                }

                if (wallList.Count != 0)
                {
                    foreach (var pl in wallList)
                    {
                        nonDeployableArea.Add(pl);
                        objs = SnapIfNeededOverlayOp.Difference(objs.BuildMPolygon().ToNTSPolygon(), pl.ToNTSPolygon()).ToDbCollection();
                    }
                }
                if (columns.Count != 0)
                {
                    foreach (var pl in columns)
                    {
                        nonDeployableArea.Add(pl);
                        objs = SnapIfNeededOverlayOp.Difference(objs.BuildMPolygon().ToNTSPolygon(), pl.ToNTSPolygon()).ToDbCollection();
                    }
                }
                if (prioritys.Count != 0)
                {
                    foreach (var pl in prioritys)
                    {
                        nonDeployableArea.Add(pl);
                    }
                }
                MPolygon mPolygon = objs.BuildMPolygon();
                acdb.ModelSpace.Add(mPolygon);
                mPolygon.SetDatabaseDefaults();

                //处理数据
                pointsWithDirection = new Dictionary<Point3d, Vector3d>();
                //计算布置点位
                layoutPoints = LayoutOpt.Calculate(mPolygon, LayoutOpt.GetPosiblePositions(nonDeployableArea, layoutList, radius), radius, equipmentType, acdb, mPolygonShell);
                //获取布置点位方向
                LayoutOpt.PointsWithDirection(frame, holeList, layoutPoints, pointsWithDirection);

                //输出
                blinds = new List<Polyline>();
                NetTopologySuite.Geometries.Geometry unCoverRegion = AreaCaculator.BlandArea(mPolygon, layoutPoints, radius, equipmentType);
                foreach (var ucr in unCoverRegion.ToDbCollection())
                {
                    blinds.Add((Polyline)ucr);
                }
                //善后
                mPolygon.UpgradeOpen();
                mPolygon.Erase();
                mPolygon.DowngradeOpen();

#if DEBUG
                //显示适配信息
                ShowInfo.SafetyCaculate(mPolygon, layoutPoints, radius);

                //显示布置点及方向
                foreach (var dir in pointsWithDirection)
                {
                    ShowInfo.ShowPointAsO(dir.Key, 130, 200);
                    ShowInfo.ShowPointWithDirection(dir.Key, dir.Value, 130);
                }
                //显示盲区
                ShowInfo.ShowGeometry(unCoverRegion, acdb, 130);
#endif      
            }
        }
    }
}