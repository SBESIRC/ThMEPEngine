﻿using System;
using AcHelper;
using Linq2Acad;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using AcHelper.Commands;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;
using ThMEPWSS.Service;
using ThMEPWSS.Bussiness;
using ThMEPWSS.Bussiness.LayoutBussiness;

namespace ThMEPWSS.Command
{
    public class ThSprinklerLayoutCmd : IAcadCommand, IDisposable
    {
        public void Dispose()
        {
            //
        }

        public void Execute()
        {
            var polylines = ThSprinklerLayoutCmdUtils.GetFrames();
            if (polylines.Count <= 0)
            {
                return;
            }
            if (!ThSprinklerLayoutCmdUtils.CalWCSLayoutDirection(out Matrix3d matrix))
            {
                return;
            }

            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                CalHolesService calHolesService = new CalHolesService();
                var holeDic = calHolesService.CalHoles(polylines);
                foreach (var holeInfo in holeDic)
                {
                    var plFrame = holeInfo.Key;
                    var holes = holeInfo.Value;

                    //清除原有构件
                    plFrame.ClearSprayLines();
                    plFrame.ClearSpray();
                    plFrame.ClearBlindArea();
                    plFrame.ClearErrorSprayMark();
                    plFrame.ClearMoveSprayMark();
                    plFrame.ClearLayouArea();

                    //获取构建信息
                    var calStructPoly = (plFrame.Clone() as Polyline).Buffer(10000)[0] as Polyline;
                    ThSprinklerLayoutCmdUtils.GetStructureInfo(acdb, calStructPoly, plFrame, out List<Polyline> columns, out List<Polyline> beams, out List<Polyline> walls);
                    
                    //转换usc
                    plFrame.TransformBy(matrix.Inverse());
                    holes.ForEach(x => x.TransformBy(matrix.Inverse()));
                    columns.ForEach(x => x.TransformBy(matrix.Inverse()));
                    beams.ForEach(x => x.TransformBy(matrix.Inverse()));
                    walls.ForEach(x => x.TransformBy(matrix.Inverse()));

                    //生成喷头
                    RayLayoutService layoutDemo = new RayLayoutService();
                    var sprayPts = layoutDemo.LayoutSpray(plFrame, columns, beams, walls, holes, matrix, false);

                    //放置喷头
                    InsertSprinklerService.Insert(sprayPts.Select(o => o.Position).ToList());

                    //打印喷头变化轨迹
                    MarkService.PrintOriginSpray(sprayPts);

                    //打印喷淋点盲区
                    CalSprayBlindAreaService calSprayBlindAreaService = new CalSprayBlindAreaService(matrix);
                    calSprayBlindAreaService.CalSprayBlindArea(sprayPts, plFrame, holes);
                }
            }
        }
    }
}
