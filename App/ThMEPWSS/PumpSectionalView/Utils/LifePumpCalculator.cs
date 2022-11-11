using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using ThMEPEngineCore.Service.Hvac;
using ThMEPWSS.PumpSectionalView.Service.Impl;

namespace ThMEPWSS.PumpSectionalView.Utils
{
    public class PolyLineLifePump
    {
        List<Polyline> pList;
        //频繁使用 水箱新旧坐标
        private double minX_Old;
        private double maxX_Old;
        private double minY_Old;
        private double maxY_Old;

        private double minX_New;
        private double maxX_New;
        private double minY_New;
        private double maxY_New;

        public PolyLineLifePump(List<Polyline> p)
        {
            //pList = new List<Polyline>(p);
            //p =  getSortedPolylineByLocate(p);//根据minX、minY排序;
            pList = p;
            minX_Old = getPolylineLocate(pList[9], 0);
            maxX_Old = getPolylineLocate(pList[9], 1);
            minY_Old = getPolylineLocate(pList[9], 2);
            maxY_Old = getPolylineLocate(pList[9], 3);
            pList[9] = getWaterTank(9, pList);//先计算水箱
            minX_New = minX_Old;
            maxX_New = pList[9].GetPoint3dAt(2).X;
            minY_New = minY_Old;
            maxY_New = pList[9].GetPoint3dAt(2).Y;
        }
        public PolyLineLifePump()
        {

        }

        /// <summary>
        /// set所有polyline的位置
        /// </summary>
        public void setPolylineList()
        {
            //水箱已计算
            pList[6] = getUnderWaterTank(6, pList);//水箱底部
            pList[3] = getOverflowPipe(3, pList);//溢流管
            pList[5] = getLeftWaterTank(5, pList);//水箱左边
            pList[8] = getLeftAndMiddleWaterTank(8, pList);//左边和中间
            pList[0] = getLeftFrame(0, pList);//左外框
            pList[1] = getBottomFrame(1, pList);//下外框
            pList[2] = getTopFrame(2, pList);//上外框
            pList[13] = getRightFrame(13, pList);//右外框
            //return pList;
        }


        /// <summary>
        /// 0左外框
        /// </summary>
        /// <param name="index"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private Polyline getLeftFrame(int index, List<Polyline> p)
        {
            double minX = getPolylineLocate(p[index], 0);
            double maxX = getPolylineLocate(p[index], 1);
            double minY = getPolylineLocate(p[index], 2);
            double change = maxY_New - maxY_Old;
            double maxY = getPolylineLocate(p[index], 3) + change;

            var pt1 = new Point3d(minX, minY, 0);
            var pt2 = new Point3d(maxX, minY, 0);
            var pt3 = new Point3d(maxX, maxY, 0);
            var pt4 = new Point3d(minX, maxY, 0);
            p[index].SetPointAt(0, pt1.ToPoint2D());
            p[index].SetPointAt(1, pt2.ToPoint2D());
            p[index].SetPointAt(2, pt3.ToPoint2D());
            p[index].SetPointAt(3, pt4.ToPoint2D());
            return p[index];
        }

        /// <summary>
        /// 1下外框
        /// </summary>
        /// <param name="index"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private Polyline getBottomFrame(int index, List<Polyline> p)
        {
            double minX = getPolylineLocate(p[index], 0);
            double minY = getPolylineLocate(p[index], 2);
            double change = maxX_New - maxX_Old;
            double maxX = getPolylineLocate(p[index], 1) + change+200;
            double maxY = getPolylineLocate(p[index], 3);

            var pt1 = new Point3d(minX, minY, 0);
            var pt2 = new Point3d(maxX, minY, 0);
            var pt3 = new Point3d(maxX, maxY, 0);
            var pt4 = new Point3d(minX, maxY, 0);
            p[index].SetPointAt(0, pt1.ToPoint2D());
            p[index].SetPointAt(1, pt2.ToPoint2D());
            p[index].SetPointAt(2, pt3.ToPoint2D());
            p[index].SetPointAt(3, pt4.ToPoint2D());
            return p[index];
        }

        /// <summary>
        /// 2上外框
        /// </summary>
        /// <param name="index"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private Polyline getTopFrame(int index, List<Polyline> p)
        {
            double changeX = maxX_New - maxX_Old;
            double changeY = maxY_New - maxY_Old;
            double minX = getPolylineLocate(p[index], 0);
            double minY = getPolylineLocate(p[index], 2) + changeY;
            double maxX = getPolylineLocate(p[index], 1) + changeX+200;
            double maxY = getPolylineLocate(p[index], 3) + changeY;

            var pt1 = new Point3d(minX, minY, 0);
            var pt2 = new Point3d(maxX, minY, 0);
            var pt3 = new Point3d(maxX, maxY, 0);
            var pt4 = new Point3d(minX, maxY, 0);
            p[index].SetPointAt(0, pt1.ToPoint2D());
            p[index].SetPointAt(1, pt2.ToPoint2D());
            p[index].SetPointAt(2, pt3.ToPoint2D());
            p[index].SetPointAt(3, pt4.ToPoint2D());
            return p[index];
        }

        /// <summary>
        /// 13右外框
        /// </summary>
        /// <param name="index"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private Polyline getRightFrame(int index, List<Polyline> p)
        {
            double changeX = maxX_New - maxX_Old;
            double changeY = maxY_New - maxY_Old;
            double minX = getPolylineLocate(p[index], 0) + changeX+200;
            double minY = getPolylineLocate(p[index], 2);
            double maxX = getPolylineLocate(p[index], 1) + changeX+200;
            double maxY = getPolylineLocate(p[index], 3) + changeY;

            var pt1 = new Point3d(minX, minY, 0);
            var pt2 = new Point3d(maxX, minY, 0);
            var pt3 = new Point3d(maxX, maxY, 0);
            var pt4 = new Point3d(minX, maxY, 0);
            p[index].SetPointAt(0, pt1.ToPoint2D());
            p[index].SetPointAt(1, pt2.ToPoint2D());
            p[index].SetPointAt(2, pt3.ToPoint2D());
            p[index].SetPointAt(3, pt4.ToPoint2D());
            return p[index];
        }

        /// <summary>
        /// 3 溢流管
        /// </summary>
        /// <param name="index"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private Polyline getOverflowPipe(int index, List<Polyline> p)
        {
            double change = maxY_New - maxY_Old;
            int maxYIndex = getPolylineLocateIndex(p[index], 3);

            double maxX = p[index].GetPoint3dAt(maxYIndex).X;
            double maxY = p[index].GetPoint3dAt(maxYIndex).Y + change;

            var pt = new Point3d(maxX, maxY, 0);
            p[index].SetPointAt(maxYIndex, pt.ToPoint2D());
            return p[index];

        }

        /// <summary>
        /// 5 水箱左边
        /// </summary>
        /// <param name="index"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private Polyline getLeftWaterTank(int index, List<Polyline> p)
        {
            double minX = getPolylineLocate(p[index], 0);
            double minY = getPolylineLocate(p[index], 2);
            double maxX = getPolylineLocate(p[index], 1);

            double maxY = maxY_New;

            var pt1 = new Point3d(minX, minY, 0);
            var pt2 = new Point3d(maxX, minY, 0);
            var pt3 = new Point3d(maxX, maxY, 0);
            var pt4 = new Point3d(minX, maxY, 0);
            Polyline frame = new Polyline { Closed = true };
            frame.Layer = p[index].Layer;
            frame.AddVertexAt(0, pt1.ToPoint2D(), 0, 0, 0);
            frame.AddVertexAt(1, pt2.ToPoint2D(), 0, 0, 0);
            frame.AddVertexAt(2, pt3.ToPoint2D(), 0, 0, 0);
            frame.AddVertexAt(3, pt4.ToPoint2D(), 0, 0, 0);

            return frame;
        }

        /// <summary>
        /// 6 获得水箱下面的
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private Polyline getUnderWaterTank(int index, List<Polyline> p)//6 
        {

            double minX = getPolylineLocate(p[index], 0);
            double minY = getPolylineLocate(p[index], 2);
            double change = minX_Old - minX;
            double maxX = maxX_New + change;
            double maxY = getPolylineLocate(p[index], 3);

            var pt1 = new Point3d(minX, minY, 0);
            var pt2 = new Point3d(maxX, minY, 0);
            var pt3 = new Point3d(maxX, maxY, 0);
            var pt4 = new Point3d(minX, maxY, 0);
            Polyline frame = new Polyline { Closed = true };
            frame.Layer = p[index].Layer;
            frame.AddVertexAt(0, pt1.ToPoint2D(), 0, 0, 0);
            frame.AddVertexAt(1, pt2.ToPoint2D(), 0, 0, 0);
            frame.AddVertexAt(2, pt3.ToPoint2D(), 0, 0, 0);
            frame.AddVertexAt(3, pt4.ToPoint2D(), 0, 0, 0);

            return frame;
        }

        /// <summary>
        /// 8液位计高位右边的
        /// </summary>
        /// <param name="index"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private Polyline getLeftAndMiddleWaterTank(int index, List<Polyline> p)
        {
            double minX = getPolylineLocate(p[index], 0);
            double change = maxY_New - maxY_Old;
            double Y = getPolylineLocate(p[index], 2) + change;
            double maxX = getPolylineLocate(p[index], 1);

            var pt1 = new Point3d(minX, Y, 0);
            var pt2 = new Point3d(maxX, Y, 0);
            p[index].SetPointAt(0, pt1.ToPoint2D());
            p[index].SetPointAt(1, pt2.ToPoint2D());
            return p[index];
        }

        /// <summary>
        /// 9 获得水箱 初始化时即计算
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private Polyline getWaterTank(int index, List<Polyline> p)//9 水箱
        {
            //double w = ThLifePumpCommon.Input_Width>2? ThLifePumpCommon.Input_Width:2;
            //double h = ThLifePumpCommon.Input_Height>2? ThLifePumpCommon.Input_Height:2;

            double w = getDrawData(ThLifePumpCommon.Input_Width);
            double h= getDrawData(ThLifePumpCommon.Input_Height);

            double minX = getPolylineLocate(p[index], 0);
            double minY = getPolylineLocate(p[index], 2);

            double maxX = minX + w * 1000;
            double maxY = minY + h * 1000;

            var pt1 = new Point3d(minX, minY, 0);
            var pt2 = new Point3d(maxX, minY, 0);
            var pt3 = new Point3d(maxX, maxY, 0);
            var pt4 = new Point3d(minX, maxY, 0);
            Polyline frame = new Polyline { Closed = true };
            frame.Layer = p[index].Layer;
            frame.AddVertexAt(0, pt1.ToPoint2D(), 0, 0, 0);
            frame.AddVertexAt(1, pt2.ToPoint2D(), 0, 0, 0);
            frame.AddVertexAt(2, pt3.ToPoint2D(), 0, 0, 0);
            frame.AddVertexAt(3, pt4.ToPoint2D(), 0, 0, 0);

            return frame;
        }

        private double getDrawData(double d)
        {
            if (d >= 2 && d <= 5)
                return d;
            else if (d < 2)
                return 2;
            else
                return 5;
        }

        /// <summary>
        /// 获取polyline的四个点坐标
        /// </summary>
        /// <param name="p"></param>
        /// <param name="type">0：minX；1：maxX；2：minY；3：maxY</param>
        /// <returns></returns>

        public static double getPolylineLocate(Polyline p, int type)
        {

            if (type == 0)//minX
            {
                double d = p.GetPoint3dAt(0).X;
                for (int i = 1; i < p.NumberOfVertices; i++)
                {
                    if (p.GetPoint3dAt(i).X < d)
                        d = p.GetPoint3dAt(i).X;
                }
                return d;
            }
            else if (type == 1)//maxX
            {
                double d = p.GetPoint3dAt(0).X;
                for (int i = 1; i < p.NumberOfVertices; i++)
                {
                    if (p.GetPoint3dAt(i).X > d)
                        d = p.GetPoint3dAt(i).X;
                }
                return d;
            }
            else if (type == 2)//minY
            {
                double d = p.GetPoint3dAt(0).Y;
                for (int i = 1; i < p.NumberOfVertices; i++)
                {
                    if (p.GetPoint3dAt(i).Y < d)
                        d = p.GetPoint3dAt(i).Y;
                }
                return d;
            }
            else if (type == 3)//maxY
            {
                double d = p.GetPoint3dAt(0).Y;
                for (int i = 1; i < p.NumberOfVertices; i++)
                {
                    if (p.GetPoint3dAt(i).Y > d)
                        d = p.GetPoint3dAt(i).Y;
                }
                return d;
            }

            return -1;
        }

        /// <summary>
        /// 获取polyline的四个点的index
        /// </summary>
        /// <param name="p"></param>
        /// <param name="type">0：minX；1：maxX；2：minY；3：maxY</param>
        /// <returns></returns>
        public static int getPolylineLocateIndex(Polyline p, int type)
        {

            if (type == 0)//minX
            {
                double d = p.GetPoint3dAt(0).X;
                int index = 0;
                for (int i = 1; i < p.NumberOfVertices; i++)
                {
                    if (p.GetPoint3dAt(i).X < d)
                    {
                        index = i;
                        d = p.GetPoint3dAt(i).X;
                    }

                }
                return index;
            }
            else if (type == 1)//maxX
            {
                double d = p.GetPoint3dAt(0).X;
                int index = 0;
                for (int i = 1; i < p.NumberOfVertices; i++)
                {
                    if (p.GetPoint3dAt(i).X > d)
                    {
                        index = i;
                        d = p.GetPoint3dAt(i).X;
                    }

                }
                return index;
            }
            else if (type == 2)//minY
            {
                double d = p.GetPoint3dAt(0).Y;
                int index = 0;
                for (int i = 1; i < p.NumberOfVertices; i++)
                {
                    if (p.GetPoint3dAt(i).Y < d)
                    {
                        index = i;
                        d = p.GetPoint3dAt(i).Y;
                    }

                }
                return index;
            }
            else if (type == 3)//maxY
            {
                double d = p.GetPoint3dAt(0).Y;
                int index = 0;
                for (int i = 1; i < p.NumberOfVertices; i++)
                {
                    if (p.GetPoint3dAt(i).Y > d)
                    {
                        index = i;
                        d = p.GetPoint3dAt(i).Y;
                    }

                }
                return index;
            }

            return -1;
        }

        /// <summary>
        /// 获得水箱变化前后四个点的坐标
        /// </summary>
        /// <returns></returns>
        public double[] getWaterLocate()
        {
            double[] pLocate = { minX_Old, maxX_Old, minY_Old, maxY_Old, minX_New, maxX_New, minY_New, maxY_New };
            return pLocate;
        }


        /// <summary>
        /// 获得3溢流管的最高点的坐标
        /// </summary>
        /// <returns></returns>
        public double[] getOverflowHighestLocate()
        {

            int maxYIndex = getPolylineLocateIndex(pList[3], 3);

            double maxX = pList[3].GetPoint3dAt(maxYIndex).X;
            double maxY = pList[3].GetPoint3dAt(maxYIndex).Y;

            double[] d = { maxX, maxY };
            //double[] d = {}
            return d;
        }


        /// <summary>
        /// 8液位计高位
        /// </summary>
        /// <returns></returns>
        public double[] getLevelGaugeLocate()
        {
            double[] d = { pList[8].GetPoint3dAt(0).X, pList[8].GetPoint3dAt(0).Y };
            return d;
        }

        /// <summary>
        /// 水箱的maxX maxY
        /// </summary>
        /// <returns></returns>
        public double[] getWaterMaxXYLocate()
        {
            return new double[] { maxX_New, maxY_New };
        }



        /// <summary>
        /// 获得3溢流管的maxX和middleY
        /// </summary>
        /// <returns></returns>
        public double[] getOverflowMaxXMidY()
        {
            return new double[] { pList[3].GetPoint3dAt(1).X, pList[3].GetPoint3dAt(1).Y };
        }

        /// <summary>
        /// 获得外框
        /// </summary>
        /// <returns></returns>
        public List<Polyline> getOutFrame()
        {
            List<Polyline> outFrame = new List<Polyline>();
            outFrame.Add(pList[0]);
            outFrame.Add(pList[1]);
            outFrame.Add(pList[2]);
            outFrame.Add(pList[13]);
            return outFrame;
        }
    }


    public class LineLifePump
    {
        List<Line> lList;
        List<Line> hList;
        List<Line> vList;
        List<Line> aList;
        //频繁使用 水箱新旧坐标
        private double minX_Old;
        private double maxX_Old;
        private double minY_Old;
        private double maxY_Old;

        private double minX_New;
        private double maxX_New;
        private double minY_New;
        private double maxY_New;

        public LineLifePump(double[] locate, List<Line> l)
        {
            lList = l;
            hList = new List<Line>(); vList = new List<Line>(); aList = new List<Line>();
            minX_Old = locate[0]; maxX_Old = locate[1]; minY_Old = locate[2]; maxY_Old = locate[3];
            minX_New = locate[4]; maxX_New = locate[5]; minY_New = locate[6]; maxY_New = locate[7];


        }
        public void setLineList()
        {
            sortLineList();//分类

            //针对水平线
            hList[2] = getHorizontalMiddleLong(2, hList);//2中间那根比较长的线
            getHorizontalNearTop(hList);//5-8靠近水箱顶端的四根直线
            lList.Remove(hList[3]);//删掉那根短的
                                   //hList.RemoveAt(3);

            //针对垂直线 不动

            //针对斜线 不动
        }



        /// <summary>
        /// 2中间那根比较长的线
        /// </summary>
        /// <param name="index"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private Line getHorizontalMiddleLong(int index, List<Line> p)
        {
            double startX = minX_New;
            double endX = maxX_New;
            double Y = minY_New + 400;
            p[index].StartPoint = new Point3d(startX, Y, 0);
            p[index].EndPoint = new Point3d(endX, Y, 0);
            return p[index];
        }

        /// <summary>
        /// 5-8靠近水箱顶端的四根直线
        /// </summary>
        /// <param name="p"></param>
        private void getHorizontalNearTop(List<Line> p)
        {


            double startX = minX_New;
            double endX = maxX_New;
            double change = maxY_New - maxY_Old;
            double Y = p[5].StartPoint.Y + change;

            p[5].StartPoint = new Point3d(startX, Y, 0); p[5].EndPoint = new Point3d(endX, Y, 0);
            p[6].StartPoint = new Point3d(startX, Y + 50, 0); p[6].EndPoint = new Point3d(endX, Y + 50, 0);
            p[7].StartPoint = new Point3d(startX, Y + 100, 0); p[7].EndPoint = new Point3d(endX, Y + 100, 0);
            p[8].StartPoint = new Point3d(startX, Y + 150, 0); p[8].EndPoint = new Point3d(endX, Y + 150, 0);

        }


        /// <summary>
        /// 对所有的line进行分类，改变地址
        /// </summary>
        private void sortLineList()
        {
            foreach (Line l in lList)
            {
                double angle = l.Angle * 180 / Math.PI;
                angle = Math.Round(angle, 0);
                if (angle == 0 || angle == 180)
                    hList.Add(l);
                else if (angle == 90 || angle == 270)
                    vList.Add(l);
                else
                    aList.Add(l);
            }
            hList = hList.OrderBy(i => i.StartPoint.Y).ThenByDescending(i => i.Length).ToList();//直线

            //aLine.OrderBy(i => i.Length);
        }

        /// <summary>
        /// 5-8靠近水箱顶端的四根直线中最靠近水箱顶的那根线-1
        /// </summary>
        /// <returns></returns>
        public double[] getCallout1Locate()
        {
            double[] d = { hList[8].EndPoint.X, hList[8].EndPoint.Y };
            return d;
        }

        /// <summary>
        /// 5-8靠近水箱顶端的四根直线中最低的那根线-4
        /// </summary>
        /// <returns></returns>
        public double[] getOverflowDiaLocate()
        {
            double[] d = { hList[5].EndPoint.X, hList[5].EndPoint.Y };
            return d;
        }

        /// <summary>
        /// 5-8靠近水箱顶端的四根直线中第二根线-2
        /// </summary>
        /// <returns></returns>
        public double[] getFiveEightTwo()
        {
            double[] d = { hList[7].EndPoint.X, hList[7].EndPoint.Y };
            return d;
        }

        /// <summary>
        /// 5-8靠近水箱顶端的四根直线中第三根线-3
        /// </summary>
        /// <returns></returns>
        public double[] getFiveEightThree()
        {
            double[] d = { hList[6].EndPoint.X, hList[6].EndPoint.Y };
            return d;
        }


    }

    /// <summary>
    /// 块的方法
    /// </summary>
    public class BlockLifePump
    {

        List<BlockReference> bList;
        double[] overflowLocate;//溢水管最高的位置
        double[] callout1Locate;//靠近水箱顶四根线中最高的一根
        double[] levelGaugeHighLocate;//液位计高位
        double[] waterLocate;//水箱新旧坐标
        //ThLifePumpService calValues;
        //double[] waterMaxXYLocate;//水箱的maxX和maxY
        //double[] waterMinXYLocate;//水箱的minX和minY



        public BlockLifePump(List<BlockReference> b, double[] ol, double[] cl, double[] ll, double[] wl)
        {
            bList = b;
            overflowLocate = ol;
            callout1Locate = cl;
            levelGaugeHighLocate = ll;
            waterLocate = wl;
            //calValues = new ThLifePumpService();
        }

        public void setBlockList()
        {
            setFunnel(bList);//倒立的漏斗
            setArchTick(bList);//_ArchTick-中线位置
            setCallout1(bList);//生活水箱标注1
            setRoomPump(bList);//生活水泵房泵组
            setSpinPrevent(bList);// 旋流防止器高度
            setInletPipe(bList);// 进水管组件
            setSnorkel2(bList); //通气管2
            setWaterTopHeight(bList);//水箱顶高度，在进水管组件和通气管2中间
            setLevelGauge(bList);// 液位计高位
        }

        /// <summary>
        /// 倒立的漏斗
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        private void setFunnel(List<BlockReference> b)
        {
            int i = b.FindIndex(i => i.Name == "$ATTACHMENT$00000068");

            var pt = new Point3d(overflowLocate[0] - b[i].Position.X, overflowLocate[1] - b[i].Position.Y, 0);
            var mat = Matrix3d.Displacement(pt.GetAsVector());

            b[i].TransformBy(mat);
        }

        /// <summary>
        /// _ArchTick-中线位置
        /// </summary>
        /// <param name="b"></param>
        private void setArchTick(List<BlockReference> b)
        {
            //double w = ThLifePumpCommon.Input_Width>2? ThLifePumpCommon.Input_Width:2;
            double w = getDrawData(ThLifePumpCommon.Input_Width);

            int i = b.FindIndex(i => i.Name == "_ArchTick");
            double x;

            if (w * 1000 >= 2175)
            {
                x = b[i].Position.X;
            }
            else
            {
                x = waterLocate[4] + 1115;
            }

            double y = waterLocate[6] + 400;

            var pt = new Point3d(x - b[i].Position.X, y - b[i].Position.Y, 0);
            var mat = Matrix3d.Displacement(pt.GetAsVector());

            b[i].TransformBy(mat);

        }

        /// <summary>
        /// 生活水箱标注1
        /// </summary>
        /// <param name="b"></param>
        private void setCallout1(List<BlockReference> b)
        {
            int i = b.FindIndex(i => i.Name == "生活水箱标注1");

            var pt = new Point3d(callout1Locate[0] - b[i].Position.X, callout1Locate[1] - b[i].Position.Y, 0);
            var mat = Matrix3d.Displacement(pt.GetAsVector());
            b[i].TransformBy(mat);
            //b[i].Position = pt;
        }

        /// <summary>
        /// 生活水泵房泵组
        /// </summary>
        /// <param name="b"></param>
        private void setRoomPump(List<BlockReference> b)
        {
            //double w = ThLifePumpCommon.Input_Width > 2 ? ThLifePumpCommon.Input_Width : 2;
            double w = getDrawData(ThLifePumpCommon.Input_Width);

            int i = b.FindIndex(i => i.Name == "生活水泵房泵组");
            double x;

            if (w * 1000 >= 2625)
            {
                x = waterLocate[5] - 625;
            }
            else
            {
                x = waterLocate[5] - 450;
            }

            //double y = b[i].Position.Y;
            var pt = new Point3d(x - b[i].Position.X, 0, 0);
            var mat = Matrix3d.Displacement(pt.GetAsVector());
            b[i].TransformBy(mat);
        }

        /// <summary>
        /// 旋流防止器高度
        /// </summary>
        /// <param name="b"></param>
        private void setSpinPrevent(List<BlockReference> b)
        {
            int i = b.FindIndex(i => i.Name == "旋流防止器高度");
            int j1 = b.FindIndex(i => i.Name == "_ArchTick");
            int j2 = b.FindIndex(i => i.Name == "生活水泵房泵组");
            double between = b[j2].Position.X - b[j1].Position.X;
            double x;
            if (between >= 840)
            {
                x = b[j2].Position.X;

                var pt = new Point3d(x - b[i].Position.X, 0, 0);
                var mat = Matrix3d.Displacement(pt.GetAsVector());
                b[i].TransformBy(mat);
            }
            else
            {
                b.RemoveAt(i);
            }

        }

        /// <summary>
        /// 进水管组件
        /// </summary>
        /// <param name="b"></param>
        private void setInletPipe(List<BlockReference> b)
        {
            int i = b.FindIndex(i => i.Name == "进水管组件");
            var pt = new Point3d(0, waterLocate[7] - b[i].Position.Y, 0);
            var mat = Matrix3d.Displacement(pt.GetAsVector());
            b[i].TransformBy(mat);
        }

        /// <summary>
        /// 通气管2
        /// </summary>
        /// <param name="b"></param>
        private void setSnorkel2(List<BlockReference> b)
        {
            //double w = ThLifePumpCommon.Input_Width > 2 ? ThLifePumpCommon.Input_Width : 2;
            double w = getDrawData(ThLifePumpCommon.Input_Width);

            int i = b.FindIndex(i => i.Name == "通气管2");

            double x;
            if (w * 1000 >= 2500)
            {
                x = waterLocate[5] - 500;
            }
            else
            {
                x = waterLocate[5] - 100;
            }
            var pt = new Point3d(x - b[i].Position.X, waterLocate[7] - b[i].Position.Y, 0);

            var mat = Matrix3d.Displacement(pt.GetAsVector());
            b[i].TransformBy(mat);
        }

        /// <summary>
        /// 水箱顶高度，在进水管组件和通气管2中间
        /// </summary>
        /// <param name="b"></param>
        private void setWaterTopHeight(List<BlockReference> b)
        {
            int i1 = b.FindIndex(i => i.Name == "进水管组件");
            double leftX = b[i1].Position.X + 800;
            int i2 = b.FindIndex(i => i.Name == "通气管2");
            double rightX = b[i2].Position.X - 200;

            double x = (leftX + rightX) / 2 - 218;
            double y = b[i1].Position.Y;

            int i = b.FindIndex(i => i.Name == "水箱顶高度");
            var pt = new Point3d(x - b[i].Position.X, y - b[i].Position.Y, 0);
            var mat = Matrix3d.Displacement(pt.GetAsVector());
            b[i].TransformBy(mat);
        }

        /// <summary>
        /// 液位计高位
        /// </summary>
        /// <param name="b"></param>
        private void setLevelGauge(List<BlockReference> b)
        {
            int i = b.FindIndex(i => i.Name == "液位计高位");

            var pt = new Point3d(levelGaugeHighLocate[0] - b[i].Position.X, levelGaugeHighLocate[1] - b[i].Position.Y, 0);
            var mat = Matrix3d.Displacement(pt.GetAsVector());
            b[i].TransformBy(mat);

            //磁耦合液位计：高位h+<hs-0.15>
            var value = new Dictionary<string, string>() { { "液位计高位", "111" } };
            //b[i].ObjectId.UpdateAttributesInBlock(value);

        }

        private double getDrawData(double d)
        {
            if (d >= 2 && d <= 5)
                return d;
            else if (d < 2)
                return 2;
            else
                return 5;
        }

        /// <summary>
        /// 获得archtick的坐标
        /// </summary>
        /// <returns></returns>
        public double[] getArchTickLocate()
        {
            int i = bList.FindIndex(i => i.Name == "_ArchTick");
            return new double[] { bList[i].Position.X, bList[i].Position.Y };
        }


    }

    /// <summary>
    /// 文字和属性定义的方法
    /// </summary>
    public class TALifePump
    {
        List<DBText> taList;
        List<AttributeDefinition> aList;//找出属性定义
        double[] OverflowMaxXMidYLocate;//3溢流管的maxX和middleY
        double[] OverflowDiaLocate;//5-8靠近水箱顶端的四根直线中最低的那根线

        public TALifePump(List<DBText> t, double[] oml, double[] odl)
        {
            taList = t;
            OverflowMaxXMidYLocate = oml;
            OverflowDiaLocate = odl;
            aList = new List<AttributeDefinition>();
            findAttrDefinition();//拿出所有的属性定义
        }

        public void setTAList()
        {
            setOverflowDia1(aList);
        }

        private void findAttrDefinition()
        {
            foreach (var f in taList)
            {
                if (f is AttributeDefinition a)
                {
                    aList.Add(a);
                }
            }
        }
        /// <summary>
        /// 溢流管管径1
        /// </summary>
        /// <param name="t"></param>
        private void setOverflowDia1(List<AttributeDefinition> a)
        {
            int i = a.FindIndex(i => i.Tag == "溢流管管径1");
            //double l = ThLifePumpCommon.Input_Height;
            double l = getDrawData(ThLifePumpCommon.Input_Height);

            double x = a[i].Position.X;
            double y;
            if (l * 1000 > 2500)
            {
                y = (OverflowDiaLocate[1] + OverflowMaxXMidYLocate[1]) / 2;
            }
            else
                y = OverflowMaxXMidYLocate[1] + 50;


            var pt = new Point3d(x, y, 0);
            a[i].Position = pt;
        }
        private double getDrawData(double d)
        {
            if (d >= 2 && d <= 5)
                return d;
            else if (d < 2)
                return 2;
            else
                return 5;
        }

    }

    /// <summary>
    /// 标注的方法
    /// </summary>
    public class ADLifePump
    {
        List<AlignedDimension> adList;


        double[] fiveEightOneLocate;//靠近水箱顶四根线中最高的一根
        double[] fiveEightTwoLocate;
        double[] fiveEightThreeLocate;
        double[] fiveEightFourLocate;
        double[] archTickLocate;//中线位置
        double[] waterLocate;//水箱位置



        public ADLifePump(List<AlignedDimension> a, double[] f1, double[] f2, double[] f3, double[] f4, double[] al, double[] wl)
        {
            adList = a.OrderBy(x => x.TextPosition.X).ThenBy(x => x.TextPosition.Y).ToList(); ;
            fiveEightOneLocate = f1;
            fiveEightTwoLocate = f2;
            fiveEightThreeLocate = f3;
            fiveEightFourLocate = f4;
            archTickLocate = al;
            waterLocate = wl;


        }
        public void setADList()
        {
            setAD150(2);//dn150
            setEffectiveWater(3);
            setWaterBottomHeight(4);
            set400(5);//400
            setAD50U(6);//dn50 上
            setAD50D(7);//dn50 下
            setAD50R(8);//dn50 右
        }

        /// <summary>
        /// 2 dn150
        /// </summary>
        /// <param name="index"></param>
        private void setAD150(int index)
        {
            var pt = new Point3d(0, waterLocate[7] - waterLocate[3], 0);
            var mat = Matrix3d.Displacement(pt.GetAsVector());
            adList[index].TransformBy(mat);
        }

        /// <summary>
        /// 3 有效水深 先平移在压缩
        /// </summary>
        /// <param name="index"></param>
        private void setEffectiveWater(int index)
        {
            //先平移 以下面(左）为基点 只需左右平移
            //double changeY = waterLocate[7] - waterLocate[3];
            double originX = adList[index].DimLinePoint.X;//拉出的x都相同

            double endX = archTickLocate[0];

            var pt = new Point3d(endX - originX, 0, 0);
            var mat = Matrix3d.Displacement(pt.GetAsVector());
            adList[index].TransformBy(mat);

            //放缩
            double DX = adList[index].DimLinePoint.X;
            double PX = adList[index].XLine1Point.X;
            double Y = fiveEightFourLocate[1];//x基点坐标都相同

            var ptD = new Point3d(DX, Y, 0);
            adList[index].DimLinePoint = ptD;

            var ptP = new Point3d(PX, Y, 0);
            if (adList[index].XLine1Point.Y > adList[index].XLine2Point.Y)
                adList[index].XLine1Point = ptP;
            else
                adList[index].XLine2Point = ptP;

            var ptT = new Point3d(adList[index].TextPosition.X, (adList[index].XLine1Point.Y + adList[index].XLine2Point.Y) / 2 + 100, 0);
            adList[index].TextPosition = ptT;
        }

        /// <summary>
        /// 4 水箱底高度
        /// </summary>
        /// <param name="index"></param>
        private void setWaterBottomHeight(int index)
        {
            //transform 原点坐标选择下方 y不变
            double originX = adList[index].DimLinePoint.X;//拉出的x都相同
            double endX = archTickLocate[0];

            var pt = new Point3d(endX - originX, 0, 0);
            var mat = Matrix3d.Displacement(pt.GetAsVector());
            adList[index].TransformBy(mat);


        }

        /// <summary>
        /// 5 400
        /// </summary>
        /// <param name="index"></param>
        private void set400(int index)
        {
            //transform 原点坐标选择下方 y不变
            double originX = adList[index].DimLinePoint.X;//拉出的x都相同
            double endX = archTickLocate[0];

            var pt = new Point3d(endX - originX, 0, 0);
            var mat = Matrix3d.Displacement(pt.GetAsVector());
            adList[index].TransformBy(mat);
        }

        /// <summary>
        /// 6 dn50 上
        /// </summary>
        /// <param name="index"></param>
        private void setAD50U(int index)
        {
            //transform 原点坐标选择左上
            double originX = adList[index].DimLinePoint.X;//拉出的x都相同
            //y选择基点x
            double originY = adList[index].XLine1Point.Y > adList[index].XLine2Point.Y ? adList[index].XLine1Point.Y : adList[index].XLine2Point.Y;

            double endX = archTickLocate[0];
            double endY = fiveEightOneLocate[1];

            var pt = new Point3d(endX - originX, endY - originY, 0);
            var mat = Matrix3d.Displacement(pt.GetAsVector());
            adList[index].TransformBy(mat);
        }

        /// <summary>
        /// 7 dn50 下
        /// </summary>
        /// <param name="index"></param>
        private void setAD50D(int index)
        {
            //transform 原点坐标选择minX maxY 左上
            double originX = adList[index].DimLinePoint.X;//拉出的x都相同
            //y选择基点x
            double originY = adList[index].XLine1Point.Y > adList[index].XLine2Point.Y ? adList[index].XLine1Point.Y : adList[index].XLine2Point.Y;

            double endX = archTickLocate[0];
            double endY = fiveEightThreeLocate[1];

            var pt = new Point3d(endX - originX, endY - originY, 0);
            var mat = Matrix3d.Displacement(pt.GetAsVector());
            adList[index].TransformBy(mat);
        }

        /// <summary>
        /// 8 dn50 右
        /// </summary>
        /// <param name="index"></param>
        private void setAD50R(int index)
        {
            //transform 原点坐标选择minX maxY 左上
            double originX = adList[index].DimLinePoint.X;//拉出的x都相同
            //y选择基点x
            double originY = adList[index].XLine1Point.Y > adList[index].XLine2Point.Y ? adList[index].XLine1Point.Y : adList[index].XLine2Point.Y;

            double endX = archTickLocate[0];
            double endY = fiveEightTwoLocate[1];

            var pt = new Point3d(endX - originX, endY - originY, 0);
            var mat = Matrix3d.Displacement(pt.GetAsVector());
            adList[index].TransformBy(mat);
        }
    }


    /// <summary>
    /// 计算数据
    /// </summary>
    public class CalDataLifePump
    {
        private Dictionary<string, string> LifePumpData = new Dictionary<string, string>();

        public Dictionary<string, string> getData()
        {
            CalLifePumpData();
            return LifePumpData;
        }

        //计算所有数据
        private void CalLifePumpData()
        {

            var a1 = CalLifePumpEssentialAttr();//基础属性

            a1.Add(ThLifePumpCommon.SuctionTotalPipeDiameter, "DN" + CalSuctionTotal().ToString());
            a1.Add(ThLifePumpCommon.PumpSuctionPipeDiameter, "DN" + CalSuction().ToString());
            a1.Add(ThLifePumpCommon.PumpOutletHorizontalPipeDiameter, "DN" + CalOutletHorizontal().ToString());
            a1.Add(ThLifePumpCommon.PumpOutletPipeDiameter, "DN" + CalOutlet().ToString());
            a1.Add(ThLifePumpCommon.WaterTankInletPipeDiameter, "DN" + CalWaterTankInlet().ToString());
            a1.Add(ThLifePumpCommon.OverflowPipeDiameter, "DN" + CalOverflow().ToString());
            a1.Add(ThLifePumpCommon.DrainPipeDiameter, "DN" + CalDrain().ToString());

            foreach (var att in a1)
            {
                if (LifePumpData.ContainsKey(att.Key))
                    LifePumpData[att.Key] = att.Value;
                else
                    LifePumpData.Add(att.Key, att.Value);
            }

        }




        //得到生活泵房基础属性
        private Dictionary<string, string> CalLifePumpEssentialAttr()
        {
            double hs = ThLifePumpCommon.Input_Height;
            double h0 = ThLifePumpCommon.Input_BasicHeight;

            var attNameValues = new Dictionary<string, string>();
            attNameValues.Add(ThLifePumpCommon.MagneticLevel, "h+" + (hs - 0.15).ToString("0.00"));
            attNameValues.Add(ThLifePumpCommon.MinimumAlarmWaterLevel, "h+" + (hs - 0.45).ToString("0.00"));
            attNameValues.Add(ThLifePumpCommon.MaximumEffectiveWaterLevel, "h+" + (hs - 0.40).ToString("0.00"));
            attNameValues.Add(ThLifePumpCommon.MaximumAlarmWaterLevel, "h+" + (hs - 0.35).ToString("0.00"));
            attNameValues.Add(ThLifePumpCommon.OverflowWaterLevel, "h+" + (hs - 0.30).ToString("0.00"));
            attNameValues.Add(ThLifePumpCommon.TankTopHeight, "h+" + (hs).ToString("0.00"));
            attNameValues.Add(ThLifePumpCommon.TankBottomHeight, ((h0 + 0.1) * 1000).ToString());
            attNameValues.Add(ThLifePumpCommon.EffectiveWaterDepth, ((hs - 0.85) * 1000).ToString());
            attNameValues.Add(ThLifePumpCommon.PumpBaseHeight, "h-" + (h0).ToString("0.00"));
            attNameValues.Add(ThLifePumpCommon.BuildingFinishElevation, "h-" + (h0 + 0.10).ToString("0.00"));


            return attNameValues;
        }

        //计算吸水总管管径
        private int CalSuctionTotal()
        {
            List<Pump_Arr> pl = ThLifePumpCommon.Input_PumpList;

            double sum = 0;
            foreach (var i in pl)
            {
                int j = i.Num;
                if (i.NoteSelect.Contains("一备"))
                    j--;
                sum += i.Flow_Info * j;
            }

            if (0 < sum && sum <= 9)
                return 50;
            else if (9 < sum && sum <= 12)
                return 65;
            else if (12 < sum && sum <= 18)
                return 80;
            else if (18 < sum && sum <= 37)
                return 100;
            else if (37 < sum && sum <= 81)
                return 150;
            else if (81 < sum && sum <= 153)
                return 200;
            else if (153 < sum && sum <= 238)
                return 250;
            else if (238 < sum)
                return 300;

            return -1;//数据错误
        }

        //计算水泵吸水管管径
        private int CalSuction()
        {
            List<Pump_Arr> pl = ThLifePumpCommon.Input_PumpList;
            int choice = ThLifePumpCommon.PumpSuctionPipeDiameterChoice;

            //double sum = pl[choice - 1].Flow_Info * pl[choice - 1].Num;
            double sum = pl[choice - 1].Flow_Info ;
            if (0 < sum && sum <= 9)
                return 50;
            else if (9 < sum && sum <= 12)
                return 65;
            else if (12 < sum && sum <= 18)
                return 80;
            else if (18 < sum && sum <= 37)
                return 100;
            else if (37 < sum && sum <= 81)
                return 150;
            else if (81 < sum && sum <= 153)
                return 200;
            else if (153 < sum && sum <= 238)
                return 250;
            else if (238 < sum)
                return 300;

            return -1;//数据错误
        }

        //计算水泵出水横管管径
        private int CalOutletHorizontal()
        {
            List<Pump_Arr> pl = ThLifePumpCommon.Input_PumpList;
            int choice = ThLifePumpCommon.PumpOutletHorizontalPipeDiameterChoice;

            int j = pl[choice - 1].Num;
            if (pl[choice - 1].NoteSelect.Contains("一备"))
                j--;

            double sum = pl[choice - 1].Flow_Info * j;
            if (0 < sum && sum <= 1.5)
                return 25;
            else if (1.5 < sum && sum <= 3.5)
                return 32;
            else if (3.5 < sum && sum <= 4.5)
                return 40;
            else if (4.5 < sum && sum <= 10)
                return 50;
            else if (10 < sum && sum <= 17)
                return 65;
            else if (17 < sum && sum <= 30)
                return 80;
            else if (30 < sum && sum <= 53)
                return 100;
            else if (53 < sum && sum <= 117)
                return 150;
            else if (117 < sum && sum <= 207)
                return 200;
            else if (207 < sum)
                return 250;


            return -1;//数据错误
        }

        //计算水泵出水立管管径
        private int CalOutlet()
        {
            List<Pump_Arr> pl = ThLifePumpCommon.Input_PumpList;
            int choice = ThLifePumpCommon.PumpOutletPipeDiameterChoice;

            double sum = pl[choice - 1].Flow_Info;
            if (0 < sum && sum <= 1.5)
                return 25;
            else if (1.5 < sum && sum <= 3.5)
                return 32;
            else if (3.5 < sum && sum <= 4.5)
                return 40;
            else if (4.5 < sum && sum <= 10)
                return 50;
            else if (10 < sum && sum <= 17)
                return 65;
            else if (17 < sum && sum <= 30)
                return 80;
            else if (30 < sum && sum <= 53)
                return 100;
            else if (53 < sum && sum <= 117)
                return 150;
            else if (117 < sum && sum <= 207)
                return 200;
            else if (207 < sum)
                return 250;


            return -1;//数据错误
        }

        //计算水箱进水管管径
        private int CalWaterTankInlet()
        {
            double l = ThLifePumpCommon.Input_Length;
            double w = ThLifePumpCommon.Input_Width ;
            double hs = ThLifePumpCommon.Input_Height;


            double sum = 5.00 / 16.00 * l * w * hs;
            if (0 < sum && sum <= 1.5)
                return 25;
            else if (1.5 < sum && sum <= 3.5)
                return 32;
            else if (3.5 < sum && sum <= 4.5)
                return 40;
            else if (4.5 < sum && sum <= 10)
                return 50;
            else if (10 < sum && sum <= 17)
                return 65;
            else if (17 < sum && sum <= 30)
                return 80;
            else if (30 < sum && sum <= 53)
                return 100;
            else if (53 < sum && sum <= 117)
                return 150;
            else if (117 < sum && sum <= 207)
                return 200;
            else if (207 < sum)
                return 250;


            return -1;//数据错误
        }

        //计算溢流管管径-进水管管径加一档
        private int CalOverflow()
        {
            double l = ThLifePumpCommon.Input_Length;
            double w = ThLifePumpCommon.Input_Width;
            double hs = ThLifePumpCommon.Input_Height;

            double sum = 5.00 / 16.00 * l * w * hs;
            if (0 < sum && sum <= 1.5)
                return 32;
            else if (1.5 < sum && sum <= 3.5)
                return 40;
            else if (3.5 < sum && sum <= 4.5)
                return 50;
            else if (4.5 < sum && sum <= 10)
                return 65;
            else if (10 < sum && sum <= 17)
                return 80;
            else if (17 < sum && sum <= 30)
                return 100;
            else if (30 < sum && sum <= 53)
                return 150;
            else if (53 < sum && sum <= 117)
                return 200;
            else if (117 < sum && sum <= 207)
                return 250;
            else if (207 < sum)
                return 300;


            return -1;//数据错误
        }

        //计算泄水管管径-进水管管径减一档
        private int CalDrain()
        {
            double l = ThLifePumpCommon.Input_Length;
            double w = ThLifePumpCommon.Input_Width ;
            double hs = ThLifePumpCommon.Input_Height;

            double sum = 5.00 / 16.00 * l * w * hs;
            if (0 < sum && sum <= 1.5)
                return 50;
            else if (1.5 < sum && sum <= 3.5)
                return 50;
            else if (3.5 < sum && sum <= 4.5)
                return 50;
            else if (4.5 < sum && sum <= 10)
                return 50;
            else if (10 < sum && sum <= 17)
                return 50;
            else if (17 < sum && sum <= 30)
                return 65;
            else if (30 < sum && sum <= 53)
                return 80;
            else if (53 < sum && sum <= 117)
                return 100;
            else if (117 < sum && sum <= 207)
                return 150;
            else if (207 < sum)
                return 200;


            return -1;//数据错误
        }
    }

    /// <summary>
    /// 计算好的数据进行填充
    /// </summary>
    public class ResetLifePumpData
    {

        List<BlockReference> bList;
        List<DBText> taList;//文字和属性定义
        List<AttributeDefinition> aList;//找出属性定义
        List<AlignedDimension> adList;
        Dictionary<string, string> data;

        public ResetLifePumpData(List<BlockReference> bList, List<DBText> taList, List<AlignedDimension> adList)
        {

            this.bList = bList;
            this.taList = taList;
            this.adList = adList.OrderBy(x => x.TextPosition.X).ThenBy(x => x.TextPosition.Y).ToList();

            CalDataLifePump c = new CalDataLifePump();
            data = c.getData();

            aList = new List<AttributeDefinition>();
            findAttrDefinition();//拿出所有的属性定义
        }
        public void setLifePumpData()
        {
            setLevelGauge(bList);// 液位计高位
            setCallout1(bList);//生活水箱标注1
            setWaterTopHeight(bList);//水箱顶高
            setWaterBottomHeight(4);//水箱底高度
            setEffectiveWater(3);//有效水深
            setRoomPump(bList);//生活水泵房泵组
            setInletPipe(bList);//进水管
            setOverflowDia(aList);//溢流管
            setDrainDia(aList);//泄水管
        }
        private void findAttrDefinition()
        {
            foreach (var f in taList)
            {
                if (f is AttributeDefinition a)
                {
                    aList.Add(a);
                }
            }
        }
        /// <summary>
        /// 液位计高位
        /// </summary>
        /// <param name="b"></param>
        private void setLevelGauge(List<BlockReference> b)
        {
            int i = b.FindIndex(i => i.Name == "液位计高位");
            //磁耦合液位计：高位h+<hs-0.15>
            var value = new Dictionary<string, string>() { { "液位计高位", data[ThLifePumpCommon.MagneticLevel] } };
            b[i].ObjectId.UpdateAttributesInBlock(value);
        }

        /// <summary>
        /// 生活水箱标注1
        /// </summary>
        /// <param name="b"></param>
        private void setCallout1(List<BlockReference> b)
        {
            int i = b.FindIndex(i => i.Name == "生活水箱标注1");
            //最低报警水位：h+<hs-0.45>
            var value = new Dictionary<string, string>() { { ThLifePumpCommon.MinimumAlarmWaterLevel, data[ThLifePumpCommon.MinimumAlarmWaterLevel] } };
            b[i].ObjectId.UpdateAttributesInBlock(value);

            //最高有效水位：h+<hs-0.40>
            value = new Dictionary<string, string>() { { ThLifePumpCommon.MaximumEffectiveWaterLevel, data[ThLifePumpCommon.MaximumEffectiveWaterLevel] } };
            b[i].ObjectId.UpdateAttributesInBlock(value);

            //最高报警水位：h+<hs-0.35>
            value = new Dictionary<string, string>() { { ThLifePumpCommon.MaximumAlarmWaterLevel, data[ThLifePumpCommon.MaximumAlarmWaterLevel] } };
            b[i].ObjectId.UpdateAttributesInBlock(value);

            //溢流水位：h+<hs-0.30>
            value = new Dictionary<string, string>() { { ThLifePumpCommon.OverflowWaterLevel, data[ThLifePumpCommon.OverflowWaterLevel] } };
            b[i].ObjectId.UpdateAttributesInBlock(value);
        }

        /// <summary>
        /// 水箱顶高度
        /// </summary>
        /// <param name="b"></param>
        private void setWaterTopHeight(List<BlockReference> b)
        {
            int i = b.FindIndex(i => i.Name == "水箱顶高度");
            //水箱顶高：h+<hs>
            var value = new Dictionary<string, string>() { { ThLifePumpCommon.TankTopHeight, data[ThLifePumpCommon.TankTopHeight] } };
            b[i].ObjectId.UpdateAttributesInBlock(value);
        }

        /// <summary>
        /// 水箱底高度
        /// </summary>
        /// <param name="index"></param>
        private void setWaterBottomHeight(int index)
        {
            //水箱底高度：<(hs-0.85)*1000>
            adList[index].DimensionText = data[ThLifePumpCommon.TankBottomHeight];
            //adList[index].DimensionText = "水箱test";
        }

        /// <summary>
        /// 有效水深
        /// </summary>
        /// <param name="index"></param>
        private void setEffectiveWater(int index)
        {
            //有效水深：<(h0+0.1)*1000>
            adList[index].DimensionText = data[ThLifePumpCommon.EffectiveWaterDepth];
            //adList[index].DimensionText = "水箱test";
        }

        /// <summary>
        /// 生活水泵房泵组
        /// </summary>
        /// <param name="b"></param>
        private void setRoomPump(List<BlockReference> b)
        {
            int i = b.FindIndex(i => i.Name == "生活水泵房泵组");
            //泵基础高度：h-<h0>
            var value = new Dictionary<string, string>() { { ThLifePumpCommon.PumpBaseHeight, data[ThLifePumpCommon.PumpBaseHeight] } };
            b[i].ObjectId.UpdateAttributesInBlock(value);

            //建筑完成面标高：h-<h0+0.10>
            value = new Dictionary<string, string>() { { ThLifePumpCommon.BuildingFinishElevation, data[ThLifePumpCommon.BuildingFinishElevation] } };
            b[i].ObjectId.UpdateAttributesInBlock(value);

            //吸水总管管径：DN<总管管径数值>
            value = new Dictionary<string, string>() { { ThLifePumpCommon.SuctionTotalPipeDiameter, data[ThLifePumpCommon.SuctionTotalPipeDiameter] } };
            b[i].ObjectId.UpdateAttributesInBlock(value);

            //水泵吸水管管径：DN<吸水管管径数值>
            value = new Dictionary<string, string>() { { ThLifePumpCommon.PumpSuctionPipeDiameter, data[ThLifePumpCommon.PumpSuctionPipeDiameter] } };
            b[i].ObjectId.UpdateAttributesInBlock(value);

            //水泵出水横管管径：DN<出水横管管径数值>
            value = new Dictionary<string, string>() { { ThLifePumpCommon.PumpOutletHorizontalPipeDiameter, data[ThLifePumpCommon.PumpOutletHorizontalPipeDiameter] } };
            b[i].ObjectId.UpdateAttributesInBlock(value);

            //水泵出水立管管径：DN<出水立管管径数值>
            value = new Dictionary<string, string>() { { ThLifePumpCommon.PumpOutletPipeDiameter, data[ThLifePumpCommon.PumpOutletPipeDiameter] } };
            b[i].ObjectId.UpdateAttributesInBlock(value);

        }

        /// <summary>
        /// 进水管组件
        /// </summary>
        /// <param name="b"></param>
        private void setInletPipe(List<BlockReference> b)
        {
            int i = b.FindIndex(i => i.Name == "进水管组件");
            //水箱进水管管径：DN<水箱进水管管径数值>
            var value = new Dictionary<string, string>() { { ThLifePumpCommon.WaterTankInletPipeDiameter, data[ThLifePumpCommon.WaterTankInletPipeDiameter] } };
            b[i].ObjectId.UpdateAttributesInBlock(value);
        }

        /// <summary>
        /// 溢流管管径
        /// </summary>
        /// <param name="a"></param>
        private void setOverflowDia(List<AttributeDefinition> a)
        {
            int i = a.FindIndex(i => i.Tag == "溢流管管径1");
            a[i].Tag = data[ThLifePumpCommon.OverflowPipeDiameter];

            i = a.FindIndex(i => i.Tag == "溢流管管径");
            a[i].Tag = data[ThLifePumpCommon.OverflowPipeDiameter];
        }

        /// <summary>
        /// 泄水管管径
        /// </summary>
        /// <param name="a"></param>
        private void setDrainDia(List<AttributeDefinition> a)
        {
            int i = a.FindIndex(i => i.Tag == "泄水管管径");
            a[i].Tag = data[ThLifePumpCommon.DrainPipeDiameter];


        }
    }

    /// <summary>
    /// 多行文字
    /// </summary>
    public class MTextLifePump
    {
        private Point3d pt;

        public MTextLifePump(Point3d pt)
        {
            this.pt = new Point3d(pt.X, pt.Y - 3500, 0);
        }

        public MText WriteIntro()
        {
            var text = new MText();
            text.Location = pt;
            text.Contents = getText();
            //text.Rotation = 0;
            text.Height = 250;
            text.TextHeight = 150;
            text.TextStyleId = DbHelper.GetTextStyleId("TH-STYLE3");
            text.Layer = "W-NOTE";
            text.Width = 6000;
            return text;
        }

        private string getText()
        {
            double bh = ThLifePumpCommon.Input_BasicHeight * 1000;

            string nums = "1";
            if (ThLifePumpCommon.Input_PumpList.Count > 1)
                nums = nums + "~" + ThLifePumpCommon.Input_PumpList.Count;
            else
                nums = nums + "#";

            var input = ThLifePumpCommon.Input_PumpList.OrderBy(i => i.Head).ToList();
            string s = "";
            for (int i = 0; i < input.Count - 1; i++)
            {
                double p = input[i].Head / 100.00;
                s = s + "加压" + (i + 1).ToString() + "区运行压力值设定为" + p + "MPa，";
            }
            s = s + "加压" + input.Count + "区运行压力值设定为" + (input[input.Count - 1].Head / 100.00) + "MPa。";

            string t = String.Format(
                "说明:\r\n" +
                "1.图中标高以米计,尺寸以毫米计,标高为相对标高。泵房内明沟宽400mm，起点深100mm，坡向集水井，坡度1%。\r\n" +
                "2.水箱基础高出完成面{0}mm，基础上再做100mm高型钢，各水泵基础均高出建筑完成面100mm，各基础的具体做法由专业厂家深化设计，需在确定厂家后核对基础尺寸再施工。与设备相接的管道标高需按设备的进出口中心确定。\r\n" +
                "3.生活水泵{1}为调频加压泵组，由电接点压力表控制启停。\r\n" +
                "4.{2}\r\n" +
                "5.管道除有特别说明外均沿梁底敷设。\r\n" +
                "6.水泵吸水管与吸水总管的连接应采用管顶平接。\r\n" +
                "7. 水泵的运行噪声应符合现行国家标准《泵的噪声测量与评价方法》JB/T 8098中A级的规定；水泵的运行振动应符合现行国家标准《泵的振动测量与评价方法》JB/T 8097中A级的规定。\r\n" +
                "8. 二次供水泵房设施的安全防范系统应符合现行国家标准《城市供水行业反恐怖防范工作标准》及行业标准《安全防范工程技术标准》GB 50348、《安全防范系统通用图形符号》 GA/T 74 的有关规定。\r\n" +
                "9. 泵房的内墙、地面应选用符合环保要求、使用易清洁的材料铺砌或涂覆，泵房内应整洁，严禁存放易燃、易爆、易腐蚀可能造成环境污染的物品。泵房四周墙壁应铺设1.5m高白色瓷砖，1.5m及以上部分及天花板应进行隔音、吸音处理； 顶部应涂刷防水防霉的涂料或加吊浅色顶棚，地面应铺设白色防滑地砖，水箱（池）、水泵机组基础应与地面一致。\r\n" +
                "10. 门口应装设挡鼠板， 挡鼠板要求高度不低于 0.5m，并采用防潮材质， 贴警示标识。\r\n11.泵组控制由供水设备厂家二次深化。\r\n" +
                "12.泵房应设置入侵报警系统等技防、物防安全防范和监控措施。\r\n", bh, nums, s);

            return t;
        }

    }

    public class BlockMaterialLifePump{
        List<ObjectId> blkM;
        public BlockMaterialLifePump(List<ObjectId> blkM)
        {
            this.blkM = blkM;
        }

        public void setMaterial()
        {
            double totalQ = 0.0;
            for (int j = 0; j <ThLifePumpCommon.Input_PumpList.Count; j++)
            {
                totalQ += ThLifePumpCommon.Input_PumpList[j].Flow_Info;
            }

            int i = 1;
            //水泵
            for (; i <= ThLifePumpCommon.Input_PumpList.Count; i++)
            {
                string s =String.Format("泵组：Q={0}m%%1403%%141/h  单泵Q={1}m%%1403%%141/h  h={2}m  N={3}kW/台，配气压罐，控制柜，每台水泵设独立变频器", totalQ, ThLifePumpCommon.Input_PumpList[i-1].Flow_Info, ThLifePumpCommon.Input_PumpList[i - 1].Head, ThLifePumpCommon.Input_PumpList[i - 1].Power);
                string n = "";
                if (!String.IsNullOrEmpty(ThLifePumpCommon.Input_PumpList[i - 1].Note.Trim()))
                    n += "，" + ThLifePumpCommon.Input_PumpList[i - 1].Note;

                var value = new Dictionary<string, string>() { { "序号", i.ToString() }, { "设备名称", "生活水泵加压"+ getCountRefundInfoInChanese(i.ToString()) + "区" },
                    { "规格型号",s},{ "单位","台"},{ "数量",ThLifePumpCommon.Input_PumpList[i-1].Num.ToString()},{ "备注",ThLifePumpCommon.Input_PumpList[i-1].NoteSelect+n} };
                blkM[i - 1].UpdateAttributesInBlock(value);
            }

            //水箱
            var v = new Dictionary<string, string>() { { "序号", i.ToString() }, { "设备名称", ThLifePumpCommon.Input_No },
                    { "规格型号",String.Format("组合式SUS304不锈钢生活水箱，有效容积{0}m%%1403%%141/h。",ThLifePumpCommon.Input_Volume)},{ "单位","座"},{ "数量",ThLifePumpCommon.Input_Num.ToString()},{ "备注",ThLifePumpCommon.Input_Note} };
            blkM[i - 1].UpdateAttributesInBlock(v);
            i++;

            //紫外线
            var v1 = new Dictionary<string, string>() { { "序号", i.ToString() }, { "设备名称", "紫外线消毒器" },
                    { "规格型号",getDisinfection(totalQ)},{ "单位","台"},{ "数量","2"},{ "备注","消毒功能"} };
            blkM[i - 1].UpdateAttributesInBlock(v1);

            //return blkM;
        }

        public DBText getText(string text,Point3d pt)
        {
            DBText t=new DBText();
            t.Position = pt;
            t.TextString = text;
            return t;
        }
        //数字转换为中文
        private string getCountRefundInfoInChanese(string inputNum)
        {
            string[] intArr = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", };
            string[] strArr = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九", };
            string[] Chinese = { "", "十", "百", "千", "万", "十", "百", "千", "亿" };
            //金额
            //string[] Chinese = { "元", "十", "百", "千", "万", "十", "百", "千", "亿" };
            char[] tmpArr = inputNum.ToString().ToArray();
            string tmpVal = "";
            for (int i = 0; i < tmpArr.Length; i++)
            {
                tmpVal += strArr[tmpArr[i] - 48];//ASCII编码 0为48
                tmpVal += Chinese[tmpArr.Length - 1 - i];//根据对应的位数插入对应的单位
            }

            return tmpVal;
        }

        private string getDisinfection(double d)
        {
            if (d <= 25)
                return "RZ-UV2-DH25FW";
            else if(25<d&&d<=50)
                return "RZ-UV2-DH50FW";
            else if (50 < d && d <= 75)
                return "RZ-UV2-DH75FW";
            else if (75 < d && d <= 100)
                return "RZ-UV2-DH100FW";
            else if (100 < d && d <= 150)
                return "RZ-UV2-DH150FW";
            else if (150 < d && d <= 200)
                return "RZ-UV2-DH200FW";
            else if (200 < d && d <= 250)
                return "RZ-UV2-DH250FW";
            else if (250 < d && d <= 300)
                return "RZ-UV2-DH300FW";
            else if (300 < d && d <= 350)
                return "RZ-UV2-DH350FW";
            else
                return "RZ-UV2-DH400FW";
        }
    }

   
}
