using AcHelper;
using ThMEPArchitecture.ParkingStallArrangement.Model;

namespace ThMEPArchitecture.ParkingStallArrangement.General
{
    public static class ParkingSpace
    {
        public static void GetSingleParkingSpace(Serilog.Core.Logger Logger, LayoutParameter layoutPara, int parkingStallCount)
        {
            var totalArea = layoutPara.OuterBoundary.Area / 1e6;
            var singleParkingSpace = totalArea / parkingStallCount;

            var totalAreaStr = totalArea.ToString("f2");
            var singleParkingSpaceStr = singleParkingSpace.ToString("f2");
            Logger?.Information($"地库总面积: {totalAreaStr} 平米");
            Logger?.Information($"车位数: { parkingStallCount} 辆");
            Logger?.Information($"单车位指标: {singleParkingSpaceStr } 平米/辆");
            Active.Editor.WriteMessage($"地库总面积: { totalAreaStr} 平米\n");
            Active.Editor.WriteMessage($"车位数: { parkingStallCount} 辆\n");
            Active.Editor.WriteMessage($"单车位指标: {singleParkingSpaceStr } 平米/辆 \n");
        }
    }
}
