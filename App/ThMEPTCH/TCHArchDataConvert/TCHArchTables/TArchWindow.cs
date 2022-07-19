namespace ThMEPTCH.TCHArchDataConvert.TCHArchTables
{
    class TArchWindow:TArchEntity
    {
        /*
        CREATE TABLE TArchWindow(
    ID INT64,
    BasePointX DOUBLE,
    BasePointY DOUBLE,
    BasePointZ DOUBLE,
    Width DOUBLE,
    Height DOUBLE,
    SillHeight DOUBLE,
    Number CHAR,
    Kind INT64,
    SubKind CHAR,
    TextLayer CHAR,
    TextStyle CHAR,
    StyleID CHAR,
    dThickness DOUBLE,
    TextPointX DOUBLE,
    TextPointY DOUBLE,
    TextPointZ DOUBLE
);*/
        public string Number { get; set; }
        public double TextPointZ { get; set; }
        public double TextPointX { get; set; }
        public double TextPointY { get; set; }
        public double BasePointX { get; set; }
        public double BasePointY { get; set; }
        public double BasePointZ { get; set; }
        public ulong Quadrant { get; set; }
        public double Height { get; set; }
        public double SillHeight { get; set; }
        public double Width { get; set; }
        public double Thickness { get; set; }
        public int Kind { get; set; }
        public string SubKind { get; set; }
        public double Rotation { get; set; }
    }
}
