namespace ThMEPTCH.TCHTables
{
    struct TCHTelecInterface
    {
        /*
         CREATE TABLE TelecInterface (
    InterfaceId        INT64,
    Position           CHAR(255),
    Breadth            DOUBLE
    Normal             CHAR(255),
    Direction          CHAR(255),
);
         */
        public ulong InterfaceId;
        public string Position;
        public double Breadth;
        public string Normal;
        public string Direction;
    }
}
