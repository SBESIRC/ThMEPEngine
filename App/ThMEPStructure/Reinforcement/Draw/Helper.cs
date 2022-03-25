﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
namespace ThMEPStructure.Reinforcement.Draw
{
    public class StrToReinforce
    {
        //例子：6C16+10C14
        public int num;//纵筋之和
        public List<ReinforceDetail> Rein_Detail_list;
    }
    public class ReinforceDetail
    {
        //例子：6C16
        public int TypeNum;//当前类型纵筋数量
        public string Type;//纵筋类型
        public int TypeDist;//当前类型的纵筋直径
    }
    public class LinkDetail
    {
        //例子：1C8@200
        public int num;//Link的数量
        public string Type;//当前Link使用的钢筋符号
        public int numNo2;//第二个数字
        public int numNo3;//第三个数字
    }

    class Helper
    {
        public static StrToReinforce StrToRein(string Reinforce)
        {
            //解析表格中的Reinforce
            StrToReinforce Res = new StrToReinforce();

            string tmp = "";
            while (Reinforce.Contains("+"))
            {
                tmp = Reinforce.Substring(0, Reinforce.IndexOf("+"));
                Reinforce = Reinforce.Substring(Reinforce.IndexOf("+") + 1);
                Res.Rein_Detail_list = new List<ReinforceDetail>();
                ReinforceDetail tmpDetail1 = new ReinforceDetail();
                if (tmp.Contains("C"))
                {
                    string num1 = "";
                    string num2 = "";
                    num1 = tmp.Substring(0, tmp.IndexOf("C"));
                    num2 = tmp.Substring(tmp.IndexOf("C") + 1);
                    if (!num1.Contains("(") && !num2.Contains("("))
                    {
                        tmpDetail1.Type = "C";
                        tmpDetail1.TypeNum = int.Parse(num1);
                        tmpDetail1.TypeDist = int.Parse(num2);
                    }
                    Res.Rein_Detail_list.Add(tmpDetail1);
                }
            }
            if (Res.Rein_Detail_list == null)
            {
                Res.Rein_Detail_list = new List<ReinforceDetail>();
            }
            ReinforceDetail tmpDetail = new ReinforceDetail();
            if (Reinforce.Contains("C"))
            {
                string num1 = "";
                string num2 = "";
                num1 = Reinforce.Substring(0, Reinforce.IndexOf("C"));
                num2 = Reinforce.Substring(Reinforce.IndexOf("C") + 1);
                if (!num1.Contains("(") && !num2.Contains("("))
                {
                    tmpDetail.Type = "C";
                    tmpDetail.TypeNum = int.Parse(num1);
                    tmpDetail.TypeDist = int.Parse(num2);
                }
                Res.Rein_Detail_list.Add(tmpDetail);
            }
            if (Res.Rein_Detail_list != null)
            {
                for (int i = 0; i < Res.Rein_Detail_list.Count; i++)
                {
                    Res.num += Res.Rein_Detail_list[i].TypeNum;
                }
            }
            return Res;

        }

        public static LinkDetail StrToLinkDetail(string link)
        {
            
            //解析表格当中的link字符串,并将得到的信息返回到LinkDetail类里
            LinkDetail res = new LinkDetail();
            if (link.IsNullOrEmpty())
            {
                res.num = 0;
            }
            string num1 = "", num2 = "", num3 = "";
            if (link.Contains("C"))
            {
                num1 = link.Substring(0, link.IndexOf("C"));
                num2 = link.Substring(link.IndexOf("C") + 1, link.IndexOf("@")-link.IndexOf("C"));
                num3 = link.Substring(link.IndexOf("@") + 1);
                if (!num1.Contains("(") && !num2.Contains("(") && num3.Contains("("))
                {
                    res.num = int.Parse(num1);
                    res.numNo2 = int.Parse(num2);
                    res.numNo3 = int.Parse(num3);
                    res.Type = "C";
                }
            }
            return res;
        }


        public static double CalScale(string str)
        {
            double result = 100.0 / (double)int.Parse(str.Substring(2));
            return result;
        }

        /// <summary>
        /// 计算方差，平方的均值减去均值的平方
        /// </summary>
        /// <param name="numbers"></param>
        public static double CalVariance(List<double> numbers)
        {
            double squareSum = 0;
            double sum = 0;
            foreach(var num in numbers)
            {
                squareSum += num * num;
                sum += num;
            }

            return (squareSum / numbers.Count) - (sum / numbers.Count) * (sum / numbers.Count);
        }

        /// <summary>
        /// 计算文字居中放置位置
        /// </summary>
        /// <param name="startX"></param>
        /// <param name="startY"></param>
        /// <param name="finalX"></param>
        /// <param name="finalY"></param>
        /// <param name="strHeight"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Point3d CalCenterPosition(double startX,double startY,double finalX,double finalY,double strHeight,string str)
        {
            double x, y;
            //纵向
            y = (startY - finalY)/2.0 + finalY - strHeight/2.0;
            //横向
            double strWidth = str.Length * strHeight / 2.0;
            x = (startX - finalX) / 2.0 + finalX - strWidth / 2.0;
            Point3d point = new Point3d(x, y, 0);
            return point;
        }
        /// <summary>
        /// 多段线缩小二分之一
        /// </summary>
        /// <param name="polyline"></param>
        /// <param name="move">位移矢量</param>
        /// <param name="basicPt">缩放基准点</param>
        /// <returns></returns>
        public static Polyline ShrinkToHalf(Polyline polyline, Vector2d move, Point2d basicPt)
        {
            Polyline res = new Polyline();
            double width = polyline.ConstantWidth;
            width /= 2;
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                Point2d pt = basicPt + (polyline.GetPoint2dAt(i) - basicPt) / 2;
                res.AddVertexAt(i, pt + move, polyline.GetBulgeAt(i), width, width);
            }
            return res;
        }

    }
}
