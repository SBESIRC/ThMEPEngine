using Linq2Acad;
using System.Linq;
using ThCADExtension;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.DatabaseServices;
using TianHua.Publics.BaseCode;
using ThMEPEngineCore.Model.Common;

namespace ThMEPEngineCore.Model.Electrical
{
    public enum EStoreyType
    {
        Unknown,
        LargeRoof,
        SmallRoof,
        StandardStorey,
        NonStandardStorey,
        RefugeStorey,
        PodiumRoof,
        EvenStorey,
        OddStorey,
    }
    public class ThEStoreys : ThIfcSpatialStructureElement
    {
        public ObjectId ObjectId { get; }

        public ThBlockReferenceData Data { get; }
        public ThEStoreys(ObjectId id)
        {
            ObjectId = id;
            Data = new ThBlockReferenceData(id);
        }
        public string StoreyNumber
        {
            get
            {
                switch (StoreyType)
                {
                    case EStoreyType.StandardStorey:
                        {
                            return Data.Attributes[ThPipeCommon.STOREY_ATTRIBUTE_VALUE_STANDAD_NUMBER];
                        }
                    case EStoreyType.NonStandardStorey:
                        {
                            return Data.Attributes[ThPipeCommon.STOREY_ATTRIBUTE_VALUE_NONSTANDAD_NUMBER];
                        }
                    case EStoreyType.RefugeStorey:
                        {
                            return Data.Attributes[ThPipeCommon.STOREY_ATTRIBUTE_VALUE_REFUGE_NUMBER];
                        }
                    case EStoreyType.PodiumRoof:
                        {
                            return Data.Attributes[ThPipeCommon.STOREY_ATTRIBUTE_VALUE_PODIUM_NUMBER];
                        }
                    case EStoreyType.EvenStorey:
                        {
                            return Data.Attributes[ThPipeCommon.STOREY_ATTRIBUTE_VALUE_EVEN_FLOOR_NUMBER];
                        }
                    case EStoreyType.OddStorey:
                        {
                            return Data.Attributes[ThPipeCommon.STOREY_ATTRIBUTE_VALUE_ODD_FLOOR_NUMBER];
                        }
                    default:
                        {
                            return string.Empty;
                        }
                }
            }
        }
        public string StoreyTypeString => (string)Data.CustomProperties.GetValue(ThPipeCommon.STOREY_DYNAMIC_PROPERTY_TYPE);

        public EStoreyType StoreyType
        {
            get
            {
                switch (StoreyTypeString)
                {
                    case ThPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_TOP_ROOF_FLOOR: return EStoreyType.SmallRoof;
                    case ThPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_ROOF_FLOOR: return EStoreyType.LargeRoof;
                    case ThPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_STANDARD_FLOOR: return EStoreyType.StandardStorey;
                    case ThPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_NON_STANDARD_FLOOR: return EStoreyType.NonStandardStorey;
                    case ThPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_NOT_STANDARD_FLOOR: return EStoreyType.NonStandardStorey;
                    case ThPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_REFUGE_FLOOR: return EStoreyType.RefugeStorey;
                    case ThPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_PODIUM_ROOF: return EStoreyType.PodiumRoof;
                    case ThPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_EVEN_FLOOR: return EStoreyType.EvenStorey;
                    case ThPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_ODD_FLOOR: return EStoreyType.OddStorey;
                    default: return EStoreyType.Unknown;
                }
            }
        }

        public List<int> Storeys
        {
            get
            {
                var storeys = new List<int>();
                switch (StoreyType)
                {
                    case EStoreyType.StandardStorey:
                    case EStoreyType.NonStandardStorey:
                    case EStoreyType.RefugeStorey:
                    case EStoreyType.PodiumRoof:
                        {
                            var parser = new VentSNCalculator(StoreyNumber);
                            storeys.AddRange(parser.SerialNumbers);
                            break;
                        }
                    case EStoreyType.EvenStorey:
                        {
                            var parser = new VentSNCalculator(StoreyNumber);
                            storeys.AddRange(parser.SerialNumbers);
                            storeys.RemoveAll(o => (o & 1) == 1);
                            break;
                        }
                    case EStoreyType.OddStorey:
                        {
                            var parser = new VentSNCalculator(StoreyNumber);
                            storeys.AddRange(parser.SerialNumbers);
                            storeys.RemoveAll(o => (o & 1) == 0);
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
