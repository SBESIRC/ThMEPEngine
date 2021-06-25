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
    }

    public enum EFloorRangeType
    {
        Unknown,
        Self,
        AllFloors,
        EvenFloors,
        OddFloors,
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
                            if (Data.Attributes.ContainsKey(ThPipeCommon.STOREY_ATTRIBUTE_VALUE_STANDAD_NUMBER))
                            {
                                return Data.Attributes[ThPipeCommon.STOREY_ATTRIBUTE_VALUE_STANDAD_NUMBER];
                            }
                            else if (Data.Attributes.ContainsKey(ThPipeCommon.STOREY_ATTRIBUTE_VALUE_NUMBER))
                            {
                                return Data.Attributes[ThPipeCommon.STOREY_ATTRIBUTE_VALUE_NUMBER];
                            }
                            return string.Empty;
                        }
                    case EStoreyType.NonStandardStorey:
                        {
                            if (Data.Attributes.ContainsKey(ThPipeCommon.STOREY_ATTRIBUTE_VALUE_NONSTANDAD_NUMBER))
                            {
                                return Data.Attributes[ThPipeCommon.STOREY_ATTRIBUTE_VALUE_NONSTANDAD_NUMBER];
                            }
                            else if (Data.Attributes.ContainsKey(ThPipeCommon.STOREY_ATTRIBUTE_VALUE_NUMBER))
                            {
                                return Data.Attributes[ThPipeCommon.STOREY_ATTRIBUTE_VALUE_NUMBER];
                            }
                            return string.Empty;
                        }
                    case EStoreyType.RefugeStorey:
                        {
                            if (Data.Attributes.ContainsKey(ThPipeCommon.STOREY_ATTRIBUTE_VALUE_REFUGE_NUMBER))
                            {
                                return Data.Attributes[ThPipeCommon.STOREY_ATTRIBUTE_VALUE_REFUGE_NUMBER];
                            }
                            return string.Empty;
                        }
                    case EStoreyType.PodiumRoof:
                        {
                            if (Data.Attributes.ContainsKey(ThPipeCommon.STOREY_ATTRIBUTE_VALUE_PODIUM_NUMBER))
                            {
                                return Data.Attributes[ThPipeCommon.STOREY_ATTRIBUTE_VALUE_PODIUM_NUMBER];
                            }
                            return string.Empty;
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
                    default: return EStoreyType.Unknown;
                }
            }
        }

        public EFloorRangeType FloorRange
        {
            get
            {
                if (StoreyType == EStoreyType.StandardStorey && Data.Attributes.ContainsKey(ThPipeCommon.STOREY_ATTRIBUTE_VALUE_RANGE))
                {
                    switch (Data.Attributes[ThPipeCommon.STOREY_ATTRIBUTE_VALUE_RANGE])
                    {
                        case ThPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_ALL_FLOOR: return EFloorRangeType.AllFloors;
                        case ThPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_EVEN_FLOOR: return EFloorRangeType.EvenFloors;
                        case ThPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_ODD_FLOOR: return EFloorRangeType.OddFloors;
                        default: return EFloorRangeType.Unknown;
                    }
                }
                else
                {
                    return EFloorRangeType.Self;
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
                            switch (FloorRange)
                            {
                                case EFloorRangeType.EvenFloors:
                                    {
                                        storeys.RemoveAll(o => (o & 1) == 1);
                                        break;
                                    }
                                case EFloorRangeType.OddFloors:
                                    {
                                        storeys.RemoveAll(o => (o & 1) == 0);
                                        break;
                                    }
                                default:
                                    break;
                            }
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
