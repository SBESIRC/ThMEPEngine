using System;
using Linq2Acad;
using System.Linq;
using ThCADExtension;
using Dreambuild.AutoCAD;
using TianHua.Publics.BaseCode;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Model.Common
{
    public enum StoreyType
    {
        Unknown,
        LargeRoof,
        SmallRoof,
        StandardStorey,
        NonStandardStorey,
    }

    public class VentSNCalculator
    {
        public List<int> SerialNumbers { get; private set; }
        public VentSNCalculator(string ventNum)
        {
            string _Sign = string.Empty;
            var _SerialNumbers = new List<int>();
            MatchCollection _Matche = Regex.Matches(ventNum, @"\d+\,*\-*");
            if (_Matche.Count > 0)
            {
                for (int i = 0; i < _Matche.Count; i++)
                {
                    string _Str = string.Empty;
                    string _TmpSign = string.Empty;
                    if (FuncStr.NullToStr(_Matche[i]).Contains("-"))
                    {
                        _TmpSign = "-";
                    }
                    if (FuncStr.NullToStr(_Matche[i]).Contains(","))
                    {

                        _TmpSign = ",";
                    }
                    _Str = FuncStr.NullToStr(_Matche[i]).Replace(",", "").Replace("-", "");
                    if (_Str == string.Empty) continue;

                    var _Tmp = FuncStr.NullToInt(_Str);

                    CalcVentQuan(_SerialNumbers, _Tmp, _Sign);
                    _Sign = _TmpSign;
                }
            }

            SerialNumbers = _SerialNumbers.Distinct().ToList();
            SerialNumbers.Sort();
        }

        private void CalcVentQuan(List<int> _ListVentQuan, int _Tmp, string _Sign)
        {
            if (_ListVentQuan.Count == 0 || _Sign == string.Empty || _Sign == ",") { _ListVentQuan.Add(_Tmp); return; }
            var _OldValue = FuncStr.NullToInt(_ListVentQuan.Last());
            if (_OldValue > _Tmp)
            {
                for (int i = _Tmp + 1; i <= _OldValue; i++)
                {
                    _ListVentQuan.Add(i);
                }
            }
            else if (_OldValue < _Tmp)
            {
                for (int i = _OldValue + 1; i <= _Tmp; i++)
                {
                    _ListVentQuan.Add(i);
                }
            }

        }
    }
    public class ThStoreys : ThIfcSpatialStructureElement
    {
        public ObjectId ObjectId { get; }

        public ThBlockReferenceData Data { get; }
        public ThStoreys(ObjectId id)
        {
            ObjectId = id;
            Data = new ThBlockReferenceData(id);
        }
        public string StoreyNumber
        {
            get
            {
                return Data.Attributes[ThPipeCommon.STOREY_ATTRIBUTE_VALUE_NUMBER];
            }
        }
        public string StoreyTypeString => (string)Data.CustomProperties.GetValue(ThPipeCommon.STOREY_DYNAMIC_PROPERTY_TYPE);
        public StoreyType StoreyType
        {
            get
            {
                switch (StoreyTypeString)
                {
                    case ThPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_TOP_ROOF_FLOOR: return StoreyType.SmallRoof;
                    case ThPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_ROOF_FLOOR: return StoreyType.LargeRoof;
                    case ThPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_STANDARD_FLOOR: return StoreyType.StandardStorey;
                    case ThPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_NON_STANDARD_FLOOR: return StoreyType.NonStandardStorey;
                    case ThPipeCommon.STOREY_DYNAMIC_PROPERTY_VALUE_NOT_STANDARD_FLOOR: return StoreyType.NonStandardStorey;
                    default: return StoreyType.Unknown;
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

    public class FloorCalculator
    {
        public List<string> Floors { get; private set; }
        public FloorCalculator(string ventNum)
        {
            Floors = new List<string>();
            ventNum = ventNum.Replace('，', ',').Replace("F", "").Replace(" ", "");
            var FloorExpression= ventNum.Split(',');
            FloorExpression.ForEach(o => Floors.AddRange(Parsestring(o)));
            Floors = Floors.Distinct().ToList();
            //Floors.Sort();
        }

        private List<string> Parsestring(string floorString)
        { 
            try
            {
                List<string> FloorList = new List<string>();
                var floorListString = floorString.Split('-');
                if (floorListString.Length == 1)
                {
                    //判断是否是纯数字
                    if (Regex.IsMatch(floorString, "^[0-9]*$"))
                        floorString += "F";
                    FloorList.Add(floorString);
                }
                else if (floorListString.Length == 2)
                {
                    if (floorString.Contains('M')) return FloorList;
                    int minFloor = int.Parse(floorListString[0].Replace('B', '-').Replace("F", "").Replace("M", ""));
                    int maxFloor = int.Parse(floorListString[1].Replace('B', '-').Replace("F", "").Replace("M", ""));
                    for (int floorNo = minFloor; floorNo <= maxFloor; floorNo++)
                    {
                        if (floorNo != 0)
                            FloorList.Add(floorNo > 0 ? floorNo + "F" : "B" + Math.Abs(floorNo));
                    }
                }
                else
                {
                    //do Not 违反既定规则
                }
                return FloorList;
            }
            catch (Exception)
            {
                //do Not 违反既定规则
                return new List<string>();
            }
            
        }
    }
}
