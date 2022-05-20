namespace ThMEPTCH.TCHTables
{
    struct TCHTwtVerticalPipeDim
    {
        /*
         CREATE TABLE TwtVerticalPipeDim (
    ID          INT,
    StartPtID   INT,
    TurnPtID    INT,
    DirectionID INT,
    VPipeID     INT,
    System      TEXT,
    TextStyle   TEXT,
    FloorNum    TEXT,
    DimTypeText TEXT,
    PipeNum     TEXT,
    VPipeB      TEXT,
    FloorType   INT,
    DimType     INT,
    Radius      DOUBLE,
    Spacing     DOUBLE,
    TextHeight  DOUBLE,
    DocScale    DOUBLE
);
         */
        public ulong ID;
        public ulong StartPtID;
        public ulong TurnPtID;
        public ulong DirectionID;
        public ulong VPipeID;
        public string System;
        public string TextStyle;
        public string FloorNum;
        public string DimTypeText;
        public string PipeNum;
        public string VPipeB;
        public int FloorType;
        public int DimType;
        public double Radius;
        public double Spacing;
        public double TextHeight;
        public double DocScale;
    }
}
