using System.Linq;
using TianHua.Publics.BaseCode;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TianHua.FanSelection.Function
{
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
}
