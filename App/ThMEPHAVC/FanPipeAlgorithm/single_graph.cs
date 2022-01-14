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
    class single_graph
    {
        //工具包
        tool tool_run = new tool();
        data_process data_p = new data_process();

        //最终输出
        public List<edge> processed_edges = new List<edge>();

        //记录所有节点
        public List<Point3d> mark_node = new List<Point3d>();

        public single_graph(List<Point3d> real_end_points, Point3d real_start_point, List<edge> boundary, List<edge> hole) {

            single_area(real_end_points, real_start_point, boundary, hole);
        }



        public void single_area(List<Point3d> real_end_points, Point3d real_start_point, List<edge> boundary, List<edge> hole)
        {
            for (int loop = 0; loop < 1; loop++)
            {
                //List<edge> pipes;
                //vector<Block*> current_space_group;
                //vector<vector<Block*>> space_output;
                //int start_space = -1;

                //vector<Block*> pt_set;
                //pt_set.push_back(new_input[0]);

                //int counter = 0;

                // construct
                graph sample = new graph(real_end_points, real_start_point, boundary, hole);

                // for(int p = 0; p < sample.height; p++)
                // {
                //     for(int q = 0; q < sample.width; q++)
                //     {
                //         fout << sample.start_distance[p][q] << " ";
                //     }
                //     
                // }

                // solve
                //剔除无法到达的点
          

                List<int> waiting_list = new List<int>();

                for (int i = 0; i < sample.end_points.Count; i++)
                {
                    int index = sample.nodes.Count;
                    if (sample.indexmap.ContainsKey(new node(sample.end_points[i].x, sample.end_points[i].y)) == false)
                    {
                        sample.add_node(index, sample.end_points[i]);
                    }
                    else 
                    {
                        continue;
                    }
                    //剔除无法到达的点

                    if (sample.nodes[index].distance[sample.start_point.x][sample.start_point.y] > 100000)
                    {
                        sample.nodes.RemoveAt(index);
                        sample.real_end_points.RemoveAt(i);
                        sample.end_points.RemoveAt(i);
                    }
                    else
                    {
                        waiting_list.Add(index);
                    }

                    //waiting_list.Add(index);
                }

                try
                {
                    while (waiting_list.Count > 1)
                    {
                        // cout << "[" << waiting_list.size() << "]" << " ";
                        // for(int i = 0; i < waiting_list.size(); i++)
                        // {
                        //     cout << waiting_list[i] << " ";
                        // }
                        // cout << endl;

                        int max_dis = -1;
                        //int append_index;
                        int choosen_pt1_index = -1, choosen_pt2_index = -1;
                        int num_candidate = waiting_list.Count;

                        grid_point choosen_inter = new grid_point(0, 0);
                        List<edge> p1_path = new List<edge>();
                        List<edge> p2_path = new List<edge>();


                        //从所有点中选择最远的交汇点
                        for (int i = 0; i < num_candidate; i++)
                        {
                            for (int j = i + 1; j < num_candidate; j++)
                            {
                                List<edge> p1_to_p0_path = new List<edge>();
                                List<edge> p2_to_p0_path = new List<edge>();
                                //sample.get_xy(waiting_list[i], pt1);
                                //sample.get_xy(waiting_list[j], pt2);

                                node node1 = sample.nodes[i];
                                node node2 = sample.nodes[j];
                                grid_point inter = new grid_point(0, 0);

                                try
                                {
                                    sample.find_best_intersection(waiting_list[i], waiting_list[j], ref inter, ref p1_to_p0_path, ref p2_to_p0_path);
                                }
                                catch (Exception ex)
                                {

                                    Console.WriteLine(ex.Message);
                                    Console.WriteLine("find_best_intersection");
                                }

                                if (sample.start_distance[inter.x][inter.y] > max_dis)
                                {
                                    max_dis = sample.start_distance[inter.x][inter.y];
                                    // append_index = sample.get_index(inter.first, inter.second);
                                    p1_path = p1_to_p0_path;
                                    p2_path = p2_to_p0_path;
                                    choosen_pt1_index = waiting_list[i];
                                    choosen_pt2_index = waiting_list[j];
                                    choosen_inter = inter;
                                }
                            }
                        }


                        //选定了交汇点之后，进行处理
                        node test_node = new node(choosen_inter.x, choosen_inter.y);
                        //判断是否重复
                        if (sample.indexmap.ContainsKey(test_node) == true)
                        {
                            if (test_node.Equals(sample.nodes[choosen_pt1_index]))
                            {
                                waiting_list.Remove(choosen_pt2_index);
                                //正式加入真实序列
                                sample.nodes[choosen_pt1_index].paths.Add(p2_path);
                                sample.nodes[choosen_pt1_index].child_index.Add(choosen_pt2_index);
                                sample.nodes[choosen_pt2_index].father_index = choosen_pt1_index;
                            }
                            else if (test_node.Equals(sample.nodes[choosen_pt2_index]))
                            {
                                waiting_list.Remove(choosen_pt1_index);
                                sample.nodes[choosen_pt2_index].paths.Add(p1_path);
                                sample.nodes[choosen_pt2_index].child_index.Add(choosen_pt1_index);
                                sample.nodes[choosen_pt1_index].father_index = choosen_pt2_index;
                            }
                            else
                            {
                                //从列表中移除已经访问完全的点
                                waiting_list.Remove(choosen_pt2_index);
                                waiting_list.Remove(choosen_pt1_index);

                                int index3 = sample.indexmap[test_node];
                                sample.nodes[index3].child_index.Add(choosen_pt1_index);
                                sample.nodes[index3].paths.Add(p1_path);
                                sample.nodes[index3].child_index.Add(choosen_pt2_index);
                                sample.nodes[index3].paths.Add(p2_path);

                                sample.nodes[choosen_pt1_index].father_index = index3;
                                sample.nodes[choosen_pt2_index].father_index = index3;

                            }

                        }
                        else
                        {
                            waiting_list.Remove(choosen_pt2_index);
                            waiting_list.Remove(choosen_pt1_index);

                            //记载子节点以及路径
                            int index3 = sample.nodes.Count;
                            sample.add_node(index3, choosen_inter);
                            sample.nodes[index3].child_index.Add(choosen_pt1_index);
                            sample.nodes[index3].paths.Add(p1_path);
                            sample.nodes[index3].child_index.Add(choosen_pt2_index);
                            sample.nodes[index3].paths.Add(p2_path);

                            //记载父节点
                            sample.nodes[choosen_pt1_index].father_index = index3;
                            sample.nodes[choosen_pt2_index].father_index = index3;

                            //入队
                            waiting_list.Add(index3);
                        }
                    }
                }
                catch (Exception ex)
                {

                    Console.WriteLine(ex.Message);
                    Console.WriteLine("waiting");
                }

                if (waiting_list.Count != 0 && waiting_list[0] != 0)
                {
                    int index3 = 0;
                    List<edge> final_path = new List<edge>();
                    if (sample.indexmap_tmp.ContainsKey(sample.nodes[0]) == false)
                    {
                        index3 = sample.nodes_tmp.Count;
                        sample.add_node_tmp(index3, sample.start_point);
                    }
                    else
                    {
                        index3 = sample.indexmap_tmp[sample.nodes[0]];
                    }

                    //寻找最短路，连接
                    sample.find_shortest_path(index3, waiting_list[0], ref final_path);
                    sample.nodes[0].paths.Add(final_path);
                    sample.nodes[0].child_index.Add(waiting_list[0]);
                    sample.nodes[waiting_list[0]].father_index = 0;
                }


                // 将网格坐标调整为真实坐标
                sample.analysis_edge();
                sample.point_to_real(-1);
                sample.find_point_edge_relation(-1);
                sample.connect_edge(-1);
                sample.post_processing(-1);

                for (int i = 0; i < sample.long_edges.Count; i++)
                {
                    processed_edges.Add(sample.long_edges[i]);
                }

                //记录所有node
                for (int i = 0; i < sample.nodes.Count; i++) 
                {
                    double x = sample.space_min_x + sample.nodes[i].x * PublicValue.CELL;
                    double y = sample.space_min_y + sample.nodes[i].y * PublicValue.CELL;
                    mark_node.Add(new Point3d(x, y, 0));
                }

            }
        }
    }
}
