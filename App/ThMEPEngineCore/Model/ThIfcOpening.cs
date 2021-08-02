namespace ThMEPEngineCore.Model
{
    public class ThIfcOpening : ThIfcElement
    {
        public double Width { get; set; }
        public double Height { get; set; }        
        
        public ThIfcOpening()
        {
            Useage = "";
        }
    }
}
