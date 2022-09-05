namespace ThMEPTCH.TCHTables
{
    struct TCHTwtPipe
    {
        /*CREATE TABLE TCHTwtPipe (
         *  ID          INT,
         *  StartPtID   INT,
         *  EndPtID     INT,
         *  System      TEXT,
         *  Material    TEXT,
         *  DnType      TEXT,
         *  Dn          DOUBLE,
         *  Gradient    DOUBLE,
         *  Weight      DOUBLE,
         *  HideLevel   INT,
         *  DocScale    DOUBLE,
         *  DimID       INT);*/

        public ulong ID;
        public ulong StartPtID;
        public ulong EndPtID;
        public string System;
        public string Material;
        public string DnType;
        public double Dn;
        public double Gradient;
        public double Weight;
        public int HideLevel;
        public double DocScale;
        public ulong DimID;
    }
}
