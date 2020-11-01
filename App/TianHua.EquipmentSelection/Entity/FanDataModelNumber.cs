using System;
using TianHua.Publics.BaseCode;

namespace TianHua.FanSelection
{
    public partial class FanDataModel
    {
        /// <summary>
        /// 风机编号
        /// </summary>
        public string FanNum
        {
            get
            {
                string _FanNum = string.Empty;
                if (PID != "0")
                { return _FanNum; }
                var _PrefixDict = PubVar.g_ListFanPrefixDict.Find(p => p.FanUse == Scenario);
                if (_PrefixDict != null)
                    _FanNum = _PrefixDict.Prefix;
                else
                    _FanNum = " ";

                if (FuncStr.NullToStr(InstallSpace) != string.Empty && FuncStr.NullToStr(InstallSpace) != "未指定子项")
                    _FanNum += "-" + InstallSpace;
                else
                    _FanNum += "-无";

                if (FuncStr.NullToStr(InstallFloor) != string.Empty && FuncStr.NullToStr(InstallFloor) != "未指定楼层")
                    _FanNum += "-" + InstallFloor;
                else
                    _FanNum += "- ";

                if (ListVentQuan != null && ListVentQuan.Count > 0)
                {
                    _FanNum += "-";
                    ListVentQuan.ForEach(p =>
                    {
                        _FanNum += p + ",";
                    });
                    _FanNum = _FanNum.TrimEnd(',');
                }
                else
                    _FanNum += "- ";

                return _FanNum;
            }
        }

        public string OverViewFanNum
        {
            get
            {
                string _FanNum = string.Empty;
                if (PID == "0") { return _FanNum; }
                var _PrefixDict = PubVar.g_ListFanPrefixDict.Find(p => p.FanUse == Scenario);
                if (_PrefixDict != null)
                    _FanNum = _PrefixDict.Prefix;
                else
                    _FanNum = " ";

                if (FuncStr.NullToStr(InstallSpace) != string.Empty && FuncStr.NullToStr(InstallSpace) != "未指定子项")
                    _FanNum += "-" + InstallSpace;
                else
                    _FanNum += "-无";

                if (FuncStr.NullToStr(InstallFloor) != string.Empty && FuncStr.NullToStr(InstallFloor) != "未指定楼层")
                    _FanNum += "-" + InstallFloor;
                else
                    _FanNum += "- ";

                if (ListVentQuan != null && ListVentQuan.Count > 0)
                {
                    _FanNum += "-";
                    ListVentQuan.ForEach(p =>
                    {
                        _FanNum += p + ",";
                    });
                    _FanNum = _FanNum.TrimEnd(',');
                }
                else
                    _FanNum += "- ";

                return _FanNum;
            }
        }


        public int SplitAirVolume
        {
            get
            {
                if (Scenario != "消防加压送风") { return AirVolume; }

                var _Value = this.AirCalcValue * this.AirCalcFactor / this.VentQuan;

                var _Rem = FuncStr.NullToInt(_Value) % 50;

                if (_Rem != 0)
                {
                    var _UnitsDigit = FindNum(FuncStr.NullToInt(_Value), 1);

                    var _TensDigit = FindNum(FuncStr.NullToInt(_Value), 2);

                    var _Tmp = FuncStr.NullToInt(_TensDigit.ToString() + _UnitsDigit.ToString());

                    if (_Tmp < 50)
                    {
                        var _DifferenceValue = 50 - _Tmp;
                        return FuncStr.NullToInt(_Value) + _DifferenceValue;
                    }
                    else
                    {
                        var _DifferenceValue = 100 - _Tmp;
                        return FuncStr.NullToInt(_Value) + _DifferenceValue;
                    }
                }
                else
                {
                    return FuncStr.NullToInt(_Value);
                }
            }
        }



        public int FindNum(int _Num, int _N)
        {
            int _Power = (int)Math.Pow(10, _N);
            return (_Num - _Num / _Power * _Power) * 10 / _Power;
        }

        public string FanPrefix
        {
            get
            {
                string _FanPrefix = string.Empty;
                var _PrefixDict = PubVar.g_ListFanPrefixDict.Find(p => p.FanUse == Scenario);
                if (_PrefixDict != null)
                    _FanPrefix = _PrefixDict.Prefix;
                else
                    _FanPrefix = " ";

                return _FanPrefix;
            }
        }

    }
}
