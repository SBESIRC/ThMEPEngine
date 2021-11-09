using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPElectrical.CAD;
using ThMEPEngineCore.Algorithm;

namespace ThMEPLighting.DSFEL.Service
{
    public class PrintPathService
    {
        public void PrintPath(List<Line> extendLines, List<Line> lanes, ThMEPOriginTransformer originTransformer)
        {
            var paths = new List<Line>(extendLines);
            paths.AddRange(lanes);

            paths = ConnectLine(paths);
            foreach (var path in paths)
            {
                //originTransformer.Reset(path);
                InsertConnectPipe(path);
            }
        }

        public void InsertConnectPipe(Line path)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.ImportLayer(ThMEPLightingCommon.MAIN_EVACUATIONPATH_BYHOISTING_LAYERNAME);
                acadDatabase.Database.ImportLayer(ThMEPLightingCommon.MAIN_EVACUATIONPATH_BYWALL_LAYERNAME);
                acadDatabase.Database.ImportLayer(ThMEPLightingCommon.AUXILIARY_EVACUATIONPATH_BYHOISTING_LAYERNAME);
                acadDatabase.Database.ImportLayer(ThMEPLightingCommon.AUXILIARY_EVACUATIONPATH_BYWALL_LAYERNAME);

                path.ColorIndex = 256;
                path.Layer = ThMEPLightingCommon.MAIN_EVACUATIONPATH_BYHOISTING_LAYERNAME;
                acadDatabase.ModelSpace.Add(path);
            }
        }

        /// <summary>
        /// 将连接并平行的线合成一根路径
        /// </summary>
        /// <param name="pathModels"></param>
        /// <returns></returns>
        private List<Line> ConnectLine(List<Line> paths)
        {
            List<Line> resPaths = new List<Line>();
            while (paths.Count > 0)
            {
                var firLine = paths.First();
                paths.Remove(firLine);

                var parellelPaths = paths
                    .Where(x => (x.EndPoint - x.StartPoint).IsParallelTo(firLine.EndPoint - firLine.StartPoint, new Tolerance(0.001, 0.001)))
                    .ToList();
                if (parellelPaths.Count > 0)
                {
                    Point3d sPt = firLine.StartPoint;
                    Point3d ePt = firLine.EndPoint;
                    while (paths.Count > 0)
                    {
                        var sMLine = parellelPaths.FirstOrDefault(x => x.StartPoint.DistanceTo(sPt) < 3);
                        if (sMLine != null)
                        {
                            parellelPaths.Remove(sMLine);
                            paths.Remove(sMLine);
                            sPt = sMLine.EndPoint;
                            firLine = new Line(sPt, ePt);
                        }

                        var eMLine = parellelPaths.FirstOrDefault(x => x.EndPoint.DistanceTo(sPt) < 3);
                        if (eMLine != null)
                        {
                            parellelPaths.Remove(eMLine);
                            paths.Remove(eMLine);
                            sPt = eMLine.StartPoint;
                            firLine = new Line(sPt, ePt);
                        }

                        if (sMLine == null && eMLine == null)
                        {
                            break;
                        }
                    }

                    while (paths.Count > 0)
                    {
                        var sMLine = parellelPaths.FirstOrDefault(x => x.StartPoint.DistanceTo(ePt) < 3);
                        if (sMLine != null)
                        {
                            parellelPaths.Remove(sMLine);
                            paths.Remove(sMLine);
                            ePt = sMLine.EndPoint;
                            firLine = new Line(sPt, ePt);
                        }

                        var eMLine = parellelPaths.FirstOrDefault(x => x.EndPoint.DistanceTo(ePt) < 3);
                        if (eMLine != null)
                        {
                            parellelPaths.Remove(eMLine);
                            paths.Remove(eMLine);
                            ePt = eMLine.StartPoint;
                            firLine = new Line(sPt, ePt);
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
