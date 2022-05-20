namespace ThMEPTCH.TCHTables
{
    struct TCHTwtSprinkler
    {
        /*
         CREATE TABLE TwtSprinkler (
    ID         INT,
    LocationID INT,
    Type       INT,
    LinkMode   INT,
    System     TEXT,
    K          INT,
    Angle      DOUBLE,
    Radius     DOUBLE,
    PipeLength DOUBLE,
    PipeDn     DOUBLE,
    SizeX      DOUBLE,
    SizeY      DOUBLE,
    DocScale   DOUBLE,
    MirrorByX  INT,
    MirrorByY  INT,
    HidePipe   INT
);
         */
        public ulong ID;
        public ulong LocationID;
        public int Type;
        public int LinkMode;
        public int K;
        public string System;
        public double Angle;
        public double Radius;
        public double PipeLength;
        public double PipeDN;
        public double SizeX;
        public double SizeY;
        public double DocScale;
        public int MirrorByX;
        public int MirrorByY;
        public int HidePipe;
    }
}
