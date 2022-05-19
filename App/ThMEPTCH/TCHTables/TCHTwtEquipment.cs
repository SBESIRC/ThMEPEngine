using System;

namespace ThMEPTCH.TCHTables
{
    struct TCHTwtEquipment
    {
        /*CREATE TABLE TwtEquipment(
         *  ID INT, 
         *  LocationID INT, 
         *  DirectionID INT, 
         *  BlockID INT, 
         *  System TEXT, 
         *  Style INT, 
         *  SizeX DOUBLE,
         *  SizeY DOUBLE, 
         *  DocScale DOUBLE, 
         *  HidePipe INT, 
         *  MirrorByX INT,  
         *  MirrorByY INT );*/
        public ulong ID;
        public ulong LocationID;
        public ulong DirectionID;
        public ulong BlockID;
        public string System;
        public Int32 Style;
        public double SizeX;
        public double SizeY;
        public double DocScale;
        public Int32 HidePipe;
        public Int32 MirrorByX;
        public Int32 MirrorByY;
    }
}
