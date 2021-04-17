using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Linq2Acad;
using QuickGraph;
using System.Linq;
using ThMEPEngineCore.Service.Hvac;
using ThMEPHVAC.CAD;
using ThMEPHVAC.Duct;

namespace ThMEPHVAC.Model
{
    public class ThServiceTee
    {
        internal static bool Is_bypass(Point3d tar_srt_pos,
                                       Point3d tar_end_pos,
                                       DBObjectCollection bypass_lines)
        {
            if (bypass_lines == null || bypass_lines.Count == 0)
                return false;
            Tolerance t = new Tolerance(2.5, 2.5);
            foreach (Line l in bypass_lines)
            {
                if ((l.StartPoint.IsEqualTo(tar_srt_pos, t) &&
                    l.EndPoint.IsEqualTo(tar_end_pos, t)) ||
                    (l.StartPoint.IsEqualTo(tar_end_pos, t) &&
                    l.EndPoint.IsEqualTo(tar_srt_pos, t)))
                {

                    return true;
                }
            }
            return false;
        }
        public static void Fine_tee_duct(AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> LineGraph,
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
                    if (Is_bypass(out1.Source.Position, out1.Target.Position, bypass_lines))
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

        private static object CreateDuctTextStyle()
        {
            throw new System.NotImplementedException();
        }

        private static object CreateLayer(string textlayer)
        {
            throw new System.NotImplementedException();
        }

        public static ObjectId Insert_electric_valve(Vector3d fan_cp_vec,
                                                    double valvewidth,
                                                    double angle)
        {
            var e = new ThValve()
            {
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

                blockRef.TransformBy(mat);
                return objId;
            }
        }
    }
}
