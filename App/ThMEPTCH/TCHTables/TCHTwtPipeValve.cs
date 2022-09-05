namespace ThMEPTCH.TCHTables
{
    struct TCHTwtPipeValve
    {
        /*CREATE TABLE TCHTwtPipeValve (
         *  ID                  INT,
         *  LocationID          INT,
         *  DirectionID         INT,
         *  BlockID             INT,
         *  PipeID              INT,
         *  System              TEXT,
         *  Length              DOUBLE,
         *  Width               DOUBLE,
         *  InterruptWidth      DOUBLE,
         *  DocScale            DOUBLE);*/

        public ulong ID;
        public ulong LocationID;
        public ulong DirectionID;
        public ulong BlockID;
        public ulong PipeID;
        public string System;
        public double Length;
        public double Width;
        public double InterruptWidth;
        public double DocScale;
    }
}
