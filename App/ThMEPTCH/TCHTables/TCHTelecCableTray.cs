namespace ThMEPTCH.TCHTables
{
    struct TCHTelecCableTray
    {
        /*
         CREATE TABLE TelecCabletry (
    ObjectId            INT64,
    Type                CHAR(255),
    Style               INT(8),
    CabletraySystem     CHAR(255),
    Height              DOUBLE,
    Cover               INT(8),
    ClapboardId         INT64,
    StratInterfaceId    INT64,
    EndInterfaceId      INT64,
);
         */
        public ulong ObjectId;
        public string Type;
        public int Style;
        public string CabletraySystem;
        public double Height;
        public int Cover;
        public ulong? ClapboardId;
        public ulong StartInterfaceId;
        public ulong EndInterfaceId;
    }
}
