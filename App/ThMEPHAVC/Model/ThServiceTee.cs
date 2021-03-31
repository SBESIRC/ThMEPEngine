using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using QuickGraph;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.CAD;
using ThMEPHVAC.Duct;

namespace ThMEPHVAC.Model
{
    public class ThServiceTee
    {
        internal static bool is_bypass(Point3d tar_srt_pos,
                                       Point3d tar_end_pos,
                                       DBObjectCollection bypass_lines)
        {
            if (bypass_lines == null)
                return false;
            foreach (Line l in bypass_lines)
            {
                if ((l.StartPoint.IsEqualTo(tar_srt_pos) &&
                    l.EndPoint.IsEqualTo(tar_end_pos)) ||
                    (l.StartPoint.IsEqualTo(tar_end_pos) &&
                    l.EndPoint.IsEqualTo(tar_srt_pos)))
                {
                    return true;
                }
            }
            return false;
        }

        public static void TeeFineTuneDuct(AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> LineGraph,
                                           double IShrink,
                                           double Shrinkb,
                                           double Shrinkm)
        {
            foreach (var edge in LineGraph.Edges)
            {
                if (LineGraph.OutDegree(edge.Target) == 2)
                {
                    var out1 = LineGraph.OutEdges(edge.Target).First();
                    var out2 = LineGraph.OutEdges(edge.Target).Last();
                    edge.TargetShrink = IShrink;
                    out1.SourceShrink = Shrinkb;
                    out2.SourceShrink = Shrinkm;
                }
            }
        }
        public static ObjectId InsertElectricValve( Point3d DisVec,
                                                    double valvewidth,
                                                    double rotationangle)
        {
            var e = CreateElectricValve(DisVec, valvewidth, rotationangle);
            return SetElectricValve(e);
        }
        private static ObjectId SetElectricValve(ThValve ValveModel)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blockName = ValveModel.ValveBlockName;
                var layerName = ValveModel.ValveBlockLayer;
                Active.Database.ImportLayer(layerName);
                Active.Database.ImportValve(blockName);
                var objId = Active.Database.InsertValve(blockName, layerName);
                objId.SetValveWidth(ValveModel.Width, ValveModel.WidthPropertyName);
                objId.SetValveModel(ValveModel.ValveVisibility);

                var blockRef = acadDatabase.Element<BlockReference>(objId, true);
                blockRef.TransformBy(ValveModel.Marix);

                return objId;
            }
        }
        private static ThValve CreateElectricValve( Point3d DisVec,
                                                    double valvewidth,
                                                    double rotationangle)
        {
            return new ThValve()
            {
                Length = 200,
                Width = valvewidth,
                RotationAngle = rotationangle,
                ValvePosition = DisVec,
                ValveBlockName = ThHvacCommon.AIRVALVE_BLOCK_NAME,
                ValveBlockLayer = "H-DAPP-EDAMP",
                ValveVisibility = ThDuctUtils.ElectricValveModelName(),
                WidthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA,
                LengthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_HEIGHT,
                VisibilityPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY,
            };
        }
    }
}
