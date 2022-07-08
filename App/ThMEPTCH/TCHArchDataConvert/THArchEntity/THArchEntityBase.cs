using Autodesk.AutoCAD.DatabaseServices;
using System;
using ThMEPTCH.TCHArchDataConvert.TCHArchTables;

namespace ThMEPTCH.TCHArchDataConvert.THArchEntity
{
    abstract class THArchEntityBase
    {
        public string Id { get; }
        public ulong DBId { get; }
        public MPolygon OutLine { get; set; }
        public TArchEntity DBArchEntiy { get; }
        public THArchEntityBase(TArchEntity dbEntity)
        {
            Id = Guid.NewGuid().ToString();
            DBId = dbEntity.Id;
            DBArchEntiy = dbEntity;
        }
    }
}
