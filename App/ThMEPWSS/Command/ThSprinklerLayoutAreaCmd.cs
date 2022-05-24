using System;
using AcHelper;
using Linq2Acad;
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

namespace ThMEPWSS.Command
{
    public class ThSprinklerLayoutAreaCmd : IAcadCommand, IDisposable
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
                    plFrame.ClearLayouArea();

                    //获取构建信息
                    var calStructPoly = (plFrame.Clone() as Polyline).Buffer(10000)[0] as Polyline;
                    ThSprinklerLayoutCmdUtils.GetStructureInfo(acdb, calStructPoly, plFrame, out List<Polyline> columns, out List<Entity> beams, out List<Polyline> walls);

                    //转换usc
                    plFrame.TransformBy(matrix.Inverse());
                    holes.ForEach(x => x.TransformBy(matrix.Inverse()));
                    columns.ForEach(x => x.TransformBy(matrix.Inverse()));
                    beams.ForEach(x => x.TransformBy(matrix.Inverse()));
                    walls.ForEach(x => x.TransformBy(matrix.Inverse()));
                    holes.AddRange(walls);

                    //不考虑梁
                    if (!ThWSSUIService.Instance.Parameter.ConsiderBeam)
                    {
                        beams = new List<Entity>();
                    }

                    //计算可布置区域
                    var layoutAreas = CreateLayoutAreaService.GetLayoutArea(plFrame, beams, columns, holes, 300);

                    //打印可布置区域
                    MarkService.PrintLayoutArea(layoutAreas, matrix);
                }
            }
        }
    }
}
