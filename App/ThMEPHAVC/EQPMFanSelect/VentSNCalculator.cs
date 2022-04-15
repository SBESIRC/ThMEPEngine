using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ThMEPHVAC.EQPMFanSelect
{
    public class VentSNCalculator
    {
        public VentSNCalculator() 
        { 
        }
        public List<int> SerialNumbers(string ventNum)
        {
            List<int> serialNumbers = new List<int>();
            string sign = string.Empty;
            MatchCollection matche = Regex.Matches(ventNum, @"\d+\,*\-*");
            if (matche.Count > 0)
            {
                for (int i = 0; i < matche.Count; i++)
                {
                    string strRep = string.Empty;
                    string tmpSign = string.Empty;
                    var str = matche[i].ToString();
                    if (str.Contains("-"))
                    {
                        tmpSign = "-";
                    }
                    if (str.Contains(","))
                    {

                        tmpSign = ",";
                    }
                    strRep = str.Replace(",", "").Replace("-", "");
                    if (strRep == string.Empty) 
                        continue;

                    var tep = Convert.ToInt32(strRep);

                    CalcVentQuan(serialNumbers, tep, sign);
                    sign = tmpSign;
                }
            }

            serialNumbers = serialNumbers.Distinct().OrderBy(c=>c).ToList();
            return serialNumbers;
        }

        private void CalcVentQuan(List<int> ventQuans, int tmp, string sign)
        {
            if (ventQuans.Count == 0 || sign == string.Empty || sign == ",") 
            { 
                ventQuans.Add(tmp);
                return; 
            }
            var _OldValue = ventQuans.Last();
            if (_OldValue > tmp)
            {
                for (int i = tmp + 1; i <= _OldValue; i++)
                {
                    ventQuans.Add(i);
                }
            }
            else if (_OldValue < tmp)
            {
                for (int i = _OldValue + 1; i <= tmp; i++)
                {
                    ventQuans.Add(i);
                }
            }
        }
        public string VentQuanToString(List<int> ventQuans) 
        {
            string str = "";
            if (ventQuans == null || ventQuans.Count < 1)
                return "";
            ventQuans = ventQuans.OrderBy(c => c).ToList();
            for (int i = 0; i < ventQuans.Count;) 
            {
                var start = ventQuans[i];
                var end = start;
                for (int j = i + 1; j < ventQuans.Count; j++) 
                {
                    var current = ventQuans[j];
                    if (current - end != 1) 
                    {
                        break;
                    }
                    end += 1;
                }
                if (start < end)
                {
                    str += string.Format("{0}-{1},", start,end);
                    i = i + (end - start);
                }
                else 
                {
                    str += string.Format("{0},", start);
                }
                i += 1;
            }
            str = str.Substring(0, str.LastIndexOf(","));
            return str;
        }
    }
}
