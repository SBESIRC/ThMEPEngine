using System;
using System.Linq;
using System.Collections.Generic;

using NFox.Cad;
using AcHelper;
using Linq2Acad;
using DotNetARX;
using Dreambuild.AutoCAD;
using GeometryExtensions;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;

using ThCADExtension;
using ThMEPEngineCore.Service.Hvac;

namespace ThMEPElectrical.BlockConvert
{
    public class ThBConvertRefreshService
    {
        public void Refresh()
        {
            using (var docLock = Active.Document.LockDocument())
            using (var currentDb = AcadDatabase.Active())
            using (PointCollector pc = new PointCollector(PointCollector.Shape.Window, new List<string>()))
            {
                try
                {
                    pc.Collect();
                }
                catch
                {
                    return;
                }
                Point3dCollection winCorners = pc.CollectedPoints;
                var frame = new Polyline();
                frame.CreateRectangle(winCorners[0].ToPoint2d(), winCorners[1].ToPoint2d());
                frame.TransformBy(Active.Editor.UCS2WCS());

                var targetNames = new List<string>
                {
                    ThBConvertCommon.BLOCK_MOTOR_AND_LOAD_DIMENSION,
                    ThBConvertCommon.BLOCK_MOTOR_AND_LOAD_DIMENSION + "2",
                    ThBConvertCommon.BLOCK_LOAD_DIMENSION,
                    ThBConvertCommon.BLOCK_LOAD_DIMENSION + "2",
                    ThBConvertCommon.BLOCK_PUMP_LABEL,
                };

                var targetEngine = new ThBConvertBlockExtractionEngine()
                {
                    NameFilter = targetNames,
                };
                targetEngine.ExtractFromMS(currentDb.Database);
                var blocks = ThBConvertSpatialIndexService.SelectCrossingPolygon(targetEngine.Results, frame);

                if (blocks.Count == 0)
                {
                    return;
                }

                var ucsToWcs = ThBConvertMatrix3dTools.DecomposeWithoutDisplacement();
                blocks.ForEach(targetBlockData =>
                {
                    var targetBlock = currentDb.Element<BlockReference>(targetBlockData.ObjId, true);
                    targetBlockData.Transform(targetBlock, ucsToWcs);

                    //如果不是动态块，则返回
                    if (targetBlock == null || !targetBlock.IsDynamicBlock)
                    {
                        return;
                    }
                    var targetProperties = targetBlock.DynamicBlockReferencePropertyCollection;
                    if (targetProperties.IsNull())
                    {
                        return;
                    }

                    if (targetProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_POSITION1_X)
                        && targetProperties.Contains(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_POSITION1_Y))
                    {
                        var labelX = (double)targetProperties.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_POSITION1_X);
                        var labelY = (double)targetProperties.GetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_POSITION1_Y);
                        Matrix3d roration;
                        if (targetBlockData.ScaleFactors.X > 0)
                        {
                            roration = Matrix3d.Rotation(targetBlockData.Rotation, Vector3d.ZAxis, Point3d.Origin);
                        }
                        else
                        {
                            roration = Matrix3d.Rotation(-targetBlockData.Rotation, Vector3d.ZAxis, Point3d.Origin);
                        }
                        var vector = new Vector3d(labelX, labelY, 0).TransformBy(ucsToWcs.Inverse().PreMultiplyBy(roration));

                        targetProperties.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_POSITION1_X, vector.X);
                        targetProperties.SetValue(ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_POSITION1_Y, vector.Y);
                    }
                });
            }
        }
    }
}
