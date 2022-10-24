using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPHVAC.FanConnect.Service
{
    public class ThQueryDNService
    {
        public string QuerySupplyPipeDN(string coeff, double flow)
        {
            string strDN = "DN100";
            if (coeff.Equals("150"))
            {
                strDN = QuerySupplyPipeDN150(flow);
            }
            else if (coeff.Equals("200"))
            {
                strDN = QuerySupplyPipeDN200(flow);
            }

            return strDN;
        }
        public string QueryCondPipeDN(double flow)//查询冷凝水管 管径
        {
            string strDN = "DN25";
            if (flow < 25)
            {
                strDN = "DN25";
            }
            else if (flow >= 25 && flow < 100)
            {
                strDN = "DN32";
            }
            else if (flow >= 100 && flow < 300)
            {
                strDN = "DN40";
            }
            else if (flow >= 300 && flow < 800)
            {
                strDN = "DN50";
            }
            else if (flow >= 800 && flow < 1600)
            {
                strDN = "DN80";
            }
            else if (flow >= 1600 && flow < 3000)
            {
                strDN = "DN100";
            }
            else if (flow >= 3000 && flow < 12000)
            {
                strDN = "DN125";
            }
            else if (flow >= 12000)
            {
                strDN = "DN150";
            }
            return strDN;
        }
        private string QuerySupplyPipeDN150(double flow)
        {
            string strDN = "DN100";
            if (flow <= 0.2)
            {
                strDN = "DN15";
            }
            else if (flow > 0.2 && flow <= 0.44)
            {
                strDN = "DN20";
            }
            else if (flow > 0.44 && flow <= 0.85)
            {
                strDN = "DN25";
            }
            else if (flow > 0.85 && flow <= 1.8)
            {
                strDN = "DN32";
            }
            else if (flow > 1.8 && flow <= 2.5)
            {
                strDN = "DN40";
            }
            else if (flow > 2.5 && flow <= 5.1)
            {
                strDN = "DN50";
            }
            else if (flow > 5.1 && flow <= 9.8)
            {
                strDN = "DN70";
            }
            else if (flow > 9.8 && flow <= 15.5)
            {
                strDN = "DN80";
            }
            else if (flow > 15.5 && flow <= 33)
            {
                strDN = "DN100";
            }
            else if (flow > 33.0 && flow <= 57)
            {
                strDN = "DN125";
            }
            else if (flow > 57 && flow <= 90)
            {
                strDN = "DN150";
            }
            else if (flow > 90 && flow <= 215)
            {
                strDN = "DN200";
            }
            else if (flow > 215 && flow <= 340)
            {
                strDN = "DN250";
            }
            else if (flow > 340 && flow <= 550)
            {
                strDN = "DN300";
            }
            else if (flow > 550 && flow <= 880)
            {
                strDN = "DN350";
            }
            else if (flow > 880 && flow <= 1200)
            {
                strDN = "DN400";
            }
            else if (flow > 1200 && flow <= 1600)
            {
                strDN = "DN450";
            }
            else if (flow > 1600 && flow <= 2100)
            {
                strDN = "DN500";
            }
            else if (flow > 2100 && flow <= 3250)
            {
                strDN = "DN600";
            }
            else if (flow > 3250 && flow <= 4250)
            {
                strDN = "DN700";
            }
            else if (flow > 4250 && flow <= 5600)
            {
                strDN = "DN800";
            }
            else if (flow > 5600)
            {
                strDN = "DN900";
            }
            return strDN;
        }
        private string QuerySupplyPipeDN200(double flow)
        {
            string strDN = "DN100";
            if (flow <= 0.22)
            {
                strDN = "DN15";
            }
            else if (flow > 0.22 && flow <= 0.5)
            {
                strDN = "DN20";
            }
            else if (flow > 0.5 && flow <= 1.1)
            {
                strDN = "DN25";
            }
            else if (flow > 1.1 && flow <= 2.1)
            {
                strDN = "DN32";
            }
            else if (flow > 2.1 && flow <= 3.0)
            {
                strDN = "DN40";
            }
            else if (flow > 3.0 && flow <= 6.0)
            {
                strDN = "DN50";
            }
            else if (flow > 6.0 && flow <= 11.5)
            {
                strDN = "DN70";
            }
            else if (flow > 11.5 && flow <= 18.0)
            {
                strDN = "DN80";
            }
            else if (flow > 18 && flow <= 38)
            {
                strDN = "DN100";
            }
            else if (flow > 38 && flow <= 66)
            {
                strDN = "DN125";
            }
            else if (flow > 66 && flow <= 105)
            {
                strDN = "DN150";
            }
            else if (flow > 105 && flow <= 250)
            {
                strDN = "DN200";
            }
            else if (flow > 250 && flow <= 400)
            {
                strDN = "DN250";
            }
            else if (flow > 400 && flow <= 650)
            {
                strDN = "DN300";
            }
            else if (flow > 650 && flow <= 1000)
            {
                strDN = "DN350";
            }
            else if (flow > 1000 && flow <= 1400)
            {
                strDN = "DN400";
            }
            else if (flow > 1400 && flow <= 1800)
            {
                strDN = "DN450";
            }
            else if (flow > 1800 && flow <= 2250)
            {
                strDN = "DN500";
            }
            else if (flow > 2250 && flow <= 3250)
            {
                strDN = "DN600";
            }
            else if (flow > 3250 && flow <= 4250)
            {
                strDN = "DN700";
            }
            else if (flow > 4250 && flow <= 5600)
            {
                strDN = "DN800";
            }
            else if (flow > 5600 && flow <= 7000)
            {
                strDN = "DN900";
            }
            else if (flow > 7000)
            {
                strDN = "DN1000";
            }
            return strDN;
        }
        public static int QuerySupplyPipeDNInt(string coeff, double flow)
        {
            int dn = 100;
            if (coeff.Equals("150"))
            {
                dn = QuerySupplyPipeDN150Int(flow);
            }
            else if (coeff.Equals("200"))
            {
                dn = QuerySupplyPipeDN200Int(flow);
            }

            return dn;
        }

        public static int QueryCondPipeDNInt(double flow)//查询冷凝水管 管径
        {
            int dn = 25;
            if (flow < 25)
            {
                dn = 25;
            }
            else if (flow >= 25 && flow < 100)
            {
                dn = 32;
            }
            else if (flow >= 100 && flow < 300)
            {
                dn = 40;
            }
            else if (flow >= 300 && flow < 800)
            {
                dn = 50;
            }
            else if (flow >= 800 && flow < 1600)
            {
                dn = 80;
            }
            else if (flow >= 1600 && flow < 3000)
            {
                dn = 100;
            }
            else if (flow >= 3000 && flow < 12000)
            {
                dn = 125;
            }
            else if (flow >= 12000)
            {
                dn = 150;
            }
            return dn;
        }
        private static int QuerySupplyPipeDN150Int(double flow)
        {
            int dn = 100;
            if (flow <= 0.2)
            {
                dn = 15;
            }
            else if (flow > 0.2 && flow <= 0.44)
            {
                dn = 20;
            }
            else if (flow > 0.44 && flow <= 0.85)
            {
                dn = 25;
            }
            else if (flow > 0.85 && flow <= 1.8)
            {
                dn = 32;
            }
            else if (flow > 1.8 && flow <= 2.5)
            {
                dn = 40;
            }
            else if (flow > 2.5 && flow <= 5.1)
            {
                dn = 50;
            }
            else if (flow > 5.1 && flow <= 9.8)
            {
                dn = 70;
            }
            else if (flow > 9.8 && flow <= 15.5)
            {
                dn = 80;
            }
            else if (flow > 15.5 && flow <= 33)
            {
                dn = 100;
            }
            else if (flow > 33.0 && flow <= 57)
            {
                dn = 125;
            }
            else if (flow > 57 && flow <= 90)
            {
                dn = 150;
            }
            else if (flow > 90 && flow <= 215)
            {
                dn = 200;
            }
            else if (flow > 215 && flow <= 340)
            {
                dn = 250;
            }
            else if (flow > 340 && flow <= 550)
            {
                dn = 300;
            }
            else if (flow > 550 && flow <= 880)
            {
                dn = 350;
            }
            else if (flow > 880 && flow <= 1200)
            {
                dn = 400;
            }
            else if (flow > 1200 && flow <= 1600)
            {
                dn = 450;
            }
            else if (flow > 1600 && flow <= 2100)
            {
                dn = 500;
            }
            else if (flow > 2100 && flow <= 3250)
            {
                dn = 600;
            }
            else if (flow > 3250 && flow <= 4250)
            {
                dn = 700;
            }
            else if (flow > 4250 && flow <= 5600)
            {
                dn = 800;
            }
            else if (flow > 5600)
            {
                dn = 900;
            }
            return dn;
        }
        private static int QuerySupplyPipeDN200Int(double flow)
        {
            int dn = 100;
            if (flow <= 0.22)
            {
                dn = 15;
            }
            else if (flow > 0.22 && flow <= 0.5)
            {
                dn = 20;
            }
            else if (flow > 0.5 && flow <= 1.1)
            {
                dn = 25;
            }
            else if (flow > 1.1 && flow <= 2.1)
            {
                dn = 32;
            }
            else if (flow > 2.1 && flow <= 3.0)
            {
                dn = 40;
            }
            else if (flow > 3.0 && flow <= 6.0)
            {
                dn = 50;
            }
            else if (flow > 6.0 && flow <= 11.5)
            {
                dn = 70;
            }
            else if (flow > 11.5 && flow <= 18.0)
            {
                dn = 80;
            }
            else if (flow > 18 && flow <= 38)
            {
                dn = 100;
            }
            else if (flow > 38 && flow <= 66)
            {
                dn = 125;
            }
            else if (flow > 66 && flow <= 105)
            {
                dn = 150;
            }
            else if (flow > 105 && flow <= 250)
            {
                dn = 200;
            }
            else if (flow > 250 && flow <= 400)
            {
                dn = 250;
            }
            else if (flow > 400 && flow <= 650)
            {
                dn = 300;
            }
            else if (flow > 650 && flow <= 1000)
            {
                dn = 350;
            }
            else if (flow > 1000 && flow <= 1400)
            {
                dn = 400;
            }
            else if (flow > 1400 && flow <= 1800)
            {
                dn = 450;
            }
            else if (flow > 1800 && flow <= 2250)
            {
                dn = 500;
            }
            else if (flow > 2250 && flow <= 3250)
            {
                dn = 600;
            }
            else if (flow > 3250 && flow <= 4250)
            {
                dn = 700;
            }
            else if (flow > 4250 && flow <= 5600)
            {
                dn = 800;
            }
            else if (flow > 5600 && flow <= 7000)
            {
                dn = 900;
            }
            else if (flow > 7000)
            {
                dn = 1000;
            }
            return dn;
        }
        public static Tuple<double, double> QueryACPipeDNInt(double flow, List<Tuple<double, double>> gasDNList, List<Tuple<double, double>> liquidDNList)
        {
            //气
            var dnGas = GetDN(flow, gasDNList);

            //液
            var dnLiquid = GetDN(flow, liquidDNList);

            var dn = new Tuple<double, double>(dnGas, dnLiquid);
            return dn;

        }

        private static double GetDN(double flow, List<Tuple<double, double>> dnList)
        {
            double dn = 25;
            if (dnList.Count > 0)
            {
                dn = dnList.First().Item2;

                for (int i = 0; i < dnList.Count; i++)
                {
                    var rangeDown = 0.0;
                    if (i != 0)
                    {
                        rangeDown = dnList[i - 1].Item1;
                    }
                    var rangeUp = dnList[i].Item1;
                    if (rangeDown <= flow && flow < rangeUp)
                    {
                        dn = dnList[i].Item2;
                        break;
                    }
                }

                if (dnList.Last().Item1 <= flow)
                {
                    dn = dnList.Last().Item2;
                }
            }

            return dn;

        }

    }
}
