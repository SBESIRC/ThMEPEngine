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
        public static void SeperateIODuct(DBObjectCollection IOLineSet, double SepDis)
        {
            if (IOLineSet.Count == 7)
            {
                int ind = 0;
                double maxX = 0;
                foreach (Line line in IOLineSet)
                {
                    if (line.StartPoint.X == line.EndPoint.X)
                    {
                        maxX = line.StartPoint.X;
                        break;
                    }
                }
                for (int i = 0; i < 7; ++i)
                {
                    Line line = IOLineSet[i] as Line;
                    if (line.StartPoint.X == line.EndPoint.X && line.StartPoint.X > maxX)
                    {
                        ind = i;
                        break;
                    }
                }
                Line l = IOLineSet[ind] as Line;
                IOLineSet.RemoveAt(ind);
                // 先插后一条再插前一条
                Point3d intP = l.StartPoint + new Vector3d(0, SepDis + 125, 0);

                IOLineSet.Insert(ind, new Line(intP + new Vector3d(0, 10, 0), l.EndPoint));
                IOLineSet.Insert(ind, new Line(l.StartPoint, intP + new Vector3d(0, -10, 0)));

                // intP是伐的插入点
            }
        }
        public static void TeeRefineDuct(AdjacencyGraph<ThDuctVertex, ThDuctEdge<ThDuctVertex>> LineGraph,
                                         double IShrink,
                                         double OShrink1,
                                         double OShrink2)
        {
            foreach (var edge in LineGraph.Edges)
            {
                if (LineGraph.OutDegree(edge.Target) == 2)
                {
                    var out1 = LineGraph.OutEdges(edge.Target).First();
                    var out2 = LineGraph.OutEdges(edge.Target).Last();
                    edge.TargetShrink = IShrink;
                    out1.SourceShrink = OShrink1;
                    out2.SourceShrink = OShrink2;
                }
            }
        }
        public static ObjectId InsertElectricValve(Point3d DisVec,
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
        private static ThValve CreateElectricValve(Point3d DisVec,
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
