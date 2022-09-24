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


namespace ThMEPHVAC.FloorHeatingCoil.PassingWay
{
    class MainPipeGet3
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

        public MainPipeGet3(Polyline room, List<BufferPoly> shortest_way, int main_index, double buffer, double room_buffer, bool main_has_output)
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

        public MainPipeGet3(Polyline mainPipeRoad, Polyline room)
        {
            this.MainRegion = room;
            MainPipeRoad = mainPipeRoad;
            MainPipeIn = mainPipeRoad.GetPoint3dAt(0);
        }

        public void Pipeline2()
        {
            GetMainPipeArea(0, 0);
            GetMainPipeArea(0, 1);

            if (MainReigonListCopy.Count == 0) return;
            if (CheckSmallBetter()) MainReigonList = MainReigonListCopy;

            List<Polyline> MainRegionListRoom = new List<Polyline>();
            for (int i = 0; i < MainReigonList.Count; i++)
            {
                Polyline mainReigonListRoom = PassageWayUtils.Buffer(MainReigonList[i].Clone() as Polyline, 0.25 * Buffer + Parameter.SuggestDistanceWall).First();
                DrawUtils.ShowGeometry(mainReigonListRoom, "l4AdjustedRoom", 4, lineWeightNum: 30);
                MainRegionListRoom.Add(mainReigonListRoom);
            }

            if (!IfFind) return;

            for (int i = 0; i < MainRegionListRoom.Count; i++)
            {
                Point3d pin = new Point3d();
                Point3d point = new Point3d();
                FindPin2(MainReigonList[i], ref pin, ref point);
                if (point == new Point3d() || (point - pin).Length > Buffer) continue;

                DrawUtils.ShowGeometry(MainReigonList[i], "l4MainShell", 5, 30);
                Polyline pinToPoint = new Polyline();

                pinToPoint.AddVertexAt(0, pin.ToPoint2D(), 0, 0, 0);
                pinToPoint.AddVertexAt(0, point.ToPoint2D(), 0, 0, 0);
                point = IntersectUtils.PolylineIntersectionPolyline(MainRegionListRoom[i], pinToPoint).First();

                double pinBuffer = GetPinBuffer(pin, point);
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
                DrawUtils.ShowGeometry(MainPipeRoad, "l4MainPipeRoad", pipeInList[0].PipeId % 7 + 1, 30);
            }

            //DrawUtils.ShowGeometry(Skeleton, "l2skeleton", 2, lineWeightNum: 30);
            //Skeleton.Add(MainPipeRoad);
        }

        public void Pipeline3()
        {
            GetMainPipeArea(1, 0);
            GetMainPipeArea(1, 1);

            if (MainReigonListCopy.Count == 0) return;
            if (CheckSmallBetter()) MainReigonList = MainReigonListCopy;

            List<Polyline> MainRegionListRoom = new List<Polyline>();
            for (int i = 0; i < MainReigonList.Count; i++)
            {
                Polyline mainReigonListRoom = PassageWayUtils.Buffer(MainReigonList[i].Clone() as Polyline, Parameter.SuggestDistanceWall).First();
                DrawUtils.ShowGeometry(mainReigonListRoom, "l2AdjustedRoom", 4, lineWeightNum: 30);
                MainRegionListRoom.Add(mainReigonListRoom);
            }

            if (!IfFind) return;

            for (int i = 0; i < MainRegionListRoom.Count; i++)
            {
                Point3d pin = new Point3d();
                Point3d point = MainPipeRoad.StartPoint;
                double halfBuffer = PipeList[main_index].buff[0];
                DrawUtils.ShowGeometry(point, "l4SPin", 0, 30, (int)halfBuffer);

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

        public Polyline GetMainPipeArea(int mode, int isSmall)
        {
            double smallCoefficient = 1;
            if (isSmall == 1) smallCoefficient = 0.85;

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
            DBObjectCollection rest0 = new DBObjectCollection();

            //rest.Add(shortest_way[main_index].Buffer(3));  //出口
            if (pipeInputs.Count > 1)
            {
                if (mode == 0)
                {
                    if (main_index > 0)
                        rest0.Add(PassageWayUtils.Buffer(PipeList[main_index - 1].Buffer(1), 0.5 * Buffer * smallCoefficient).First());
                    if (main_index < pipeInputs.Count - 1)
                        rest0.Add(PassageWayUtils.Buffer(PipeList[main_index + 1].Buffer(1), 0.5 * Buffer * smallCoefficient).First());
                }
                else if (mode == 1)
                {
                    if (main_index > 0)
                        rest0.Add(PipeList[main_index - 1].Buffer(3 * smallCoefficient));
                    if (main_index < pipeInputs.Count - 1)
                        rest0.Add(PipeList[main_index + 1].Buffer(3 * smallCoefficient));
                }
            }
            // get rest
            if (mode == 0)
            {
                rest0.Add(PassageWayUtils.Buffer(PipeList[main_index].Buffer(1), 0.5 * Buffer * smallCoefficient).First());

                DrawUtils.ShowGeometry(PipeList[main_index].Buffer(1), "l4Test", 9, lineWeightNum: 30);
            }

            rest0.OfType<Polyline>().ToList().ForEach(x => DrawUtils.ShowGeometry(x, "l2Rest", 5, lineWeightNum: 30));
            DrawUtils.ShowGeometry(main_region, "l2OldMainArea", 6, lineWeightNum: 30);

            if (main_region.Area < 1000)
            {
                Skeleton.Add(MainPipeRoad);
                IfFind = false;
                return new Polyline();
            }

            var rest = main_region.Difference(rest0);

            List<Polyline> newRegionList = rest.OfType<Polyline>().ToList();

            //var newRoom = AdjustBufferRoom();
            var newRoom = Room;
            double smallRoomBuffer = -RoomBuffer - 0.5 * Buffer / 2;
            if (mode == 1) smallRoomBuffer = -RoomBuffer;
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

        public void FindPin(List<Point3d> coords, ref Point3d pin, ref Point3d point)
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
                    if (tmpRoadDis < roadDis || (!oldRec && rec))
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
            for (int i = 1; i < mainCoords.Count - 1; i++)
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

        public double GetPinBuffer(Point3d pin, Point3d point)
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
                double down = (pin - coords[index + 1]).Length;
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
    }
}
