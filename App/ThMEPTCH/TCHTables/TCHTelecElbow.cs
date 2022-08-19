namespace ThMEPTCH.TCHTables
{
    struct TCHTelecElbow
    {
        /*
         CREATE TABLE TelecElbow (
    ObjectId            INT64,
    Type                CHAR(255),
    ElbowStyle          CHAR(255),
    Style               INT(8),
    CabletraySystem     CHAR(255),
    Height              DOUBLE,
    Length              DOUBLE,
    Cover               INT(8),
    MidPosition         CHAR(255),
    ClapboardId         INT64,
    MajInterfaceId      INT64,
    MinInterfaceId      INT64,
);
         */
        public ulong ObjectId;
        public string Type;
        public string ElbowStyle;
        public int Style;
        public string CabletraySystem;
        public double Height;
        public double Length;
        public int Cover;
        public string MidPosition;
        public ulong ClapboardId;
        public ulong MajInterfaceId;
        public ulong MinInterfaceId;
    }
}
