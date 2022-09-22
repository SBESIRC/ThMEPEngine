using System;

namespace ThMEPTCH.TCHTables
{
    struct TCHTwtDimensionDim
    {
        //CREATE TABLE TwtDimensionDim(
        //    ID INT,
        //    LocationID INT,
        //    System TEXT,
        //    Rotation DOUBLE,
        //    Dist2DimLine DOUBLE,
        //    DocScale DOUBLE,
        //    DimStyle TEXT,
        //    LayoutRotation DOUBLE,
        //    SegmentStartID INT
        //);

        public ulong ID;
        public ulong LocationID;
        public string System;
        public double Rotation;
        public double Dist2DimLine;
        public double DocScale;
        public string DimStyle;
        public double LayoutRotation;
        public ulong SegmentStartID;

    }
}
