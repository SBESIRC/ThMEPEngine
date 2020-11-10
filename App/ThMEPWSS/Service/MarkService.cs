using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPWSS.Model;
using ThWSS;

namespace ThMEPWSS.Service
{
    public static class MarkService
    {
        /// <summary>
        /// 打印错误喷淋点位
        /// </summary>
        /// <param name="errorSprays"></param>
        public static void PrintErrorSpray(List<SprayLayoutData> errorSprays, Matrix3d matrix)
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var layerId = LayerTools.AddLayer(acdb.Database, ThWSSCommon.Layout_Error_Spray_LayerName);
                acdb.Database.UnFrozenLayer(ThWSSCommon.Layout_Error_Spray_LayerName);
                acdb.Database.UnLockLayer(ThWSSCommon.Layout_Error_Spray_LayerName);
                acdb.Database.UnOffLayer(ThWSSCommon.Layout_Error_Spray_LayerName);
                //打印有问题的但无法移动的喷淋点位
                foreach (var spray in errorSprays)
                {
                    var transPt = spray.Position.TransformBy(matrix);
                    var sprayInnerCircle = new Circle(transPt, Vector3d.ZAxis, 300);
                    sprayInnerCircle.ColorIndex = 30;
                    sprayInnerCircle.LayerId = layerId;
                    acdb.ModelSpace.Add(sprayInnerCircle);

                    var sprayOuterCircle = new Circle(transPt, Vector3d.ZAxis, 400);
                    sprayOuterCircle.ColorIndex = 30;
                    sprayOuterCircle.LayerId = layerId;
                    acdb.ModelSpace.Add(sprayOuterCircle);

                    // 填充面积框线
                    Hatch hatch = new Hatch();
                    hatch.LayerId = layerId;
                    acdb.ModelSpace.Add(hatch);
                    hatch.ColorIndex = 30;
                    hatch.SetHatchPattern(HatchPatternType.PreDefined, "Solid");
                    hatch.Associative = true;
                    hatch.AppendLoop(HatchLoopTypes.External, new ObjectIdCollection() { sprayOuterCircle.Id });
                    hatch.AppendLoop(HatchLoopTypes.Default, new ObjectIdCollection() { sprayInnerCircle.Id });
                    // 重新生成Hatch纹理
                    hatch.EvaluateHatch(true);

                    //打成group
                    ObjectIdList ids = new ObjectIdList(new ObjectId[3] { sprayInnerCircle.Id, sprayOuterCircle.Id, hatch.Id });
                    acdb.Database.CreateGroup(hatch.Id.ToString(), ids);
                }
            }
        }

        /// <summary>
        /// 打印喷淋挪动后前后位置对比
        /// </summary>
        /// <param name="allSpray"></param>
        public static void PrintOriginSpray(List<SprayLayoutData> allSpray)
        {
            var moveSprays = allSpray.Where(x => !x.OriginPt.IsEqualTo(x.Position, new Tolerance(1, 1)));
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var layerId = LayerTools.AddLayer(acdb.Database, ThWSSCommon.Layout_Origin_Spray_LayerName);
                acdb.Database.UnFrozenLayer(ThWSSCommon.Layout_Origin_Spray_LayerName);
                acdb.Database.UnLockLayer(ThWSSCommon.Layout_Origin_Spray_LayerName);
                acdb.Database.UnOffLayer(ThWSSCommon.Layout_Origin_Spray_LayerName);
                //打印有问题的但无法移动的喷淋点位
                foreach (var spray in moveSprays)
                {
                    var sprayInnerCircle = new Circle(spray.OriginPt, Vector3d.ZAxis, 100);
                    sprayInnerCircle.ColorIndex = 2;
                    sprayInnerCircle.LayerId = layerId;
                    acdb.ModelSpace.Add(sprayInnerCircle);

                    var sprayOuterCircle = new Circle(spray.OriginPt, Vector3d.ZAxis, 150);
                    sprayOuterCircle.ColorIndex = 2;
                    sprayOuterCircle.LayerId = layerId;
                    acdb.ModelSpace.Add(sprayOuterCircle);

                    // 填充面积框线
                    Hatch hatch = new Hatch();
                    hatch.LayerId = layerId;
                    acdb.ModelSpace.Add(hatch);
                    hatch.ColorIndex = 2;
                    hatch.SetHatchPattern(HatchPatternType.PreDefined, "Solid");
                    hatch.Associative = true;
                    hatch.AppendLoop(HatchLoopTypes.External, new ObjectIdCollection() { sprayOuterCircle.Id });
                    hatch.AppendLoop(HatchLoopTypes.Default, new ObjectIdCollection() { sprayInnerCircle.Id });
                    // 重新生成Hatch纹理
                    hatch.EvaluateHatch(true);

                    Polyline connectPl = new Polyline() { Closed = true };
                    connectPl.AddVertexAt(0, spray.OriginPt.ToPoint2D(), 0, 0, 0);
                    connectPl.AddVertexAt(0, spray.Position.ToPoint2D(), 0, 0, 0);
                    connectPl.ColorIndex = 2;
                    connectPl.LayerId = layerId;
                    connectPl.ConstantWidth = 50;
                    acdb.ModelSpace.Add(connectPl);

                    //打成group
                    ObjectIdList ids = new ObjectIdList(new ObjectId[4] { sprayInnerCircle.Id, sprayOuterCircle.Id, hatch.Id, connectPl.Id });
                    acdb.Database.CreateGroup(hatch.Id.ToString(), ids);
                }
            }
        }

        /// <summary>
        /// 打印可布置区域
        /// </summary>
        /// <param name="polylines"></param>
        public static void PrintLayoutArea(List<Polyline> polylines, Matrix3d matrix)
        {
            using (AcadDatabase acdb = AcadDatabase.Active())
            {
                var layerId = LayerTools.AddLayer(acdb.Database, ThWSSCommon.Layout_Area_LayerName);
                acdb.Database.UnFrozenLayer(ThWSSCommon.Layout_Area_LayerName);
                acdb.Database.UnLockLayer(ThWSSCommon.Layout_Area_LayerName);
                acdb.Database.UnOffLayer(ThWSSCommon.Layout_Area_LayerName);
                foreach (var area in polylines)
                {
                    area.TransformBy(matrix);
                    area.ColorIndex = 3;
                    area.LayerId = layerId;
                    acdb.ModelSpace.Add(area);
                }
            }
        }
    }
}
