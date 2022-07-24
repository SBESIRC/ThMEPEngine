using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.BlockConvert
{
    public class ThRevcloudParameter
    {
        public Database Database { get; private set; }
        public Polyline Obb { get; private set; }
        public short ColorIndex { get; private set; }
        public string Linetype { get; private set; }
        public double Scale { get; private set; }

        public ThRevcloudParameter(Database database, Polyline obb, short colorIndex, string linetype, double scale)
        {
            Database = database;
            Obb = obb;
            ColorIndex = colorIndex;
            Linetype = linetype;
            Scale = scale;
        }
    }
}
