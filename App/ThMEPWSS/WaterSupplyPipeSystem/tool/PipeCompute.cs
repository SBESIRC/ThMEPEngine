using System;
using System.Collections.Generic;

namespace ThMEPWSS.WaterSupplyPipeSystem.tool
{
    public class PipeCompute  //管径计算
    {
        private double U0 { get; set; }//用水总当量/分区内住户总数
        private double Ng { get; set; }//用水总当量/分区内住户总数

        public PipeCompute(double u0, double ng)
        {
            Ng = ng;//给水当量数
            U0 = u0;//平均出流概率
        }
        public string PipeDiameterCompute()
        {
            Dictionary<double, double> U0ToAlphaC = new Dictionary<double, double>
            {{1.0, 0.00323 }, {1.5, 0.00697 }, {2.0, 0.01097 }, {2.5, 0.01512 }, {3.0, 0.01939 }, {3.5, 0.02374 },
            {4.0, 0.02816 }, {4.5, 0.03263 }, {5.0, 0.03715 }, {6.0, 0.04629 }, {7.0, 0.05555 }, {8.0, 0.06489 }};
            double key1 = 1.0;
            double alphaC = 0;//对应于 U0 的系数，线性插入
            foreach (double key in U0ToAlphaC.Keys)
            {
                if (U0 >= key)
                {
                    key1 = key;
                }
                if (U0 < key)
                {
                    alphaC = (U0ToAlphaC[key] - U0ToAlphaC[key1]) * (U0 - key1) / (key - key1) + U0ToAlphaC[key1];
                    break;
                }
            }
            double U = (1 + alphaC * Math.Pow((Ng - 1), 0.49)) / (Math.Sqrt(Ng));
            double qg = 0.2 * U * Ng;  //管段的设计秒流量
            //管径列表
            Dictionary<string, double> pipeDList = new Dictionary<string, double>
            { {"DN20",0.0213 }, {"DN25",0.0273 },  {"DN32",0.0354 }, {"DN40",0.0413 },
              {"DN50",0.0527 }, {"DN65",0.0681 },  {"DN80",0.0809 }, {"DN100",0.1063 },
              {"DN125",0.131 }, {"DN150",0.1593 }, {"DN200",0.2071 } };

            foreach (string key in pipeDList.Keys)
            {
                double d = pipeDList[key];
                double FlowRate = qg * 4 / (Math.PI * Math.Pow(d, 2) * 1000);  // 不同管径下的流速
                switch (key)
                {
                    case "DN20":
                        if (FlowRate <= 0.8)
                        {
                            return key;
                        }
                        break;
                    case "DN25":
                    case "DN32":
                    case "DN40":
                        if (FlowRate <= 1)
                        {
                            return key;
                        }
                        break;
                    case "DN50":
                    case "DN65":
                        if (FlowRate <= 1.2)
                        {
                            return key;
                        }
                        break;
                    default:
                        if (FlowRate <= 1.5)
                        {
                            return key;
                        }
                        break;
                }
            }
            return "DN15";
        }
        public string PipeDiameterCompute(out double qg)
        {
            Dictionary<double, double> U0ToAlphaC = new Dictionary<double, double>
            {{1.0, 0.00323 }, {1.5, 0.00697 }, {2.0, 0.01097 }, {2.5, 0.01512 }, {3.0, 0.01939 }, {3.5, 0.02374 },
            {4.0, 0.02816 }, {4.5, 0.03263 }, {5.0, 0.03715 }, {6.0, 0.04629 }, {7.0, 0.05555 }, {8.0, 0.06489 }};
            double key1 = 1.0;
            double alphaC = 0;//对应于 U0 的系数，线性插入
            foreach (double key in U0ToAlphaC.Keys)
            {
                if (U0 >= key)
                {
                    key1 = key;
                }
                if (U0 < key)
                {
                    alphaC = (U0ToAlphaC[key] - U0ToAlphaC[key1]) * (U0 - key1) / (key - key1) + U0ToAlphaC[key1];
                    break;
                }
            }
            double U = (1 + alphaC * Math.Pow((Ng - 1), 0.49)) / (Math.Sqrt(Ng));
            qg = 0.2 * U * Ng;  //管段的设计秒流量
            //管径列表
            Dictionary<string, double> pipeDList = new Dictionary<string, double>
            { {"DN20",0.0213 }, {"DN25",0.0273 },  {"DN32",0.0354 }, {"DN40",0.0413 },
              {"DN50",0.0527 }, {"DN65",0.0681 },  {"DN80",0.0809 }, {"DN100",0.1063 },
              {"DN125",0.131 }, {"DN150",0.1593 }, {"DN200",0.2071 } };

            foreach (string key in pipeDList.Keys)
            {
                double d = pipeDList[key];
                double FlowRate = qg * 4 / (Math.PI * Math.Pow(d, 2) * 1000);  // 不同管径下的流速
                switch (key)
                {
                    case "DN20":
                        if (FlowRate <= 0.8)
                        {
                            return key;
                        }
                        break;
                    case "DN25":
                    case "DN32":
                    case "DN40":
                        if (FlowRate <= 1)
                        {
                            return key;
                        }
                        break;
                    case "DN50":
                    case "DN65":
                        if (FlowRate <= 1.2)
                        {
                            return key;
                        }
                        break;
                    default:
                        if (FlowRate <= 1.5)
                        {
                            return key;
                        }
                        break;
                }
            }
            return "DN15";
        }
    }
}
