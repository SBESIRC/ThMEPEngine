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
       // public bool straightLineMode { get; set; }//盲区类型
        public BlindType equipmentType { get; set; }//盲区类型

        //outputs
        public List<Point3d> layoutPoints { get; set; }//布置点位

        public Dictionary<Point3d, UcsTable> pointsWithDirection { get; set; } //布置点位以及其方向//------------------------------UcsTable
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
                MPolygon mPolygon = objs.BuildMPolygon();
                //加入数据库
                acdb.ModelSpace.Add(mPolygon);
                mPolygon.SetDatabaseDefaults();


                //Get Can not Layout Area List
                List<Polyline> nonDeployableArea = new List<Polyline>();
                
                if (wallList.Count != 0)
                {
                    foreach (var pl in wallList)
                    {
                        nonDeployableArea.Add(pl);
                    }
                }
                if (columns.Count != 0)
                {
                    foreach (var pl in columns)
                    {
                        nonDeployableArea.Add(pl);
                    }
                }
                if (prioritys.Count != 0)
                {
                    foreach (var pl in prioritys)
                    {
                        nonDeployableArea.Add(pl);
                    }
                }
                //Get Can Layout Positions
                List<Point3d> canLayoutPoints = GetPosiblePositions(nonDeployableArea, layoutList, radius);

                layoutPoints = LayoutOpt.Calculate(mPolygon, canLayoutPoints, radius, acdb, equipmentType);
            }
        }

        public static List<Point3d> GetPosiblePositions(List<Polyline> nonDeployableArea, List<Polyline> layoutList, double radius)
        {
            List<Point3d> pointsInLayoutList = PointsDealer.PointsInAreas(layoutList, radius);
            pointsInLayoutList.Distinct();
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
            ans.Distinct();
            return ans;
        }


    }
}
