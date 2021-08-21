namespace ThMEPWSS.WaterSupplyPipeSystem.model
{
    public class ThWSSDPipeUnit  //竖管单元
    {
        private string PipeDiameter { get; set; }
        private int FloorNumber { get; set; }

        public ThWSSDPipeUnit(string pipeDiameter, int floorNumber)  //构造函数
        {
            PipeDiameter = pipeDiameter;
            FloorNumber = floorNumber;
        }

        public string GetPipeDiameter()
        {
            return PipeDiameter;
        }
    }
}
