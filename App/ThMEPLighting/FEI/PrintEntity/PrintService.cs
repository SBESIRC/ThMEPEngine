using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;
using ThMEPElectrical.CAD;
using ThMEPEngineCore.Algorithm;
using ThMEPEngineCore.LaneLine;
using ThMEPLighting.FEI.Model;

namespace ThMEPLighting.FEI.PrintEntity
{
    public class PrintService
    {
        public void PrintPath(List<ExtendLineModel> extendLines, List<Line> lanes, ThMEPOriginTransformer originTransformer)
        {
            CalEvacuationPathTypeService pathType = new CalEvacuationPathTypeService();
            var pathModels = pathType.CalPathType(extendLines, lanes);

            pathModels = ConnectLine(pathModels);
            foreach (var path in pathModels)
            {
                originTransformer.Reset(path.line);
                InsertConnectPipe(path);
            }
        }

        public void InsertConnectPipe(EvacuationPathModel pathModel)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            using (AcadDatabase blockDb = AcadDatabase.Open(ThCADCommon.ElectricalDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                acadDatabase.Layers.Import(
                    blockDb.Layers.ElementOrDefault(ThMEPLightingCommon.MAIN_EVACUATIONPATH_BYHOISTING_LAYERNAME), false);
                acadDatabase.Layers.Import(
                    blockDb.Layers.ElementOrDefault(ThMEPLightingCommon.MAIN_EVACUATIONPATH_BYWALL_LAYERNAME), false);
                acadDatabase.Layers.Import(
                    blockDb.Layers.ElementOrDefault(ThMEPLightingCommon.AUXILIARY_EVACUATIONPATH_BYHOISTING_LAYERNAME), false);
                acadDatabase.Layers.Import(
                    blockDb.Layers.ElementOrDefault(ThMEPLightingCommon.AUXILIARY_EVACUATIONPATH_BYWALL_LAYERNAME), false);

                var path = SetPathInfo(pathModel.line);
                path.ColorIndex = 256;
                if (pathModel.evaPathType == PathType.MainPath)
                {
                    if (pathModel.setUpType == SetUpType.ByHoisting)
                    {
                        path.Layer = ThMEPLightingCommon.MAIN_EVACUATIONPATH_BYHOISTING_LAYERNAME;
                    }
                    else if (pathModel.setUpType == SetUpType.ByWall)
                    {
                        path.Layer = ThMEPLightingCommon.MAIN_EVACUATIONPATH_BYWALL_LAYERNAME;
                    }
                }
                else if (pathModel.evaPathType == PathType.AuxiliaryPath)
                {
                    if (pathModel.setUpType == SetUpType.ByHoisting)
                    {
                        path.Layer = ThMEPLightingCommon.AUXILIARY_EVACUATIONPATH_BYHOISTING_LAYERNAME;
                    }
                    else if (pathModel.setUpType == SetUpType.ByWall)
                    {
                        path.Layer = ThMEPLightingCommon.AUXILIARY_EVACUATIONPATH_BYWALL_LAYERNAME;
                    }
                }
                acadDatabase.ModelSpace.Add(path);
            }
        }

        /// <summary>
        /// 设置路径信息
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private Polyline SetPathInfo(Line line)
        {
            Polyline polyline = new Polyline();
            polyline.AddVertexAt(0, line.StartPoint.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(1, line.EndPoint.ToPoint2D(), 0, 0, 0);

            polyline.ConstantWidth = 200;
            return polyline;
        }

        /// <summary>
        /// 将连接并平行的线合成一根路径
        /// </summary>
        /// <param name="pathModels"></param>
        /// <returns></returns>
        private List<EvacuationPathModel> ConnectLine(List<EvacuationPathModel> pathModels)
        {
            List<EvacuationPathModel> resPaths = new List<EvacuationPathModel>();
            while (pathModels.Count > 0)
            {
                var firLine = pathModels.First();
                pathModels.Remove(firLine);

                var parellelPaths = pathModels
                    .Where(x=> x.evaPathType == firLine.evaPathType && x.setUpType == firLine.setUpType)
                    .Where(x => (x.line.EndPoint - x.line.StartPoint).IsParallelTo(firLine.line.EndPoint - firLine.line.StartPoint, new Tolerance(0.001, 0.001)))
                    .ToList();
                if (parellelPaths.Count > 0)
                {
                    Point3d sPt = firLine.line.StartPoint;
                    Point3d ePt = firLine.line.EndPoint;
                    while (pathModels.Count > 0)
                    {
                        var sMLine = parellelPaths.FirstOrDefault(x => x.line.StartPoint.DistanceTo(sPt) < 3);
                        if (sMLine != null)
                        {
                            parellelPaths.Remove(sMLine);
                            pathModels.Remove(sMLine);
                            sPt = sMLine.line.EndPoint;
                            firLine.line = new Line(sPt, ePt);
                        }

                        var eMLine = parellelPaths.FirstOrDefault(x => x.line.EndPoint.DistanceTo(sPt) < 3);
                        if (eMLine != null)
                        {
                            parellelPaths.Remove(eMLine);
                            pathModels.Remove(eMLine);
                            sPt = eMLine.line.StartPoint;
                            firLine.line = new Line(sPt, ePt);
                        }

                        if (sMLine == null && eMLine == null)
                        {
                            break;
                        }
                    }

                    while (pathModels.Count > 0)
                    {
                        var sMLine = parellelPaths.FirstOrDefault(x => x.line.StartPoint.DistanceTo(ePt) < 3);
                        if (sMLine != null)
                        {
                            parellelPaths.Remove(sMLine);
                            pathModels.Remove(sMLine);
                            ePt = sMLine.line.EndPoint;
                            firLine.line = new Line(sPt, ePt);
                        }

                        var eMLine = parellelPaths.FirstOrDefault(x => x.line.EndPoint.DistanceTo(ePt) < 3);
                        if (eMLine != null)
                        {
                            parellelPaths.Remove(eMLine);
                            pathModels.Remove(eMLine);
                            ePt = eMLine.line.StartPoint;
                            firLine.line = new Line(sPt, ePt);
                        }

                        if (sMLine == null && eMLine == null)
                        {
                            break;
                        }
                    }
                }
                resPaths.Add(firLine);
            }

            return resPaths;
        }
    }
}

