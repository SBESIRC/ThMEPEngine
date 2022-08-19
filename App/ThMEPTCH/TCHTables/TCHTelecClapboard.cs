namespace ThMEPTCH.TCHTables
{
    struct TCHTelecClapboard
    {
        /*
         CREATE TABLE TelecClapboard (
    ClapboardId        INT64,
    HaveClapboard      INT(8),
    ClapboardData      CHAR(255),
    ClapboardData2     CHAR(255),
    ClapboardMainBr    CHAR(255),
    ClapboardSubBr     CHAR(255),
);
         */
        public ulong ClapboardId;
        public int HaveClapboard;
        public string ClapboardData;
        public string ClapboardData2;
        public string ClapboardMainBr;
        public string ClapboardSubBr;
    }
}
