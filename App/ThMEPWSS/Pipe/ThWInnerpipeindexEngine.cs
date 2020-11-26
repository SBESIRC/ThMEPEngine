using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Dreambuild.AutoCAD;


namespace ThMEPWSS.Pipe
{
   public  class ThWInnerPipeIndexEngine : IDisposable
    {
        public void Dispose()
        {
        }
        public Point3dCollection Fpipeindex { get; set; }
        public Point3dCollection Fpipeindex_tag { get; set; }
        public Point3dCollection Tpipeindex { get; set; }
        public Point3dCollection Tpipeindex_tag { get; set; }
        public Point3dCollection Wpipeindex { get; set; }
        public Point3dCollection Wpipeindex_tag { get; set; }
        public Point3dCollection Ppipeindex { get; set; }
        public Point3dCollection Ppipeindex_tag { get; set; }

        public Point3dCollection Dpipeindex { get; set; }
        public Point3dCollection Dpipeindex_tag { get; set; }

        public Point3dCollection Npipeindex { get; set; }
        public Point3dCollection Npipeindex_tag { get; set; }
        public Point3dCollection Rainpipeindex { get; set; }
        public Point3dCollection Rainpipeindex_tag { get; set; }

        public ThWInnerPipeIndexEngine()
        {
            Fpipeindex = new Point3dCollection();
            Fpipeindex_tag = new Point3dCollection();
            Tpipeindex = new Point3dCollection();
            Tpipeindex_tag = new Point3dCollection();
            Wpipeindex = new Point3dCollection();
            Wpipeindex_tag = new Point3dCollection();
            Ppipeindex = new Point3dCollection();
            Ppipeindex_tag = new Point3dCollection();
            Dpipeindex = new Point3dCollection();
            Dpipeindex_tag = new Point3dCollection();
            Npipeindex = new Point3dCollection();
            Npipeindex_tag = new Point3dCollection();
            Rainpipeindex = new Point3dCollection();
            Rainpipeindex_tag = new Point3dCollection();
        }
        public void Run(List<Polyline> fpipe, List<Polyline> tpipe, List<Polyline> wpipe, List<Polyline> ppipe, List<Polyline> dpipe, List<Polyline> npipe, List<Polyline> rainpipe, Polyline pboundary)
        {
            Fpipeindex = Fpiperun(fpipe,  pboundary);
            Fpipeindex_tag = Taggingpoint(Fpipeindex, pboundary);
            Tpipeindex = Tpiperun(tpipe, pboundary);
            Tpipeindex_tag = Taggingpoint(Tpipeindex, pboundary);
            Wpipeindex = Wpiperun(wpipe, pboundary);
            Wpipeindex_tag = Taggingpoint(Wpipeindex, pboundary);
            Ppipeindex = Ppiperun(ppipe, pboundary);
            Ppipeindex_tag = Taggingpoint(Ppipeindex, pboundary);
            Dpipeindex = Dpiperun(dpipe, pboundary);
            Dpipeindex_tag = Taggingpoint(Dpipeindex, pboundary);
            Npipeindex = Npiperun(npipe, pboundary);
            Npipeindex_tag = Taggingpoint(Npipeindex, pboundary);
            Rainpipeindex = Rpiperun(rainpipe, pboundary);
            Rainpipeindex_tag = Taggingpoint(Rainpipeindex, pboundary);
        }
        private Point3dCollection Fpiperun(List<Polyline> fpipe, Polyline pboundary)
        {
            var pipeindex = new Point3dCollection();
            var pipelist = Pipelist(fpipe);//型心几何
            pipeindex.Add(pipelist[Getvertices(pipelist, pboundary)]);//加入起点
            List<int> num = new List<int>();
            num.Add(Getvertices(pipelist, pboundary));//加入起点序号
            for (int i=0;i< fpipe.Count-1;i++)
            {   
                if (i > 0)
                {
                    pipeindex.Add(pipelist[Index(num[0],num[i - 1], pipeindex[i], pipelist, pboundary)]);
                    num.Add(Index(num[0],num[i - 1], pipeindex[i], pipelist, pboundary));
                }
                else
                { 
                    pipeindex.Add(pipelist[Index(num[0],num[0],pipeindex[i], pipelist, pboundary)]);
                    num.Add(Index(num[0],num[0], pipeindex[i], pipelist, pboundary));
                }
            }
            return pipeindex;
        }
        private Point3dCollection Tpiperun(List<Polyline> tpipe, Polyline pboundary)
        {
            var pipelist = Pipelist(tpipe);
            Fpipeindex.Add(pipelist[Getvertices(pipelist, pboundary)]);
            List<int> num = new List<int>();
            num.Add(Getvertices(pipelist, pboundary));
            for (int i = 0; i < tpipe.Count - 1; i++)
            {
                if (i > 0)
                {
                    Tpipeindex.Add(pipelist[Index(num[0], num[i - 1], Tpipeindex[i], pipelist, pboundary)]);
                    num.Add(Index(num[0], num[i - 1], Tpipeindex[i], pipelist, pboundary));
                }
                else
                {
                    Tpipeindex.Add(pipelist[Index(num[0], num[0], Tpipeindex[i], pipelist, pboundary)]);
                    num.Add(Index(num[0], num[0], Tpipeindex[i], pipelist, pboundary));
                }
            }
            return Tpipeindex;
        }
        private Point3dCollection Wpiperun(List<Polyline> wpipe, Polyline pboundary)
        {
            var pipelist = Pipelist(wpipe);
            Wpipeindex.Add(pipelist[Getvertices(pipelist, pboundary)]);
            List<int> num = new List<int>();
            num.Add(Getvertices(pipelist, pboundary));
            for (int i = 0; i < wpipe.Count - 1; i++)
            {
                if (i > 0)
                {
                    Wpipeindex.Add(pipelist[Index(num[0], num[i - 1], Wpipeindex[i], pipelist, pboundary)]);
                    num.Add(Index(num[0], num[i - 1], Wpipeindex[i], pipelist, pboundary));
                }
                else
                {
                    Wpipeindex.Add(pipelist[Index(num[0], num[0], Wpipeindex[i], pipelist, pboundary)]);
                    num.Add(Index(num[0], num[0], Wpipeindex[i], pipelist, pboundary));
                }
            }
            return Wpipeindex;
        }
        private Point3dCollection Ppiperun(List<Polyline> ppipe, Polyline pboundary)
        {
            var pipelist = Pipelist(ppipe);
            Ppipeindex.Add(pipelist[Getvertices(pipelist, pboundary)]);
            List<int> num = new List<int>();
            num.Add(Getvertices(pipelist, pboundary));
            for (int i = 0; i < ppipe.Count - 1; i++)
            {
                if (i > 0)
                {
                    Ppipeindex.Add(pipelist[Index(num[0], num[i - 1], Ppipeindex[i], pipelist, pboundary)]);
                    num.Add(Index(num[0], num[i - 1], Ppipeindex[i], pipelist, pboundary));
                }
                else
                {
                    Ppipeindex.Add(pipelist[Index(num[0], num[0], Ppipeindex[i], pipelist, pboundary)]);
                    num.Add(Index(num[0], num[0], Ppipeindex[i], pipelist, pboundary));
                }
            }
            return Ppipeindex;
        }
        private Point3dCollection Dpiperun(List<Polyline> dpipe, Polyline pboundary)
        {
            var pipelist = Pipelist(dpipe);
            Dpipeindex.Add(pipelist[Getvertices(pipelist, pboundary)]);
            List<int> num = new List<int>();
            num.Add(Getvertices(pipelist, pboundary));
            for (int i = 0; i < dpipe.Count - 1; i++)
            {
                if (i > 0)
                {
                    Dpipeindex.Add(pipelist[Index(num[0], num[i - 1], Dpipeindex[i], pipelist, pboundary)]);
                    num.Add(Index(num[0], num[i - 1], Dpipeindex[i], pipelist, pboundary));
                }
                else
                {
                    Dpipeindex.Add(pipelist[Index(num[0], num[0], Dpipeindex[i], pipelist, pboundary)]);
                    num.Add(Index(num[0], num[0], Dpipeindex[i], pipelist, pboundary));
                }
            }
            return Dpipeindex;
        }
        private Point3dCollection Npiperun(List<Polyline> npipe, Polyline pboundary)
        {
            var pipelist = Pipelist(npipe);
            Npipeindex.Add(pipelist[Getvertices(pipelist, pboundary)]);
            List<int> num = new List<int>();
            num.Add(Getvertices(pipelist, pboundary));
            for (int i = 0; i < npipe.Count - 1; i++)
            {
                if (i > 0)
                {
                    Npipeindex.Add(pipelist[Index(num[0], num[i - 1], Npipeindex[i], pipelist, pboundary)]);
                    num.Add(Index(num[0], num[i - 1], Npipeindex[i], pipelist, pboundary));
                }
                else
                {
                    Npipeindex.Add(pipelist[Index(num[0], num[0], Npipeindex[i], pipelist, pboundary)]);
                    num.Add(Index(num[0], num[0], Npipeindex[i], pipelist, pboundary));
                }
            }
            return Npipeindex;
        }
        private Point3dCollection Rpiperun(List<Polyline> rainpipe, Polyline pboundary)
        {
            var pipelist = Pipelist(rainpipe);
            Rainpipeindex.Add(pipelist[Getvertices(pipelist, pboundary)]);
            List<int> num = new List<int>();
            num.Add(Getvertices(pipelist, pboundary));
            for (int i = 0; i < rainpipe.Count - 1; i++)
            {
                if (i > 0)
                {
                    Rainpipeindex.Add(pipelist[Index(num[0], num[i - 1], Rainpipeindex[i], pipelist, pboundary)]);
                    num.Add(Index(num[0], num[i - 1], Rainpipeindex[i], pipelist, pboundary));
                }
                else
                {
                    Rainpipeindex.Add(pipelist[Index(num[0], num[0], Rainpipeindex[i], pipelist, pboundary)]);
                    num.Add(Index(num[0], num[0], Rainpipeindex[i], pipelist, pboundary));
                }
            }
            return Rainpipeindex;
        }
        private Point3dCollection Pipelist(List<Polyline> fpipe)//取所有立管的中心
        { var pipelist = new Point3dCollection();
            for(int i=0;i< fpipe.Count;i++)
            {
                pipelist.Add(fpipe[i].GetCenter());
            }
            return pipelist;
        }

        private static int Getvertices(Point3dCollection pipe, Polyline pboundary)//选起点
        {     
            var point = new Point3d(pboundary.GetCenter().X,double.MaxValue,0); 
            Line line = new Line(pboundary.GetCenter(),point); //中心Y轴   
            double dst = 0.0;
            int num = 0;
            for (int i=0;i< pipe.Count;i++)//取上半部距Y轴最远
            {
                if (pipe[i].Y > pboundary.GetCenter().Y)
                {
                    if (dst < line.GetDistToPoint(pipe[i]))
                    {
                        dst = line.GetDistToPoint(pipe[i]);
                        num = i;
                    }
                }
            }
            return num;
        }
        private  int Index(int num_0,int num,Point3d pipevertivces, Point3dCollection pipe, Polyline pboundary)
        {

            double dst = double.MaxValue;
            int num1 = 0;
           
            for (int i=0;i<pipe.Count;i++)
            {  if (num != num_0)
                {
                    if (i != num)
                    {
                        if ((dst > pipe[i].DistanceTo(pipevertivces)) && (pipe[i].DistanceTo(pipevertivces)) > 0)
                        {
                            dst = pipe[i].DistanceTo(pipevertivces);
                            num1 = i;
                        }
                    }

                }
            else
                {
                    Point3d pipevertivces_line = new Point3d(pboundary.GetCenter().X, pipevertivces.Y,0);
                    Point3d pipevertivces_i = new Point3d(pipe[i].X, pipevertivces.Y, 0);
                    if (i != num&&(pipevertivces.GetVectorTo(pipevertivces_line).IsCodirectionalTo(pipevertivces.GetVectorTo(pipevertivces_i))))
                    {
                        if ((dst > pipe[i].DistanceTo(pipevertivces)) && (pipe[i].DistanceTo(pipevertivces)) > 0)
                        {
                            dst = pipe[i].DistanceTo(pipevertivces);
                            num1 = i;
                        }
                    }

                }

            }
            return num1;
        }
        private Point3dCollection Taggingpoint(Point3dCollection Fpipeindex, Polyline pboundary)
        {             
            var taggingpoint = new Point3dCollection();
            for (int i=0;i< Fpipeindex.Count;i++)
            {          
                var pts1 = new Point3dCollection();
                var pts2 = new Point3dCollection();
                var pts3 = new Point3dCollection();
                var pts4 = new Point3dCollection();
                Point3d upper = new Point3d(Fpipeindex[i].X, Fpipeindex[i].Y + 1260, 0);
                Point3d upper1 = new Point3d(1, Fpipeindex[i].Y + 1260, 0);
                Line linup = new Line(upper, upper1);
                pboundary.IntersectWith(linup, Intersect.ExtendArgument, pts1, (IntPtr)0, (IntPtr)0);
                Point3d lower = new Point3d(Fpipeindex[i].X, Fpipeindex[i].Y-1260, 0);
                Point3d lower1 = new Point3d(1, Fpipeindex[i].Y- 1260, 0);
                Line lindown = new Line(lower, lower1);
                pboundary.IntersectWith(lindown, Intersect.ExtendArgument, pts2, (IntPtr)0, (IntPtr)0);
                Point3d lefter = new Point3d(Fpipeindex[i].X- 540, Fpipeindex[i].Y, 0);
                Point3d lefter1 = new Point3d(Fpipeindex[i].X - 540, 1, 0);
                Line linleft = new Line(lefter, lefter1);
                pboundary.IntersectWith(linleft, Intersect.ExtendArgument, pts3, (IntPtr)0, (IntPtr)0);
                Point3d righter = new Point3d(Fpipeindex[i].X + 540, Fpipeindex[i].Y, 0);
                Point3d righter1 = new Point3d(Fpipeindex[i].X + 540, 1, 0);
                Line linright = new Line(righter, righter1);
                pboundary.IntersectWith(linright, Intersect.ExtendArgument, pts4, (IntPtr)0, (IntPtr)0);             
                if(pts1.Count==0)
                    {
                        taggingpoint.Add(upper);
                        Point3d upper_1 = new Point3d(upper.X + 800, upper.Y, 0);
                        Point3d upper_2 = new Point3d(upper.X + 35, upper.Y+35, 0);
                        taggingpoint.Add(upper_1);
                        taggingpoint.Add(upper_2 );
                     }
                else if (pts2.Count == 0)
                    {
                        taggingpoint.Add(lower);
                        Point3d down_1 = new Point3d(lower.X + 800, lower.Y, 0);
                        Point3d down_2 = new Point3d(lower.X + 35, lower.Y + 35, 0);
                        taggingpoint.Add(down_1);
                        taggingpoint.Add(down_2);
                    }
                else if(pts3.Count == 0)
                    {
                        Point3d left_1 = new Point3d(Fpipeindex[i].X - 540, Fpipeindex[i].Y+1260, 0);
                        Point3d left_2 = new Point3d(Fpipeindex[i].X - 1340, Fpipeindex[i].Y + 1260, 0);
                        Point3d left_3 = new Point3d(Fpipeindex[i].X -505, Fpipeindex[i].Y + 1295, 0);
                        taggingpoint.Add(left_1);
                        taggingpoint.Add(left_2);
                        taggingpoint.Add(left_3);
                     }
                else
                    {
                        Point3d right_1 = new Point3d(Fpipeindex[i].X +540, Fpipeindex[i].Y + 1260, 0);
                        Point3d right_2 = new Point3d(Fpipeindex[i].X +1340, Fpipeindex[i].Y + 1260, 0);
                        Point3d right_3 = new Point3d(Fpipeindex[i].X + 575, Fpipeindex[i].Y + 1295, 0);
                        taggingpoint.Add(right_1);
                        taggingpoint.Add(right_2);
                        taggingpoint.Add(right_3);
                    }
               
            }
            return taggingpoint;
        }
    }
}
