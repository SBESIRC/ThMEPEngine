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
    class MainPipeGet2
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
        List<Polyline> MainReigonList = new List<Polyline>(); 
        List<int> SkeletonType = new List<int>();
        List<Polyline> ExcessPlList = new List<Polyline>();
        List<Polyline> MainReigonListCopy = new List<Polyline>();

        // output
        public List<Polyline> Connector = new List<Polyline>();
        public List<Polyline> Skeleton = new List<Polyline>();
        public List<Polyline> TmpSkeleton = new List<Polyline>();
        public List<Polyline> BufferedPipeList = new List<Polyline>();
        bool IsCCW = false;
        bool IfFind = true;

        public MainPipeGet2(Polyline room, List<BufferPoly> shortest_way, int main_index, double buffer, double room_buffer, bool main_has_output)
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

        public MainPipeGet2(Polyline mainPipeRoad, Polyline room)
        {
            this.MainRegion = room;
            MainPipeRoad = mainPipeRoad;
            MainPipeIn = mainPipeRoad.GetPoint3dAt(0);
        }

        //主导管线带出口
        public void Pipeline2() 
        {
            GetMainPipeArea(0,0);
            GetMainPipeArea(0,1);

            if (MainReigonListCopy.Count == 0) return;
            if (CheckSmallBetter()) MainReigonList = MainReigonListCopy;

            //将最外层框线构造成房间
            List<Polyline> MainRegionListRoom = new List<Polyline>();
            for (int i = 0; i < MainReigonList.Count; i++)
            {
                Polyline mainReigonListRoom = PassageWayUtils.Buffer(MainReigonList[i].Clone() as Polyline, 0.25 *Buffer+ Parameter.SuggestDistanceWall).First();
                DrawUtils.ShowGeometry(mainReigonListRoom, "l4AdjustedRoom", 4, lineWeightNum: 30);
                MainRegionListRoom.Add(mainReigonListRoom);
            }

            if (!IfFind) return;

            for (int i = 0; i < MainRegionListRoom.Count; i++)
            {
                Point3d pin = new Point3d();     //主导管线最短路径上的点
                Point3d point = new Point3d();   //要连接的剩余区域最外层框线上的点
                FindPin2(MainReigonList[i], ref pin, ref point);
                if (point == new Point3d() || (point - pin).Length> Buffer*1.5) continue;

                //此处是为了将最外层框线上的点生造成房间，因此point点要变换
                Point3d newPt = pin + (pin - point).GetNormal() * Buffer;
                Polyline pinToPoint = new Polyline();
                pinToPoint.AddVertexAt(0, newPt.ToPoint2D(), 0, 0, 0);
                pinToPoint.AddVertexAt(0, point.ToPoint2D(), 0, 0, 0);
                DrawUtils.ShowGeometry(MainReigonList[i], "l4MainShell", 5, 30);
                DrawUtils.ShowGeometry(MainPipeRoad, "l4MainPipeRoad", 0, 30);
                DrawUtils.ShowGeometry(pinToPoint, "l8Tmp", 1, 30);
                point = IntersectUtils.PolylineIntersectionPolyline(MainRegionListRoom[i],pinToPoint).First();

                double pinBuffer = GetPinBuffer(pin,point);
                DrawUtils.ShowGeometry(point, "l4SPin", 0, 30, (int)pinBuffer);

                List<DrawPipeData> pipeInList = new List<DrawPipeData>();
                pipeInList.Add(new DrawPipeData(point, pinBuffer, 0, 20));
                RoomPipeGenerator1 roomPipeGenerator = new RoomPipeGenerator1(MainRegionListRoom[i], pipeInList, Buffer, Parameter.SuggestDistanceWall);
                roomPipeGenerator.CalculatePipeline();
                // show result
                //var show = roomPipeGenerator.skeleton;
                //show.ForEach(x => DrawUtils.ShowGeometry(x, "l4RoomPipe", pipeInList[0].PipeId % 7 + 1, 30));
                PipeOutput output = roomPipeGenerator.output;

                BufferedPipeList.Add(output.shape);
                //chatou
                Line line0 = new Line(pin, point + (point - pin).GetNormal() * 50);
                Polyline chatou = line0.Buffer(pinBuffer);
                BufferedPipeList.Add(chatou);
                DrawUtils.ShowGeometry(BufferedPipeList, "l4SinglePipe", pipeInList[0].PipeId % 7 + 1, 30);
            }

            //DrawUtils.ShowGeometry(Skeleton, "l2skeleton", 2, lineWeightNum: 30);
            //Skeleton.Add(MainPipeRoad);
        }

        public void Pipeline3()
        {
            GetMainPipeArea(1, 0);
            GetMainPipeArea(1,1);

            if (MainReigonListCopy.Count == 0) return;
            if (CheckSmallBetter()) MainReigonList = MainReigonListCopy;

            Point3d point = MainPipeRoad.StartPoint;  //起点
            List<Polyline> MainRegionListRoom = new List<Polyline>();
            for (int i = 0; i < MainReigonList.Count; i++)
            {
                //if (ProcessedData.HaveVirtualPipe) 
                //{
                //    Polyline mainPipeBuffer = PipeList[main_index].Buffer(1);
                //    DrawUtils.ShowGeometry(mainPipeBuffer, "l8MainPipeBuffer", 4, lineWeightNum: 30);

                //    DBObjectCollection shape = new DBObjectCollection();
                //    shape.Add(mainPipeBuffer);
                //    shape.Add(MainReigonList[i]);

                //    MainReigonList[i] = shape.UnionPolygons().Cast<Polyline>().ToList().FindByMax(x => x.Area);
                //    DrawUtils.ShowGeometry(MainReigonList[i], "l8UnionedPl", 4, lineWeightNum: 30);
                //}

                Polyline mainReigonListRoom = PassageWayUtils.Buffer(MainReigonList[i].Clone() as Polyline, Parameter.SuggestDistanceWall).First();
                DrawUtils.ShowGeometry(mainReigonListRoom, "l4AdjustedRoom", 4, lineWeightNum: 30);

                double disToStart = mainReigonListRoom.GetClosestPointTo(point,false).DistanceTo(point);
                if (disToStart < 2000)
                {
                    MainRegionListRoom.Add(mainReigonListRoom);
                }
                else
                {
                    if (mainReigonListRoom.Area > 6000000)
                    {
                        ProcessedData.HaveVirtualPipe = true;
                        ProcessedData.VirtualPlNow = mainReigonListRoom;
                        return;
                    }
                }
            }

            if (!IfFind) return;

            for (int i = 0; i < MainRegionListRoom.Count; i++)
            {
                Point3d pin = new Point3d();
                double halfBuffer = PipeList[main_index].buff[0];
                DrawUtils.ShowGeometry(point, "l4SPin", 5, 30, (int)halfBuffer);

                List<DrawPipeData> pipeInList = new List<DrawPipeData>();
                pipeInList.Add(new DrawPipeData(point, halfBuffer, 0, 20));
                RoomPipeGenerator1 roomPipeGenerator = new RoomPipeGenerator1(MainRegionListRoom[i], pipeInList, Buffer, Parameter.SuggestDistanceWall);
                roomPipeGenerator.CalculatePipeline();
                // show result
                //var show = roomPipeGenerator.skeleton;
                //show.ForEach(x => DrawUtils.ShowGeometry(x, "l4RoomPipe", pipeInList[0].PipeId % 7 + 1, 30));
                PipeOutput output = roomPipeGenerator.output;
                DrawUtils.ShowGeometry(output.shape, "l4SinglePipe", pipeInList[0].PipeId % 7 + 1, 30);
                BufferedPipeList.Add(output.shape);
            }
            
            //DrawUtils.ShowGeometry(Skeleton, "l2skeleton", 2, lineWeightNum: 30);
            //Skeleton.Add(MainPipeRoad);
        }

        public Polyline GetMainPipeArea(int mode,int isSmall)
        {
            //容差
            double smallCoefficient = 1;
            if (isSmall == 1) smallCoefficient = 0.85;

            //管道中心线
            List<Polyline> pipeInputs = new List<Polyline>();
            for (int i = 0; i < PipeList.Count; i++)
            {
                pipeInputs.Add(PassageWayUtils.BuildPolyline(PipeList[i].poly));
            }


            // init region
            //寻找可放置区域
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
            DrawUtils.ShowGeometry(main_region, "l2OldMainArea", 6, lineWeightNum: 30);

            //移除管道Buffer
            // init remove part
            DBObjectCollection rest0 = new DBObjectCollection();
            //rest.Add(shortest_way[main_index].Buffer(3));  //出口
            if (pipeInputs.Count > 1)
            {
                if (mode == 0)
                {
                    if (main_index > 0)
                        rest0.Add(PassageWayUtils.Buffer(PipeList[main_index - 1].Buffer(1), 0.75 * Buffer* smallCoefficient).First());
                    if (main_index < pipeInputs.Count - 1)
                        rest0.Add(PassageWayUtils.Buffer(PipeList[main_index + 1].Buffer(1), 0.75 * Buffer* smallCoefficient).First());
                }
                else if(mode == 1)
                {
                    if (main_index > 0)
                        rest0.Add(PipeList[main_index - 1].Buffer(3*smallCoefficient));
                    if (main_index < pipeInputs.Count - 1)
                        rest0.Add(PipeList[main_index + 1].Buffer(3*smallCoefficient));
                }
            }
            //移除主导管线管道Buffer
            // get rest
            if (mode == 0)
            {
                rest0.Add(PassageWayUtils.Buffer(PipeList[main_index].Buffer(1), 0.75 * Buffer * smallCoefficient).First());
                
                DrawUtils.ShowGeometry(PipeList[main_index].Buffer(1), "l4Test", 9, lineWeightNum: 30);
            }

            rest0.OfType<Polyline>().ToList().ForEach(x => DrawUtils.ShowGeometry(x, "l2Rest", 5, lineWeightNum: 30));
           

            if (main_region.Area < 1000)
            {
                Skeleton.Add(MainPipeRoad);
                IfFind = false;
                return new Polyline();
            }


            //注意此处，如果是Pipeline3，即主导管线没有出口的情况，理论上只能出来一个区域，这里需要进一步修改
            var rest = main_region.Difference(rest0);
            List<Polyline> newRegionList = rest.OfType<Polyline>().ToList();



            //var newRoom = AdjustBufferRoom();
            var newRoom = Room;
            double smallRoomBuffer = -RoomBuffer - 0.5 * Buffer / 2;
            if (mode == 1) smallRoomBuffer = smallRoomBuffer + 0.5 * Buffer / 2;
            var smallRegionPl = newRoom.Buffer(smallRoomBuffer);
            DrawUtils.ShowGeometry(smallRegionPl.OfType<Polyline>().ToList(), "l3SmallMainRoom", 8, lineWeightNum: 30);
            Polyline newRegion = new Polyline();
            
            if (newRegionList.Count > 0)
            {
                DrawUtils.ShowGeometry(newRegionList, "l3BeforeInterMainRegion", 5, lineWeightNum: 30);
                newRegion = newRegionList.FindByMax(x => x.Area);
                List<Polyline> interNewRegionList = new List<Polyline>();
                for (int i = 0; i < newRegionList.Count; i++) 
                {
                    rest = newRegionList[i].Intersection(smallRegionPl);
                    interNewRegionList.AddRange(rest.OfType<Polyline>().ToList());
                }
                
                if (interNewRegionList.Count > 0)
                {
                    newRegion = interNewRegionList.FindByMax(x => x.Area);
                    interNewRegionList.RemoveAll(x => x.Area < 5000);
                    if (isSmall == 1) MainReigonListCopy = interNewRegionList;
                    else MainReigonList = interNewRegionList;
                    DrawUtils.ShowGeometry(interNewRegionList, "l2MainArea", 170, lineWeightNum: 30);
                    IfFind = true;
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

        void AdjustRoom()
        {
            MainRegion = PolylineProcessService.PlRegularization2(MainRegion, Buffer / 2);
        }

        BufferTreeNode GetBufferTree(Polyline poly, bool flag = false)
        {
            //if (!poly.IsCCW()) poly.ReverseCurve();
            PolylineProcessService.ClearPolyline(ref poly);
            DrawUtils.ShowGeometry(poly, "l3OrBufferedPl", 5, lineWeightNum: 30);

            //ClearSingleBuffer clearSingleBuffer = new ClearSingleBuffer(poly, poly, Buffer * 0.5);
            //clearSingleBuffer.Pipeline();
            //poly = clearSingleBuffer.ClearedPl;

            BufferTreeNode node = new BufferTreeNode(poly);
            DrawUtils.ShowGeometry(poly, "l2BufferedPl", 3, lineWeightNum: 30);
            var next_buffer = PassageWayUtils.Buffer(poly, -Buffer);
            //if (next_buffer.Count == 0) return node;
            var next_small_buffer = PassageWayUtils.Buffer(poly, -(Buffer * 0.86));
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

            if (next_buffer.Count == 0) return node;

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

        BufferTreeNode GetClearedBufferTree(Polyline poly, bool flag = false)
        {
            //if (!poly.IsCCW()) poly.ReverseCurve();
            PolylineProcessService.ClearPolyline(ref poly);


            BufferTreeNode node = new BufferTreeNode(poly);
            DrawUtils.ShowGeometry(poly, "l2BufferedPl", 3, lineWeightNum: 30);
            var next_buffer = PassageWayUtils.Buffer(poly, -Buffer);
            //if (next_buffer.Count == 0) return node;
            var next_small_buffer = PassageWayUtils.Buffer(poly, -(Buffer * 0.86));
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


            if (next_buffer.Count == 0) return node;


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
            if (node.parent == null && IfFind == false) return;
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

                Point3d pin = new Point3d();
                Point3d point = new Point3d();

                FindPin(coords,ref pin,ref point);
                
                //Point3d point = GetInputPoint();
                //coords = PassageWayUtils.GetPolyPoints(node.shell);
                if (point.Equals(new Point3d(0, 0, 0)))
                {
                    return;
                }
                else if (pin.DistanceTo(point) > 1.5 * Buffer)
                {
                    return;
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
                    double scoreCCW = dirPipeIn.GetNormal().DotProduct((coords[pre] - coords[index]).GetNormal());
                    if ((coords[pre] - coords[index]).Length < Buffer) scoreCCW = scoreCCW - 2;

                    double scoreCW = dirPipeIn.GetNormal().DotProduct((coords[next] - coords[index]).GetNormal());
                    if ((coords[next] - coords[index]).Length < Buffer) scoreCW = scoreCW - 2;

                    if (scoreCCW > scoreCW)
                    {
                        IsCCW = true;
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

                    double lengthCCW = (coords[index] - point).Length;
                    double lengthCW = (coords[next] - point).Length;

                    bool toCCW = false;
                    if (Math.Min(lengthCW, lengthCCW) < Buffer / 2)
                    {
                        toCCW = lengthCCW < lengthCW;
                    }
                    else toCCW = lengthCCW > lengthCW;

                    if (toCCW)
                    {
                        IsCCW = true;
                        coords = PassageWayUtils.GetPolyPoints(node.shell);
                        coords.Reverse();
                        index = PassageWayUtils.GetSegIndexOnPolygon(point, coords);
                        next = (index + 1) % coords.Count;
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
                if (point.DistanceTo(coords[0]) < 1)
                    //coords[0] = pin;
                    coords.Insert(0, pin);
                else
                {
                    coords.Insert(0, point);
                    coords.Insert(0, pin);
                }

                DrawUtils.ShowGeometry(point, "l2PIN", 30, lineWeightNum: 30, 200, "C");
            }
            else
            {
                bool isParentLast = false;
                Point3d parentLast = node.parent.shell.GetPoint3dAt(node.parent.shell.NumberOfVertices - 1);
                Point3d point = new Point3d();
                Point3d pin = new Point3d();
                if (node.shell.GetClosePoint(parentLast).DistanceTo(parentLast) < 2 * Buffer)
                {
                    point = node.shell.GetClosePoint(parentLast);
                    Point3d tmpPin = node.parent.shell.GetClosePoint(point);
                    if (tmpPin.DistanceTo(point) < (point.DistanceTo(parentLast) - 20))
                    {
                        pin = tmpPin;
                    }
                    else
                    {
                        pin = parentLast;
                        int tmpIndex = PassageWayUtils.GetPointIndex(point, coords);
                        if (tmpIndex == -1)
                        {
                            tmpIndex = PassageWayUtils.GetSegIndexOnPolygon(point, coords);
                            var pre = (tmpIndex + coords.Count - 1) % coords.Count;
                            var next = (tmpIndex + 1) % coords.Count;

                            double lengthCCW = (coords[tmpIndex] - point).Length;
                            double lengthCW = (coords[next] - point).Length;
                            if (Math.Min(lengthCW, lengthCCW) < 0.32 * Buffer)
                            {
                                if (lengthCW < lengthCCW)
                                {

                                    Point3d newLast = pin + (coords[next] - point);
                                    point = coords[next];
                                    pin = newLast;
                                    node.parent.shell.RemoveVertexAt(node.parent.shell.NumberOfVertices - 1);
                                    node.parent.shell.AddVertexAt(node.parent.shell.NumberOfVertices, newLast.ToPoint2D(), 0, 0, 0);
                                }
                                else
                                {
                                    Point3d newLast = pin + (coords[tmpIndex] - point);
                                    point = coords[tmpIndex];
                                    pin = newLast;
                                    node.parent.shell.RemoveVertexAt(node.parent.shell.NumberOfVertices - 1);
                                    node.parent.shell.AddVertexAt(node.parent.shell.NumberOfVertices, newLast.ToPoint2D(), 0, 0, 0);
                                }

                                DrawUtils.ShowGeometry(node.parent.shell, "l3AdjustNodeShell", 8, lineWeightNum: 30);
                            }
                        }
                        else if (Math.Abs(point.X - pin.X) > 5 && Math.Abs(point.Y - pin.Y) > 5)
                        {
                            if (Math.Abs(point.X - pin.X) < Buffer * 0.25)
                            {
                                pin = new Point3d(point.X, pin.Y, 0);
                                node.parent.shell.RemoveVertexAt(node.parent.shell.NumberOfVertices - 1);
                                node.parent.shell.AddVertexAt(node.parent.shell.NumberOfVertices, pin.ToPoint2D(), 0, 0, 0);
                            }
                            else if (Math.Abs(point.Y - pin.Y) < Buffer * 0.25)
                            {
                                pin = new Point3d(pin.X, point.Y, 0);
                                node.parent.shell.RemoveVertexAt(node.parent.shell.NumberOfVertices - 1);
                                node.parent.shell.AddVertexAt(node.parent.shell.NumberOfVertices, pin.ToPoint2D(), 0, 0, 0);
                            }

                        }
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
                    double scoreCCW = dirPipeIn.GetNormal().DotProduct((coords[pre] - coords[index]).GetNormal());
                    double scoreCW = dirPipeIn.GetNormal().DotProduct((coords[next] - coords[index]).GetNormal());
                    if (scoreCCW > scoreCW)
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

                DrawUtils.ShowGeometry(pin, "l2pin", 20, lineWeightNum: 30, 200, "C");
            }
            ///

            coords = ClearUnclosedCoords(coords);
            node.SetShell(PassageWayUtils.BuildPolyline(coords));
            TmpSkeleton.Add(node.shell);

            DrawUtils.ShowGeometry(node.shell, "l2NodeShell", 7, lineWeightNum: 30);
        }

        public Point3d GetInputPoint()
        {
            Polyline polyStart = new Polyline();
            Polyline polyEnd = new Polyline();
            Point3d start = new Point3d();
            Point3d end = new Point3d();

            List<Point3d> intersectionPointList = IntersectUtils.PolylineIntersectionPolyline(MainPipeRoad, buffer_tree.shell);

            //没有交到，分两种情况，没有buffer/buffer有偏移
            if (intersectionPointList.Count == 0)
            {
                List<Point3d> ptList = buffer_tree.shell.GetPoints().ToList();
                int index = -1;
                double minDis = 10000;

                for (int i = 0; i < ptList.Count; i++)
                {
                    double nowDis = MainPipeRoad.DistanceTo(ptList[i], false);
                    if (nowDis < minDis)
                    {
                        minDis = nowDis;
                        index = i;
                    }
                }

                if (index != -1 && buffer_tree.shell.Area > 5000)
                {

                    List<int> changeIndex = new List<int>();
                    changeIndex.Add(index);

                    Point3d pre = ptList[(index - 1 + ptList.Count) % ptList.Count];
                    Point3d next = ptList[(index + 1) % ptList.Count];
                    if (Math.Abs(MainPipeRoad.DistanceTo(pre, false) - minDis) < 20)
                    {
                        changeIndex.Add((index - 1 + ptList.Count) % ptList.Count);
                    }
                    else if (Math.Abs(MainPipeRoad.DistanceTo(next, false) - minDis) < 20)
                    {
                        changeIndex.Add((index + 1) % ptList.Count);
                    }

                    if (changeIndex.Count == 2)
                    {
                        for (int i = 0; i < changeIndex.Count; i++)
                        {
                            Point3d oldPt = ptList[changeIndex[i]];
                            Point3d newPt = MainPipeRoad.GetClosestPointTo(oldPt, false);
                            buffer_tree.shell.RemoveVertexAt(changeIndex[i]);
                            buffer_tree.shell.AddVertexAt(changeIndex[i], newPt.ToPoint2D(), 0, 0, 0);
                        }
                        var ptListTmp = buffer_tree.shell.GetPoints().ToList();

                        intersectionPointList = IntersectUtils.PolylineIntersectionPolyline(MainPipeRoad, buffer_tree.shell);
                    }
                }
            }

            if (intersectionPointList.Count == 0) return new Point3d(0, 0, 0);

            Dictionary<Point3d, double> pointDis = new Dictionary<Point3d, double>();
            for (int i = 0; i < intersectionPointList.Count; i++)
            {
                double ptDis = GetDis(MainPipeRoad, intersectionPointList[i]);
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
                    Point3d pt1 = a.GetPoint3dAt(i - 1);
                    Polyline newLine = new Polyline();
                    newLine.AddVertexAt(0, pt0.ToPoint2D(), 0, 0, 0);
                    newLine.AddVertexAt(1, pt1.ToPoint2D(), 0, 0, 0);
                    for (int j = 0; j < b.NumberOfVertices; j++)
                    {
                        Point3d ptb = b.GetPoint3dAt(j);
                        cur_dis = newLine.DistanceTo(ptb, false);
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
            for (int i = 0; i < index; i++)
            {
                dis += (coords[i + 1] - coords[i]).Length;
            }
            dis += (pt - coords[index]).Length;
            return dis;
        }

        public void FindPin(List<Point3d> coords, ref Point3d pin , ref Point3d point) 
        {
            double minDis = 100000;
            double roadDis = 100000;
            for (int i = 0; i < coords.Count; i++)
            {
                Point3d nowPt = coords[i];
                Point3d close = MainPipeRoad.GetClosestPointTo(nowPt, false);
                double nowDis = close.DistanceTo(nowPt);
                bool rec = Math.Abs(close.X - nowPt.X) < 1 || Math.Abs(close.Y - nowPt.Y) < 1;

                if (nowDis < minDis - 20)
                {
                    minDis = nowDis;
                    pin = close;
                    point = nowPt;
                    roadDis = GetDis(MainPipeRoad, close);
                }
                else if (Math.Abs(nowDis - minDis) < 20 && rec) 
                {
                    bool oldRec = Math.Abs(pin.X - point.X) < 1 || Math.Abs(point.Y - pin.Y) < 1;
                    double tmpRoadDis = GetDis(MainPipeRoad, close);
                    if (tmpRoadDis < roadDis ||(!oldRec && rec)) 
                    {
                        minDis = nowDis;
                        pin = close;
                        point = nowPt;
                        roadDis = tmpRoadDis;
                    }
                }
            }

            if (minDis == 100000) 
            {
                pin = new Point3d(0, 0, 0);
                point = new Point3d(0, 0, 0);
            }
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

        public void FindPin2(Polyline roomPl, ref Point3d pin, ref Point3d point)
        {
            var coords = PassageWayUtils.GetPolyPoints(roomPl);
            coords = SmoothUtils.SmoothPoints(coords);
            coords.Add(coords.First());

            double minDis = 100000;
            double roadDis = 100000;
            for (int i = 0; i < coords.Count; i++)
            {
                Point3d nowPt = coords[i];
                Point3d close = MainPipeRoad.GetClosestPointTo(nowPt, false);
                Point3d closeContrary = roomPl.GetClosestPointTo(close, false);

                if (close.DistanceTo(MainPipeRoad.StartPoint) < Parameter.SuggestDistanceWall ||
                    close.DistanceTo(MainPipeRoad.EndPoint) < Parameter.SuggestDistanceWall) continue;
                double nowDis = close.DistanceTo(nowPt);
                double nowDisC = closeContrary.DistanceTo(close);
                bool rec = Math.Abs(close.X - nowPt.X) < 1 || Math.Abs(close.Y - nowPt.Y) < 1;
                bool recC = Math.Abs(close.X - closeContrary.X) < 1 || Math.Abs(close.Y - closeContrary.Y) < 1;

                if (nowDis < minDis - 20)
                {
                    minDis = nowDis;
                    pin = close;
                    point = nowPt;
                    roadDis = GetDis(MainPipeRoad, close);
                }
                else if (Math.Abs(nowDis - minDis) < 20 && rec)
                {
                    bool oldRec = Math.Abs(pin.X - point.X) < 1 || Math.Abs(point.Y - pin.Y) < 1;
                    double tmpRoadDis = GetDis(MainPipeRoad, close);
                    if (tmpRoadDis < roadDis || (!oldRec && rec))
                    {
                        minDis = nowDis;
                        pin = close;
                        point = nowPt;
                        roadDis = tmpRoadDis;
                    }
                }

                if (nowDisC < minDis - 20)
                {
                    minDis = nowDisC;
                    pin = close;
                    point = closeContrary;
                    roadDis = GetDis(MainPipeRoad, close);
                }
                else if (Math.Abs(nowDisC - minDis) < 20 && recC)
                {
                    bool oldRec = Math.Abs(pin.X - point.X) < 1 || Math.Abs(point.Y - pin.Y) < 1;
                    double tmpRoadDis = GetDis(MainPipeRoad, close);
                    if (tmpRoadDis < roadDis || (!oldRec && recC))
                    {
                        minDis = nowDisC;
                        pin = close;
                        point = closeContrary;
                        roadDis = tmpRoadDis;
                    }
                }
            }

            if (minDis == 100000)
            {
                pin = new Point3d();
                point = new Point3d();
            }
        }


        public void FindPin3(Polyline roomPl, ref Point3d pin, ref Point3d point)
        {
            var coords = PassageWayUtils.GetPolyPoints(roomPl);
            var mainCoords = PassageWayUtils.GetPolyPoints(MainPipeRoad);

            bool find = false;
            for (int i = 1;i< mainCoords.Count-1;i++) 
            {
                Point3d nowPt = mainCoords[i];
                Point3d tmpPoint = roomPl.GetClosestPointTo(nowPt, false);
                if (nowPt.DistanceTo(tmpPoint) < 0.8 * Buffer) 
                {
                    find = true;
                    pin = nowPt;
                    point = tmpPoint;
                    break;
                }
            }

            if (!find)
            {
                pin = new Point3d();
                point = new Point3d();
            }
        }

        public double GetPinBuffer(Point3d pin,Point3d point) 
        {
            double r = Buffer / 4;
            List<Point3d> coords = PassageWayUtils.GetPolyPoints(MainPipeRoad);
            Vector3d nowVec = (point - pin).GetNormal(); 

            int index = PassageWayUtils.GetPointIndex(pin, coords);
            if (index != -1)
            {
                if (index == 0 || index == coords.Count - 1) return r;
                //r = Math.Min(PipeList[main_index].buff[index], PipeList[main_index].buff[index - 1]);
                Vector3d vec0 = (coords[index] - coords[index - 1]).GetNormal();
                Vector3d vec1 = (coords[index] - coords[index + 1]).GetNormal();
                if (Math.Abs(vec0.DotProduct(nowVec)) > Math.Abs(vec1.DotProduct(nowVec)) + 0.5)
                {
                    r = PipeList[main_index].buff[index - 1];
                }
                else if (Math.Abs(vec1.DotProduct(nowVec)) > Math.Abs(vec0.DotProduct(nowVec)) + 0.5)
                {
                    r = PipeList[main_index].buff[index];
                }
                else r = Math.Min(PipeList[main_index].buff[index], PipeList[main_index].buff[index - 1]);

            }
            else
            {
                index = PassageWayUtils.GetSegIndexOnPolyline(pin, coords);
                double up = (pin - coords[index]).Length;
                double down = (pin - coords[index+1]).Length;
                if (index - 1 >= 0 && up + PipeList[main_index].buff[index - 1] < r)
                    r = up + PipeList[main_index].buff[index - 1];
                if (index + 1 < PipeList[main_index].buff.Count && down + PipeList[main_index].buff[index + 1] < r)
                    r = down + PipeList[main_index].buff[index + 1];
            }
            return r;
        }

        public bool CheckSmallBetter()
        {
            bool flag = false;
            if (MainReigonListCopy.Count > MainReigonList.Count) return true;
            else 
            {
                Polyline copyPl = MainReigonListCopy.FindByMax(x => x.Area);
                Polyline pl = MainReigonList.FindByMax(x => x.Area);

                if ((copyPl.NumberOfVertices > pl.NumberOfVertices && copyPl.Area > pl.Area + 50000)
                    ) return true;
            }
            return flag;
        }










        //废弃
        //public void Pipeline()
        //{
        //    MainRegion = GetMainPipeArea(0,1);

        //    Polyline clonedMainRegion = MainRegion.Clone() as Polyline;
        //    //PassageShowUtils.ShowEntity(PassageWayUtils.Copy(MainRegion), 4);
        //    //AdjustRoom();
        //    //PassageShowUtils.ShowEntity(MainRegion, 5);

        //    if (!IfFind) return;
        //    DrawUtils.ShowGeometry(MainRegion, "l2AdjustedRoom", 4, lineWeightNum: 30);

        //    for (int i = 0; i < MainReigonList.Count; i++)
        //    {
        //        buffer_tree = GetBufferTree(MainReigonList[i]);
        //        GetSkeleton(buffer_tree);
        //        AdjustPolyline adjustPolyline = new AdjustPolyline(TmpSkeleton, Connector, clonedMainRegion, Buffer * 0.85);
        //        adjustPolyline.Pipeline3();
        //        TmpSkeleton.Clear();
        //        Skeleton.AddRange(adjustPolyline.Skeleton);
        //    }
        //    if (!Skeleton.Contains(MainPipeRoad)) 
        //    {
        //        Skeleton.Add(MainPipeRoad);
        //    }

        //    DrawUtils.ShowGeometry(Skeleton, "l2skeleton", 2, lineWeightNum: 30);
        //}
    }
}
