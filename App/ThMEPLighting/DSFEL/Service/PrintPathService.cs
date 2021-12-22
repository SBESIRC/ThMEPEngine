using Linq2Acad;
using System.Linq;
using ThCADExtension;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Algorithm;

namespace ThMEPLighting.DSFEL.Service
{
    public class PrintPathService
    {
        public void PrintPath(List<Line> extendLines, List<Line> lanes, ThMEPOriginTransformer originTransformer)
        {
            extendLines.ForEach(x => originTransformer.Reset(x));
            lanes.ForEach(x => originTransformer.Reset(x));
            //paths = ConnectLine(paths);
            InsertPath(extendLines, ThMEPLightingCommon.MAIN_EVACUATIONPATH_BYHOISTING_LAYERNAME);
            InsertPath(lanes, ThMEPLightingCommon.MAIN_EVACUATIONPATH_BYWALL_LAYERNAME);
        }

        public void InsertPath(List<Line> paths, string layer)
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

                foreach (var path in paths)
                {
                    var pathLine = path.Clone() as Line;
                    pathLine.ColorIndex = 256;
                    pathLine.Layer = layer;
                    acadDatabase.ModelSpace.Add(pathLine);
                }
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
