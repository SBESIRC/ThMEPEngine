using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using ThCADCore.NTS;
using ThCADExtension;
using ThMEPEngineCore.Engine;
using ThMEPEngineCore.Service;
using ThMEPWSS.CADExtensionsNs;
using ThMEPWSS.Pipe.Engine;

namespace ThMEPWSS.WaterWellPumpLayout.Model
{
    public class ThWaterWellModel
    {
        public int Length { set; get; }//集水井长
        public int Width { set; get; }//集水井宽
        public bool IsHavePump { set; get; }//是否包含泵
        public string EffName { set; get; }//集水井块的名称
        public Point3d Position { set; get; }//集水井位置
        public Polyline WellObb { set; get; }//集水井外包框
        public BlockReference Geometry { set; get; }//集水井图块数据
        public ThWaterPumpModel PumpModel { set; get; }//泵数据模型
        public List<Point3d> WellVertex { set; get; }//集水井顶点坐标（全局坐标系）
        public List<Tuple<int, int>> WellEdge { set; get; }//集水井边
        public List<Line> WallLines { set; get; }//墙线
        public List<int> NearWallEdge { set; get; }//靠墙边
        public static ThWaterWellModel Create(ThRawIfcDistributionElementData data)
        {
            ThWaterWellModel waterWell = null;
            if (data.Geometry is BlockReference blk)
            {
                waterWell = new ThWaterWellModel();
                var elementInfo = data.Data as WWaterWellElementInfo;
                waterWell.Geometry = blk;
                waterWell.IsHavePump = false;
                waterWell.WellObb = elementInfo.Outline;
                waterWell.EffName = ThStructureUtils.OriginalFromXref(elementInfo.BlkEffectiveName);
                waterWell.WellEdge = new List<Tuple<int, int>>();
                waterWell.WellVertex = new List<Point3d>();
                waterWell.NearWallEdge = new List<int>();
            }
            return waterWell;
        }
        private void VertexSortOri()
        {
            if (Geometry != null)
            {
                var vertex = WellObb.Vertices();
                Point3d postion = Geometry.Position;
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

                for (int i = 0; i < 4; i++)
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
                    if (i != index0 && i != index2)
                    {
                        Vector3d vector1 = vertex[index0].GetVectorTo(vertex[i]);
                        Vector3d vector2 = vector0.CrossProduct(vector1);
                        vector2 = vector2.GetNormal();

                        if (vector2.IsEqualTo(vectorz1, new Tolerance(1e-5, 1e-5)))
                        {
                            index1 = i;
                        }
                        if (vector2.IsEqualTo(vectorz2, new Tolerance(1e-5, 1e-5)))
                        {
                            index3 = i;
                        }
                    }
                }
                WellVertex.Add(vertex[index0]);
                WellVertex.Add(vertex[index1]);
                WellVertex.Add(vertex[index2]);
                WellVertex.Add(vertex[index3]);
            }
        }

        private void VertexSort()
        {
            if (Geometry != null)
            {
                if (WellObb.IsCCW() == true)
                {
                    WellObb.ReverseCurve();
                }
                var v1 = WellObb.GetPoint3dAt(1) - WellObb.GetPoint3dAt(0);
                var v2 = WellObb.GetPoint3dAt(3) - WellObb.GetPoint3dAt(0);
                var startI = 0;
                if (v2.Length > v1.Length)
                {
                    startI = 1;
                }

                for (int i = startI; i < (4 + startI); i++)
                {
                    WellVertex.Add(WellObb.GetPoint3dAt(i % 4));
                }
            }
        }

        private void EdgeSort()
        {
            //得到四条边顶点index,按照逆时针排序
            Tuple<int, int> edge1 = Tuple.Create(0, 1);
            Tuple<int, int> edge2 = Tuple.Create(3, 0);
            Tuple<int, int> edge3 = Tuple.Create(2, 3);
            Tuple<int, int> edge4 = Tuple.Create(1, 2);
            WellEdge.Add(edge1);
            WellEdge.Add(edge2);
            WellEdge.Add(edge3);
            WellEdge.Add(edge4);
        }
        private int GetMidEdge()//取靠墙边的中间边
        {
            int index = 0;
            if (NearWallEdge[0] == 0 && NearWallEdge[1] == 1 && NearWallEdge[2] == 2)
            {
                index = 1;
            }
            else if (NearWallEdge[0] == 0 && NearWallEdge[1] == 1 && NearWallEdge[2] == 3)
            {
                index = 0;
            }
            else if (NearWallEdge[0] == 0 && NearWallEdge[1] == 2 && NearWallEdge[2] == 3)
            {
                index = 3;
            }
            else if (NearWallEdge[0] == 1 && NearWallEdge[1] == 2 && NearWallEdge[2] == 3)
            {
                index = 2;
            }
            return index;
        }
        private int GetInstalEdge0(int number)//0条边靠墙
        {
            int side = 0;
            if (number == 1)
            {
                //找到距离墙最近的边L
                double minDistance = 9999.0;
                for (int i = 0; i < WellEdge.Count(); i++)
                {
                    IsNearWall(i, 50, out double tempDistance);
                    if (minDistance > tempDistance)
                    {
                        minDistance = tempDistance;
                        side = i;
                    }
                }
            }
            else
            {
                if (IsSquare())
                {
                    //找到距离墙最近的边L
                    double minDistance = 9999.0;
                    for (int i = 0; i < WellEdge.Count(); i++)
                    {
                        IsNearWall(i, 50, out double tempDistance);
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
                    IsNearWall(side1, 1, out double tempDistance1);
                    IsNearWall(side2, 1, out double tempDistance2);
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
        private int GetInstalEdge1(int number)//1条边靠墙
        {
            int side = 0;
            if (IsSquare())
            {
                //靠墙边布置泵
                side = NearWallEdge[0];
            }
            else
            {
                if (1 == number)
                {
                    //靠墙边布置泵
                    side = NearWallEdge[0];
                }
                else
                {
                    //靠墙边是长边,取靠墙边L
                    if (IsLength(NearWallEdge[0]))
                    {
                        side = NearWallEdge[0];
                    }
                    else//找2条长边中距离其他墙最近的边L
                    {
                        int side1;
                        int side2;
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
                        IsNearWall(side1, 1, out double tempDistance1);
                        IsNearWall(side2, 1, out double tempDistance2);
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
        private int GetInstalEdge2(int number)//2条边靠墙
        {
            int side = 0;
            bool isParallel = IsParallel(NearWallEdge[0], NearWallEdge[1]);//靠墙边平行
            if (IsSquare())
            {
                //取第一个靠墙边L
                side = NearWallEdge[0];
            }
            else
            {
                if (isParallel)
                {
                    //取第一个靠墙边L
                    side = NearWallEdge[0];
                }
                else
                {
                    //取靠墙边中的长边L
                    foreach (int index in NearWallEdge)
                    {
                        if (IsLength(index))
                        {
                            side = index;
                            break;
                        }
                    }
                }
            }
            return side;
        }
        private int GetInstalEdge3(int number)//3条边靠墙
        {
            int side = 0;
            if (number == 1)
            {
                //取靠中间墙的边L
                side = GetMidEdge();
            }
            else
            {
                if (IsSquare())
                {
                    //取靠中间墙的边L
                    side = GetMidEdge();
                }
                else
                {
                    //if(中间墙是长边L，取边L)
                    int tmpSide = GetMidEdge();
                    if (IsLength(tmpSide))
                    {
                        side = tmpSide;
                    }
                    else//取第一个长边
                    {
                        foreach (int index in NearWallEdge)
                        {
                            if (IsLength(index))
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
        private Point3d GetInstalPosition1(int index, out double space)//泵数量为1
        {
            Point3d pumpPos = new Point3d();
            Tuple<int, int> sideLine = WellEdge[index];
            Point3d point1 = WellVertex[sideLine.Item1];
            Point3d point2 = WellVertex[sideLine.Item2];
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
                if (WellObb.Contains(point))
                {
                    pumpPos = point;
                    break;
                }
            }
            space = 0.0;
            return pumpPos;
        }
        private Point3d GetInstalPosition2(int side, out double space)
        {
            Point3d pumpPos = new Point3d();
            Tuple<int, int> sideLine = WellEdge[side];
            Point3d point1 = WellVertex[sideLine.Item1];
            Point3d point2 = WellVertex[sideLine.Item2];
            double length = point1.DistanceTo(point2);
            Vector3d normal = point1.GetVectorTo(point2).GetNormal();
            double angle = Vector3d.XAxis.GetAngleTo(normal);
            double margin = 300.0;//边距
            space = length - margin * 2.0;
            int tmpInt = (int)((space + 50) / 100);
            space = tmpInt * 100;
            margin = (length - space) / 2.0;
            if ((space - 900) > 0)
            {
                space = 900;
                margin = (length - space) / 2.0;
            }
            else if ((space - 500) < 0)
            {
                tmpInt = (int)(space / 100);
                space = (tmpInt + 1) * 100;
                margin = (length - space) / 2.0;
            }
            Vector3d vector = normal * margin;
            Point3d tmpPos = point1 + vector;
            List<Point3d> offsetPoints = GetOffsetPoint(tmpPos, angle + Math.PI * 0.5, 150);
            foreach (Point3d point in offsetPoints)
            {
                //如果point在区域内，获取该点
                if (WellObb.Contains(point))
                {
                    pumpPos = point;
                    break;
                }
            }
            return pumpPos;
        }
        private Point3d GetInstalPosition3(int side, out double space)
        {
            Point3d pumpPos = new Point3d();
            Tuple<int, int> sideLine = WellEdge[side];
            Point3d point1 = WellVertex[sideLine.Item1];
            Point3d point2 = WellVertex[sideLine.Item2];
            double length = point1.DistanceTo(point2);
            Vector3d normal = point1.GetVectorTo(point2).GetNormal();
            double angle = Vector3d.XAxis.GetAngleTo(normal);
            double margin = 300.0;
            space = (length - margin * 2) / 2;
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
                if (WellObb.Contains(point))
                {
                    pumpPos = point;
                    break;
                }
            }
            return pumpPos;
        }
        private Point3d GetInstalPosition4(int side, out double space)
        {
            Point3d pumpPos = new Point3d();
            Tuple<int, int> sideLine = WellEdge[side];
            Point3d point1 = WellVertex[sideLine.Item1];
            Point3d point2 = WellVertex[sideLine.Item2];
            double length = point1.DistanceTo(point2);
            Vector3d normal = point1.GetVectorTo(point2).GetNormal();
            double angle = Vector3d.XAxis.GetAngleTo(normal);
            double margin = 300.0;
            space = (length - margin * 2) / 3;
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
                if (WellObb.Contains(point))
                {
                    pumpPos = point;
                    break;
                }
            }
            return pumpPos;
        }
        private bool IsNearWall(int side, double tol, out double dist)//判断是否靠近墙
        {
            //如果边与墙的距离，小于tol，认为该边是靠墙
            dist = 9999.0;
            Tuple<int, int> edge = WellEdge[side];
            Point3d start_point = WellVertex[edge.Item1];
            Point3d end_point = WellVertex[edge.Item2];
            Line edgeLine = new Line(start_point, end_point);
            foreach (Line line in WallLines)
            {
                Vector3d line_vector = line.Delta;
                {
                    double tmpDist = line.DistanceToPoint(edgeLine.GetMidpoint());
                    if (dist > tmpDist)
                    {
                        dist = tmpDist;
                    }
                    if (tmpDist < tol)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private bool IsLength(int side)//判断是否是长边
        {
            if (Length > Width)
            {
                if (side == 0 || side == 2)
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
        private bool IsParallel(int index1, int index2)//判断index1边和index2边是否平行
        {
            Tuple<int, int> edge1 = WellEdge[index1];
            Tuple<int, int> edge2 = WellEdge[index2];
            Point3d edge1_point1 = WellVertex[edge1.Item1];
            Point3d edge1_point2 = WellVertex[edge1.Item2];

            Point3d edge2_point1 = WellVertex[edge2.Item1];
            Point3d edge2_point2 = WellVertex[edge2.Item2];
            Vector3d edge1_vector = edge1_point1.GetVectorTo(edge1_point2);
            Vector3d edge2_vector = edge2_point1.GetVectorTo(edge2_point2);
            if (edge1_vector.IsParallelTo(edge2_vector))
            {
                return true;
            }
            return false;
        }
        private List<Point3d> GetOffsetPoint(Point3d point, double angle, double t)
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
        public void InitWellData()
        {
            VertexSortOri();
            //VertexSort();//将点进行顺时针排序
            EdgeSort();//将边进行逆时针编号
            Length = (int)(WellVertex[0].DistanceTo(WellVertex[1]) / 50.0 + 0.5) * 50;//获取长
            Width = (int)(WellVertex[0].DistanceTo(WellVertex[3]) / 50.0 + 0.5) * 50;//获取宽
            Position = Geometry.Position;//获取位置
        }
        public bool IsSquare()//判断是否是正方形
        {
            if (Math.Abs(Width - Length) < 0.000001)
            {
                return true;
            }
            return false;
        }
        public bool IsSameType(ThWaterWellModel wellModel)//判断是否是同一类
        {
            //需要加 泵数量 / 编号
            if (this.EffName == wellModel.EffName && this.GetWellSize() == wellModel.GetWellSize())
            {
                if (this.PumpModel != null && wellModel.PumpModel != null && this.PumpModel.VisibilityValue == wellModel.PumpModel.VisibilityValue && this.PumpModel.AttriValue == wellModel.PumpModel.AttriValue)
                {
                    return true;
                }
                if (this.PumpModel == null && wellModel.PumpModel == null)
                {
                    return true;
                }
            }
            return false;
        }
        public bool IsEqual(ThWaterWellModel wellModel)//判断是否同一个集水井
        {
            if (this.EffName == wellModel.EffName && this.WellObb == wellModel.WellObb)
            {
                return true;
            }
            return false;
        }
        public double GetAcreage()//获取集水井面积
        {
            double acreage = (Length - 100) / 1000.0 * ((Width - 100) / 1000.0);
            return acreage;
        }
        public string GetWellSize()//获取集水井尺寸
        {
            string strSize = (Length - 100).ToString() + "*" + (Width - 100).ToString();
            return strSize;
        }
        //public void CheckHavePump(ThWaterPumpModel pump)
        //{
        //    if (IsHavePump)
        //    {
        //        return;
        //    }
        //    //如果point在区域内，获取该点
        //    if (WellObb.Contains(pump.Position))
        //    {
        //        PumpModel = pump;
        //        IsHavePump = true;
        //    }
        //    if (IsHavePump == false)
        //    {
        //        if (WellObb.Intersects(pump.OBB))
        //        {
        //            PumpModel = pump;
        //            IsHavePump = true;
        //        }
        //    }
        //}

        public void CheckHavePumpIndex(ThCADCoreNTSSpatialIndex pumpIndex, Dictionary<int, ThWaterPumpModel> pumpDict)
        {
            if (IsHavePump)
            {
                return;
            }
            var inWellPump = pumpIndex.SelectCrossingPolygon(WellObb);
            var pumpPoly = inWellPump.OfType<Polyline>().ToList();
            if (pumpPoly.Count > 0)
            {
                if (pumpPoly.Count == 1)
                {
                    PumpModel = pumpDict[pumpPoly[0].GetHashCode()];
                    IsHavePump = true;
                }
                else
                {
                    var wellObj = new DBObjectCollection() { WellObb };
                    var maxArea = double.MinValue;
                    Polyline maxPumpPoly = null;
                    foreach (var pump in pumpPoly)
                    {
                        var pumpArea = pump.Intersection(wellObj).OfType<Polyline>().OrderByDescending(x => x.Area).FirstOrDefault();
                        if (pumpArea.Area >= maxArea)
                        {
                            maxArea = pumpArea.Area;
                            maxPumpPoly = pump;
                        }
                    }
                    if (maxPumpPoly != null)
                    {
                        PumpModel = pumpDict[maxPumpPoly.GetHashCode()];
                        IsHavePump = true;
                    }
                }
            }
        }


        public void NearWall(List<Line> walls, double tol)
        {
            WallLines = walls;
            NearWallEdge.Clear();

            for (int i = 0; i < WellEdge.Count; i++)
            {
                if (IsNearWall(i, tol, out double dist))
                {
                    NearWallEdge.Add(i);
                }
            }
        }
        public int GetInstalEdge(int pumpCount)//获取潜水泵安装的边
        {
            int side = 0;
            int wallCount = NearWallEdge.Count();
            switch (wallCount)
            {
                case 0://没有靠墙边
                    {
                        side = GetInstalEdge0(pumpCount);
                    }
                    break;
                case 1://有一条靠墙边
                    {
                        side = GetInstalEdge1(pumpCount);
                    }
                    break;
                case 2://两条靠墙边
                    {
                        side = GetInstalEdge2(pumpCount);
                    }
                    break;
                case 3://三条靠墙边
                    {
                        side = GetInstalEdge3(pumpCount);
                    }
                    break;
                default:
                    break;
            }
            return side;
        }
        public double GetInstalEdgeAngle(int index)//获取安装边的角度
        {
            Tuple<int, int> sideLine = WellEdge[index];
            Point3d point1 = WellVertex[sideLine.Item1];
            Point3d point2 = WellVertex[sideLine.Item2];
            var vec = point1.GetVectorTo(point2);
            var angle = Vector3d.XAxis.GetAngleTo(vec, Vector3d.ZAxis);
            angle = angle / Math.PI * 180.0;
            return angle;
        }
        public Point3d GetInstalPosition(int index, int number, out double space)//获取安装位置
        {
            Point3d pumpPos = new Point3d();
            double spaceValue = 0;
            switch (number)
            {
                case 1:
                    {
                        pumpPos = GetInstalPosition1(index, out spaceValue);
                    }
                    break;
                case 2:
                    {
                        pumpPos = GetInstalPosition2(index, out spaceValue);
                    }
                    break;
                case 3:
                    {
                        pumpPos = GetInstalPosition3(index, out spaceValue);
                    }
                    break;
                case 4:
                    {
                        pumpPos = GetInstalPosition4(index, out spaceValue);
                    }
                    break;
                default:
                    break;
            }
            space = spaceValue;
            return pumpPos;
        }

    }
}
