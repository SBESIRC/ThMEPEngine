namespace ThMEPTCH.TCHTables
{
    struct TCHTelecTee
    {
        /*
         CREATE TABLE TelecTee (
    ObjectId            INT64,
    Type                CHAR(255),
    TeeStyle            CHAR(255),
    Style               INT(8),
    CabletraySystem     CHAR(255),
    Height              DOUBLE,
    Length              DOUBLE,
    Length2             DOUBLE,
    Cover               INT(8),
    MidPosition         CHAR(255),
    ClapboardId         INT64,
    MajInterfaceId      INT64,
    MinInterfaceId      INT64,
    Min2InterfaceId     INT64,
);
         */
        public ulong ObjectId;
        public string Type;
        public string TeeStyle;
        public int Style;
        public string CabletraySystem;
        public double Height;
        public double Length;
        public double Length2;
        public int Cover;
        public string MidPosition;
        public ulong ClapboardId;
        public ulong MajInterfaceId;
        public ulong MinInterfaceId;
        public ulong Min2InterfaceId;
    }
}
