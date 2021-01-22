using System;
using ThCADCore.NTS;
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
        public void Run(List<Polyline> fpipe, List<Polyline> tpipe, List<Polyline> wpipe, List<Polyline> ppipe, List<Polyline> dpipe, List<Polyline> npipe, List<Polyline> rainpipe, Polyline pboundary,List<Line> divideLines, List<Polyline> roofrainpipe,Point3d toiletPoint,Point3d balconyPoint, ThCADCoreNTSSpatialIndex obstacle)
        {
            if (fpipe.Count > 0)
            {
                Fpipeindex = Fpiperun(fpipe, pboundary, divideLines, toiletPoint, balconyPoint);
                Fpipeindex_tag = Taggingpoint(Fpipeindex, pboundary, obstacle,0);
            }
            if (tpipe.Count > 0)
            {
                Tpipeindex = Fpiperun(tpipe, pboundary, divideLines, toiletPoint, balconyPoint);
                Tpipeindex_tag = Taggingpoint(Tpipeindex, pboundary, obstacle,1);
            }
            if (wpipe.Count > 0)
            {
                Wpipeindex = Fpiperun(wpipe, pboundary, divideLines, toiletPoint, balconyPoint);
                Wpipeindex_tag = Taggingpoint(Wpipeindex, pboundary, obstacle,2);
            }
            if (ppipe.Count > 0)
            {
                Ppipeindex = Fpiperun(ppipe, pboundary, divideLines, toiletPoint, balconyPoint);
                Ppipeindex_tag = Taggingpoint(Ppipeindex, pboundary, obstacle,3);
            }
            if (dpipe.Count > 0)
            {
                Dpipeindex = Fpiperun(dpipe, pboundary, divideLines, toiletPoint, balconyPoint);
                Dpipeindex_tag = Taggingpoint(Dpipeindex, pboundary, obstacle,4);
            }
            if (npipe.Count > 0)
            {
                Npipeindex = Fpiperun(npipe, pboundary, divideLines, toiletPoint, balconyPoint);
                Npipeindex_tag = Taggingpoint(Npipeindex, pboundary, obstacle,5);
            }
            if (rainpipe.Count > 0)
            {
                Rainpipeindex = Fpiperun(rainpipe, pboundary, divideLines, toiletPoint, balconyPoint);
                Rainpipeindex_tag = Taggingpoint(Rainpipeindex, pboundary, obstacle,6);
            }
            if (roofrainpipe.Count > 0)
            {
                RoofRainpipeindex = Fpiperun(roofrainpipe, pboundary, divideLines, toiletPoint, balconyPoint);
                RoofRainpipeindex_tag = Taggingpoint(RoofRainpipeindex, pboundary, obstacle,7);
            }
        }
        private List<Point3dCollection> Fpiperun(List<Polyline> fpipe, Polyline pboundary, List<Line> divideLines, Point3d toiletPoint,Point3d balconyPoint)
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
                    var center = Getcenter(pipeList[i], pboundary, toiletPoint, balconyPoint);
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
        private static Point3d Getcenter(Point3dCollection pipes,Polyline pboundary,Point3d topoint,Point3d balpoint)
        {
            for (int i=1;i< pipes.Count;i++)
            {
                if ((pipes[0].Y - pboundary.GetCenter().Y) * (pipes[i].Y - pboundary.GetCenter().Y) < 0)
                {
                    return pboundary.GetCenter();
                }
            }
            if(Math.Abs(pipes[0].Y- balpoint.Y)< ThWPipeCommon.MAX_TAG_YPOSITION)//此处可借用，一般会小于500
            {
                return balpoint;
            }
            else
            {
                return topoint;
            }
       
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
                    else if(pipe[i].X== pipe[j].X)//横坐标相同，按纵坐标从大到小排列
                    {
                        if(pipe[i].Y<pipe[j].Y)
                        {
                            temp = pipe[i];
                            pipe[i] = pipe[j];
                            pipe[j] = temp;
                        }
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

        private List<Point3dCollection> Taggingpoint(List<Point3dCollection> Fpipeindexs, Polyline pboundary, ThCADCoreNTSSpatialIndex obstacle,int index)
        {
            //得到字宽
            double width = GetFrontWidth(index)*175;
            var taggingpoints=new List<Point3dCollection>();
            foreach (Point3dCollection Fpipeindex in Fpipeindexs)
            {
                var taggingpoint = new Point3dCollection();
                for (int i = 0; i < Fpipeindex.Count; i++)
                {  
                    var points= new Point3dCollection();
                    List<Vector3d> normals = new List<Vector3d>();
                    for (int j = 0; j < 12; j++)
                    {
                        Point3d point = new Point3d(Fpipeindex[i].X, Fpipeindex[i].Y - 1, 0);                   
                        Vector3d normal = Fpipeindex[i].GetVectorTo(point).GetNormal().RotateBy(j*Math.PI/6, Vector3d.ZAxis);
                        normals.Add(normal);
                    }
                    List<int> nums = Renum();
                    points = (GetCircularPoint(nums, Fpipeindex[i], width, normals, obstacle));
                    foreach(Point3d point in points)
                    {
                        taggingpoint.Add(point);
                    }                                     
                }
                taggingpoints.Add(taggingpoint);
            }
            return taggingpoints;
        }
        private static int GetFrontWidth(int num)
        {
            int width = 0;
            if(num<6)
            {
                width = 6;
            }
            else
            {
                width = 7;
            }
            return width;
        }
        private static Polyline GetBoundary(double width, Point3d point)
        {      
            Polyline polyline = new Polyline()
            {
                Closed = true
            };
            polyline.AddVertexAt(0, new Point2d(point.X, point.Y), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(1, new Point2d(point.X+width, point.Y), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(2, new Point2d(point.X + width, point.Y+175), 0.0, 0.0, 0.0);
            polyline.AddVertexAt(3, new Point2d(point.X, point.Y + 175), 0.0, 0.0, 0.0);                   
            return polyline;
        }
        private static Point3d GetRadialPoint(Point3d Fpipeindex,double width,Vector3d normal, ThCADCoreNTSSpatialIndex obstacle)
        {
            Point3d point = Point3d.Origin;
            for (int j = 0; j <=6; j++)
            {
                Point3d point1 = Fpipeindex + normal * 250 * (j + 2);
                if(normal.X<0)
                { width = -1 * width; }//是否在y轴左侧
                var fontBox = obstacle.SelectCrossingPolygon(GetBoundary(width, point1));
                if (fontBox.Count > 0)
                {
                    continue;
                }
                else
                {
                    point= point1;
                    break;
                }
            }
            return point;
        }
        public static List<int> Renum()
        {
            var nums = new List<int>();
            nums.Add(0);
            nums.Add(6);
            for(int i=1;i<3;i++)
            {
                nums.Add(i);
            }
            for (int i = 11; i > 9; i--)
            {
                nums.Add(i);
            }
            for (int i = 4; i < 6; i++)
            {
                nums.Add(i);
            }         
            for (int i = 8; i > 6; i--)
            {
                nums.Add(i);
            }
            return nums;
        }
        public static Point3dCollection GetCircularPoint(List<int> nums,Point3d Fpipeindex, double width, List<Vector3d> normals, ThCADCoreNTSSpatialIndex obstacle)
        {
            Point3dCollection points = new Point3dCollection();
            for (int i = 0; i < nums.Count; i++)
            {
                if (GetRadialPoint(Fpipeindex, width, normals[nums[i]], obstacle) == Point3d.Origin)
                {                                             
                }
                else
                {
                    var point=GetRadialPoint(Fpipeindex, width, normals[nums[i]], obstacle);
                    points = GetPoints(point,i);//得到标注点组
                    break;
                }
            }
            return points;
        }
        public static Point3dCollection GetPoints(Point3d point,int num)
        {
            Point3dCollection taggingpoints = new Point3dCollection();
            taggingpoints.Add(point);
            if (num == 0||(num>=1&&num<5)||(num >=6 && num <11|| num == 1))//y轴右侧
            {
                taggingpoints.Add(new Point3d(point.X + 800, point.Y, 0));
                taggingpoints.Add(new Point3d(point.X + 35, point.Y+35, 0));     
           
            }
            else
            {       
                taggingpoints.Add(new Point3d(point.X -800, point.Y, 0));
                taggingpoints.Add(new Point3d(point.X - 765, point.Y+35, 0));
            }       
            return taggingpoints;
        }
    }
}
