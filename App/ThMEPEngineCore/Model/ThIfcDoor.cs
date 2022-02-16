namespace ThMEPEngineCore.Model
{
    public class ThIfcDoor : ThIfcBuildingElement
    {
        public double OpenAngle { get; set; } 
        public string Switch { get; set; }
        public double Height { get; set; }

        public ThIfcDoor()
        {
            Switch = "";
        }
    }
}
