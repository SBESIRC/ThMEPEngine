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
using ThMEPEngineCore.Config;

using ThMEPEngineCore.AreaLayout.GridLayout.Command;
using ThMEPEngineCore.AreaLayout.GridLayout.Data;
using ThMEPEngineCore.AreaLayout.CenterLineLayout.Command;

using ThMEPElectrical.FireAlarm.Service;
using ThMEPElectrical.FireAlarmSmokeHeat.Data;
using ThMEPElectrical.FireAlarmSmokeHeat.Model;

namespace ThMEPElectrical.FireAlarmSmokeHeat.Service
{
    class ThFaAreaLayoutService
    {
        public static ThFaAreaLayoutResult ThFaAreaLayoutGrid(Polyline frame, ThSmokeDataQueryService dataQuery, double radius)
        {
            var localResult = new ThFaAreaLayoutResult();
            //ucs处理


            //engine
            var layoutCmd = new AlarmSensorLayoutCmd();
            layoutCmd.frame = frame;
            layoutCmd.holeList = dataQuery.FrameHoleList[frame];
            layoutCmd.layoutList = dataQuery.FrameLayoutList[frame];
            layoutCmd.wallList = dataQuery.FrameWallList[frame];
            layoutCmd.columns = dataQuery.FrameColumnList[frame];
            layoutCmd.prioritys = dataQuery.FramePriorityList[frame];
            layoutCmd.detectArea = dataQuery.FrameDetectAreaList[frame];
            layoutCmd.protectRadius = radius;
            layoutCmd.equipmentType = BlindType.CoverArea;

            layoutCmd.Execute();

            if (layoutCmd.layoutPoints != null && layoutCmd.layoutPoints.Count > 0)
            {
                foreach (var pt in layoutCmd.layoutPoints)
                {
                    var ucsPt = layoutCmd.ucs.Where(x => x.Key.Contains(pt)).FirstOrDefault();
                    if (ucsPt.Value != null)
                    {
                        localResult.layoutPts.Add(pt, ucsPt.Value.GetNormal ());
                    }
                    else
                    {
                        localResult.layoutPts.Add(pt, new Vector3d(0, 1, 0));
                    }
                }
            }
            localResult.blind = layoutCmd.blinds;

            return localResult;
        }

        public static ThFaAreaLayoutResult ThFaAreaLayoutCenterline(Polyline frame, ThSmokeDataQueryService dataQuery, double radius)
        {
            var localResult = new ThFaAreaLayoutResult();

            //engine
            var layoutCmd = new FireAlarmSystemLayoutCommand();
            layoutCmd.frame = frame;
            layoutCmd.holeList = dataQuery.FrameHoleList[frame];
            layoutCmd.layoutList = dataQuery.FrameLayoutList[frame];
            layoutCmd.wallList = dataQuery.FrameWallList[frame];
            layoutCmd.columns = dataQuery.FrameColumnList[frame];
            layoutCmd.prioritys = dataQuery.FramePriorityList[frame];
            layoutCmd.detectArea = dataQuery.FrameDetectAreaList[frame];
            layoutCmd.radius = radius;
            layoutCmd.equipmentType = BlindType.CoverArea;

            layoutCmd.Execute();

            if (layoutCmd.pointsWithDirection != null && layoutCmd.pointsWithDirection.Count > 0)
            {
                localResult.layoutPts  = layoutCmd.pointsWithDirection;
            }
            localResult. blind = layoutCmd.blinds;

            return localResult;
        }

        public static bool isAisleArea(Polyline frame, List<Polyline> HoleList, double shrinkValue, double threshold)
        {
            var objs = new DBObjectCollection();
            objs.Add(frame);
            HoleList.ForEach(x => objs.Add(x));
            var geometry = objs.BuildAreaGeometry();
            var isAisleArea = ThMEPEngineCoreGeUtils.IsAisleArea(geometry, shrinkValue, threshold);

            return isAisleArea;
        }
    }
}
