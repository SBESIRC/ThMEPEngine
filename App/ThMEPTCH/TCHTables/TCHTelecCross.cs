namespace ThMEPTCH.TCHTables
{
    struct TCHTelecCross
    {
        /*
         CREATE TABLE TelecTee (
    ObjectId            INT64,
    Type                CHAR(255),
    CrossStyle          CHAR(255),
    Style               INT(8),
    CabletraySystem     CHAR(255),
    Height              DOUBLE,
    Length              DOUBLE,
    Cover               INT(8),
    MidPosition         CHAR(255),
    InclineFit          INT(8),
    ClapboardId         INT64,
    MajInterfaceId      INT64,
    MinInterfaceId      INT64,
    InterfaceId3        INT64,
    InterfaceId4        INT64,
);
         */
        public ulong ObjectId;
        public string Type;
        public string CrossStyle;
        public int Style;
        public string CabletraySystem;
        public double Height;
        public double Length;
        public int Cover;
        public string MidPosition;
        public int InclineFit;
        public ulong ClapboardId;
        public ulong MajInterfaceId;
        public ulong MinInterfaceId;
        public ulong InterfaceId3;
        public ulong InterfaceId4;
    }
}
