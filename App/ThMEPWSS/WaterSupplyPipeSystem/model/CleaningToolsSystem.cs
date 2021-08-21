namespace ThMEPWSS.WaterSupplyPipeSystem.model
{
    public class CleaningToolsSystem
    {
        private readonly int FloorNumber;//楼层号
        private readonly int PartNumber;//分区号
        private readonly int HouseholdNums;//住户数
        private readonly int[] CleaningTools;//卫生洁具数组

        public CleaningToolsSystem(int floorNumber, int partNumber, int householdNums, int[] cleaningTools)
        {
            FloorNumber = floorNumber;
            PartNumber = partNumber;
            CleaningTools = cleaningTools;
            HouseholdNums = householdNums;
        }

        public int[] GetCleaningTools()
        {
            return CleaningTools;
        }

        public int GetHouseholdNums()
        {
            return HouseholdNums;
        }

        public int GetFloorNumber()
        {
            return FloorNumber;
        }
    }
}
