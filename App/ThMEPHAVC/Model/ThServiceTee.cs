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
                                           double Shrinkm,
                                           DBObjectCollection bypass_lines)
        {
            foreach (var edge in LineGraph.Edges)
            {
                if (LineGraph.OutDegree(edge.Target) == 2)
                {
                    var out1 = LineGraph.OutEdges(edge.Target).First();
                    var out2 = LineGraph.OutEdges(edge.Target).Last();
                    if (is_bypass(out1.Source.Position, out1.Target.Position, bypass_lines))
                    {
                        edge.TargetShrink = IShrink;
                        out1.SourceShrink = Shrinkb;
                        out2.SourceShrink = Shrinkm;
                    }
                    else
                    {
                        edge.TargetShrink = IShrink;
                        out1.SourceShrink = Shrinkm;
                        out2.SourceShrink = Shrinkb;
                    }
                    break;
                }
            }
        }
        public static ObjectId InsertElectricValve( Vector3d fan_cp_vec,
                                                    double valvewidth,
                                                    double angle, 
                                                    bool hasTee)
        {
            var e = new ThValve() {
                Length = 200,
                Width = valvewidth,
                ValveBlockName = ThHvacCommon.AIRVALVE_BLOCK_NAME,
                ValveBlockLayer = "H-DAPP-EDAMP",
                ValveVisibility = ThDuctUtils.ElectricValveModelName(),
                WidthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_WIDTHDIA,
                LengthPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_HEIGHT,
                VisibilityPropertyName = ThHvacCommon.BLOCK_DYNAMIC_PROPERTY_VALVE_VISIBILITY,
            };
            using (AcadDatabase acadDatabase = AcadDatabase.Active())
            {
                var blockName = e.ValveBlockName;
                var layerName = e.ValveBlockLayer;
                Active.Database.ImportLayer(layerName);
                Active.Database.ImportValve(blockName);
                var objId = Active.Database.InsertValve(blockName, layerName);
                objId.SetValveWidth(e.Width, e.WidthPropertyName);
                objId.SetValveModel(e.ValveVisibility);

                var blockRef = acadDatabase.Element<BlockReference>(objId, true);
                Matrix3d mat = Matrix3d.Displacement(fan_cp_vec) *
                               Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin);
                mat *= Matrix3d.Displacement(new Vector3d(-valvewidth / 2, 125, 0));
                //if (hasTee)
                //{
                //    mat *= Matrix3d.Displacement(new Vector3d(-valvewidth / 2, 125, 0));
                //}

                blockRef.TransformBy(mat);
                return objId;
            }
        }
    }
}
