namespace ThMEPWSS.Pipe.Model
{
    public class ThWKitchenPipeParameters
    {
        public double Diameter { get; set; }
        public string Identifier { get; set; }
        public ThWKitchenPipeParameters(int number, double diameter)
        {
            Diameter = diameter;
            Identifier = string.Format("废水FLx{0}", number);
        }
    }

    public class ThWKitchenPipe : ThWPipe
    {
    }
}
