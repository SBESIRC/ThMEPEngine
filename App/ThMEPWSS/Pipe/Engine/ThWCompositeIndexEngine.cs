using System;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPWSS.Pipe.Model;
using System.Collections.Generic;

namespace ThMEPWSS.Pipe.Engine
{
    public class ThWCompositeIndexEngine : IDisposable
    {
        public ThWInnerPipeIndexEngine PipeEngine { get; set; }
        public List<List<Point3dCollection>> FpipeDublicated { get; set; }


        public void Dispose()
        {
        }
        public ThWCompositeIndexEngine(ThWInnerPipeIndexEngine pipeEngine)
        {
            PipeEngine = pipeEngine;
            FpipeDublicated = new List<List<Point3dCollection>>();
        }

        public void Run(List<Polyline> fpipe, List<Polyline> tpipe, List<Polyline> wpipe, List<Polyline> ppipe, List<Polyline> dpipe, List<Polyline> npipe, List<Polyline> rainpipe, Polyline pboundary, List<Line> divideLines, List<Polyline> roofrainpipe)
        {
            PipeEngine.Run(fpipe,tpipe, wpipe, ppipe,dpipe, npipe,rainpipe,pboundary,divideLines, roofrainpipe);
            FpipeDublicated = GetDublicated(PipeEngine.Fpipeindex, PipeEngine.Tpipeindex, PipeEngine.Wpipeindex, PipeEngine.Ppipeindex, PipeEngine.Dpipeindex, PipeEngine.Npipeindex, PipeEngine.Rainpipeindex, PipeEngine.RoofRainpipeindex) ;
           
        }
        private static List<List<Point3dCollection>> GetDublicated(List<Point3dCollection> pipe1,List<Point3dCollection> pipe2, List<Point3dCollection> pipe3,List<Point3dCollection> pipe4,  List<Point3dCollection> pipe5, List<Point3dCollection> pipe6, List<Point3dCollection> pipe7, List<Point3dCollection> pipe8)
        {
            var result = new List<List<Point3dCollection>>();
            for (int i=0;i< pipe1.Count; i++)
            { var column = new Point3dCollection();
                //添加同一区域的所有管子
                if (pipe1.Count > 0)
                {
                    foreach (Point3d pipe in pipe1[i])
                    {
                        column.Add(pipe);
                    }
                }
                if (pipe2.Count > 0)
                {
                    foreach (Point3d pipe in pipe2[i])
                    {
                        column.Add(pipe);
                    }
                }
                if (pipe3.Count > 0)
                {
                    foreach (Point3d pipe in pipe3[i])
                    {
                        column.Add(pipe);
                    }
                }
                if (pipe4.Count > 0)
                {
                    foreach (Point3d pipe in pipe4[i])
                    {
                        column.Add(pipe);
                    }
                }
                if (pipe5.Count > 0)
                {
                    foreach (Point3d pipe in pipe5[i])
                    {
                        column.Add(pipe);
                    }
                }
                if (pipe6.Count > 0)
                {
                    foreach (Point3d pipe in pipe6[i])
                    {
                        column.Add(pipe);
                    }
                }
                if (pipe7.Count > 0)
                {
                    foreach (Point3d pipe in pipe7[i])
                    {
                        column.Add(pipe);
                    }
                }
                if (pipe8.Count > 0)
                {
                    foreach (Point3d pipe in pipe8[i])
                    {
                        column.Add(pipe);
                    }
                }
                result.Add(Getorder(column));//区间组合
            }
            return result;
        }
        private static List<Point3dCollection> Getorder(Point3dCollection pipe)
        {//
            var columns = new List<Point3dCollection>();
            for (int i = 0; i < pipe.Count-1; i++)
            {
                var column = new Point3dCollection();
                column.Add(pipe[i]);
                if (IfNum(pipe, i))
                {                
                    var s=new List<int>();
                    s.Add(i);
                    for (int j = i + 1; j < pipe.Count; j++)
                    {                      
                        if (pipe[j].X == pipe[i].X)
                        {
                            if (IsTrue(s, pipe[j],pipe))
                            {
                                column.Add(pipe[j]);
                                s.Add(j);
                            }
                        }                      
                    }
                }
                columns.Add(GetSort(column));//排序子元素
            }
            return columns;
        }
        private static bool IfNum(Point3dCollection pipe,int num)//判断是否横坐标重合
        {
            if(num==0)
            {
                return true;
            }
            else
            {
                for(int i=0;i< num;i++)
                {
                    if(pipe[num]== pipe[i])
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        private static bool IsTrue(List<int> s,Point3d num,Point3dCollection pipe)//判断是否沿Y轴正负500mm
        {
            foreach(var value in s)
            {
                if (Math.Abs(pipe[value].Y - num.Y) < 500)
                {
                    return true;
                }              
            }
            return false;
        }
        private static Point3dCollection GetSort(Point3dCollection pipes)
        {
            Point3d temp;
            for (int i=0;i< pipes.Count-1;i++)
            {             
                for (int j=i+1;j< pipes.Count;j++)
                {                  
                    if (pipes[i].Y> pipes[j].Y )
                    {
                        temp = pipes[i];
                        pipes[i] = pipes[j];
                        pipes[j] = temp;
                    }
                }
            }
            return pipes;
        }
    }
}
