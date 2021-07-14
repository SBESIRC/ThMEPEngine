using System;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.CAD;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.Service
{
    public class ThBuildDoorService
    {
        public List<Polyline> Outlines { get; private set; }
        public double TesslateLength { get; set; }
        private Dictionary<DBText, List<Polyline>> DoorMarkStones { get; set; }
        private double Interval { get; set; }
        private double MaxDoorWidth { get; set; }
        public ThBuildDoorService()
        {        
            MaxDoorWidth = 300;
            TesslateLength = 1000;
            Outlines = new List<Polyline>();
            Interval = ThMEPEngineCoreCommon.DoorStoneInterval; //门垛与邻居的间隔，用于搜索相邻的元素
        }
        
        public void Build(List<Tuple<Entity, List<Polyline>, double>> doorMarkStones)
        {
            // 门标注，对应两个门垛，门长度
            foreach(var group in doorMarkStones)
            {
                if (group.Item2.Count == 2)
                {
                    Outlines.Add(BuildDoor(group.Item2[0], group.Item2[1], group.Item3));
                }
            }
            Outlines = Outlines.Where(o => o.Area > 0.0).ToList();
        }
        private Polyline BuildDoor(Polyline firstStone,Polyline secondStone, double length)
        {
            //检查两个门垛的两端是否与Length在指定容差内
            if(!IsValid(firstStone, secondStone, length))
            {
                return new Polyline();
            }
            //获取门垛的邻居
            var firstNeibourService = new ThFindDoorstoneNeighborService();
            firstNeibourService.Find(firstStone);

            var secondNeibourService = new ThFindDoorstoneNeighborService();
            secondNeibourService.Find(secondStone);

            if(firstNeibourService.Neighbor.Area==0 || secondNeibourService.Neighbor.Area == 0)
            {
                //throw new Exception("门垛未能找到邻居，无法构建门。");
                return new Polyline();
            }

            //找到门垛与相邻柱或墙的匹配边
            var firstPairs = FindPairEdges(firstStone, firstNeibourService.Neighbor);
            var secondPairs = FindPairEdges(secondStone, secondNeibourService.Neighbor);

            //找到适合创建门的两边
            var doorPairs = new List<Tuple<Line, Line,Line,Line>>(); //左边门垛线，左边相邻元素边线,右边门垛线，右边相邻元素边线
            for (int i=0;i< firstPairs.Count;i++)
            {
                for (int j = 0; j < secondPairs.Count; j++)
                {
                    if(IsValid(firstPairs[i].Item1, secondPairs[j].Item1,length))
                    {
                        doorPairs.Add(Tuple.Create(
                            firstPairs[i].Item1,
                            firstPairs[i].Item2, 
                            secondPairs[j].Item1,
                            secondPairs[j].Item2));
                    }                    
                }
            }

            // 创建门
            var doorOutlines = CreateDoor(doorPairs);
            doorOutlines = doorOutlines.OrderBy(o => Math.Abs(o.Length - length)).ToList(); //与给出的门长度，差值最接近的
            return doorOutlines.Count > 0 ? doorOutlines.First():new Polyline();
        }

        private List<Polyline> CreateDoor(
            List<Tuple<Line, Line, Line, Line>> doorPairs)
        {
            // 左右是相对的
            var results = new List<Polyline>();
            doorPairs.ForEach(o =>
            {
                //用矩形框框住门垛的两根线
                var poly = ToOBB(o.Item1, o.Item3);
                var width = GetWidth(Math.Max(o.Item1.Length, o.Item3.Length),
                    o.Item2.Length, o.Item4.Length);
                results.Add(BufferCenter(poly, width));
            });
            return results;
        }
        private double GetWidth(double doorStoneWidth,double neibour1Width,double neibour2Width)
        {
            if(neibour1Width> doorStoneWidth && neibour1Width<=MaxDoorWidth && 
                neibour2Width > doorStoneWidth && neibour2Width <= MaxDoorWidth)
            {
                return Math.Min(neibour1Width, neibour2Width);
            }
            else if (neibour1Width > doorStoneWidth && neibour1Width <= MaxDoorWidth)
            {
                return neibour1Width;
            }
            else if (neibour2Width > doorStoneWidth && neibour2Width <= MaxDoorWidth)
            {
                return neibour2Width;
            }
            else
            {
                return MaxDoorWidth;
            }
        }
        private Line GetCenter(Line first,Line second)
        {
            var firstMidPt = GetMidPt(first, second);
            var secondMidPt = GetMidPt(second,first);
            return new Line(firstMidPt, secondMidPt);
        }

        private Point3d GetMidPt(Line first, Line second)
        {
            var firstVec = first.LineDirection();
            var vec1 = first.StartPoint.GetVectorTo(second.StartPoint);
            var vec2 = first.StartPoint.GetVectorTo(second.EndPoint);
            var spDis = firstVec.DotProduct(vec1);
            var epDis = firstVec.DotProduct(vec2);
            var startPt = first.StartPoint + firstVec.MultiplyBy(spDis);
            var endPt = first.StartPoint + firstVec.MultiplyBy(epDis);
            return startPt.GetMidPt(endPt);
        }
        
        /// <summary>
        /// 查找门垛与障碍物相邻的边
        /// </summary>
        /// <param name="doorStone"></param>
        /// <param name="obstacle"></param>
        /// <returns></returns>
        private List<Tuple<Line,Line>> FindPairEdges(Polyline doorStone,Entity neighbor)
        {
            //Tuple.Create(门垛边，邻居边);
            var results = new List<Tuple<Line, Line>>();
            var stoneLines = doorStone.ToLines();         
            var neighborLines = ToLines(neighbor, TesslateLength);
            for (int i = 0; i < stoneLines.Count; i++)
            {
                for (int j = 0; j < neighborLines.Count; j++)
                {
                    if(ThDoorUtils.IsQualified(neighborLines[j], stoneLines[i]))
                    {
                        results.Add(Tuple.Create(stoneLines[i], neighborLines[j]));
                    }
                }
            }
            return results;
        }
        private List<Line> ToLines(Entity entity,double length)
        {
            if(entity is Polyline polyline)
            {
                return polyline.ToLines(length);
            }
            else if(entity is MPolygon mPolygon)
            {
                return mPolygon.Loops().SelectMany(o => o.ToLines(length)).ToList();
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        private bool IsValid(Polyline first, Polyline second, double length)
        {
            var firstLines = first.ToLines();
            var secondLines = second.ToLines();
            for (int i = 0; i < firstLines.Count; i++)
            {
                for (int j = 0; j < secondLines.Count; j++)
                {
                    if (IsValid(firstLines[i], secondLines[j], length))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private bool IsValid(Line first, Line second, double length)
        {            
            if (!(first.Length > 0 && second.Length > 0.0))
            {
                return false;
            }
            if (!ThDoorUtils.IsValidAngle(first.LineDirection(), second.LineDirection()))
            {
                return false;
            }
            return Math.Abs(first.Distance(second) - length) <= ThMEPEngineCoreCommon.DoorStoneWidthTolerance;
        }
        private Polyline ToOBB(Line left,Line right)
        {
            //ToDO
            var pts = new Point3dCollection();
            pts.Add(left.StartPoint);
            pts.Add(left.EndPoint);
            if(left.LineDirection().DotProduct(right.LineDirection())>0)
            {
                pts.Add(right.EndPoint);
                pts.Add(right.StartPoint);
            }
            else
            {
                pts.Add(right.StartPoint);
                pts.Add(right.EndPoint);
            }
            var poly = pts.CreatePolyline();
            return poly.OBB();
        }
        /// <summary>
        /// 取矩形较长的中心线，绘制一定宽度的矩形
        /// </summary>
        /// <param name="rectangle">矩形Polyline</param>
        /// <param name="width">矩形宽带</param>
        /// <returns>返回矩形Polyline</returns>
        private Polyline BufferCenter(Polyline rectangle, double width)
        {
            List<Line> lines = rectangle.ToLines();
            lines = lines.OrderBy(o=>o.Length).ToList(); 
            var center = GetCenter(lines[1], lines[2]);
            return center.Buffer(width / 2);
        }
    }
}
