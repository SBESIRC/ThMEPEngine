using System.Linq;
using Linq2Acad;
using ThCADCore.NTS;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using System.Collections;
using ThMEPEngineCore.Command;
using ThMEPElectrical.AlarmLayout.Utils;
using ThMEPElectrical.AlarmLayout.LayoutProcess;
using ThMEPElectrical.AlarmSensorLayout.Data;
using NetTopologySuite.Operation.Overlay.Snap;
using NetTopologySuite.Geometries;
using ThMEPElectrical.AlarmSensorLayout.Method;
using NetTopologySuite.Operation.OverlayNG;

namespace ThMEPElectrical.AlarmLayout.Command
{
    class FireAlarmSystemLayoutCommand : ThMEPBaseCommand
    {
        //inputs
        public Polyline frame { get; set; }//房间外框线
        public List<Polyline> holeList { get; set; }//洞
        public List<Polyline> layoutList { get; set; }//可布置区域
        public List<Polyline> wallList { get; set; } //墙
        public List<Polyline> columns { get; set; }//柱子
        public List<Polyline> prioritys { get; set; }//优先级更高点位，比如要躲避已布置好的区域
        public List<Polyline> detectArea { get; set; }//探测区域
        public Dictionary<Polyline, Vector3d> ucs { get; set; }//UCS， maybe input or output
        public double radius { get; set; }//保护半径
        public BlindType equipmentType { get; set; }//盲区类型

        //outputs
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
                foreach (var hole in holeList)
                {
                    objs.Add(hole);
                }

                //Get Can not Layout Area List
                List<Polyline> nonDeployableArea = new List<Polyline>();
                
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

                //加入数据库
                acdb.ModelSpace.Add(mPolygon);
                mPolygon.SetDatabaseDefaults();

                pointsWithDirection = new Dictionary<Point3d, Vector3d>();
                //处理数据
                layoutPoints = LayoutOpt.Calculate(mPolygon, GetPosiblePositions(nonDeployableArea, layoutList, radius), radius, acdb, equipmentType, pointsWithDirection);


                //输出
                blinds = new List<Polyline>();
                NetTopologySuite.Geometries.Geometry unCoverRegion = AreaCaculator.BlandArea(mPolygon, layoutPoints, radius, equipmentType);
                foreach(var ucr in unCoverRegion.ToDbCollection())
                {
                    blinds.Add((Polyline)ucr);
                }

                //善后
                mPolygon.UpgradeOpen();
                mPolygon.Erase();
                mPolygon.DowngradeOpen();

                //显示适配信息
                ShowInfo.SafetyCaculate(mPolygon, layoutPoints, radius);
                //显示布置点
                foreach(var dir in pointsWithDirection)
                {
                    ShowInfo.ShowPointWithDirection(dir.Key, dir.Value, 130);
                }
                //ShowInfo.ShowPoints(layoutPoints, 'X');
                //显示盲区
                ShowInfo.ShowGeometry(unCoverRegion, acdb, 130);
            }
        }

        /// <summary>
        /// 获取可布置点位
        /// </summary>
        /// <param name="nonDeployableArea"></param>
        /// <param name="layoutList"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static List<Point3d> GetPosiblePositions(List<Polyline> nonDeployableArea, List<Polyline> layoutList, double radius)
        {
            List<Point3d> pointsInLayoutList = PointsDealer.PointsInAreas(layoutList, radius).Distinct().ToList();
            Hashtable ht = new Hashtable();
            foreach(var pt in pointsInLayoutList)
            {
                ht[pt] = true;
            }
            foreach(var pl in nonDeployableArea)
            {
                foreach(var pt in pointsInLayoutList)
                {
                    if (pl.ContainsOrOnBoundary(pt))
                    {
                        ht[pt] = false;
                    }
                }
            }
            List<Point3d> ans = new List<Point3d>();
            foreach (DictionaryEntry xx in ht)
            {
                if ((bool)xx.Value == true)
                {
                    ans.Add((Point3d)xx.Key);
                }
            }
            return ans.Distinct().ToList();
        }
    }
}
