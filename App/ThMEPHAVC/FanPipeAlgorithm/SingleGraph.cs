using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

using ThMEPHVAC.FanConnect.Model;
using ThMEPHVAC.FanPipeAlgorithm;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;

namespace ThMEPHVAC.FanPipeAlgorithm
{
    class SingleGraph
    {
        //工具包
        Tool Tool0 = new Tool();
        DataProcess DataP0 = new DataProcess();

        //最终输出
        public List<Edge> ProcessedEdges = new List<Edge>();

        //记录所有节点 暂时弃用
        public List<Point3d> MarkNode = new List<Point3d>();

        //共用变量
        Graph SmallRoom;
        List<int> WaitingList = new List<int>();

        //构造函数
        public SingleGraph(List<Point3d> real_end_points, Point3d real_start_point, List<Edge> boundary, List<Edge> hole)
        {
            SingleArea(real_end_points, real_start_point, boundary, hole);
        }


        //连接一个房间内的风机的主函数
        public void SingleArea(List<Point3d> real_end_points, Point3d real_start_point, List<Edge> boundary, List<Edge> hole)
        {
            //构建单个小房间内的网格图
            SmallRoom = new Graph(real_end_points, real_start_point, boundary, hole);

            //填充waitng list          
            FillWaitingList();

            //
            //开始循环
            try
            {
                while (WaitingList.Count > 1)
                {

                    int max_dis = -1;
                    int choosen_pt1_index = -1, choosen_pt2_index = -1;
                    
                    GridPoint choosen_inter = new GridPoint(0, 0);
                    List<Edge> p1_path = new List<Edge>();
                    List<Edge> p2_path = new List<Edge>();

                    //从所有点中选择最远的交汇点,并记录交汇点到另外两点的路径
                    FindFarthestInterPoint(max_dis, ref choosen_pt1_index, ref choosen_pt2_index, ref choosen_inter, ref p1_path, ref p2_path);
                      
                    //选定了交汇点之后，将路径记录入对应的Node下
                    Node test_node = new Node(choosen_inter.x, choosen_inter.y);
                    //判断是否重复
                    if (SmallRoom.indexmap.ContainsKey(test_node) == true)
                    {
                        if (test_node.Equals(SmallRoom.nodes[choosen_pt1_index]))
                        {
                            WaitingList.Remove(choosen_pt2_index);
                            //正式加入真实序列
                            SmallRoom.nodes[choosen_pt1_index].paths.Add(p2_path);
                            SmallRoom.nodes[choosen_pt1_index].ChildIndex.Add(choosen_pt2_index);
                            SmallRoom.nodes[choosen_pt2_index].FatherIndex = choosen_pt1_index;
                        }
                        else if (test_node.Equals(SmallRoom.nodes[choosen_pt2_index]))
                        {
                            WaitingList.Remove(choosen_pt1_index);
                            SmallRoom.nodes[choosen_pt2_index].paths.Add(p1_path);
                            SmallRoom.nodes[choosen_pt2_index].ChildIndex.Add(choosen_pt1_index);
                            SmallRoom.nodes[choosen_pt1_index].FatherIndex = choosen_pt2_index;
                        }
                        else
                        {
                            //从列表中移除已经访问完全的点
                            WaitingList.Remove(choosen_pt2_index);
                            WaitingList.Remove(choosen_pt1_index);

                            //记录节点之间的关系
                            int index3 = SmallRoom.indexmap[test_node];
                            SmallRoom.nodes[index3].ChildIndex.Add(choosen_pt1_index);
                            SmallRoom.nodes[index3].paths.Add(p1_path);
                            SmallRoom.nodes[index3].ChildIndex.Add(choosen_pt2_index);
                            SmallRoom.nodes[index3].paths.Add(p2_path);

                            SmallRoom.nodes[choosen_pt1_index].FatherIndex = index3;
                            SmallRoom.nodes[choosen_pt2_index].FatherIndex = index3;
                        }
                    }
                    else //不重复
                    {
                        WaitingList.Remove(choosen_pt2_index);
                        WaitingList.Remove(choosen_pt1_index);

                        //记载子节点以及路径
                        int index3 = SmallRoom.nodes.Count;
                        SmallRoom.add_node(index3, choosen_inter);
                        SmallRoom.nodes[index3].ChildIndex.Add(choosen_pt1_index);
                        SmallRoom.nodes[index3].paths.Add(p1_path);
                        SmallRoom.nodes[index3].ChildIndex.Add(choosen_pt2_index);
                        SmallRoom.nodes[index3].paths.Add(p2_path);

                        //记载父节点
                        SmallRoom.nodes[choosen_pt1_index].FatherIndex = index3;
                        SmallRoom.nodes[choosen_pt2_index].FatherIndex = index3;

                        //入队
                        WaitingList.Add(index3);
                    }
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
                Console.WriteLine("waiting");
            }

            //最后只剩下房间的起点没有连接上，于是单独连接房间起点
            if (WaitingList.Count != 0 && WaitingList[0] != 0)
            {
                ConnectStartPoint();
            }

            //整线
            ArrangeSingleArea();

            //记录所有node
            //for (int i = 0; i < sample.nodes.Count; i++)
            //{
            //    double x = sample.space_min_x + sample.nodes[i].x * PublicValue.CELL;
            //    double y = sample.space_min_y + sample.nodes[i].y * PublicValue.CELL;
            //    mark_node.Add(new Point3d(x, y, 0));
            //}
        }

        ////辅助SingleArea的函数
        //将已知的节点填入waiting_list
        public void FillWaitingList()
        {
            for (int i = 0; i < SmallRoom.end_points.Count; i++)
            {
                //将所有风机（终点）都放入 nodes 列表中
                int index = SmallRoom.nodes.Count;
                if (SmallRoom.indexmap.ContainsKey(new Node(SmallRoom.end_points[i].x, SmallRoom.end_points[i].y)) == false)
                {
                    SmallRoom.add_node(index, SmallRoom.end_points[i]);
                }
                else
                {
                    continue;
                }

                //剔除无法到达的终点
                if (SmallRoom.nodes[index].DistanceMap[SmallRoom.start_point.x][SmallRoom.start_point.y] > 100000)
                {
                    SmallRoom.nodes.RemoveAt(index);
                    SmallRoom.real_end_points.RemoveAt(i);
                    SmallRoom.end_points.RemoveAt(i);
                }
                else
                {
                    WaitingList.Add(index);
                }
            }

        }

        //选定下一个交汇点
        public void FindFarthestInterPoint(int max_dis, ref int choosen_pt1_index, ref int choosen_pt2_index, ref GridPoint choosen_inter, ref List<Edge> p1_path, ref List<Edge> p2_path) 
        {
            int num_candidate = WaitingList.Count;

            for (int i = 0; i < num_candidate; i++)
            {
                for (int j = i + 1; j < num_candidate; j++)
                {
                    List<Edge> p1_to_p0_path = new List<Edge>();
                    List<Edge> p2_to_p0_path = new List<Edge>();

                    Node node1 = SmallRoom.nodes[i];
                    Node node2 = SmallRoom.nodes[j];
                    GridPoint inter = new GridPoint(0, 0);

                    try
                    {
                        SmallRoom.find_best_intersection(WaitingList[i], WaitingList[j], ref inter, ref p1_to_p0_path, ref p2_to_p0_path);
                    }
                    catch (Exception ex)
                    {

                        Console.WriteLine(ex.Message);
                        Console.WriteLine("find_best_intersection");
                    }

                    if (SmallRoom.start_distance[inter.x][inter.y] > max_dis)
                    {
                        max_dis = SmallRoom.start_distance[inter.x][inter.y];
                       
                        p1_path = p1_to_p0_path;
                        p2_path = p2_to_p0_path;
                        choosen_pt1_index = WaitingList[i];
                        choosen_pt2_index = WaitingList[j];
                        choosen_inter = inter;
                    }
                }
            }
        }

        //最后连接房间起点
        public void ConnectStartPoint()
        {
            int index3 = 0;
            List<Edge> final_path = new List<Edge>();
            if (SmallRoom.indexmap_tmp.ContainsKey(SmallRoom.nodes[0]) == false)
            {
                index3 = SmallRoom.nodes_tmp.Count;
                SmallRoom.add_node_tmp(index3, SmallRoom.start_point);
            }
            else
            {
                index3 = SmallRoom.indexmap_tmp[SmallRoom.nodes[0]];
            }

            //寻找最短路，连接
            SmallRoom.find_shortest_path(index3, WaitingList[0], ref final_path);
            SmallRoom.nodes[0].paths.Add(final_path);
            SmallRoom.nodes[0].ChildIndex.Add(WaitingList[0]);
            SmallRoom.nodes[WaitingList[0]].FatherIndex = 0;
        }

        //单个房间整线流程
        public void ArrangeSingleArea()
        {
            //开始整线
            List<Edge> long_edges_to_arrange = new List<Edge>();
            List<List<GridPoint>> important_points = new List<List<GridPoint>>();
            List<List<Point3d>> real_important_points = new List<List<Point3d>>();
            SmallRoom.extract_edges(ref long_edges_to_arrange);
            SmallRoom.extract_important_points(ref important_points, ref real_important_points);

            ArrangePipe single_room_arrange = new ArrangePipe(long_edges_to_arrange, real_important_points, important_points, SmallRoom.board_0, SmallRoom.space_min_x, SmallRoom.space_min_y);

            //走一遍标准整线流程
            single_room_arrange.standard_process();

            //将整完的线存放起来
            ProcessedEdges.AddRange(single_room_arrange.long_edges);
        }
    }
}
