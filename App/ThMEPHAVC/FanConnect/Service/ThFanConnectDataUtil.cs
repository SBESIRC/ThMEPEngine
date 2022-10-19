using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;

using ThMEPEngineCore.IO.ExcelService;

namespace ThMEPHVAC.FanConnect.Service
{
    public class ThFanConnectDataUtil
    {
        public static bool ReadCoolACPipeDNConfig(string filePath, ref List<Tuple<double, double>> gasDnList, ref List<Tuple<double, double>> liquidDnList)
        {
            var bReturn = false;
       
            if (File.Exists(filePath))
            {
                try
                {
                    var readServer = new ReadExcelService();
                    var fileDS = readServer.ReadExcelToDataSet(filePath, true);
                    var dt = fileDS.Tables["标准"];

                    var flowIdx = 1;
                    var gasDNIdx = 2;
                    var liquidDNIdx = 3;
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        if (dt.Columns[i].ColumnName.Contains("额定制冷容量"))
                        {
                            flowIdx = i;
                        }
                        if (dt.Columns[i].ColumnName.Contains("气管管径"))
                        {
                            gasDNIdx = i;
                        }
                        if (dt.Columns[i].ColumnName.Contains("液管管径"))
                        {
                            liquidDNIdx = i;
                        }
                    }

                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        var row = dt.Rows[i];
                        var flow = Convert.ToDouble(row[flowIdx]);
                        var gas = Convert.ToDouble(row[gasDNIdx]);
                        var liquid = Convert.ToDouble(row[liquidDNIdx]);
                        gasDnList.Add(new Tuple<double, double>(flow, gas));
                        liquidDnList.Add(new Tuple<double, double>(flow, liquid));
                    }

                    bReturn = true;
                }
                catch (Exception ex)
                {

                }
            }

            return bReturn;
        }
    }
}
