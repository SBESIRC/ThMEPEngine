namespace ThMEPTCH.TCHArchDataConvert.TCHArchTables
{
    class TArchDoor
    {
        /*
        CREATE TABLE TArchDoor(
    ID INT64,
    TextPointZ DOUBLE,
    TextPointY DOUBLE,
    TextPointX DOUBLE,
    BasePointX DOUBLE,
    BasePointY DOUBLE,
    BasePointz DOUBLE,
    Quadrant INT64,
    Width DOUBLE,
    dThickness DOUBLE,
    Height DOUBLE,
    Name CHAR,
    Kind INT64,
    SubKind CHAR,
    EvacuationType INT64,
    TextStyle CHAR,
    TextLayer CHAR,
    StyleID CHAR
);*/
        public ulong Id { get; set; }
        public double TextPointZ { get; set; }
        public double TextPointX { get; set; }
        public double TextPointY { get; set; }
        public double BasePointX { get; set; }
        public double BasePointY { get; set; }
        public double BasePointZ { get; set; }
        public ulong Quadrant { get; set; }
        public string Name { get; set; }
        public double Height { get; set; }
        public double Width { get; set; }
        public double Thickness { get; set; }
        public string LineType { get; set; }
        public int Kind { get; set; }
        public string SubKind { get; set; }
        public string StyleID { get; set; }
    }
}
