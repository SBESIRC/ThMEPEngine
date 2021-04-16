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
using ThMEPLighting.FEI.Model;

namespace ThMEPLighting.FEI.PrintEntity
{
    public class PrintService
    {
        public void PrintPath(List<ExtendLineModel> extendLines, List<Line> lanes, ThMEPOriginTransformer originTransformer)
        {
            CalEvacuationPathTypeService pathType = new CalEvacuationPathTypeService();
            var pathModels = pathType.CalPathType(extendLines, lanes);

            foreach (var path in pathModels)
            {
                originTransformer.Reset(path.line);
                InsertConnectPipe(path);
            }
        }

        public void InsertConnectPipe(EvacuationPathModel pathModel)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                acadDatabase.Database.ImportLayer(ThMEPLightingCommon.MAIN_EVACUATIONPATH_BYHOISTING_LAYERNAME);
                acadDatabase.Database.ImportLayer(ThMEPLightingCommon.MAIN_EVACUATIONPATH_BYWALL_LAYERNAME);
                acadDatabase.Database.ImportLayer(ThMEPLightingCommon.AUXILIARY_EVACUATIONPATH_BYHOISTING_LAYERNAME);
                acadDatabase.Database.ImportLayer(ThMEPLightingCommon.AUXILIARY_EVACUATIONPATH_BYWALL_LAYERNAME);
                //acadDatabase.Database.ImportLinetype(ThMEPLightingCommon.ConnectPipeLineType);

                var path = SetPathInfo(pathModel.line);
                path.ColorIndex = 256;
                //pipe.Linetype = ThMEPCommon.ConnectPipeLineType;
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

        private Polyline SetPathInfo(Line line)
        {
            Polyline polyline = new Polyline();
            polyline.AddVertexAt(0, line.StartPoint.ToPoint2D(), 0, 0, 0);
            polyline.AddVertexAt(1, line.EndPoint.ToPoint2D(), 0, 0, 0);

            polyline.ConstantWidth = 200;
            return polyline;
        }
    }
}
