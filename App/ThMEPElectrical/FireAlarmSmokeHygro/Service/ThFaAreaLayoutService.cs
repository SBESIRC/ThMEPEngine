using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;

using AcHelper;
using Linq2Acad;
using GeometryExtensions;
using NFox.Cad;

using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.Command;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.IO;
using ThMEPEngineCore.IO.GeoJSON;

using ThMEPElectrical.AlarmSensorLayout.Command;
using ThMEPElectrical.AlarmSensorLayout.Data;
using ThMEPElectrical.AlarmLayout.Command;

using ThMEPElectrical.FireAlarm.Service;
using ThMEPElectrical.FireAlarmSmokeHeat.Data;

namespace ThMEPElectrical.FireAlarmSmokeHeat.Service
{
    class ThFaAreaLayoutService
    {
        public static Dictionary<Point3d, Vector3d> ThFaAreaLayoutGrid(Polyline frame, ThSmokeDataQueryService dataQuery, double radius, out AlarmSensorLayoutCmd layoutCmd)
        {
            //ucs处理


            //engine
            var layoutResult = new Dictionary<Point3d, Vector3d>();
            layoutCmd = new AlarmSensorLayoutCmd();
            layoutCmd.frame = frame;
            layoutCmd.holeList = dataQuery.frameHoleList[frame];
            layoutCmd.layoutList = dataQuery.frameLayoutList[frame];
            layoutCmd.wallList = dataQuery.frameWallList[frame];
            layoutCmd.columns = dataQuery.frameColumnList[frame];
            layoutCmd.prioritys = dataQuery.framePriorityList[frame];
            layoutCmd.detectArea = dataQuery.FrameDetectAreaList[frame];
            layoutCmd.protectRadius = radius;
            layoutCmd.equipmentType = BlindType.VisibleArea;

            DrawUtils.ShowGeometry(frame, "l0room", 30);
            DrawUtils.ShowGeometry(layoutCmd.wallList, "l0Wall", 10);
            DrawUtils.ShowGeometry(layoutCmd.columns, "l0Column", 3);
            DrawUtils.ShowGeometry(layoutCmd.holeList, "l0hole", 140);
            DrawUtils.ShowGeometry(layoutCmd.prioritys, "l0AvoidEquipments", 200);
            DrawUtils.ShowGeometry(layoutCmd.layoutList, "l0PlaceCoverage", 200);

            layoutCmd.Execute();

            if (layoutCmd.layoutPoints != null && layoutCmd.layoutPoints.Count > 0)
            {
                foreach (var pt in layoutCmd.layoutPoints)
                {
                    var ucsPt = layoutCmd.ucs.Where(x => x.Key.Contains(pt)).FirstOrDefault();
                    if (ucsPt.Value != null)
                    {
                        layoutResult.Add(pt, ucsPt.Value);
                    }
                    else
                    {
                        layoutResult.Add(pt, new Vector3d(0, 1, 0));
                    }
                }

                //debug
                foreach (var re in layoutResult)
                {
                    DrawUtils.ShowGeometry(re.Key, re.Value, "l0result", 4, 35, 200);
                    DrawUtils.ShowGeometry(re.Key, "l0result", 4, 35, 50);
                }
                DrawUtils.ShowGeometry(layoutCmd.blinds, "l0blines", 1);
            }
            return layoutResult;
        }


        public static Dictionary<Point3d, Vector3d> ThFaAreaLayoutCenterline(Polyline frame, ThSmokeDataQueryService dataQuery, double radius, out FireAlarmSystemLayoutCommand layoutCmd)
        {
            //engine
            var layoutResult = new Dictionary<Point3d, Vector3d>();
            layoutCmd = new FireAlarmSystemLayoutCommand();
            layoutCmd.frame = frame;
            layoutCmd.holeList = dataQuery.frameHoleList[frame];
            layoutCmd.layoutList = dataQuery.frameLayoutList[frame];
            layoutCmd.wallList = dataQuery.frameWallList[frame];
            layoutCmd.columns = dataQuery.frameColumnList[frame];
            layoutCmd.prioritys = dataQuery.framePriorityList[frame];
            layoutCmd.detectArea = dataQuery.FrameDetectAreaList[frame];
            layoutCmd.radius = radius;
            layoutCmd.equipmentType = BlindType.VisibleArea;

            DrawUtils.ShowGeometry(frame, "l0cl-room", 30);
            DrawUtils.ShowGeometry(layoutCmd.wallList, "l0cl-Wall", 10);
            DrawUtils.ShowGeometry(layoutCmd.columns, "l0cl-Column", 3);
            DrawUtils.ShowGeometry(layoutCmd.holeList, "l0cl-hole", 140);
            DrawUtils.ShowGeometry(layoutCmd.prioritys, "l0cl-AvoidEquipments", 200);
            DrawUtils.ShowGeometry(layoutCmd.layoutList, "l0cl-PlaceCoverage", 200);

            layoutCmd.Execute();

            if (layoutCmd.pointsWithDirection != null && layoutCmd.pointsWithDirection.Count > 0)
            {
                layoutResult = layoutCmd.pointsWithDirection;

                //debug
                foreach (var re in layoutResult)
                {
                    DrawUtils.ShowGeometry(re.Key, re.Value, "l0cl-result", 4, 35, 200);
                    DrawUtils.ShowGeometry(re.Key, "l0cl-result", 4, 35, 50);
                }
                DrawUtils.ShowGeometry(layoutCmd.blinds, "l0cl-blines", 1);
            }
            return layoutResult;
        }

        /// <summary>
        /// 计算半径
        /// </summary>
        /// <param name="roomArea"></param>
        /// <param name="hightInt"></param>
        /// <param name="thetaInt"></param>
        /// <param name="isSmokeSensor"></param>
        /// <returns></returns>
        public static double calculateRadius(double roomArea, int hightInt, int thetaInt, bool isSmokeSensor)
        {
            double radius = 5800;

            return radius;
        }

        /// <summary>
        /// 读配置表.true:smoke senser, false:heat senser
        /// </summary>
        /// <param name="frameList"></param>
        /// <returns></returns>
        public static Dictionary<Polyline, bool> getAreaSensorType(List<Polyline> frameList)
        {
            var frameSensorType = new Dictionary<Polyline, bool>();

            foreach (var frame in frameList)
            {
                frameSensorType.Add(frame, true);
            }

            return frameSensorType;

        }


    }
}
