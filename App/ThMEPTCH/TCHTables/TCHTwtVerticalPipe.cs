namespace ThMEPTCH.TCHTables
{
    struct TCHTwtVerticalPipe
    {
        /*CREATE TABLE TwtVerticalPipe (
         *  ID INT, 
         *  StartPtID INT,
         *  EndPtID INT, 
         *  System      TEXT,
         *  Material TEXT, 
         *  DnType      TEXT, 
         *  Dn DOUBLE, 
         *  ShowDn      DOUBLE, 
         *  Number TEXT,
         *  FloorNumber TEXT, 
         *  DocScale DOUBLE );*/
        public ulong ID;
        public ulong StartPtID;
        public ulong EndPtID;
        public string System;
        public string Material;
        public string DnType;
        public double Dn;
        public double ShowDn;
        public string Number;
        public string FloorNumber;
        public double DocScale;
    }
}
