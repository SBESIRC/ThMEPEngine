using System;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPWSS.Pipe.Engine
{
    public  class ThWInnerPipeIndexEngine : IDisposable
    {
        public void Dispose()
        {
        }
        public List<Point3dCollection> Fpipeindex { get; set; }
        public List<Point3dCollection> Fpipeindex_tag { get; set; }
        public List<Point3dCollection> Tpipeindex { get; set; }
        public List<Point3dCollection> Tpipeindex_tag { get; set; }
        public List<Point3dCollection> Wpipeindex { get; set; }
        public List<Point3dCollection> Wpipeindex_tag { get; set; }
        public List<Point3dCollection> Ppipeindex { get; set; }
        public List<Point3dCollection> Ppipeindex_tag { get; set; }

        public List<Point3dCollection> Dpipeindex { get; set; }
        public List<Point3dCollection> Dpipeindex_tag { get; set; }

        public List<Point3dCollection> Npipeindex { get; set; }
        public List<Point3dCollection> Npipeindex_tag { get; set; }
        public List<Point3dCollection> Rainpipeindex { get; set; }
        public List<Point3dCollection> Rainpipeindex_tag { get; set; }
        public List<Point3dCollection> RoofRainpipeindex { get; set; }
        public List<Point3dCollection> RoofRainpipeindex_tag { get; set; }
        public ThWInnerPipeIndexEngine()
        {
            Fpipeindex = new List<Point3dCollection>();
            Fpipeindex_tag = new List<Point3dCollection>();
            Tpipeindex = new List<Point3dCollection>();
            Tpipeindex_tag = new List<Point3dCollection>();
            Wpipeindex = new List<Point3dCollection>();
            Wpipeindex_tag = new List<Point3dCollection>();
            Ppipeindex = new List<Point3dCollection>();
            Ppipeindex_tag = new List<Point3dCollection>();
            Dpipeindex = new List<Point3dCollection>();
            Dpipeindex_tag = new List<Point3dCollection>();
            Npipeindex = new List<Point3dCollection>();
            Npipeindex_tag = new List<Point3dCollection>();
            Rainpipeindex = new List<Point3dCollection>();
            Rainpipeindex_tag = new List<Point3dCollection>();
            RoofRainpipeindex = new List<Point3dCollection>();
            RoofRainpipeindex_tag = new List<Point3dCollection>();
        }
        public void Run(List<Polyline> fpipe, List<Polyline> tpipe, List<Polyline> wpipe, List<Polyline> ppipe, List<Polyline> dpipe, List<Polyline> npipe, List<Polyline> rainpipe, Polyline pboundary,List<Line> divideLines, List<Polyline> roofrainpipe)
        {      
            Fpipeindex = Fpiperun(fpipe,  pboundary, divideLines);
            Fpipeindex_tag = Taggingpoint(Fpipeindex, pboundary);
            Tpipeindex = Fpiperun(tpipe, pboundary, divideLines);
            Tpipeindex_tag = Taggingpoint(Tpipeindex, pboundary);
            Wpipeindex = Fpiperun(wpipe, pboundary, divideLines);
            Wpipeindex_tag = Taggingpoint(Wpipeindex, pboundary);
            Ppipeindex = Fpiperun(ppipe, pboundary, divideLines);
            Ppipeindex_tag = Taggingpoint(Ppipeindex, pboundary);
            Dpipeindex = Fpiperun(dpipe, pboundary, divideLines);
            Dpipeindex_tag = Taggingpoint(Dpipeindex, pboundary);
            Npipeindex = Fpiperun(npipe, pboundary, divideLines);
            Npipeindex_tag = Taggingpoint(Npipeindex, pboundary);
            Rainpipeindex = Fpiperun(rainpipe, pboundary, divideLines);
            Rainpipeindex_tag = Taggingpoint(Rainpipeindex, pboundary);
            RoofRainpipeindex = Fpiperun(roofrainpipe, pboundary, divideLines);
            RoofRainpipeindex_tag = Taggingpoint(RoofRainpipeindex, pboundary);
        }
        private List<Point3dCollection> Fpiperun(List<Polyline> fpipe, Polyline pboundary,List<Line> divideLines)
        {   
            if (fpipe.Count>0)
            {
              
                var pipelist = Pipelist(fpipe);//型心几何
                var newLines = NewDivideLines(divideLines);//排序分割线          
                var pipeList = PipeGroup(pipelist, newLines);//分组
                var index = new List<Point3dCollection>();
                for (int i = 0; i < pipeList.Count; i++)
                {
                    //对各组节点重新排序
                    var pipeindex = new Point3dCollection();
                    List<int> num = new List<int>();
                    var center = Getcenter(pipeList[i]);
                    pipeindex= RightIndex(GetPositvevertices(pipeList[i], center), GetNegativevertices(pipeList[i], center), i);                                                                                                           
                    index.Add(pipeindex);
                }
                return index;
            }
            else
            {
                return new List<Point3dCollection>();
            }
        }
      
        private Point3dCollection Pipelist(List<Polyline> fpipe)//取所有立管的中心
        { var pipelist = new Point3dCollection();
            for (int i=0;i< fpipe.Count;i++)
            {         
                 pipelist.Add(fpipe[i].GetCenter()); 
           
            }
            return pipelist;
        }
        private List<Line> NewDivideLines(List<Line> divideLines)
        { 
            var newLines = new List<Line>();
            for(int i=0;i< divideLines.Count;i++)
            {
                int s = i;
                for (int j = i + 1; j < divideLines.Count; j++)
                {
                    if(divideLines[j].StartPoint.X< divideLines[s].StartPoint.X)
                    {
                        s = j;
                    }
                }
                newLines.Add(divideLines[s]);
            }
            return newLines;
        }
        private List<Point3dCollection> PipeGroup(Point3dCollection pipes, List<Line> divideLines)
        {
            var pipegroup = new List<Point3dCollection>();
            var lastCenter = new Point3dCollection();
            for (int i=0;i<divideLines.Count;i++)
            {
                var center = new Point3dCollection();
              
                if (i == 0)
                {
                    foreach (Point3d pipe in pipes)
                    {
                        if (pipe.X < divideLines[0].StartPoint.X)
                        {
                            center.Add(pipe);

                        }
                        else if(pipe.X > divideLines[0].StartPoint.X&&divideLines.Count==1)//只有一根分割线的特殊情况
                        {
                            lastCenter.Add(pipe);
                        }
                    }
                    pipegroup.Add(center);
                }
                else if (i == divideLines.Count-1)
                {
                    foreach (Point3d pipe in pipes)
                    {
                        if (pipe.X < divideLines[i].StartPoint.X&& pipe.X >divideLines[i-1].StartPoint.X)
                        {
                            center.Add(pipe);
                        }
                        else if(pipe.X >divideLines[i].StartPoint.X)
                        {
                            lastCenter.Add(pipe);
                        }
                    }
                    pipegroup.Add(center);
                }
                else
                {
                    foreach (Point3d pipe in pipes)
                    {
                        if (pipe.X < divideLines[i].StartPoint.X && pipe.X > divideLines[i - 1].StartPoint.X)
                        {
                            center.Add(pipe);

                        }
                    }
                    pipegroup.Add(center);

                }              
            }
            pipegroup.Add(lastCenter);
            return pipegroup;
        }
        private static Point3d Getcenter(Point3dCollection pipes)
        {
       
            double xvalue = 0.0;
            double yvalue = 0.0;
            foreach (Point3d pipe in pipes)
            {
                xvalue += pipe.X;
                yvalue += pipe.Y;
            }
            return new Point3d(xvalue/ pipes.Count, yvalue / pipes.Count, 0);
        }
        private static Point3dCollection GetPositvevertices(Point3dCollection pipe, Point3d pboundary)//选起点
        {
                var Pipe = new Point3dCollection();
                for (int i = 0; i < pipe.Count; i++)//取上半部距Y轴
                {
                    if (pipe[i].Y >=pboundary.Y-1)
                    {
                    Pipe.Add(pipe[i]);
                    }
                }
            return Index(Pipe);
        }
        private static Point3dCollection GetNegativevertices(Point3dCollection pipe, Point3d pboundary)//选起点
        {
            var Pipe = new Point3dCollection();
            for (int i = 0; i < pipe.Count; i++)//取下半部距Y轴
            {
                if (pipe[i].Y < pboundary.Y-1)
                {
                    Pipe.Add(pipe[i]);
                }
            }
            return Index(Pipe);
        }
        private static Point3dCollection Index(Point3dCollection pipe)//由小到大
        {
            
            for(int i=0;i< pipe.Count-1;i++)
            { 
                for(int j=i+1;j< pipe.Count;j++)
                {
                    var temp = Point3d.Origin;
                    if (pipe[i].X >pipe[j].X)
                    {
                        temp = pipe[i];
                        pipe[i] = pipe[j];
                        pipe[j] = temp;
                    }                  
                }
            }
            return pipe;
        }
        private static Point3dCollection RightIndex(Point3dCollection pipe, Point3dCollection pipe1,int num)
        {
            var pipes = new Point3dCollection();
            if (num%2==0)
            {//顺时针排序
                for(int i=0;i< pipe.Count;i++)
                {
                    pipes.Add(pipe[i]);
                }
                for(int i = pipe1.Count-1; i >=0; i--)
                {
                    pipes.Add(pipe1[i]);
                }
            }
           else
            {//逆时针排序
                for (int i = pipe.Count - 1; i >=0; i--)
                {
                    pipes.Add(pipe[i]);
                }
                for (int i = 0; i < pipe1.Count; i++)
                {
                    pipes.Add(pipe1[i]);
                }
            }
            return pipes;
        }

        private List<Point3dCollection> Taggingpoint(List<Point3dCollection> Fpipeindexs, Polyline pboundary)
        {
            //避障，目前只考虑了边界
            var taggingpoints=new List<Point3dCollection>();
            foreach (Point3dCollection Fpipeindex in Fpipeindexs)
            {
                var taggingpoint = new Point3dCollection();
                for (int i = 0; i < Fpipeindex.Count; i++)
                {
                    var pts1 = new Point3dCollection();
                    var pts2 = new Point3dCollection();
                    var pts3 = new Point3dCollection();
                    var pts4 = new Point3dCollection();
                    Point3d upper = new Point3d(Fpipeindex[i].X, Fpipeindex[i].Y + 1260, 0);
                    Point3d upper1 = new Point3d(1, Fpipeindex[i].Y + 1260, 0);
                    Line linup = new Line(upper, upper1);
                    pboundary.IntersectWith(linup, Intersect.ExtendArgument, pts1, (IntPtr)0, (IntPtr)0);
                    Point3d lower = new Point3d(Fpipeindex[i].X, Fpipeindex[i].Y - 1260, 0);
                    Point3d lower1 = new Point3d(1, Fpipeindex[i].Y - 1260, 0);
                    Line lindown = new Line(lower, lower1);
                    pboundary.IntersectWith(lindown, Intersect.ExtendArgument, pts2, (IntPtr)0, (IntPtr)0);
                    Point3d lefter = new Point3d(Fpipeindex[i].X - 540, Fpipeindex[i].Y, 0);
                    Point3d lefter1 = new Point3d(Fpipeindex[i].X - 540, 1, 0);
                    Line linleft = new Line(lefter, lefter1);
                    pboundary.IntersectWith(linleft, Intersect.ExtendArgument, pts3, (IntPtr)0, (IntPtr)0);
                    Point3d righter = new Point3d(Fpipeindex[i].X + 540, Fpipeindex[i].Y, 0);
                    Point3d righter1 = new Point3d(Fpipeindex[i].X + 540, 1, 0);
                    Line linright = new Line(righter, righter1);
                    pboundary.IntersectWith(linright, Intersect.ExtendArgument, pts4, (IntPtr)0, (IntPtr)0);
                    if (pts1.Count == 0)
                    {
                        taggingpoint.Add(upper);
                        Point3d upper_1 = new Point3d(upper.X + 800, upper.Y, 0);
                        Point3d upper_2 = new Point3d(upper.X + 35, upper.Y + 35, 0);
                        taggingpoint.Add(upper_1);
                        taggingpoint.Add(upper_2);
                    }
                    else if (pts2.Count == 0)
                    {
                        taggingpoint.Add(lower);
                        Point3d down_1 = new Point3d(lower.X + 800, lower.Y, 0);
                        Point3d down_2 = new Point3d(lower.X + 35, lower.Y + 35, 0);
                        taggingpoint.Add(down_1);
                        taggingpoint.Add(down_2);
                    }
                    else if (pts3.Count == 0)
                    {
                        Point3d left_1 = new Point3d(Fpipeindex[i].X - 540, Fpipeindex[i].Y + 1260, 0);
                        Point3d left_2 = new Point3d(Fpipeindex[i].X - 1340, Fpipeindex[i].Y + 1260, 0);
                        Point3d left_3 = new Point3d(Fpipeindex[i].X - 505, Fpipeindex[i].Y + 1295, 0);
                        taggingpoint.Add(left_1);
                        taggingpoint.Add(left_2);
                        taggingpoint.Add(left_3);
                    }
                    else
                    {
                        Point3d right_1 = new Point3d(Fpipeindex[i].X + 540, Fpipeindex[i].Y + 1260, 0);
                        Point3d right_2 = new Point3d(Fpipeindex[i].X + 1340, Fpipeindex[i].Y + 1260, 0);
                        Point3d right_3 = new Point3d(Fpipeindex[i].X + 575, Fpipeindex[i].Y + 1295, 0);
                        taggingpoint.Add(right_1);
                        taggingpoint.Add(right_2);
                        taggingpoint.Add(right_3);
                    }

                }
                taggingpoints.Add(taggingpoint);
            }
            return taggingpoints;
        }
    }
}
