using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Linq2Acad;
using NetTopologySuite.Geometries;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThCADExtension;
using System.Diagnostics;
using NetTopologySuite.Operation.Buffer;
using ThMEPEngineCore.Diagnostics;
using GeometryExtensions;
using ThMEPHVAC.FloorHeatingCoil;
using ThMEPHVAC.FloorHeatingCoil.Heating;

namespace ThMEPHVAC.FloorHeatingCoil
{
    class MainPipeGet
    {
        //外部输入数据
        Polyline Room;
        List<BufferPoly> PipeList { get; set; }
        int main_index { get; set; }

        double Buffer { get; set; } = 500;
        double RoomBuffer { get; set; } = 200;
        bool MainHasOutput { get; set; } = true;

        //整理后数据
        Point3d MainPipeIn;
        Point3d MainPipeOut;
        double MainBufferIn;
        double MainBufferOut;
        Polyline MainPipeRoad;

        //临时需要的数据
        BufferTreeNode buffer_tree = null;
        Polyline MainRegion;
        List<int> SkeletonType = new List<int>();

        // output
        public List<Polyline> Connector = new List<Polyline>();
        public List<Polyline> Skeleton = new List<Polyline>();
        bool IsCCW = true;
        bool IfFind = true;


        public MainPipeGet(Polyline room, List<BufferPoly> shortest_way, int main_index, double buffer, double room_buffer, bool main_has_output)
        {
            this.Room = room;
            this.PipeList = shortest_way;
            this.main_index = main_index;
            this.Buffer = buffer;
            this.RoomBuffer = room_buffer;
            this.MainHasOutput = main_has_output;

            MainPipeIn = PipeList[main_index].poly.First();
            MainBufferIn = PipeList[main_index].buff.First();
            if (main_has_output)
            {
                MainPipeOut = shortest_way[main_index].poly.Last();
                MainBufferOut = shortest_way[main_index].buff.Last();
            }
            MainPipeRoad = PassageWayUtils.BuildPolyline(PipeList[main_index].poly);
        }

        public MainPipeGet(Polyline mainPipeRoad, Polyline room)
        {
            this.MainRegion = room;
            MainPipeRoad = mainPipeRoad;
            MainPipeIn = mainPipeRoad.GetPoint3dAt(0);
        }

        public void Pipeline() 
        {
            MainRegion = GetMainPipeArea();
            Polyline clonedMainRegion = MainRegion.Clone() as Polyline;
            //PassageShowUtils.ShowEntity(PassageWayUtils.Copy(MainRegion), 4);
            //AdjustRoom();
            //PassageShowUtils.ShowEntity(MainRegion, 5);

            if (!IfFind) return;
            DrawUtils.ShowGeometry(MainRegion, "l2AdjustedRoom", 4, lineWeightNum: 30);
            buffer_tree = GetBufferTree(MainRegion);
            GetSkeleton(buffer_tree);
            
            AdjustPolyline adjustPolyline = new AdjustPolyline(Skeleton, Connector, clonedMainRegion, Buffer * 0.85);
            adjustPolyline.Pipeline3();
            Skeleton = adjustPolyline.Skeleton;

            DrawUtils.ShowGeometry(Skeleton, "l2skeleton", 2, lineWeightNum: 30);
        }

        public Polyline GetMainPipeArea()
        {
            List<Polyline> pipeInputs = new List<Polyline>();
            for (int i = 0; i < PipeList.Count; i++) 
            {
                pipeInputs.Add(PassageWayUtils.BuildPolyline(PipeList[i].poly));
            }
          
            // init region
            Polyline main_region = new Polyline();
            //region = region.Buffer(max_dw * 0.5).Cast<Polyline>().OrderByDescending(o => o.Area).First();
            if (pipeInputs.Count > 1)
            {
                main_region.Dispose();
                if (main_index == 0)
                    main_region = MainRegionCalculator.GetMainRegion(pipeInputs[main_index + 1], Room, true);
                else if (main_index == pipeInputs.Count - 1)
                    main_region = MainRegionCalculator.GetMainRegion(pipeInputs[main_index - 1], Room, false);
                else
                    main_region = MainRegionCalculator.GetMainRegion(pipeInputs[main_index - 1], pipeInputs[main_index + 1], Room);
            }


            // init remove part
            DBObjectCollection rest = new DBObjectCollection();

            //rest.Add(shortest_way[main_index].Buffer(3));  //出口
            if (pipeInputs.Count > 1)
            {
                if (main_index > 0)
                    rest.Add(PipeList[main_index - 1].Buffer(4));
                if (main_index < pipeInputs.Count - 1)
                    rest.Add(PipeList[main_index + 1].Buffer(4));
            }
            // get rest

            rest.OfType<Polyline>().ToList().ForEach(x => DrawUtils.ShowGeometry(x, "l2Rest", 5, lineWeightNum: 30));
            DrawUtils.ShowGeometry(main_region, "l2OldMainArea", 6, lineWeightNum: 30);

            if(main_region.Area < 1000)
            {
                Skeleton.Add(MainPipeRoad);
                IfFind = false;
                SkeletonType.Add(-1);
                return new Polyline() ;
            }


            rest = main_region.Difference(rest);
            
            //var newRoom = AdjustBufferRoom();
            var newRoom = Room;
            var smallRegionPl = newRoom.Buffer(-RoomBuffer - 0.5 * Buffer/2);

            Polyline newRegion = new Polyline();
            List<Polyline> newRegionList = rest.OfType<Polyline>().ToList();
            if (newRegionList.Count > 0)
            {
                newRegion = newRegionList.FindByMax(x => x.Area);
                rest = newRegion.Intersection(smallRegionPl);
                newRegionList = rest.OfType<Polyline>().ToList();
                if (newRegionList.Count > 0)
                {
                    newRegion = newRegionList.FindByMax(x => x.Area);
                    DrawUtils.ShowGeometry(newRegion, "l2MainArea", 170, lineWeightNum: 30);
                }
                else
                {
                    Skeleton.Add(MainPipeRoad);
                    IfFind = false;
                    SkeletonType.Add(-1);
                }
            }
            else 
            {
                Skeleton.Add(MainPipeRoad);
                IfFind = false;
                SkeletonType.Add(-1);
            }

            newRegion.Closed = true;
            return newRegion;
        }

        Polyline AdjustBufferRoom()
        {
            var points = PassageWayUtils.GetPolyPoints(Room);
            // calculate start direction
            var pre = PassageWayUtils.GetSegIndexOnPolygon(MainPipeIn, points);
            var next = (pre + 1) % points.Count;
            var dir = PassageWayUtils.GetDirBetweenTwoPoint(points[pre], points[next]);
            var start_dir = (dir + 3) % 4;
            // adjust pin's neighbor edge
            if (MainPipeIn.DistanceTo(points[pre]) <= RoomBuffer + MainBufferIn + 1)
            {
                var ppre = (pre - 1 + points.Count) % points.Count;
                var old_pre_point = points[pre];
                dir = PassageWayUtils.GetDirBetweenTwoPoint(points[pre], points[ppre]);
                if (dir == start_dir)
                {
                    points[pre] = MainPipeIn + (old_pre_point - MainPipeIn).GetNormal() * (Buffer / 4 + RoomBuffer);
                    points[ppre] += (points[pre] - old_pre_point);
                }
            }
            else if (MainPipeIn.DistanceTo(points[next]) <= RoomBuffer + MainBufferIn + 1)
            {
                var nnext = (next + 1) % points.Count;
                var old_next_point = points[next];
                dir = PassageWayUtils.GetDirBetweenTwoPoint(points[next], points[nnext]);
                if (dir == start_dir)
                {
                    points[next] = MainPipeIn + (old_next_point - MainPipeIn).GetNormal() * (Buffer / 4 + RoomBuffer);
                    points[nnext] += (points[next] - old_next_point);
                }
            }
            // adjust pout's neighbor edge
            if (MainHasOutput)
            {
                pre = PassageWayUtils.GetSegIndexOnPolygon(MainPipeOut, points);
                next = (pre + 1) % points.Count;
                dir = PassageWayUtils.GetDirBetweenTwoPoint(points[pre], points[next]);
                var end_dir = (dir + 3) % 4;
                if (MainPipeOut.DistanceTo(points[pre]) <= RoomBuffer + MainBufferOut + 1)
                {
                    var ppre = (pre - 1 + points.Count) % points.Count;
                    var old_pre_point = points[pre];
                    dir = PassageWayUtils.GetDirBetweenTwoPoint(points[ppre], points[pre]);
                    if (dir == end_dir)
                    {
                        points[pre] = MainPipeOut + (old_pre_point - MainPipeOut).GetNormal() * (Buffer / 4 + RoomBuffer);
                        points[ppre] += (points[pre] - old_pre_point);
                    }
                }
                else if (MainPipeOut.DistanceTo(points[next]) <= RoomBuffer + MainBufferOut + 1)
                {
                    var nnext = (next + 1) % points.Count;
                    var old_next_point = points[next];
                    dir = PassageWayUtils.GetDirBetweenTwoPoint(points[nnext], points[next]);
                    if (dir == end_dir)
                    {
                        points[next] = MainPipeOut + (old_next_point - MainPipeOut).GetNormal() * (Buffer / 4 + RoomBuffer);
                        points[nnext] += (points[next] - old_next_point);
                    }
                }
            }
            // build buffer region
            points.Add(points.First());
            
            return PassageWayUtils.BuildPolyline(points);
        }

        void AdjustRoom()
        {
            MainRegion = PolylineProcessService.PlRegularization2(MainRegion, Buffer/4);
        }

        BufferTreeNode GetBufferTree(Polyline poly, bool flag = false)
        {
            //if (!poly.IsCCW()) poly.ReverseCurve();
            PolylineProcessService.ClearPolyline(ref poly);

            BufferTreeNode node = new BufferTreeNode(poly);
            DrawUtils.ShowGeometry(poly, "l2BufferedPl", 3, lineWeightNum: 30);
            var next_buffer = PassageWayUtils.Buffer(poly, -Buffer);
            //if (next_buffer.Count == 0) return node;
            var next_small_buffer = PassageWayUtils.Buffer(poly, -(Buffer * 0.80));
            double lengthBig = 0;
            double lengthSmall = 0;
            int numBig = 0;
            int numSmall = 0;
            next_buffer.ForEach(x => lengthBig += x.Length);
            next_buffer.ForEach(x => numBig += x.NumberOfVertices);
            next_small_buffer.ForEach(x => lengthSmall += x.Length);
            next_small_buffer.ForEach(x => numSmall += x.NumberOfVertices);

            if ((next_buffer.Count == 0 && next_small_buffer.Count > 0) ||
                (numSmall > numBig && lengthSmall > lengthBig + 500)) 
            {
                next_buffer = next_small_buffer;
            }


            if(next_buffer.Count == 0)  return node;
       

            node.childs = new List<BufferTreeNode>();
            foreach (Polyline child_poly in next_buffer)
            {
                if (child_poly.Area > 1000)
                {
                    var child = GetBufferTree(child_poly, false);
                    child.parent = node;
                    node.childs.Add(child);
                }
            }
            return node;
        }

        void GetSkeleton(BufferTreeNode node)
        {
            //DealWithShell(node);
            DealWithShellNew(node);
            //skeleton.Add(node.shell);
            
            if (node.parent == null && IfFind == false) return; 

            if (node.childs == null) return;
            foreach (var child in node.childs)
                GetSkeleton(child);
        }

        void DealWithShellNew(BufferTreeNode node)
        {    
            //double buffer = TestData.SuggestPipeDis;
            var coords = PassageWayUtils.GetPolyPoints(node.shell);

            if (node.parent == null)
            {
                var pin = MainPipeIn;
                Point3d closePoint = node.shell.GetClosePoint(pin);

                Point3d point = GetInputPoint();
                if (point.Equals(new Point3d(0, 0, 0)))
                {
                    Skeleton.Add(MainPipeRoad);
                    IfFind = false;
                    //SkeletonType.Add(-1);
                    return;
                }
                else if(pin.DistanceTo(point) > pin.DistanceTo(closePoint) + Buffer) 
                {
                    point = closePoint;
                }

                int index = PassageWayUtils.GetPointIndex(point, coords);
                int indexFlag = index;
                if (index != -1) //在端点上
                {
                    var pre = (index + coords.Count - 1) % coords.Count;
                    var next = (index + 1) % coords.Count;
                    //if ((coords[pre] - coords[index]).CrossProduct(coords[next] - coords[index]).Z < 0)
                    //    index = next;
                    Vector3d dirPipeIn = point - pin;
                    double scoreCW = dirPipeIn.GetNormal().DotProduct((coords[pre] - coords[index]).GetNormal());
                    double scoreCCW = dirPipeIn.GetNormal().DotProduct((coords[next] - coords[index]).GetNormal());
                    if (scoreCW > scoreCCW)
                    {
                        IsCCW = false;
                        coords.Reverse();
                        index = PassageWayUtils.GetPointIndex(point, coords);
                        PassageWayUtils.RearrangePoints(ref coords, index);
                    }
                    else
                    {
                        PassageWayUtils.RearrangePoints(ref coords, index);
                    }
                }
                else //index == -1,说明不在端点上
                {
                    index = PassageWayUtils.GetSegIndexOnPolygon(point, coords);
                    var pre = (index + coords.Count - 1) % coords.Count;
                    var next = (index + 1) % coords.Count;

                    double lengthCW = (coords[index] - point).Length;
                    double lengthCCW = (coords[next] - point).Length;

                    if (lengthCW < lengthCCW)
                    {
                        node.shell.ReverseCurve();
                        IsCCW = false;
                        coords = PassageWayUtils.GetPolyPoints(node.shell);
                        index = PassageWayUtils.GetSegIndexOnPolygon(point, coords);
                        PassageWayUtils.RearrangePoints(ref coords, next);
                    }
                    else
                    {
                        PassageWayUtils.RearrangePoints(ref coords, next);
                    }
                }
                // cut last segment
                var p0 = coords.First();
                var p1 = coords.Last();
                if (indexFlag != -1)
                {
                    if (p1.DistanceTo(p0) > Buffer + 100)
                        coords.Add(p0 - (p1 - p0).GetNormal() * -Buffer);
                }
                //删除小点
                //if (p1.DistanceTo(p0) < Buffer - 100)
                //    coords.RemoveAt(coords.Count - 1);

                //while (true)
                //{
                //    if (coords.Count <= 2) break;
                //    var newP1 = coords.Last();
                //    Vector3d disVec = p0 - newP1;
                //    if (disVec.Length < Buffer - 100)
                //    {
                //        coords.RemoveAt(coords.Count - 1);
                //    }
                //    else break;
                //}
                // add first segment
                if (point.DistanceTo(coords[0]) > 1)
                {
                    coords.Insert(0, point);
                }

                DrawUtils.ShowGeometry(point, "l2PIN", 30, lineWeightNum: 30, 200, "C");
            }
            else
            {
                Point3d parentLast = node.parent.shell.GetPoint3dAt(node.parent.shell.NumberOfVertices - 1);
                Point3d point = new Point3d();
                Point3d pin = new Point3d();
                if (node.shell.GetClosePoint(parentLast).DistanceTo(parentLast) < 2 * Buffer)
                {
                    point = node.shell.GetClosePoint(parentLast);
                    Point3d tmpPin = node.parent.shell.GetClosePoint(point);
                    if (tmpPin.DistanceTo(point) < point.DistanceTo(parentLast) - 10)
                    {
                        pin = tmpPin;
                    }
                    else
                    {
                        pin = parentLast;
                    }
                }
                else
                {
                    pin = GetClosedPointAtoB(node.parent.shell, node.shell);
                    point = node.shell.GetClosePoint(pin);
                }

                if (!IsCCW) node.shell.ReverseCurve();
                int index = PassageWayUtils.GetPointIndex(point, coords);
                int indexFlag = index;
                if (index == -1)
                {
                    index = PassageWayUtils.GetSegIndexOnPolygon(point, coords);
                    var pre = (index + coords.Count - 1) % coords.Count;
                    var next = (index + 1) % coords.Count;

                    double lengthCW = (coords[index] - point).Length;
                    double lengthCCW = (coords[next] - point).Length;

                    PassageWayUtils.RearrangePoints(ref coords, next);
                }
                else
                {
                    if (!node.shell.IsCCW()) node.shell.ReverseCurve();
                    index = PassageWayUtils.GetPointIndex(point, coords);
                    var pre = (index + coords.Count - 1) % coords.Count;
                    var next = (index + 1) % coords.Count;
                    Vector3d dirPipeIn = point - pin;
                    double scoreCW = dirPipeIn.GetNormal().DotProduct((coords[pre] - coords[index]).GetNormal());
                    double scoreCCW = dirPipeIn.GetNormal().DotProduct((coords[next] - coords[index]).GetNormal());
                    if (scoreCW > scoreCCW)
                    {
                        //IsCCW = false;
                        coords.Reverse();
                        index = PassageWayUtils.GetPointIndex(point, coords);
                        PassageWayUtils.RearrangePoints(ref coords, index);
                    }
                    else
                    {
                        PassageWayUtils.RearrangePoints(ref coords, index);
                    }
                }
                // cut last segment
                var p0 = coords.First();
                var p1 = coords.Last();
                if (indexFlag != -1)
                {
                    if (p1.DistanceTo(p0) > Buffer + 100)
                        coords.Add(p0 - (p1 - p0).GetNormal() * -Buffer);
                }
                //if (p1.DistanceTo(p0) < Buffer - 100)
                //    coords.RemoveAt(coords.Count - 1);

                //while (true)
                //{
                //    if (coords.Count <= 2) break;
                //    var newP1 = coords.Last();
                //    Vector3d disVec = p0 - newP1;
                //    if (disVec.Length < Buffer - 100)
                //    {
                //        coords.RemoveAt(coords.Count - 1);
                //    }
                //    else break;
                //}


                // add first segment
                if (point.DistanceTo(coords[0]) < 1)
                    //coords[0] = pin;
                     coords.Insert(0, pin);
                else
                {
                    coords.Insert(0, point);
                    coords.Insert(0, pin);
                }
                
                DrawUtils.ShowGeometry(pin, "l2pin", 20, lineWeightNum: 30,200,"C");
            }
            ///

            coords = ClearUnclosedCoords(coords);
            node.SetShell(PassageWayUtils.BuildPolyline(coords));
            Skeleton.Add(node.shell);
            if (node.childs == null) SkeletonType.Add(0);
            else SkeletonType.Add(1);
                
            DrawUtils.ShowGeometry(node.shell, "l2NodeShell", 7, lineWeightNum: 30);
        }

        public Point3d GetInputPoint() 
        {
            Polyline polyStart = new Polyline();
            Polyline polyEnd = new Polyline();
            Point3d start = new Point3d();
            Point3d end = new Point3d();

            List<Point3d> intersectionPointList = IntersectUtils.PolylineIntersectionPolyline(MainPipeRoad,buffer_tree.shell);

            if (intersectionPointList.Count == 0) return new Point3d(0, 0, 0);

            Dictionary<Point3d, double> pointDis = new Dictionary<Point3d, double>();
            for (int i = 0; i < intersectionPointList.Count; i++) 
            {
                double ptDis = GetDis(MainPipeRoad,intersectionPointList[i]);
                pointDis.Add(intersectionPointList[i], ptDis);   
            }

            intersectionPointList = intersectionPointList.OrderBy(x => pointDis[x]).ToList();
            start = intersectionPointList.First();
            end = intersectionPointList.Last();

            List<Point3d> coords = PassageWayUtils.GetPolyPoints(MainPipeRoad);
            int indexStart = PassageWayUtils.GetSegIndex2(start, coords);
            for (int i = 0; i <= indexStart; i++) 
            {
                polyStart.AddVertexAt(polyStart.NumberOfVertices, coords[i].ToPoint2D(), 0, 0, 0);
            }
            polyStart.AddVertexAt(polyStart.NumberOfVertices, start.ToPoint2D(), 0, 0, 0);

            if (MainHasOutput)
            {
                int indexEnd = PassageWayUtils.GetSegIndex2(end, coords);
                polyEnd.AddVertexAt(polyEnd.NumberOfVertices, end.ToPoint2D(), 0, 0, 0);
                for (int i = indexEnd + 1; i <= coords.Count - 1; i++)
                {
                    polyEnd.AddVertexAt(polyEnd.NumberOfVertices, coords[i].ToPoint2D(), 0, 0, 0);
                }
            }

            DrawUtils.ShowGeometry(polyStart, "l2PolyStart", 2, lineWeightNum: 30);
            DrawUtils.ShowGeometry(polyEnd, "l2PolyEnd", 2, lineWeightNum: 30);
            if (polyStart.NumberOfVertices > 1)
            {
                polyStart.Closed = false;
                Connector.Add(polyStart);
            }
            if (polyEnd.NumberOfVertices > 1)
            {
                polyEnd.Closed = false;
                Connector.Add(polyEnd);
            }

            return start;
        } 
       
        Point3d GetClosedPointAtoB(Polyline a, Polyline b)
        {
            //double buffer = TestData.SuggestPipeDis;

            Point3d ret = a.EndPoint;
            var dis = b.Distance(ret);
            // A is open while B is closed
            for (int i = a.NumberOfVertices - 1; i >= 0; --i)
            {
                var cur_dis = b.Distance(a.GetPoint3dAt(i));
                if (cur_dis < dis - 1)
                {
                    dis = cur_dis;
                    ret = a.GetPoint3dAt(i);
                }
                if (i != 0) 
                {
                    Point3d pt0 = a.GetPoint3dAt(i);
                    Point3d pt1 = a.GetPoint3dAt(i-1);
                    Polyline newLine = new Polyline();
                    newLine.AddVertexAt(0, pt0.ToPoint2D(), 0, 0, 0);
                    newLine.AddVertexAt(1, pt1.ToPoint2D(), 0, 0, 0);
                    for (int j = 0; j < b.NumberOfVertices; j++) 
                    {
                        Point3d ptb = b.GetPoint3dAt(j);
                        cur_dis = newLine.DistanceTo(ptb,false);
                        if (cur_dis < dis - 1)
                        {
                            dis = cur_dis;
                            ret = newLine.GetClosePoint(ptb);
                        }
                    }
                }
            }

            if (dis > Math.Abs(1.5 * Buffer))
            {
                var point_on_b = b.GetClosePoint(ret);
                ret = a.GetClosePoint(point_on_b);
            }
            return ret;
        }

        public double GetDis(Polyline pl, Point3d pt) 
        {
            double dis = 0;
            var coords = PassageWayUtils.GetPolyPoints(pl);
            int index = PassageWayUtils.GetSegIndex2(pt, coords);
            for (int i = 0; i < index ; i++) 
            {
                dis += (coords[i + 1] - coords[i]).Length;
            }
            dis += (pt - coords[index]).Length;
            return dis;
        }

        public List<Point3d> ClearUnclosedCoords(List<Point3d> coords) 
        {
            // List<Point3d> newList = coords.Copy(); //change copy
            List<Point3d> newList = new List<Point3d>();
            newList.AddRange(coords);
            if (coords.Count < 3) return newList;
            if ((newList[newList.Count - 1] - newList[newList.Count - 2]).Length < Buffer - 100)
                newList.RemoveAt(newList.Count - 1);
            //else if (newList.Count >= 4 && (newList[newList.Count - 2] - newList[newList.Count - 3]).Length < Buffer - 100) 
            //{
            //    newList.RemoveAt(newList.Count - 1);
            //    newList.RemoveAt(newList.Count - 1);
            //}

            return newList;
        }

        public Polyline ClearUnclosedPl(Polyline pl)
        {
            return new Polyline();
        }
    }
}
