namespace ThMEPLighting.Garage.Model
{
    public class ThEntityParameter
    {
        public string Layer { get; set; }
        public short ColorIndex { get; set; }
        public string LineType { get; set; }
        public ThEntityParameter()
        {
            Layer = "";
            LineType = "";
        }
    }
}
