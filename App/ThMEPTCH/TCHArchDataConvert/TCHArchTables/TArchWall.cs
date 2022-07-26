namespace ThMEPTCH.TCHArchDataConvert.TCHArchTables
{
    public class TArchWall : TArchEntity
    {
        /*
        CREATE TABLE TArchWall(
    ID INT64,
    MidPointZ DOUBLE,
    MidPointY DOUBLE,
    MidPointX DOUBLE,
    StartPointX DOUBLE,
    StartPointY DOUBLE,
    StartPointZ DOUBLE,
    EndPointX DOUBLE,
    EndPointY DOUBLE,
    EndPointZ DOUBLE,
    LeftWidth DOUBLE,
    RightWidth DOUBLE,
    IsArc BOOL,
    Bulge DOUBLE,
    Height DOUBLE,
    UsageID INTEGER,
    MaterialID INTEGER,
    FireproofID INTEGER,
    leftInsulateWidth DOUBLE,
    rightInsulateWidth DOUBLE,
    Elevtion DOUBLE,
    DocScale DOUBLE,
    LineType CHAR,
    Layer CHAR,
    CONSTRAINT sqlite_autoindex_TArchWall_1 PRIMARY KEY (
        ID
    )
);*/
        public double MidPointZ { get; set; }
        public double MidPointX { get; set; }
        public double MidPointY { get; set; }
        public double StartPointX { get; set; }
        public double StartPointY { get; set; }
        public double StartPointZ { get; set; }
        public double EndPointX { get; set; }
        public double EndPointY { get; set; }
        public double EndPointZ { get; set; }
        public double LeftWidth { get; set; }
        public double LeftInsulateWidth { get; set; }
        public double RightWidth { get; set; }
        public double RightInsulateWidth { get; set; }
        public bool IsArc { get; set; }
        public double Bulge { get; set; }
        public double Height { get; set;}
        public double Elevtion { get; set; }
        public double DocScale { get; set; }
        public int UsageId { get; set; }
        public string UsageName { get; set; }
        public int MaterialID { get; set; }
        public string MaterialName { get; set; }
        public int FireproofID { get; set; }
        public string FireproofName { get; set; }
    }
}
