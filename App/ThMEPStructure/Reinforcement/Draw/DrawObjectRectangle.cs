using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPStructure.Reinforcement.Model;
using ThMEPStructure.Reinforcement.Service;
using ThMEPEngineCore.CAD;
using Dreambuild.AutoCAD;
namespace ThMEPStructure.Reinforcement.Draw
{
    class DrawObjectRectangle : DrawObjectBase
    {
        ThRectangleEdgeComponent thRectangleEdgeComponent;


        /// <summary>
        /// 一型钢筋确定点的位置,先去掉四角的点
        /// </summary>
        /// <param name="pointNum"></param>
        /// <param name="points"></param>
        protected override void CalReinforcePosition(int pointNum, Polyline polyline)
        {
            points = new List<Point3d>();
            pointsFlag = new List<int>();
            //纵筋相对轮廓的偏移值
            double offset = scale * (thRectangleEdgeComponent.C ) + thRectangleEdgeComponent.PointReinforceLineWeight + thRectangleEdgeComponent.StirrupLineWeight;
            //根据点的对数所有点的位置确定位置
            //polyline从左上角逆时针旋转，四个点先获取
            points = new List<Point3d>();
            for (int i = 0; i < polyline.NumberOfVertices; i++)
            {
                Point3d point = polyline.GetPoint3dAt(i);
                Point3d tmpPoint;
                //左上角，向右下偏移
                if (i == 0)
                {
                    tmpPoint = new Point3d(point.X + offset, point.Y - offset, 0);
                }
                //左下角，向右上偏移
                else if (i == 1)
                {
                    tmpPoint = new Point3d(point.X + offset, point.Y + offset, 0);
                }
                //右下角，左上偏移
                else if (i == 2)
                {
                    tmpPoint = new Point3d(point.X - offset, point.Y + offset, 0);
                }
                //右上角，左下偏移
                else if (i == 3)
                {
                    tmpPoint = new Point3d(point.X - offset, point.Y - offset, 0);
                }
                else
                {
                    break;
                }
                points.Add(tmpPoint);
                pointsFlag.Add(1);
            }
            //计算竖直方向最远两个纵筋的距离
            double disY = points[0].Y - points[1].Y;
            //水平方向最远纵筋距离
            double disX = points[3].X - points[0].X;

            int needLayoutPointsNum = pointNum - 4;
            //需要排布的点的对数
            int pointsPair = needLayoutPointsNum / 2;
            //遍历所有排布可能性，选取方差最小的,bw是竖直方向上的，hc是水平方向，先从竖直方向0个布点开始计算
            List<double> numbers = new List<double>();
            //记录在bw方向上有几对点
            int result = 0;
            double minVar = 100000000;
            for (int i = 0; i <= pointsPair; i++)
            {
                numbers.Clear();
                for (int j = 0; j <= i; j++)
                {
                    numbers.Add(disY / (double)(i + 1));
                }
                for (int k = 0; k <= pointsPair - i; k++)
                {
                    numbers.Add(disX / (double)(pointsPair - i + 1));
                }
                double tmp = Helper.CalVariance(numbers);
                if (tmp < minVar)
                {
                    result = i;
                    minVar = tmp;
                }
            }

            //每次新增的间隔距离
            double deltaY = disY / (double)(result + 1);
            double deltaX = disX / (double)(pointsPair - result + 1);
            //把一对对点的位置计算出来，计算竖直方向BW上的位置,0，3号点每次向下偏移deltaY,水平方向0，1号点每次向右偏移deltaX
            for (int i = 0; i < result; i++)
            {
                Point3d tmpPoint1 = new Point3d(points[0].X, points[0].Y - deltaY * (i + 1), 0);
                points.Add(tmpPoint1);
                Point3d tmpPoint2 = new Point3d(points[3].X, points[3].Y - deltaY * (i + 1), 0);
                points.Add(tmpPoint2);
                pointsFlag.Add(3);
                pointsFlag.Add(3);
            }
            for (int i = 0; i < pointsPair - result; i++)
            {
                Point3d tmpPoint1 = new Point3d(points[0].X + deltaX * (i + 1), points[0].Y, 0);
                points.Add(tmpPoint1);
                Point3d tmpPoint2 = new Point3d(points[1].X + deltaX * (i + 1), points[1].Y, 0);
                points.Add(tmpPoint2);
                pointsFlag.Add(2);
                pointsFlag.Add(2);
            }


        }
        void CalReinforceCPosition(int pointNum, int pointCNum)
        {

        }

        protected void R_FindCJin(List<Point3d> points, StrToReinforce strToRein, List<ZongjinPoint> ZongjinPoint_list)
        {
            if (strToRein.Rein_Detail_list.Count >= 1)
            {
                int dim = strToRein.Rein_Detail_list[0].TypeDist;//原始C筋的直径
                int num = strToRein.Rein_Detail_list[0].TypeNum;//原始C筋的数量
                for (int i = 0; i < points.Count; i++)
                {
                    //先将所有点装进ZongjinPoint_list
                    ZongjinPoint tmpZ = new ZongjinPoint();
                    tmpZ.position = points[i];
                    ZongjinPoint_list.Add(tmpZ);
                }
                List<int> CIndexList = new List<int>();
                if (thRectangleEdgeComponent.Bw < 300)
                {
                    if (num == 2)
                    {
                        CIndexList.Add(0);
                        CIndexList.Add(1);

                    }
                    else if (num == 4)
                    {
                        CIndexList.Add(0);
                        CIndexList.Add(1);
                        CIndexList.Add(2);
                        CIndexList.Add(3);
                    }
                    for (int i = 0; i < CIndexList.Count; i++)
                    {
                        ZongjinPoint_list[CIndexList[i]].hasUse = true;
                        ZongjinPoint_list[CIndexList[i]].size = dim;
                    }
                }
                else if (thRectangleEdgeComponent.Bw >= 300)
                {
                    if (num == 2)
                    {
                        CIndexList.Add(0);
                        CIndexList.Add(1);
                    }
                    else if (num == 4)
                    {
                        CIndexList.Add(0);
                        CIndexList.Add(1);
                        CIndexList.Add(2);
                        CIndexList.Add(3);
                    }
                    else if (num == 6)
                    {
                        CIndexList.Add(0);
                        int idx1 = Helper.FindMidPoint(points, 0, 1);
                        if (idx1 != -1)
                        {
                            CIndexList.Add(idx1);
                        }
                        CIndexList.Add(1);
                        CIndexList.Add(2);
                        int idx2 = Helper.FindMidPoint(points, 2, 3);
                        if (idx2 != -1)
                        {
                            CIndexList.Add(idx2);
                        }
                        CIndexList.Add(3);
                    }
                    for (int i = 0; i < CIndexList.Count; i++)
                    {
                        ZongjinPoint_list[CIndexList[i]].hasUse = true;
                        ZongjinPoint_list[CIndexList[i]].size = dim;
                    }
                }
                    if (thRectangleEdgeComponent.IsCalculation == true&&!thRectangleEdgeComponent.EnhancedReinforce.IsNullOrEmpty())
                    {
                        StrToReinforce CalStrToRein = new StrToReinforce();
                        CalStrToRein = Helper.StrToRein(thRectangleEdgeComponent.EnhancedReinforce);
                        int Step = 0;//迭代步数 
                        int FirstNum = CalStrToRein.Rein_Detail_list[0].TypeNum;
                        int FirstDim = CalStrToRein.Rein_Detail_list[0].TypeDist;
                        if (CalStrToRein.num == strToRein.num)
                        {
                            //迭代1-4

                            if (FirstNum == 2 && FirstDim - dim == 2)
                            {
                                Step = 1;
                            }
                            else if (FirstNum == 4 && FirstDim - dim == 2)
                            {
                                Step = 2;
                            }
                            else if (FirstNum == 2 && FirstDim - dim == 4)
                            {
                                Step = 3;
                            }
                            else if (FirstNum == 4 && FirstDim - dim == 4)
                            {
                                Step = 4;
                            }


                        }
                        else
                        {
                            if (CalStrToRein.num - strToRein.num == 2)
                            {
                                Step = 5;
                            }
                            else if (CalStrToRein.num - strToRein.num == 4)
                            {
                                Step = 6;
                            }
                        }

                        if (Step == 1)
                        {
                            //L型：A型迭代1
                            ZongjinPoint_list[0].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[1].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[0].hasUse = true;
                            ZongjinPoint_list[1].hasUse = true;


                        }
                        else if (Step == 2)
                        {
                            //迭代2

                            ZongjinPoint_list[0].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[1].size = CalStrToRein.Rein_Detail_list[0].TypeDist;

                            ZongjinPoint_list[2].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[3].size = CalStrToRein.Rein_Detail_list[0].TypeDist;

                            ZongjinPoint_list[0].hasUse = true;
                            ZongjinPoint_list[1].hasUse = true;

                            ZongjinPoint_list[2].hasUse = true;
                            ZongjinPoint_list[3].hasUse = true;


                        }
                        else if (Step == 3)
                        {
                            //迭代3
                            ZongjinPoint_list[0].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[1].size = CalStrToRein.Rein_Detail_list[0].TypeDist;

                            ZongjinPoint_list[2].size = CalStrToRein.Rein_Detail_list[1].TypeDist;
                            ZongjinPoint_list[3].size = CalStrToRein.Rein_Detail_list[1].TypeDist;

                            ZongjinPoint_list[0].hasUse = true;
                            ZongjinPoint_list[1].hasUse = true;

                            ZongjinPoint_list[2].hasUse = true;
                            ZongjinPoint_list[3].hasUse = true;
                        }
                        else if (Step == 4)
                        {
                            //迭代4

                            ZongjinPoint_list[0].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[1].size = CalStrToRein.Rein_Detail_list[0].TypeDist;

                            ZongjinPoint_list[2].size = CalStrToRein.Rein_Detail_list[0].TypeDist;
                            ZongjinPoint_list[3].size = CalStrToRein.Rein_Detail_list[0].TypeDist;

                            ZongjinPoint_list[0].hasUse = true;
                            ZongjinPoint_list[1].hasUse = true;

                            ZongjinPoint_list[2].hasUse = true;
                            ZongjinPoint_list[3].hasUse = true;
                        }
                        else if (Step == 5)
                        {
                            //迭代5
                            Point3d pt1 = new Point3d(ZongjinPoint_list[0].position.X + 100, ZongjinPoint_list[0].position.Y, 0);
                            Point3d pt2 = new Point3d(ZongjinPoint_list[1].position.X + 100, ZongjinPoint_list[1].position.Y, 0);
                            points.Add(pt1);
                            pointsFlag.Add(1);
                            points.Add(pt2);
                            pointsFlag.Add(1);

                            ZongjinPoint p1 = new ZongjinPoint();
                            p1.position = pt1;
                            p1.hasUse = true;
                            ZongjinPoint p2 = new ZongjinPoint();
                            p2.position = pt2;
                            p2.hasUse = true;
                            ZongjinPoint_list.Add(p1);
                            ZongjinPoint_list.Add(p2);



                    }
                        else if (Step == 6)
                        {
                            //迭代6
                            Point3d pt1 = new Point3d(ZongjinPoint_list[0].position.X + 100, ZongjinPoint_list[0].position.Y, 0);
                            Point3d pt2 = new Point3d(ZongjinPoint_list[1].position.X + 100, ZongjinPoint_list[1].position.Y, 0);

                            Point3d pt3 = new Point3d(ZongjinPoint_list[2].position.X - 100, ZongjinPoint_list[2].position.Y, 0);
                            Point3d pt4 = new Point3d(ZongjinPoint_list[3].position.X - 100, ZongjinPoint_list[3].position.Y, 0);
                            points.Add(pt1);
                            pointsFlag.Add(1);
                            points.Add(pt2);
                            pointsFlag.Add(1);
                            points.Add(pt3);
                            pointsFlag.Add(1);
                            points.Add(pt4);
                            pointsFlag.Add(1);
                            ZongjinPoint p1 = new ZongjinPoint();
                            p1.position = pt1;
                            p1.hasUse = true;
                            ZongjinPoint p2 = new ZongjinPoint();
                            p2.position = pt2;
                            p2.hasUse = true;
                            ZongjinPoint p3 = new ZongjinPoint();
                            p3.position = pt3;
                            p3.hasUse = true;
                            ZongjinPoint p4 = new ZongjinPoint();
                            p4.position = pt4;
                            p4.hasUse = true;
                            ZongjinPoint_list.Add(p1);
                            ZongjinPoint_list.Add(p2);
                            ZongjinPoint_list.Add(p3);
                            ZongjinPoint_list.Add(p4);

                        }
                    }


                //给非C筋赋值
                if (strToRein.Rein_Detail_list.Count == 2)
                {
                    int NoCdim = strToRein.Rein_Detail_list[1].TypeDist;//原始C筋的直径

                    for (int i = 0; i < ZongjinPoint_list.Count; i++)
                    {
                        if (ZongjinPoint_list[i].hasUse == false)
                        {
                            ZongjinPoint_list[i].size = NoCdim;
                        }
                    }

                }
                else if (strToRein.Rein_Detail_list.Count == 1)
                {
                    for (int i = 0; i < ZongjinPoint_list.Count; i++)
                    {
                        if (ZongjinPoint_list[i].hasUse == false)
                        {
                            ZongjinPoint_list[i].size = strToRein.Rein_Detail_list[0].TypeDist;
                        }
                    }
                }

            }
        }

        
        public override void DrawCJin()
        {

            StrToReinforce RReinStr = new StrToReinforce();
            RReinStr = Helper.StrToRein(thRectangleEdgeComponent.Reinforce);
            List<ZongjinPoint> ZongjinPoints = new List<ZongjinPoint>();
            R_FindCJin(points, RReinStr, ZongjinPoints);
            if (thRectangleEdgeComponent.Bw < 300)
            {
                int Cnum = RReinStr.Rein_Detail_list[0].TypeNum;
                int Csize = RReinStr.Rein_Detail_list[0].TypeDist;
                bool isCal = false;
                int Step = 0;
                if (thRectangleEdgeComponent.IsCalculation == true && !thRectangleEdgeComponent.EnhancedReinforce.IsNullOrEmpty())
                {
                    isCal = true;
                    StrToReinforce enhanceRein = new StrToReinforce();
                    enhanceRein = Helper.StrToRein(thRectangleEdgeComponent.EnhancedReinforce);
                    int FirstNum = enhanceRein.Rein_Detail_list[0].TypeNum;
                    int FirstDim = enhanceRein.Rein_Detail_list[0].TypeDist;
                    int dim = RReinStr.Rein_Detail_list[0].TypeDist;
                    if (enhanceRein.num == RReinStr.num)
                    {
                        //迭代1-4

                        if (FirstNum == 2 && FirstDim - dim == 2)
                        {
                            Step = 1;
                        }
                        else if (FirstNum == 4 && FirstDim - dim == 2)
                        {
                            Step = 2;
                        }
                        else if (FirstNum == 2 && FirstDim - dim == 4)
                        {
                            Step = 3;
                        }
                        else if (FirstNum == 4 && FirstDim - dim == 4)
                        {
                            Step = 4;
                        }


                    }
                    else
                    {
                        if (enhanceRein.num - RReinStr.num == 2)
                        {
                            Step = 5;
                        }
                        else if (enhanceRein.num - RReinStr.num == 4)
                        {
                            Step = 6;
                        }
                    }
                }//修改

                //Rect型不分A B型
                if (Cnum == 2)
                    {
                       
                       Helper.CreateRectAndLabel(points[0], points[1], 2, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 300);

                    }
                    else if (Cnum == 4)
                    {
                        if (Step == 5)
                        {
                            Helper.CreateRectAndLabel(points[0], points[1], 4, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                        }
                        else if (Step == 6)
                        {
                            Helper.CreateRectAndLabel(points[0], points[1], 4, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                            Helper.CreateRectAndLabel(points[2], points[3], 4, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                        }
                        else
                        {
                            Helper.CreateRectAndLabel(points[0], points[1], 2, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                        }
                    }
                    else
                    {
                        if (Step == 1)
                        {
                             Helper.CreateRectAndLabel(points[0], points[1], 2, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                            
                        }
                        else if (isCal == true && Step != 5 && Step != 6)
                        {
                            Helper.CreateRectAndLabel(points[0], points[1], 2, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                            Helper.CreateRectAndLabel(points[3], points[2], 2, ZongjinPoints[3].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                        }

                    }




            }
            else
            {
                int Cnum = RReinStr.Rein_Detail_list[0].TypeNum;
                int Csize = RReinStr.Rein_Detail_list[0].TypeDist;
                bool isCal = false;
                int Step = 0;
                if (thRectangleEdgeComponent.IsCalculation == true && !thRectangleEdgeComponent.EnhancedReinforce.IsNullOrEmpty())
                {
                    isCal = true;
                    StrToReinforce enhanceRein = new StrToReinforce();
                    enhanceRein = Helper.StrToRein(thRectangleEdgeComponent.EnhancedReinforce);
                    int FirstNum = enhanceRein.Rein_Detail_list[0].TypeNum;
                    int FirstDim = enhanceRein.Rein_Detail_list[0].TypeDist;
                    int dim = RReinStr.Rein_Detail_list[0].TypeDist;
                    if (enhanceRein.num == RReinStr.num)
                    {
                        //迭代1-4

                        if (FirstNum == 2 && FirstDim - dim == 2)
                        {
                            Step = 1;
                        }
                        else if (FirstNum == 4 && FirstDim - dim == 2)
                        {
                            Step = 2;
                        }
                        else if (FirstNum == 2 && FirstDim - dim == 4)
                        {
                            Step = 3;
                        }
                        else if (FirstNum == 4 && FirstDim - dim == 4)
                        {
                            Step = 4;
                        }


                    }
                    else
                    {
                        if (enhanceRein.num - RReinStr.num == 2)
                        {
                            Step = 5;
                        }
                        else if (enhanceRein.num - RReinStr.num == 4)
                        {
                            Step = 6;
                        }
                    }
                }
                if (Cnum == 2)
                {
                    if (Step == 5)
                    {
                        Helper.CreateRectAndLabel(points[0], points[0], 2, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                        Helper.CreateRectAndLabel(points[1], points[1], 2, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1000, 5, 200);
                    }
                    else if (Step == 6)
                    {
                        Helper.CreateRectAndLabel(points[0], points[0], 2, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                        Helper.CreateRectAndLabel(points[1], points[1], 2, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1000, 5, 200);
                        Helper.CreateRectAndLabel(points[2], points[2], 2, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1000, 6, 200);
                        Helper.CreateRectAndLabel(points[3], points[3], 2, ZongjinPoints[3].size, LabelAndRect, CJintText, 400, 1000, 2, 200);
                    }
                    else 
                    {
                        Helper.CreateRectAndLabel(points[0], points[0], 1, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                        Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1000, 5, 200);
                    }
                }
                else if (Cnum == 4)
                {
                    if (Step == 5)
                    {
                        Helper.CreateRectAndLabel(points[0], points[0], 2, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                        Helper.CreateRectAndLabel(points[1], points[1], 2, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1000, 5, 200);
                    }
                    else if (Step == 6)
                    {
                        Helper.CreateRectAndLabel(points[0], points[0], 2, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                        Helper.CreateRectAndLabel(points[1], points[1], 2, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1000, 5, 200);
                        Helper.CreateRectAndLabel(points[2], points[2], 2, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1000, 6, 200);
                        Helper.CreateRectAndLabel(points[3], points[3], 2, ZongjinPoints[3].size, LabelAndRect, CJintText, 400, 1000, 2, 200);
                    }
                    else
                    {
                        Helper.CreateRectAndLabel(points[0], points[0], 1, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                        Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1000, 5, 200);
                        Helper.CreateRectAndLabel(points[2], points[2], 1, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1000, 6, 200);
                        Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 400, 1000, 2, 200);
                    }
                }
                else if (Cnum == 6)
                {
                    if (Step == 5)
                    {
                        Helper.CreateRectAndLabel(points[0], points[1], 5, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                        Helper.CreateRectAndLabel(points[3], points[2], 3, ZongjinPoints[3].size, LabelAndRect, CJintText, 800, 1000, 2, 300);
                    }
                    else if (Step == 6)
                    {
                        Helper.CreateRectAndLabel(points[0], points[1], 5, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                        Helper.CreateRectAndLabel(points[3], points[2], 5, ZongjinPoints[3].size, LabelAndRect, CJintText, 800, 1000, 2, 300);
                    }
                    else if(Step==1)
                    {
                        Helper.CreateRectAndLabel(points[0], points[0], 1, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                        Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1000, 5, 200);
                        int idx1 = Helper.FindMidPoint(points, 0, 1);
                        if (idx1 != -1)
                        {
                            Helper.CreateRectAndLabel(points[idx1], points[idx1], 1, ZongjinPoints[idx1].size, LabelAndRect, CJintText, 400, 1500, 7, 200);
                        }
                        Helper.CreateRectAndLabel(points[3], points[2], 3, ZongjinPoints[3].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                        
                    }
                    else if (isCal == true)
                    {
                        Helper.CreateRectAndLabel(points[0], points[0], 1, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                        Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1000, 5, 200);
                        int idx1 = Helper.FindMidPoint(points, 0, 1);
                        if (idx1 != -1)
                        {
                            Helper.CreateRectAndLabel(points[idx1], points[idx1], 1, ZongjinPoints[idx1].size, LabelAndRect, CJintText, 400, 1500, 7, 200);
                        }
                        Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                        Helper.CreateRectAndLabel(points[2], points[2], 1, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1000, 6, 200);
                        int idx2 = Helper.FindMidPoint(points, 2, 3);
                        if (idx2 != -1)
                        {
                            Helper.CreateRectAndLabel(points[idx2], points[idx2], 1, ZongjinPoints[idx2].size, LabelAndRect, CJintText, 400, 1500, 7,200);
                        }
                    }
                    else
                    {
                        Helper.CreateRectAndLabel(points[0], points[1], 3, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 300);
                        Helper.CreateRectAndLabel(points[3], points[2], 3, ZongjinPoints[3].size, LabelAndRect, CJintText, 800, 1000, 2, 300);
                    }
                }
                else
                {
                    if (Step == 1)
                    {
                        Helper.CreateRectAndLabel(points[0], points[0], 1, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                        Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1000, 5, 200);
                    }
                    else if (isCal == true && Step != 5 && Step != 6)
                    {
                        Helper.CreateRectAndLabel(points[0], points[0], 1, ZongjinPoints[0].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                        Helper.CreateRectAndLabel(points[1], points[1], 1, ZongjinPoints[1].size, LabelAndRect, CJintText, 1000, 1000, 5, 200);
                        Helper.CreateRectAndLabel(points[2], points[2], 1, ZongjinPoints[2].size, LabelAndRect, CJintText, 1000, 1000, 6, 200);
                        Helper.CreateRectAndLabel(points[3], points[3], 1, ZongjinPoints[3].size, LabelAndRect, CJintText, 400, 1000, 1, 200);
                    }
                   
                }



            }

            //StrToReinforce TReinStr = new StrToReinforce();
            //TReinStr = Helper.StrToRein(thRectangleEdgeComponent.Reinforce);
            //List<ZongjinPoint> ZongjinPoints = new List<ZongjinPoint>();
            //R_FindCJin(points, TReinStr, ZongjinPoints);
            //for (int i = 0; i < ZongjinPoints.Count; i++)
            //{
            //    if (ZongjinPoints[i].hasUse == true)
            //    {
            //        Point3d pos = ZongjinPoints[i].position;
            //        var pts = new Point3dCollection {

            //            pos+new Vector3d(-150,150,0),
            //            pos+new Vector3d(150,150,0),
            //            pos+new Vector3d(150,-150,0),
            //            pos+new Vector3d(-150,-150,0)
            //        };
            //        Polyline rect = new Polyline();
            //        rect = pts.CreatePolyline();
            //        LabelAndRect.Add(rect);
            //        Polyline label = new Polyline();
            //        label.AddVertexAt(0, new Point2d((pts[0].X + pts[1].X) / 2, (pts[0].Y + pts[1].Y) / 2), 0, 0, 0);
            //        label.AddVertexAt(1, new Point2d((pts[0].X + pts[1].X) / 2, (pts[0].Y + pts[1].Y) / 2 + 100), 0, 0, 0);
            //        label.AddVertexAt(2, new Point2d((pts[0].X + pts[1].X) / 2 - 500, (pts[0].Y + pts[1].Y) / 2 + 100), 0, 0, 0);
            //        LabelAndRect.Add(label);
            //        DBText txt = new DBText();
            //        txt.TextString = "1C" + ZongjinPoints[i].size;
            //        txt.Height = 150;
            //        txt.Position = label.GetPoint3dAt(2) + new Vector3d(0, 50, 0);
            //        CJintText.Add(txt);
            //    }
            //}
        }
        protected override void CalLinkPosition()
        {
            //遍历所有点，找出2，3类型的钢筋，钢筋,同时查表,因为是一对对的点，所以每次加两个点
            for (int i = 0; i < points.Count; i += 2)
            {
                if (pointsFlag[i] == 2)
                {
                    if (thRectangleEdgeComponent.Link2.IsNullOrEmpty())
                    {
                        continue;
                    }
                }
                else if (pointsFlag[i] == 3)
                {
                    if (thRectangleEdgeComponent.Link3.IsNullOrEmpty())
                    {
                        continue;
                    }
                }
                else continue;
                double r = thRectangleEdgeComponent.PointReinforceLineWeight + thRectangleEdgeComponent.StirrupLineWeight / 2;
                Polyline link = GangJinLink.DrawLink(points[i], points[i + 1], r, thRectangleEdgeComponent.StirrupLineWeight, scale);
                Links.Add(link);
                LinksFlag.Add(pointsFlag[i]);
            }
        }

        protected override void CalStirrupPosition()
        {
            GangJinStirrup stirrup = new GangJinStirrup
            {
                Outline = Outline,
                scale = scale,
                GangjinType = 1
            };
            stirrup.CalPositionR(thRectangleEdgeComponent);
            foreach (var polyline in stirrup.stirrups)
            {
                Links.Add(polyline);
                LinksFlag.Add(1);
            }
        }



        public override void DrawOutline()
        {
            double width = thRectangleEdgeComponent.Hc * scale;
            double height = thRectangleEdgeComponent.Bw * scale;
            Point3d startPt = TableStartPt + new Vector3d((FirstRowWidth - width) / 2, -FirstRowHeight + height + 1500, 0);
            var pts = new Point3dCollection
            {
                startPt + new Vector3d(0, 0, 0) * scale,
                startPt + new Vector3d(0, -height, 0),
                startPt + new Vector3d(width, -height, 0),
                startPt + new Vector3d(width, 0, 0)
            };
            Outline = pts.CreatePolyline();
        }

        public override void DrawWall()
        {
            LinkedWallLines = new List<Curve>();
            double bw = thRectangleEdgeComponent.Bw * scale;
            Point3d pt1 = Outline.GetPoint3dAt(2), pt2 = Outline.GetPoint3dAt(3);
            Polyline polyline = GenPouDuan(pt1, pt2, pt1 + new Vector3d(bw * 5 / 8.0, 0, 0), out Line line1, out Line line2);

            LinkedWallLines.Add(line1);
            LinkedWallLines.Add(line2);
            LinkedWallLines.Add(polyline);

            if (thRectangleEdgeComponent.LinkWallPos == "2")
            {
                pt1 = Outline.GetPoint3dAt(0);
                pt2 = Outline.GetPoint3dAt(1);
                polyline = GenPouDuan(pt1, pt2, pt1 + new Vector3d(-bw * 5 / 8.0, 0, 0), out line1, out line2);

                LinkedWallLines.Add(line1);
                LinkedWallLines.Add(line2);
                LinkedWallLines.Add(polyline);
            }
        }

        public override void DrawDim()
        {
            rotatedDimensions = new List<RotatedDimension>();
            double bw = thRectangleEdgeComponent.Bw * scale;
            RotatedDimension rotatedDimension = new RotatedDimension
            {
                XLine1Point = Outline.GetPoint3dAt(1),
                XLine2Point = Outline.GetPoint3dAt(2),
                DimLinePoint = Outline.GetPoint3dAt(1) + new Vector3d(0, -600, 0),
                DimensionText = thRectangleEdgeComponent.Hc.ToString(),
                Rotation = 0.0
            };
            rotatedDimensions.Add(rotatedDimension);

            Point3d pt1 = Outline.GetPoint3dAt(0), pt2 = Outline.GetPoint3dAt(1);
            if (thRectangleEdgeComponent.LinkWallPos == "2")
            {
                pt1 += new Vector3d(-bw * 5 / 8.0, 0, 0);
                pt2 += new Vector3d(-bw * 5 / 8.0, 0, 0);
            }
            rotatedDimension = new RotatedDimension
            {
                XLine1Point = pt1,
                XLine2Point = pt2,
                DimLinePoint = pt1 + new Vector3d(-600, 0, 0),
                DimensionText = thRectangleEdgeComponent.Bw.ToString(),
                Rotation = Math.PI / 2.0
            };
            rotatedDimensions.Add(rotatedDimension);
        }

        public override void init(ThEdgeComponent component, string elevation, double tblRowHeight, double scale, Point3d position)
        {
            this.thRectangleEdgeComponent = component as ThRectangleEdgeComponent;
            this.elevation = elevation;
            this.tblRowHeight = tblRowHeight;
            this.scale = scale;
            this.number = thRectangleEdgeComponent.Number;
            TableStartPt = position;
            if (thRectangleEdgeComponent.IsCalculation && !thRectangleEdgeComponent.EnhancedReinforce.IsNullOrEmpty())
            {
                this.Reinforce = thRectangleEdgeComponent.EnhancedReinforce;
            }
            else
            {
                this.Reinforce = thRectangleEdgeComponent.Reinforce;
            }
            this.Stirrup = thRectangleEdgeComponent.Stirrup;
        }

        public override void CalExplo()
        {
            Point2d p1 = Outline.GetPoint2dAt(1), p2 = Outline.GetPoint2dAt(3);
            Vector2d vec = new Vector2d(0, p2.Y - p1.Y + 1200);
            Point2d centrePt = p1 + (p2 - p1) / 2;
            //生成的拉筋：link1,link2,link3
            List<List<Polyline>> plinks = new List<List<Polyline>>
            {
                new List<Polyline>(),
                new List<Polyline>(),
                new List<Polyline>(),
            };
            for (int i = 0; i < Links.Count; i++)
            {
                Polyline tmp = new Polyline();
                if (LinksFlag[i] == 1)
                {
                    tmp = Helper.ShrinkToHalf(Links[i], vec, centrePt);
                    plinks[0].Add(tmp);
                }
                else
                {
                    if (LinksFlag[i] == 2)
                    {
                        tmp = Helper.ShrinkToHalf(Links[i], vec + new Vector2d(0, 200), centrePt);
                        plinks[1].Add(tmp);
                    }
                    else if (LinksFlag[i] == 3)
                    {
                        tmp = Helper.ShrinkToHalf(Links[i], vec + new Vector2d(200, 0), centrePt);
                        plinks[2].Add(tmp);
                    }
                }
                tmp.LayerId = DbHelper.GetLayerId("LINK");
                objectCollection.Add(tmp);
            }
            if (!thRectangleEdgeComponent.Link2.IsNullOrEmpty() &&
                thRectangleEdgeComponent.Link2.Substring(1) != thRectangleEdgeComponent.Stirrup)
            {
                List<Point2d> tmpList = new List<Point2d>();
                for(int i = 0; i < plinks[1].Count; i++)
                {
                    tmpList.Add(AdjustPos(new Vector2d(0, 50), plinks, 1, i));
                }
                DrawLinkLabel(tmpList, false, thRectangleEdgeComponent.Link2, 200, 2000);
            }
            if (!thRectangleEdgeComponent.Link3.IsNullOrEmpty() &&
                thRectangleEdgeComponent.Link3.Substring(1) != thRectangleEdgeComponent.Stirrup)
            {
                List<Point2d> tmpList = new List<Point2d>();
                for (int i = 0; i < plinks[2].Count; i++)
                {
                    tmpList.Add(AdjustPos(new Vector2d(50, 0), plinks, 2, i));
                }
                DrawLinkLabel(tmpList, true, thRectangleEdgeComponent.Link3, -200, 2000);
            }
        }
    }
}
