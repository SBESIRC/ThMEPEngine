using ThCADExtension;
using ThMEPEngineCore.Model;
using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;

namespace ThMEPWSS.Pipe.Model
{
    public enum StoreyType
    {
        Unknown,
        LargeRoof,
        SmallRoof,
        StandardStorey,
        NonStandardStorey,
    }
    public class ThWStoreys : ThIfcSpatialStructureElement
    {
        public ThBlockReferenceData Data { get; }
        public ThWStoreys(ObjectId id)
        {
            Data = new ThBlockReferenceData(id);
        }
        public string StoreyNumber => Data.Attributes["楼层编号"];
        public StoreyType StoreyType
        {
            get
            {
                switch (StoreyTypeString)
                {
                    case "小屋面": return StoreyType.SmallRoof;
                    case "大屋面": return StoreyType.LargeRoof;
                    case "标准层": return StoreyType.StandardStorey;
                    case "非标准层": return StoreyType.NonStandardStorey;
                    default: return StoreyType.Unknown;
                }
            }
        }
        private string StoreyTypeString => (string)Data.CustomProperties.GetValue("楼层类型");
    }
}
