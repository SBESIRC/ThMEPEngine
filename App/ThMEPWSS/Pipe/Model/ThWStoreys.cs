using Linq2Acad;
using ThCADExtension;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPEngineCore.Model.Common;
namespace ThMEPWSS.Pipe.Model
{
    //public enum StoreyType
    //{
    //    Unknown,
    //    LargeRoof,
    //    SmallRoof,
    //    StandardStorey,
    //    NonStandardStorey,
    //}
    public class ThWStoreys : ThIfcSpatialStructureElement
    {
        public ObjectId ObjectId { get; }

        public ThBlockReferenceData Data { get; }
        public ThWStoreys(ObjectId id)
        {
            ObjectId = id;
            Data = new ThBlockReferenceData(id);
        }
        public string StoreyNumber => Data.Attributes[ThWPipeCommon.STOREY_ATTRIBUTE_VALUE_NUMBER];
        private string StoreyTypeString => (string)Data.CustomProperties.GetValue(ThWPipeCommon.STOREY_DYNAMIC_PROPERTY_TYPE);
        public StoreyType StoreyType
        {
            get
            {
                switch (StoreyTypeString)
                {
                    case ThWPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_TOP_ROOF_FLOOR: return StoreyType.SmallRoof;
                    case ThWPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_ROOF_FLOOR: return StoreyType.LargeRoof;
                    case ThWPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_STANDARD_FLOOR: return StoreyType.StandardStorey;
                    case ThWPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_NON_STANDARD_FLOOR: return StoreyType.NonStandardStorey;
                    case ThWPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_NOT_STANDARD_FLOOR: return StoreyType.NonStandardStorey;
                    default: return StoreyType.Unknown;
                }
            }
        }
        public List<int> Storeys
        {
            get
            {
                var storeys = new List<int>();
                switch(StoreyType)
                {
                    case StoreyType.StandardStorey:
                    case StoreyType.NonStandardStorey:
                        {
                            var parser = new VentSNCalculator(StoreyNumber);
                            storeys.AddRange(parser.SerialNumbers);
                            break;
                        }
                    default:
                        break;
                }
                return storeys;
            }
        }
    }
}
