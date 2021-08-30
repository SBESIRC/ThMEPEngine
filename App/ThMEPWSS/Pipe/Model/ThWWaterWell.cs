using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADExtension;
using ThMEPEngineCore.Model;
using DotNetARX;
using ThCADCore.NTS;
using ThMEPWSS.CADExtensionsNs;
using System.Diagnostics;
using ThMEPEngineCore.Engine;
using ThMEPWSS.Pipe.Engine;

namespace ThMEPWSS.Pipe.Model
{
    public class WaterWellBlockNames
    {
        public const string DeepWaterPump = "潜水泵-AI";
        public const string LocationRiser = "带定位立管";
        public const string LocationRiser150 = "带定位立管150";
        public const string WaterWellTableHeader = "集水井提资表表头";
        public const string WaterWellTableBody = "集水井提资表表身";
    }
    //边索引号是按照逆时针旋转排布
    public class ThWWaterWell : ThIfcBuildingElement
    {
        public bool IsHavePump = false;//是否包含泵
        public double Rotation = 0.0;//旋转角度
        public double Width = 0.0;//宽
        public double Length = 0.0;//长 
        public string Title = "集水井";
        public List<Point3d> WaterWellVertex = new List<Point3d>();//集水井顶点坐标（全局坐标系）
        public List<Tuple<int, int>> WaterWellEdge = new List<Tuple<int, int>>();//集水井边 
        public List<int> WallSide = new List<int>();//靠墙边
        public List<Line> WallLines = null;//墙线
        public List<Point3d> ParkSpacePoint { set; get; }
        public List<BlockReference> PipeList { set; get; }
        private ThWDeepWellPump DeepWellPump = null;//潜水泵
        public Polyline OBB { get; set; }
        public ThWWaterWell()
        {
            OBB = new Polyline();
            PipeList = new List<BlockReference>();
        }
        public void Init()
        {
            //将点进行顺时针排序
            VertexSort();
            //将边进行逆时针编号
            EdgeSort();
            //获取长
            Length = WaterWellVertex[0].DistanceTo(WaterWellVertex[1]);
            //获取宽
            Width = WaterWellVertex[0].DistanceTo(WaterWellVertex[3]);
            var blk = Outline as BlockReference;
            Rotation = blk.Rotation;
        }
        //读取供水系统模块文件的路径
        public static ThWWaterWell Create(ThRawIfcDistributionElementData data)
        {
            var elementInfo = data.Data as WWaterWellElementInfo;
            var waterWell = new ThWWaterWell();
            waterWell.Outline = data.Geometry;
            waterWell.OBB = elementInfo.Outline;
            waterWell.Uuid = Guid.NewGuid().ToString();
            waterWell.Title = elementInfo.BlkEffectiveName;
            return waterWell;
        }
        public void RemovePump()
        {
            if(IsHavePump)
            {
                using (var db = Linq2Acad.AcadDatabase.Active())
                {
                    var ent = db.Element<Entity>(DeepWellPump.PumpObjectID, true);
                    ent.Erase();
                }
            }
        }
        public void RemovePipe()
        {
            foreach (var pipe in PipeList)
            {
                using (var db = Linq2Acad.AcadDatabase.Active())
                {
                    var ent = db.Element<Entity>(pipe.ObjectId, true);
                    ent.Erase();
                }
            }
        }
        public void NearWall(List<Line> walls,double tol)
        {
            WallLines = walls;
            for(int i = 0; i  < WaterWellEdge.Count;i++)
            {
                double distance = 0.0;
                if(GetIsNearWall(i,tol,out distance))
                {
                    WallSide.Add(i);
                }
            }
        }
        public bool ContainPump(ThWDeepWellPump pump)
        {
            Point3d postion = pump.GetPosition();
            //如果point在区域内，获取该点
            if (OBB.Contains(postion))
            {
                IsHavePump = true;
                DeepWellPump = pump;
                return true;
            }
            return false;
        }
        public bool ContainPipe(BlockReference pipe)
        {
            Point3d postion = pipe.Position;
            //如果point在区域内，获取该点
            if (OBB.Contains(postion))
            {
                PipeList.Add(pipe);
                return true;
            }
            return false;
        }
        public int GetInstalSide(int number)//获取潜水泵安装的边
        {
            int side = 0;
            int wallCount = WallSide.Count();
            switch (wallCount)
            {
                case 0:
                    {
                        side = GetInstalSide0(number);
                    }
                    break;
                case 1:
                    {
                        side = GetInstalSide1(number);
                    }
                    break;
                case 2:
                    {
                        side = GetInstalSide2(number);
                    }
                    break;
                case 3:
                    {
                        side = GetInstalSide3(number);
                    }
                    break;
                default:
                    break;
            }
            return side;
        }
        public Point3d GetDeepWellPumpPosition(int side, int number, out double space)
        {
            Point3d pumpPos = new Point3d();
            double spaceValue = 0;
            switch (number)
            {
                case 1:
                    {
                        pumpPos = GetInstalPosition1(side, number, out spaceValue);
                    }
                    break;
                case 2:
                    {
                        pumpPos = GetInstalPosition2(side, number, out spaceValue);
                    }
                    break;
                case 3:
                    {
                        pumpPos = GetInstalPosition3(side, number, out spaceValue);
                    }
                    break;
                case 4:
                    {
                        pumpPos = GetInstalPosition4(side, number, out spaceValue);
                    }
                    break;
                default:
                    break;
            }
            space = spaceValue;
            return pumpPos;
        }
        public bool AddDeepWellPump(WaterWellPumpConfigInfo configInfo)
        {
            string pumpName = configInfo.PumpInfo.strNumberPrefix;
            switch (configInfo.WaterWellInfo.strFloorlocation)
            {
                case "B1":
                    pumpName = pumpName + "1";
                    break;
                case "B2":
                    pumpName = pumpName + "2";
                    break;
                case "B3":
                    pumpName = pumpName + "3";
                    break;
                case "B4":
                    pumpName = pumpName + "3";
                    break;
                default:
                    break;
            }
            double fontHeight = 525;
            string riserName = WaterWellBlockNames.LocationRiser;
            switch (configInfo.PumpInfo.strMapScale)
            {
                case "1:50":
                    fontHeight = 175;
                    break;
                case "1:100":
                    fontHeight = 350;
                    riserName = WaterWellBlockNames.LocationRiser;
                    break;
                case "1:150":
                    fontHeight = 525;
                    riserName = WaterWellBlockNames.LocationRiser150;
                    break;
                default:
                    break;
            }

            bool isOk = true;
            IsHavePump = true;
            double space;
            int side = GetInstalSide(configInfo.PumpInfo.PumpsNumber);
            double angele = GetSideAngle(side);
            Point3d position = GetDeepWellPumpPosition(side, configInfo.PumpInfo.PumpsNumber, out space);
            DeepWellPump = ThWDeepWellPump.Create("W-EQPM", WaterWellBlockNames.DeepWaterPump, pumpName, position,new Scale3d(1,1,1), angele);
            DeepWellPump.SetPumpCount(configInfo.PumpInfo.PumpsNumber);
            DeepWellPump.SetPumpSpace(space);
            DeepWellPump.SetFontHeight(fontHeight);

            Tuple<int, int> sideLine = WaterWellEdge[side];
            Point3d point1 = WaterWellVertex[sideLine.Item1];
            Point3d point2 = WaterWellVertex[sideLine.Item2];
            Vector3d normal = point1.GetVectorTo(point2).GetNormal();
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                for(int i = 0; i < configInfo.PumpInfo.PumpsNumber; i++)
                {
                    var blkId = acadDb.ModelSpace.ObjectId.InsertBlockReference("W-DRAI-EQPM", riserName, position, new Scale3d(1, 1, 1), 0);
                    var blk = acadDb.Element<BlockReference>(blkId);
                    blk.Rotation = angele * Math.PI / 180;
                    if (blk.IsDynamicBlock)
                    {
                        foreach (DynamicBlockReferenceProperty property in blk.DynamicBlockReferencePropertyCollection)
                        {
                            if (property.PropertyName == "可见性1")
                            {
                                property.Value = configInfo.PumpInfo.strPipeDiameter;
                                break;
                            }
                        }
                    }
                    position += normal * space;
                }
            }

            return isOk;
        }
        public double GetSideAngle(int side)
        {
            Tuple<int, int> sideLine = WaterWellEdge[side];
            Point3d point1 = WaterWellVertex[sideLine.Item1];
            Point3d point2 = WaterWellVertex[sideLine.Item2];
            var vec = point1.GetVectorTo(point2);
            var angle = Vector3d.XAxis.GetAngleTo(vec, Vector3d.ZAxis);
            angle = angle  / Math.PI * 180.0;
            return angle;
        }
        public double GetAcreage()
        {
            double acreage = 0.0;
            acreage = (Length / 1000.0) * (Width / 1000.0);
            return acreage;
        }
        private void VertexSort()
        {
            var vertex = OBB.Vertices();
//            var vertex = Outline.GeometricExtents.ToRectangle().Vertices();
            var blk = Outline as BlockReference;
            Point3d postion = blk.Position;
            int index0 = 0;
            int index1 = 0;
            int index2 = 0;
            int index3 = 0;

            double minDistance = 99999.0;
            for (int i = 0; i < 4; i++)
            {
                double distance = postion.DistanceTo(vertex[i]);
                if (distance < minDistance)
                {
                    index0 = i;
                    minDistance = distance;
                }
            }

            double maxDistance = 0.0;
            
            for(int i = 0;i < 4;i++ )
            {
                double distance = vertex[index0].DistanceTo(vertex[i]);
                if (maxDistance < distance)
                {
                    maxDistance = distance;
                    index2 = i;
                }
            }
            //构造向量P0P2
            Vector3d vectorz1 = new Vector3d(0, 0, 1);
            Vector3d vectorz2 = new Vector3d(0, 0, -1);
            Vector3d vector0 = vertex[index0].GetVectorTo(vertex[index2]);

            for (int i = 0; i < 4; i++)
            {
                if(i != index0 && i != index2)
                {
                    Vector3d vector1 = vertex[index0].GetVectorTo(vertex[i]);
                    Vector3d vector2 = vector0.CrossProduct(vector1);
                    vector2 = vector2.GetNormal();

                    if(vector2.IsEqualTo(vectorz1,new Tolerance(1e-5,1e-5)))
                    {
                        index1 = i;
                    }
                    if (vector2.IsEqualTo(vectorz2, new Tolerance(1e-5, 1e-5)))
                    {
                        index3 = i;
                    }
                }
            }
            WaterWellVertex.Add(vertex[index0]);
            WaterWellVertex.Add(vertex[index1]);
            WaterWellVertex.Add(vertex[index2]);
            WaterWellVertex.Add(vertex[index3]);
        }
        private void EdgeSort()
        {
            //得到四条边顶点index,按照逆时针排序
            Tuple<int, int> edge1 = Tuple.Create(0, 1);
            Tuple<int, int> edge2 = Tuple.Create(3, 0);
            Tuple<int, int> edge3 = Tuple.Create(2, 3);
            Tuple<int, int> edge4 = Tuple.Create(1, 2);
            WaterWellEdge.Add(edge1);
            WaterWellEdge.Add(edge2);
            WaterWellEdge.Add(edge3);
            WaterWellEdge.Add(edge4);
        }
        private bool GetIsSquare()
        {
            if(Math.Abs(Width - Length) < 0.000001)
            {
                return true;
            }
            return false;
        }
        private bool GetIsParallel(int index1,int index2)
        {
            Tuple<int, int> edge1 = WaterWellEdge[index1];
            Tuple<int, int> edge2 = WaterWellEdge[index2];
            Point3d edge1_point1 = WaterWellVertex[edge1.Item1];
            Point3d edge1_point2 = WaterWellVertex[edge1.Item2];

            Point3d edge2_point1 = WaterWellVertex[edge2.Item1];
            Point3d edge2_point2 = WaterWellVertex[edge2.Item2];
            Vector3d edge1_vector = edge1_point1.GetVectorTo(edge1_point2);
            Vector3d edge2_vector = edge2_point1.GetVectorTo(edge2_point2);
            if(edge1_vector.IsParallelTo(edge2_vector))
            {
                return true;
            }
            return false;
        }
        private bool GetIsLength(int side)
        {
            if(Length > Width)
            {
                if(side == 0 || side == 2)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (side == 0 || side == 2)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        private bool GetIsNearWall(int side, double tol, out double dist)
        {
            //如果边与墙的距离，小于tol，认为该边是靠墙
            dist = 9999.0;
            Tuple<int, int> edge = WaterWellEdge[side];
            Point3d start_point = WaterWellVertex[edge.Item1];
            Point3d end_point = WaterWellVertex[edge.Item2];
            Line edgeLine = new Line(start_point, end_point);
            Vector3d edge_vector = edgeLine.Delta;
            foreach (Line line in WallLines)
            {
                Vector3d line_vector = line.Delta;
//                if (edge_vector.IsParallelTo(line_vector))//如果平行
                {
                    double tmpDist = line.DistanceToPoint(edgeLine.GetMidpoint());
                    //                    double tmpDist = edgeLine.DistanceToOtherLineSegment(line);
                    if (dist > tmpDist)
                    {
                        dist = tmpDist;
                    }
                    if(tmpDist < tol)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private bool GetIsNearPark(int side, out double dist)
        {
            dist = 9999.0;
            if (ParkSpacePoint.Count == 0)
            {
                return false;
            }

            var edge = WaterWellEdge[side];
            var point1 = WaterWellVertex[edge.Item1];
            var point2 = WaterWellVertex[edge.Item2];
            Line line = new Line(point1, point2);
            foreach (var point in ParkSpacePoint)
            {
                double tmpDist = line.DistanceToPoint(point);
                if(dist > tmpDist)
                {
                    dist = tmpDist;
                }
            }
            return true;
        }
        private int GetInstalSide0(int number)
        {
            int side = 0;
            if(number == 1)
            {
                //找到距离墙最近的边L
                double minDistance = 9999.0;
                for (int i = 0; i < WaterWellEdge.Count(); i++)
                {
                    double tempDistance = 0.0;
                    GetIsNearWall(i, 50, out tempDistance);
                    if (minDistance > tempDistance)
                    {
                        minDistance = tempDistance;
                        side = i;
                    }
                }
            }
            else
            {
                if (GetIsSquare())
                {
                    //找到距离墙最近的边L
                    double minDistance = 9999.0;
                    for (int i = 0; i < WaterWellEdge.Count(); i++)
                    {
                        double tempDistance = 0.0;
                        GetIsNearWall(i, 50, out tempDistance);
                        if (minDistance > tempDistance)
                        {
                            minDistance = tempDistance;
                            side = i;
                        }
                    }
                }
                else
                {
                    //找长边中距墙最近的边L
                    int side1 = 0;
                    int side2 = 0;
                    if (Length > Width)
                    {
                        side1 = 0;
                        side2 = 2;
                    }
                    else
                    {
                        side1 = 1;
                        side2 = 3;
                    }
                    double tempDistance1 = 0.0;
                    double tempDistance2 = 0.0;
                    GetIsNearWall(side1, 1, out tempDistance1);
                    GetIsNearWall(side2, 1, out tempDistance2);
                    if (tempDistance1 < tempDistance2)
                    {
                        side = side1;
                    }
                    else
                    {
                        side = side2;
                    }
                }
            }
            return side;
        }
        private int GetInstalSide1(int number)
        {
            int side = 0;
            if (GetIsSquare())
            {
                //靠墙边布置泵
                side = WallSide[0];
            }
            else
            {
                if (1 == number)
                {
                    //靠墙边布置泵
                    side = WallSide[0];
                }
                else
                {
                    //靠墙边是长边,取靠墙边L
                    if(GetIsLength(WallSide[0]))
                    {
                        side = WallSide[0];
                    }
                    else//找2条长边中距离其他墙最近的边L
                    {
                        int side1 = 0;
                        int side2 = 0;
                        if (Length > Width)
                        {
                            side1 = 0;
                            side2 = 2;
                        }
                        else
                        {
                            side1 = 1;
                            side2 = 3;
                        }
                        double tempDistance1 = 0.0;
                        double tempDistance2 = 0.0;
                        GetIsNearWall(side1,1,out tempDistance1);
                        GetIsNearWall(side2,1,out tempDistance2);
                        if (tempDistance1 < tempDistance2)
                        {
                            side = side1;
                        }
                        else
                        {
                            side = side2;
                        }
                    }
                }
            }
            return side;
        }
        private int GetInstalSide2(int number)
        {
            int side = 0;
            bool isParallel = GetIsParallel(WallSide[0], WallSide[1]);//靠墙边平行
            if (GetIsSquare())
            {
                if (isParallel)
                {
                    //取第一个靠墙边L
                    side = WallSide[0];
                }
                else
                {
                    side = WallSide[0];
                    //找距离车位最大的靠墙边
                    double tempDistance1 = 0.0;
                    double tempDistance2 = 0.0;
                    GetIsNearPark(WallSide[0], out tempDistance1);
                    GetIsNearPark(WallSide[1], out tempDistance2);
                    if(tempDistance1 < tempDistance2)
                    {
                        side = WallSide[1];
                    }
                }
            }
            else
            {
                if (isParallel)
                {
                    //取第一个靠墙边L
                    side = WallSide[0];
                }
                else
                {
                    //取靠墙边中的长边L
                    foreach(int index in WallSide)
                    {
                        if (GetIsLength(index))
                        {
                            side = index;
                            break;
                        }
                    }
                }
            }
            return side;
        }
        private int GetInstalSide3(int number)
        {
            int side = 0;
            if(number == 1)
            {
                //取靠中间墙的边L
                side = GetMidSide();
            }
            else
            {
                if (GetIsSquare())
                {
                    //取靠中间墙的边L
                    side = GetMidSide();
                }
                else
                {
                    //if(中间墙是长边L，取边L)
                    int tmpSide = GetMidSide();
                    if (GetIsLength(tmpSide))
                    {
                        side = tmpSide;
                    }
                    else//取第一个长边
                    {
                        foreach (int index in WallSide)
                        {
                            if (GetIsLength(index))
                            {
                                side = index;
                                break;
                            }
                        }
                    }
                }
            }
            return side;
        }
        private int GetMidSide()
        {
            int index = 0;
            if(WallSide[0]== 0 && WallSide[1] == 1  && WallSide[2] == 2)
            {
                index = 1;
            }
            else if(WallSide[0] == 0 && WallSide[1] == 1 && WallSide[2] == 3)
            {
                index = 0;
            }
            else if (WallSide[0] == 0 && WallSide[1] == 2 && WallSide[2] == 3)
            {
                index = 3;
            }
            else if (WallSide[0] == 1 && WallSide[1] == 2 && WallSide[2] == 3)
            {
                index = 2;
            }
            return index;
        }
        private List<Point3d> GetOffsetPoint(Point3d point,double angle,double t)
        {
            var pts = new List<Point3d>();
            double x1 = point.X + t * Math.Cos(angle);
            double y1 = point.Y + t * Math.Sin(angle);

            double x2 = point.X - t * Math.Cos(angle);
            double y2 = point.Y - t * Math.Sin(angle);
            Point3d point1 = new Point3d(x1, y1, 0.0);
            Point3d point2 = new Point3d(x2, y2, 0.0);
            pts.Add(point1);
            pts.Add(point2);
            return pts;
        }
        private Point3d GetInstalPosition1(int side, int number,out double space)
        {
            Point3d pumpPos = new Point3d();
            Tuple<int, int> sideLine = WaterWellEdge[side];
            Point3d point1 = WaterWellVertex[sideLine.Item1];
            Point3d point2 = WaterWellVertex[sideLine.Item2];
            //求出来中点，然后，再求出来偏移点，有两个，再根据其他条件，舍去其中一个
            double x = (point1.X + point2.X) / 2.0;
            double y = (point1.Y + point2.Y) / 2.0;
            Point3d point3 = new Point3d(x, y, 0);
            Vector3d normal = point1.GetVectorTo(point2).GetNormal();
            double angle = Vector3d.XAxis.GetAngleTo(normal);
            List<Point3d> offsetPoints = GetOffsetPoint(point3, angle + Math.PI * 0.5, 150.0);
            foreach (Point3d point in offsetPoints)
            {
                //如果point在区域内，获取该点
                if (OBB.Contains(point))
                {
                    pumpPos = point;
                    break;
                }
            }
            space = 0.0;
            return pumpPos;
        }
        private Point3d GetInstalPosition2(int side, int number,out double space)
        {
            Point3d pumpPos = new Point3d();
            Tuple<int, int> sideLine  = WaterWellEdge[side];
            Point3d point1 = WaterWellVertex[sideLine.Item1];
            Point3d point2 = WaterWellVertex[sideLine.Item2];
            double length = point1.DistanceTo(point2);
            Vector3d normal = point1.GetVectorTo(point2).GetNormal();
            double angle = Vector3d.XAxis.GetAngleTo(normal);
            double margin = 300.0;//边距
            space = length - margin * 2.0;
            int tmpInt = (int)((space + 50)/ 100);
            space = tmpInt * 100;
            margin = (length - space) / 2.0;
            if ((space - 900) > 0)
            {
                space = 900;
                margin = (length - space) / 2.0;
            }
            else if((space - 500) < 0)
            {
                tmpInt = (int)(space / 100);
                space = (tmpInt + 1) * 100;
                margin = (length - space) / 2.0;
            }
            Vector3d vector = normal * margin;
            Point3d tmpPos = point1 + vector;
            List<Point3d> offsetPoints = GetOffsetPoint(tmpPos, angle + Math.PI*0.5, 150);
            foreach (Point3d point in offsetPoints)
            {
                //如果point在区域内，获取该点
                if (OBB.Contains(point))
                {
                    pumpPos = point;
                    break;
                }
            }
            return pumpPos;
        }
        private Point3d GetInstalPosition3(int side, int number,out double space)
        {
            Point3d pumpPos = new Point3d();
            Tuple<int, int> sideLine = WaterWellEdge[side];
            Point3d point1 = WaterWellVertex[sideLine.Item1];
            Point3d point2 = WaterWellVertex[sideLine.Item2];
            double length = point1.DistanceTo(point2);
            Vector3d normal = point1.GetVectorTo(point2).GetNormal();
            double angle = Vector3d.XAxis.GetAngleTo(normal);
            double margin = 300.0;
            space = (length - margin * 2)/2;
            int tmpInt = (int)((space + 50) / 100);
            space = tmpInt * 100;
            margin = (length - space * 2) / 2;
            if (space > 900)
            {
                space = 900;
                margin = (length - space * 2) / 2;
            }
            else if (space < 500)
            {
                tmpInt = (int)(space / 100);
                space = (tmpInt + 1) * 100;
                margin = (length - space * 2) / 2;
            }
            Vector3d vector = normal * margin;
            Point3d tmpPos = point1 + vector;
            List<Point3d> offsetPoints = GetOffsetPoint(tmpPos, angle + Math.PI * 0.5, 150.0);
            foreach (Point3d point in offsetPoints)
            {
                //如果point在区域内，获取该点
                if (OBB.Contains(point))
                {
                    pumpPos = point;
                    break;
                }
            }
            return pumpPos;
        }
        private Point3d GetInstalPosition4(int side, int number,out double space)
        {
            Point3d pumpPos = new Point3d();
            Tuple<int, int> sideLine = WaterWellEdge[side];
            Point3d point1 = WaterWellVertex[sideLine.Item1];
            Point3d point2 = WaterWellVertex[sideLine.Item2];
            double length = point1.DistanceTo(point2);
            Vector3d normal = point1.GetVectorTo(point2).GetNormal();
            double angle = Vector3d.XAxis.GetAngleTo(normal);
            double margin = 300.0;
            space = (length - margin * 2)/3;
            int tmpInt = (int)((space + 50) / 100);
            space = tmpInt * 100;
            margin = (length - space * 3) / 2;
            if (space > 900)
            {
                space = 900;
                margin = (length - space * 3) / 2;
            }
            else if (space < 500)
            {
                tmpInt = (int)(space / 100);
                space = (tmpInt + 1) * 100;
                margin = (length - space * 3) / 2;
            }
            Vector3d vector = normal * margin;
            Point3d tmpPos = point1 + vector;
            List<Point3d> offsetPoints = GetOffsetPoint(tmpPos, angle + Math.PI * 0.5, 150.0);
            foreach (Point3d point in offsetPoints)
            {
                //如果point在区域内，获取该点
                if (OBB.Contains(point))
                {
                    pumpPos = point;
                    break;
                }
            }
            return pumpPos;
        }
    }
}
