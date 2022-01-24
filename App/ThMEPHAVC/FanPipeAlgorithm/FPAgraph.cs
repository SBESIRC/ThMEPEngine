using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using ThMEPHVAC.FanPipeAlgorithm;
using ThMEPEngineCore.CAD;

namespace ThMEPHVAC.FanPipeAlgorithm
{

    //网格点
    //用于表示网格点的坐标
    //范围 x:(0,width),y(0,height)
    public class GridPoint : IEquatable<GridPoint>
    {
        public int x;
        public int y;
        public GridPoint(int a, int b)
        {
            this.x = a;
            this.y = b;
        }

        public override int GetHashCode()
        {
            return (int)x ^ (int)y;
        }
        public bool Equals(GridPoint other)
        {
            return x == other.x && y == other.y;
        }
    }


    //从图纸上分割出一块区域用于作图
    //所有作图操作最后都要在这张虚拟的图的上实现
    public class Graph
    {
        //总输入：排水管起点，终点，障碍物（图纸真实坐标）
        public List<Point3d> real_end_points;   
        public Point3d real_start_point;
        public List<Edge> boundary;   //房间框线（少穿越）
        public List<Edge> hole;       //AI洞口，剪力墙（不可穿越）

        //总输出：管线变量
        public List<Edge> long_edges = new List<Edge>();
   
        //网格图
        public List<List<int>> board_0 = new List<List<int>>();      //最重要的网格地图，标出了墙体所在的位置
        public List<List<int>> start_distance = new List<List<int>>();
        public long max_x, min_x, max_y, min_y;
        public int width, height;

        //排水管起点，终点，障碍物 —— 区域网格坐标
        public List<GridPoint> end_points = new List<GridPoint>();
        public GridPoint start_point = new GridPoint(0, 0);

        //节点
        //真实连接的节点
        public List<Node> nodes = new List<Node>();
        public Dictionary<Node, int> indexmap = new Dictionary<Node, int>();
        //临时考虑的节点
        public List<Node> nodes_tmp = new List<Node>();
        public Dictionary<Node, int> indexmap_tmp = new Dictionary<Node, int>();

        //图的边界
        public double space_min_x, space_min_y, space_max_x, space_max_y;

        //工具包
        Tool Tool0 = new Tool();

        //网格图构造函数
        public Graph(List<Point3d> real_end_points, Point3d real_start_point, List<Edge> boundary, List<Edge> hole)
        {
            this.real_end_points = real_end_points;
            this.real_start_point = real_start_point;
            this.hole = hole;
            this.boundary = boundary;
           
            //网格化处理
            discretize();

            //0: 能自由通行   ;  1: 尽量不穿越  ; 2 不可穿越;
            initialize_board(ref board_0, width, height, 0);
            
            //添加障碍
            mark_wall(ref board_0, width, height,ref this.boundary, 1);
            mark_wall(ref board_0, width, height, ref this.hole, 100);


            //清楚起点，终点周边障碍
            board_0[start_point.x][start_point.y] = 0;
            if (start_point.x >= 2 && start_point.y >= 2) {
                board_0[start_point.x+1][start_point.y] = 0;
                board_0[start_point.x-1][start_point.y] = 0;
                board_0[start_point.x][start_point.y+1] = 0;
                board_0[start_point.x][start_point.y-1] = 0;
                board_0[start_point.x + 2][start_point.y] = 0;
                board_0[start_point.x - 2][start_point.y] = 0;
                board_0[start_point.x][start_point.y + 2] = 0;
                board_0[start_point.x][start_point.y - 2] = 0;
            }
            for (int i = 0; i < end_points.Count; i++)
            {
                board_0[end_points[i].x][end_points[i].y] = 0;
                if (end_points[i].x > 1 && end_points[i].y > 1) 
                {
                    board_0[end_points[i].x+1][end_points[i].y] = 0;
                    board_0[end_points[i].x-1][end_points[i].y] = 0;
                    board_0[end_points[i].x][end_points[i].y+1] = 0;
                    board_0[end_points[i].x][end_points[i].y-1] = 0;
                }
            }
            
            //将起点加入nodes列表
            Node newnode = new Node(start_point.x, start_point.y);
            nodes.Add(newnode);
            indexmap.Add((newnode), 0);
            List<List<int>> distance = new List<List<int>>();
            initialize_board(ref start_distance, width, height, 0);
            copy_board(board_0, ref start_distance, width, height, -1);
            if (PublicValue.Traversable == 0)
            {
                calculate_distance(board_0, ref start_distance, start_point.x, start_point.y, width, height);
            }
            else
            {
                calculate_distance_traversable(board_0, ref start_distance, start_point.x, start_point.y, width, height);
            }
            nodes[0].DistanceMap = start_distance;
        }

        ////以下几个函数主要用于网格图的构建
        //对图形进行网格化处理（实际上是找到每个点在网格空间中对应的坐标）
        public void discretize()
        {
            space_max_x = -PublicValue.MAX_LENGTH;
            space_min_x = PublicValue.MAX_LENGTH;
            space_max_y = -PublicValue.MAX_LENGTH;
            space_min_y = PublicValue.MAX_LENGTH;

            //获取min_x 和 max_x
            List<Point3d> inter_points = new List<Point3d>();

            for (int i = 0; i < boundary.Count; i++)
            {
                Point3d pt = new Point3d(boundary[i].rx1, boundary[i].ry1, 0);
                inter_points.Add(pt);
            }
            for (int i = 0; i < hole.Count; i++)
            {
                Point3d pt = new Point3d(hole[i].rx1, hole[i].ry1, 0);
                inter_points.Add(pt);
            }
            inter_points.Add(real_start_point);

            for (int i = 0; i < inter_points.Count; i++)
            {
                Point3d pt = inter_points[i];
                if (pt.X > space_max_x)
                {
                    space_max_x = pt.X;
                }
                if (pt.X < space_min_x)
                {
                    space_min_x = pt.X;
                }
                if (pt.Y > space_max_y)
                {
                    space_max_y = pt.Y;
                }
                if (pt.Y < space_min_y)
                {
                    space_min_y = pt.Y;
                }
            }

            //留出余量，方式特殊情况下的越界
            space_min_x = space_min_x - 1000;
            space_min_y = space_min_y - 1000;

            min_x = (long)(space_min_x) / PublicValue.CELL;
            max_x = (long)(space_max_x) / PublicValue.CELL;
            min_y = (long)(space_min_y) / PublicValue.CELL;
            max_y = (long)(space_max_y) / PublicValue.CELL;

            int start_x = (int)(real_start_point.X - space_min_x) / PublicValue.CELL;
            int start_y = (int)(real_start_point.Y - space_min_y) / PublicValue.CELL;
            start_point = new GridPoint(start_x, start_y);

            for (int i = 0; i < real_end_points.Count; i++)
            {
                int end_point_x = (int)(real_end_points[i].X - space_min_x) / PublicValue.CELL;
                int end_point_y = (int)(real_end_points[i].Y - space_min_y) / PublicValue.CELL;
                end_points.Add(new GridPoint(end_point_x, end_point_y));
            }

            height = (int)(max_y - min_y + 10);
            width = (int)(max_x - min_x + 10);
        }

        //初始化网格地图（C#的容器的缺点好像就是要初始化，不然没法流畅使用）
        public void initialize_board(ref List<List<int>> board, int width, int height, int value)
        {
            board = new List<List<int>>(width);
            for (int i = 0; i < width; i++)
            {
                List<int> tmp = new List<int>(height);
                for (int j = 0; j < height; j++)
                {
                    tmp.Add(value);
                }
                board.Add(tmp);
            }
        }
        
        //复制网格地图
        public void copy_board(List<List<int>> source, ref List<List<int>> target, int width, int height, int scalar)
        {
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    target[i][j] = source[i][j] * scalar;
                }
            }
        }

        //在网格地图上标出各种实体（主要是墙体）
        public void mark_wall(ref List<List<int>> board_0, int width, int height, ref List<Edge> bound, int value) 
        {
            for (int i = 0; i < bound.Count; i++) {

                if (bound[i].rx2 < bound[i].rx1 ) {
                    Tool0.Swap(ref bound[i].rx1, ref bound[i].rx2);
                    Tool0.Swap(ref bound[i].ry1, ref bound[i].ry2);
                }
                int start_x = (int)(bound[i].rx1 - space_min_x) / PublicValue.CELL;
                int start_y = (int)(bound[i].ry1 - space_min_y) / PublicValue.CELL;
                int end_x = (int)(bound[i].rx2 - space_min_x) / PublicValue.CELL;
                int end_y = (int)(bound[i].ry2 - space_min_y) / PublicValue.CELL;

                double length = Math.Sqrt(Math.Pow((bound[i].ry2 - bound[i].ry1),2)+ Math.Pow((bound[i].rx2 - bound[i].rx1), 2));
                int step = PublicValue.LineStep;
                bool is_vertical = false;
                if ((bound[i].rx2 - bound[i].rx1) < 50) is_vertical = true;

                if (is_vertical)
                {

                    if (end_y < start_y) Tool0.Swap(ref end_y, ref start_y);

                    for (int j = start_y; j <= end_y; j++)
                    {
                        board_0[start_x][j] = value;
                    }
                }
                else
                {

                    double k = (bound[i].ry2 - bound[i].ry1) / (bound[i].rx2 - bound[i].rx1);
                    double step_x = 1 * step / (Math.Sqrt(1 + Math.Pow(k, 2)));
                    double step_y = k * step / (Math.Sqrt(1 + Math.Pow(k, 2)));

                    for (int j = 0; j * step <= length; j++) {

                        int now_x = (int)(bound[i].rx1 + step_x*j - space_min_x) / PublicValue.CELL;
                        int now_y = (int)(bound[i].ry1 + step_y*j -space_min_y) / PublicValue.CELL;
                        board_0[now_x][now_y] = value;

                        if (PublicValue.Extension == 1)
                        {
                            if (now_x > 1)
                            {
                                board_0[now_x - 1][now_y] = value;
                                board_0[now_x + 1][now_y] = value;
                            }
                            if (now_y > 1)
                            {
                                board_0[now_x][now_y - 1] = value;
                                board_0[now_x][now_y + 1] = value;
                            }
                        }
                    }
                }
            }
        }



        //// 以下几个函数为一些重要通用函数
        //计算以某一点为原点的 distance_map
        public void calculate_distance(List<List<int>> board_0, ref List<List<int>> distance, int x, int y, int width, int height)
        {

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (distance[i][j] != -1) {
                        distance[i][j] = 999999;
                    }
                }
            }

            List<List<int>> visited = new List<List<int>>();

            //visited = 1;
            initialize_board(ref visited, width, height, 0);
            copy_board(board_0, ref visited, width, height, 1);

            List<GridPoint> waiting_list = new List<GridPoint>();
            int current_distance = 0;

            waiting_list.Add(new GridPoint(x, y));
            visited[x][y] = 1;
            distance[x][y] = current_distance;

            while (waiting_list.Count > 0)
            {
                current_distance += 1;
                List<GridPoint> new_waiting_list = new List<GridPoint>();
                for (int i = 0; i < waiting_list.Count; i++)
                {
                    int p_x = waiting_list[i].x;
                    int p_y = waiting_list[i].y;
                    if (p_x >= 1 && visited[p_x - 1][p_y] == 0)
                    {
                        distance[p_x - 1][p_y] = current_distance;
                        visited[p_x - 1][p_y] = 1;
                        new_waiting_list.Add(new GridPoint(p_x - 1, p_y));
                    }
                    if (p_y >= 1 && visited[p_x][p_y - 1] == 0)
                    {
                        distance[p_x][p_y - 1] = current_distance;
                        visited[p_x][p_y - 1] = 1;
                        new_waiting_list.Add(new GridPoint(p_x, p_y - 1));
                    }
                    if (p_x <= width- 2 && visited[p_x + 1][p_y] == 0)
                    {
                        distance[p_x + 1][p_y] = current_distance;
                        visited[p_x + 1][p_y] = 1;
                        new_waiting_list.Add(new GridPoint(p_x + 1, p_y));
                    }
                    if (p_y <= height - 2 && visited[p_x][p_y + 1] == 0)
                    {
                        distance[p_x][p_y + 1] = current_distance;
                        visited[p_x][p_y + 1] = 1;
                        new_waiting_list.Add(new GridPoint(p_x, p_y + 1));
                    }
                }

                waiting_list.Clear();
                for (int i = 0; i < new_waiting_list.Count; i++)
                {
                    waiting_list.Add(new_waiting_list[i]);
                }
            }

        }

        //增加真实节点
        public void add_node(int index, GridPoint point)
        {
            Node tmpnode = new Node(point.x, point.y);
            nodes.Add(tmpnode);
            // nodes[index] = new Path(point);
            indexmap.Add((tmpnode), index);
            List<List<int>> distance = new List<List<int>>();
            initialize_board(ref distance, width, height, 0);
            copy_board(board_0, ref distance, width, height, -1);

            if (PublicValue.Traversable == 0)
            {
                calculate_distance(board_0, ref distance, point.x, point.y, width, height);
            }
            else
            {
                calculate_distance_traversable(board_0, ref distance, point.x, point.y, width, height);
            }
            
            nodes[index].DistanceMap = distance;
        }

        //增加临时节点
        public void add_node_tmp(int index, GridPoint point)
        {
            Node tmpnode = new Node(point.x, point.y);
            nodes_tmp.Add(tmpnode);
            // nodes[index] = new Path(point);
            indexmap_tmp.Add((tmpnode), index);
            List<List<int>> distance = new List<List<int>>();
            initialize_board(ref distance, width, height, 0);
            copy_board(board_0, ref distance, width, height, -1);
            if (PublicValue.Traversable == 0)
            {
                calculate_distance(board_0, ref distance, point.x, point.y, width, height);
            }
            else
            {
                calculate_distance_traversable(board_0, ref distance, point.x, point.y, width, height);
            }
            nodes_tmp[index].DistanceMap = distance;
        }



        //// 以下几个函数用于计算交汇点
        //寻找最佳交汇点
        public void find_best_intersection(int index1, int index2, ref GridPoint choose_point, ref List<Edge> path1, ref List<Edge> path2)
        {
            //int index1 = get_index(p1_x, p1_y);
            //int index2 = get_index(p2_x, p2_y);

            List<List<int>> total_distance = new List<List<int>>();
            initialize_board(ref total_distance, width, height, 0);
            get_total_distance(ref total_distance, index1, index2, width, height);

            GridPoint shortest_point = new GridPoint(0, 0);
            find_shortest_point(total_distance, width, height, ref shortest_point);


            //判断是否是之前已经标记过的交汇点，如果是，则借用之前的计算数据。
            int index3 = -1;
            Node test_node = new Node(shortest_point.x, shortest_point.y);
            if (indexmap_tmp.ContainsKey(test_node) == false)
            {
                index3 = nodes_tmp.Count;
                add_node_tmp(index3, shortest_point);
            }
            else
            {
                index3 = indexmap_tmp[test_node];
            }

            find_shortest_path(index3, index1, ref path1);
            find_shortest_path(index3, index2, ref path2);

            //记录下选中的点，传递出去
            choose_point.x = shortest_point.x;
            choose_point.y = shortest_point.y;
        }

        //获得不同的distance_map之和，寻找交汇点的核心，也是过程中的辅助函数
        public void get_total_distance(ref List<List<int>> total, int index1, int index2, int width, int height)
        {
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    total[i][j] = nodes[index1].DistanceMap[i][j] + nodes[index2].DistanceMap[i][j] + start_distance[i][j] + board_0[i][j] * 1000;
                }
            }
        }
        
        //寻找最佳交汇点过程中的辅助函数
        public void find_shortest_point(List<List<int>> board, int width, int height, ref GridPoint shortest_point)
        {
            int shortest = 10000;
            int mindis = 1000000;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (board[i][j] < shortest)
                    {
                        shortest = board[i][j];
                        shortest_point.x = i;
                        shortest_point.y = j;
                        mindis = start_distance[i][j];
                    }
                    else if (board[i][j] == shortest)
                    {
                        if (start_distance[i][j] < mindis)
                        {
                            shortest = board[i][j];
                            shortest_point.x = i;
                            shortest_point.y = j;
                            mindis = start_distance[i][j];
                        }

                    }
                }
            }
        }



        ////以下几个算法是为了寻找点与点之间最短路的函数，但是模式有所不同
        ////原则上，sp是起点，distance map是以ep为原点建立的，路径从sp一段段延申向ep。
        
        //基本的寻找最短路
        public void find_shortest_path(int sp_index, int ep_index, ref List<Edge> path)
        {
            int x = nodes_tmp[sp_index].x;
            int y = nodes_tmp[sp_index].y;

            if (nodes[ep_index].DistanceMap[x][y] == 0)
            {
                return;
            }

            List<Edge> min_path = new List<Edge>();

            int num_edge = 10000;

            for (int i = 0; i < PublicValue.ITER; i++)
            {

                List<Edge> current_path = new List<Edge>();

                int direction = find_direction_general(ep_index, 0, x, y);
                if (direction == 999)
                {
                    continue;
                }

                int current_x = x;
                int current_y = y;
                int current_distance = nodes[ep_index].DistanceMap[current_x][current_y];

                GridPoint edge_start = new GridPoint(x, y);
                GridPoint edge_end = new GridPoint(0, 0);

                while (current_distance > 0)
                {
                    int next_x = current_x, next_y = current_y;
                    if (direction == 2)
                    {
                        next_x -= 1;
                    }
                    else if (direction == 3)
                    {
                        next_y -= 1;
                    }
                    else if (direction == 1)
                    {
                        next_y += 1;
                    }
                    else if (direction == 0)
                    {
                        next_x += 1;
                    }

                    if (next_x < 0 || next_x >= width || next_y < 0 || next_y >= height || nodes[ep_index].DistanceMap[next_x][next_y] == -1 || nodes[ep_index].DistanceMap[next_x][next_y] >= nodes[ep_index].DistanceMap[current_x][current_y])
                    {
                        edge_end.x = current_x;
                        edge_end.y = current_y;
                        Edge new_edge = new Edge(edge_start.x, edge_start.y, edge_end.x, edge_end.y);

                        current_path.Add(new_edge);
                        edge_start.x = current_x;
                        edge_start.y = current_y;

                        direction = find_direction_general(ep_index, 0, current_x, current_y);
                        if (direction == 999)
                        {
                            break;
                        }
                    }
                    else if (board_0[next_x][next_y] == 1)
                    {
                        edge_end.x = current_x;
                        edge_end.y = current_y;
                        Edge new_edge = new Edge(edge_start.x, edge_start.y, edge_end.x, edge_end.y);

                        current_path.Add(new_edge);
                        edge_start.x = current_x;
                        edge_start.y = current_y;

                        direction = find_direction_general(ep_index, 0, current_x, current_y);

                        if (direction == 999)
                        {
                            break;
                        }

                        current_x = next_x;
                        current_y = next_y;
                        current_distance = nodes[ep_index].DistanceMap[current_x][current_y];
                    }
                    else
                    {
                        current_x = next_x;
                        current_y = next_y;
                        current_distance = nodes[ep_index].DistanceMap[current_x][current_y];
                    }
                }


                Edge new_edge_end = new Edge(edge_start.x, edge_start.y, current_x, current_y);

                current_path.Add(new_edge_end);

                if (current_distance == 0 && current_path.Count < num_edge)
                {
                    num_edge = current_path.Count;
                    min_path = current_path;
                }
            }

            for (int i = 0; i < min_path.Count; i++)
            {
                path.Add(min_path[i]);
            }

            //先别记录路径
            //nodes[sp_index].child_index.Add(ep_index);
            //nodes[sp_index].paths.Add(min_path);
            //nodes[ep_index].father_index = sp_index;    
        }
        
        //类似于上一种方法的重载，输入不同
        public void find_shortest_path_clear(GridPoint sp_index, int ep_index, ref List<Edge> path)
        {
            int x = sp_index.x;
            int y = sp_index.y;

            if (nodes[ep_index].DistanceMap[x][y] == 0)
            {
                return;
            }

            List<Edge> min_path = new List<Edge>();

            int num_edge = 10000;

            for (int i = 0; i < PublicValue.ITER; i++)
            {

                List<Edge> current_path = new List<Edge>();

                int direction = find_direction_general(ep_index,0, x, y);
                if (direction == 999)
                {
                    continue;
                }

                int current_x = x;
                int current_y = y;
                int current_distance = nodes[ep_index].DistanceMap[current_x][current_y];

                GridPoint edge_start = new GridPoint(x, y);
                GridPoint edge_end = new GridPoint(0, 0);

                while (current_distance > 0)
                {
                    int next_x = current_x, next_y = current_y;
                    if (direction == 2)
                    {
                        next_x -= 1;
                    }
                    else if (direction == 3)
                    {
                        next_y -= 1;
                    }
                    else if (direction == 1)
                    {
                        next_y += 1;
                    }
                    else if (direction == 0)
                    {
                        next_x += 1;
                    }

                    if (next_x < 0 || next_x >= width || next_y < 0 || next_y >= height || nodes[ep_index].DistanceMap[next_x][next_y] == -1 || nodes[ep_index].DistanceMap[next_x][next_y] >= nodes[ep_index].DistanceMap[current_x][current_y])
                    {
                        edge_end.x = current_x;
                        edge_end.y = current_y;
                        Edge new_edge = new Edge(edge_start.x, edge_start.y, edge_end.x, edge_end.y);

                        current_path.Add(new_edge);
                        edge_start.x = current_x;
                        edge_start.y = current_y;

                        direction = find_direction_general(ep_index,0, current_x, current_y);
                        if (direction == 999)
                        {
                            break;
                        }
                    }
                    //else if (board_0[next_x][next_y] == 1)
                    //{
                    //    edge_end.x = current_x;
                    //    edge_end.y = current_y;
                    //    edge new_edge = new edge(edge_start.x, edge_start.y, edge_end.x, edge_end.y);

                    //    current_path.Add(new_edge);
                    //    edge_start.x = current_x;
                    //    edge_start.y = current_y;

                    //    direction = find_direction_general(ep_index,0, current_x, current_y);

                    //    if (direction == 999)
                    //    {
                    //        break;
                    //    }

                    //    current_x = next_x;
                    //    current_y = next_y;
                    //    current_distance = nodes[ep_index].distance[current_x][current_y];
                    //}
                    else
                    {
                        current_x = next_x;
                        current_y = next_y;
                        current_distance = nodes[ep_index].DistanceMap[current_x][current_y];
                    }
                }


                Edge new_edge_end = new Edge(edge_start.x, edge_start.y, current_x, current_y);

                current_path.Add(new_edge_end);

                if (current_distance == 0 && current_path.Count < num_edge)
                {
                    num_edge = current_path.Count;
                    min_path = current_path;
                }
            }

            for (int i = 0; i < min_path.Count; i++)
            {
                path.Add(min_path[i]);
            }

            //先别记录路径
            //nodes[sp_index].child_index.Add(ep_index);
            //nodes[sp_index].paths.Add(min_path);
            //nodes[ep_index].father_index = sp_index;    
        }

        //原理同寻找最短路，但是遇到墙面则停止，用于寻找房间出口。
        public void find_shortest_path_start(GridPoint sp_index, int ep_index , int style ,ref GridPoint room_start,ref GridPoint room_out)
        {
            int sx = -1000;
            int sy = -1000;
            List<List<int>> distance_map = new List<List<int>>();
            if (style == 0)
            {
                distance_map = nodes[ep_index].DistanceMap;
            }
            else if (style == 1)
            {
                distance_map = nodes_tmp[ep_index].DistanceMap;
            }


            bool flag = false;
            int x = sp_index.x;
            int y = sp_index.y;

            GridPoint tmp_start = new GridPoint(0,0);

            if (distance_map[x][y] == 0)
            {
                return;
            }

            List<Edge> min_path = new List<Edge>();

            int num_edge = 10000;
            GridPoint tmp_room_start = new GridPoint(0, 0);
            GridPoint tmp_room_out = new GridPoint(0, 0);
            int maxdis = -100;

            for (int i = 0; i < 10 && flag == false ; i++)
            {

                List<Edge> current_path = new List<Edge>();

                //int old = -1;
                //int direction = find_direction_tabu(ep_index,style, x, y,old);
                //old = direction;
                int direction = find_direction_general(ep_index, style, x, y);


                if (direction == 999)
                {
                    continue;
                }

                int current_x = x;
                int current_y = y;
                int current_distance = distance_map[current_x][current_y];

                GridPoint edge_start = new GridPoint(x, y);
                GridPoint edge_end = new GridPoint(0, 0);
                int last =  -1;

                while (current_distance > 0)
                {

                    int next_x = current_x, next_y = current_y;
                    if (direction == 2)
                    {
                        next_x -= 1;
                        last = 2;
                    }
                    else if (direction == 3)
                    {
                        next_y -= 1;
                        last = 3;
                    }
                    else if (direction == 1)
                    {
                        next_y += 1;
                        last = 1;
                    }
                    else if (direction == 0)
                    {
                        next_x += 1;
                        last = 0;
                    }
                    else 
                    {
                        break;
                    }

                    if (next_x < 0 || next_x >= width || next_y < 0 || next_y >= height || distance_map[next_x][next_y] == -1 || distance_map[next_x][next_y] >= distance_map[current_x][current_y])
                    {
                        direction = find_direction_general(ep_index, style , current_x, current_y);
                        if (direction == 999)
                        {
                            break;
                        }
                    }
                    else if (board_0[next_x][next_y] == 1)
                    {

                        //找到出口
                        if (distance_map[current_x][current_y] - distance_map[next_x][next_y] > 45)
                        {
                            tmp_start.x = current_x;
                            tmp_start.y = current_y;
                            tmp_room_start = new GridPoint(tmp_start.x,tmp_start.y);
                            tmp_room_out = new GridPoint(next_x, next_y);
                            //flag = true;
                            break;
                        }
                        else //出口是假的
                        {
                            int direction_old = direction;
                            int iter = 0;
                            while (direction == direction_old && iter < 999) 
                            {
                                direction = find_direction_tabu(ep_index, style, current_x, current_y,direction);
                                if (direction == 999)
                                {
                                    break;
                                }
                                iter++;
                            }
                        }                                                                                 
                    }
                    else
                    {
                        current_x = next_x;
                        current_y = next_y;
                        current_distance = distance_map[current_x][current_y];
                    }
                }

                if (distance_map[tmp_room_start.x][tmp_room_start.y] > maxdis) 
                {
                    maxdis = distance_map[tmp_room_start.x][tmp_room_start.y];
                    room_start = new GridPoint(tmp_room_start.x,tmp_room_start.y);
                    room_out = new GridPoint(tmp_room_out.x, tmp_room_out.y);
                }
            }

            //避免一种非常奇怪的bug，即起点在墙里
            if (tmp_start.x == 0 && tmp_start.y == 0) 
            {
                room_start = sp_index; 
            }
        }

        //选择下一段线的前进方向，被寻找最短路的算法调用
        public int find_direction_general(int ep_index, int style, int x, int y)
        {
            List<List<int>> distance_map = new List<List<int>>();
            if (style == 0)
            {
                distance_map = nodes[ep_index].DistanceMap;
            }
            else if (style == 1)
            {
                distance_map = nodes_tmp[ep_index].DistanceMap;
            }

            int max_shorten = 0;

            List<int> directions = new List<int>();
            if (x >= 1 && distance_map[x - 1][y] != -1)
            {
                int shorten = distance_map[x][y] - distance_map[x - 1][y];
                if ((shorten == 1 && board_0[x][y] != 1) || (shorten == 1 &&  board_0[x-1][y] != 1)|| shorten == 51)
                {
                    if (shorten > max_shorten)
                    {
                        max_shorten = shorten;
                        directions.Clear();
                        directions.Add(2);
                    }
                    else if (shorten == max_shorten)
                    {
                        directions.Add(2);
                    }
                }
            }
            if (y >= 1 && distance_map[x][y - 1] != -1)
            {
                int shorten = distance_map[x][y] - distance_map[x][y-1];
                if ((shorten == 1 && board_0[x][y] != 1) || (shorten == 1 && board_0[x][y-1] != 1) || shorten == 51)
                {
                    if (shorten > max_shorten)
                    {
                        max_shorten = shorten;
                        directions.Clear();
                        directions.Add(3);
                    }
                    else if (shorten == max_shorten)
                    {
                        directions.Add(3);
                    }
                }
            }
            if (x <= width - 2 && distance_map[x + 1][y] != -1)
            {
                int shorten = distance_map[x][y] - distance_map[x+1][y];
                if ((shorten == 1 && board_0[x][y] != 1) || (shorten == 1 && board_0[x + 1][y] != 1) || shorten == 51)
                {
                    if (shorten > max_shorten)
                    {
                        max_shorten = shorten;
                        directions.Clear();
                        directions.Add(0);
                    }
                    else if (shorten == max_shorten)
                    {
                        directions.Add(0);
                    }
                }
            }
            if (y <= height  - 2 && distance_map[x][y + 1] != -1)
            {
                int shorten = distance_map[x][y] - distance_map[x][y+1];
                if ((shorten == 1 && board_0[x][y] != 1) || (shorten == 1 && board_0[x][y+1] != 1) || shorten == 51)
                {
                    if (shorten > max_shorten)
                    {
                        max_shorten = shorten;
                        directions.Clear();
                        directions.Add(1);
                    }
                    else if (shorten == max_shorten)
                    {
                        directions.Add(1);
                    }
                }
            }

            if (directions.Count == 0) {

                return 999;
            }

            int ddd = 0;
            try
            {
                byte[] buffer = Guid.NewGuid().ToByteArray();//生成字节数组
                int iRoot = BitConverter.ToInt32(buffer, 0);//利用BitConvert方法把字节数组转换为整数
                Random rdmNum = new Random(iRoot);//以这个生成的整数为种子
                int rand_index = rdmNum.Next(0, directions.Count);             
                ddd = directions[rand_index];
                //int rand_index = (int)DateTime.Now.Ticks % directions.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("random");
            }
            return ddd;
        }

        //对某些方向进行禁忌，寻找方向的特殊情况
        public int find_direction_tabu(int ep_index, int style , int x, int y ,int tabu)
        {
            List<List<int>> distance_map = new List<List<int>>();
            if (style == 0)
            {
                distance_map = nodes[ep_index].DistanceMap;
            }
            else if (style == 1)
            {
                distance_map = nodes_tmp[ep_index].DistanceMap;
            }

            int max_shorten = 0;

            List<int> directions = new List<int>();
            if (x >= 1 && distance_map[x - 1][y] != -1 && tabu!=2)
            {
                int shorten = distance_map[x][y] - distance_map[x - 1][y];
                if (shorten == 1 || shorten == 51)
                {
                    if (shorten > max_shorten)
                    {
                        max_shorten = shorten;
                        directions.Clear();
                        directions.Add(2);
                    }
                    else if (shorten == max_shorten)
                    {
                        directions.Add(2);
                    }
                }
            }
            if (y >= 1 && distance_map[x][y - 1] != -1 && tabu != 3)
            {
                int shorten = distance_map[x][y] - distance_map[x][y - 1];
                if (shorten == 1 || shorten == 51)
                {
                    if (shorten > max_shorten)
                    {
                        max_shorten = shorten;
                        directions.Clear();
                        directions.Add(3);
                    }
                    else if (shorten == max_shorten)
                    {
                        directions.Add(3);
                    }
                }
            }
            if (x <= width - 2 && distance_map[x + 1][y] != -1 && tabu != 0)
            {
                int shorten = distance_map[x][y] - distance_map[x + 1][y];
                if (shorten == 1 || shorten == 51)
                {
                    if (shorten > max_shorten)
                    {
                        max_shorten = shorten;
                        directions.Clear();
                        directions.Add(0);
                    }
                    else if (shorten == max_shorten)
                    {
                        directions.Add(0);
                    }
                }
            }
            if (y <= height - 2 && distance_map[x][y + 1] != -1 && tabu != 1)
            {
                int shorten = distance_map[x][y] - distance_map[x][y + 1];
                if (shorten == 1 || shorten == 51)
                {
                    if (shorten > max_shorten)
                    {
                        max_shorten = shorten;
                        directions.Clear();
                        directions.Add(1);
                    }
                    else if (shorten == max_shorten)
                    {
                        directions.Add(1);
                    }
                }
            }

            if (directions.Count == 0)
            {

                return 999;
            }

            int ddd = 0;
            try
            {
                int rand_index = (int)DateTime.Now.Ticks % directions.Count;
                ddd = directions[rand_index];
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
                Console.WriteLine("random");
            }
            return ddd;
        }



        ////提取nodes下存储的路径，用于整线
        public void extract_edges(ref List<Edge> extracted_edges) 
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = 0; j < nodes[i].paths.Count; j++)
                {
                    for (int k = 0; k < nodes[i].paths[j].Count; k++)
                    {
                        Edge tmp_e = nodes[i].paths[j][k];
                        if (tmp_e.x1 == tmp_e.x2 && tmp_e.y1 == tmp_e.y2) continue;
                        extracted_edges.Add(tmp_e);
                    }
                }
            }
        }

        public void extract_important_points(ref List<List<GridPoint>> important_points,ref List<List<Point3d>> real_important_points)
        {
            important_points.Add(new List<GridPoint>());
            important_points.Add(new List<GridPoint>());
            real_important_points.Add(new List<Point3d>());
            real_important_points.Add(new List<Point3d>());

            important_points[0].Add(start_point);
            real_important_points[0].Add(real_start_point);

            for (int i = 0; i < end_points.Count; i++) 
            {
                important_points[1].Add(end_points[i]);
                real_important_points[1].Add(real_end_points[i]);
            }
        }


        ////其他的计算distance_map 的方法
        
        //可穿越墙面版本
        public void calculate_distance_traversable(List<List<int>> board_0, ref List<List<int>> distance, int x, int y, int width, int height)
        {
            int weight = 50;

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    //if (distance[i][j] != -1 && distance[i][j] != -100)
                    //{
                    //    distance[i][j] = 999999;
                    //}
                    if (distance[i][j] != -1)
                    {
                        distance[i][j] = 999999;
                    }
                }
            }

            List<List<int>> visited = new List<List<int>>();

            //visited = 1;
            initialize_board(ref visited, width, height, 0);
            copy_board(board_0, ref visited, width, height, 10);

            List<GridPoint> waiting_list = new List<GridPoint>();
            int current_distance = 0;

            waiting_list.Add(new GridPoint(x, y));
            visited[x][y] = 1;
            distance[x][y] = current_distance;

            List<List<GridPoint>> waiting_wall = new List<List<GridPoint>>();
            for (int i = 0; i < weight; i++)
            {
                List<GridPoint> tmp = new List<GridPoint>();
                waiting_wall.Add(tmp);
            }

            int count_wall = 0;

            while (waiting_list.Count > 0 || count_wall > 0)
            {
                current_distance += 1;
                List<GridPoint> new_waiting_list = new List<GridPoint>();
                List<GridPoint> new_waiting_wall = new List<GridPoint>();

                for (int i = 0; i < waiting_list.Count; i++)
                {
                    int p_x = waiting_list[i].x;
                    int p_y = waiting_list[i].y;
                    if (p_x >= 1 && visited[p_x - 1][p_y] == 0)
                    {
                        distance[p_x - 1][p_y] = current_distance;
                        visited[p_x - 1][p_y] = 1;
                        new_waiting_list.Add(new GridPoint(p_x - 1, p_y));
                    }
                    else if (p_x >= 1 && visited[p_x - 1][p_y] == 10)
                    {
                        distance[p_x - 1][p_y] = current_distance;
                        visited[p_x - 1][p_y] = 1;
                        new_waiting_wall.Add(new GridPoint(p_x - 1, p_y));
                        count_wall++;

                    }
                    if (p_y >= 1 && visited[p_x][p_y - 1] == 0)
                    {
                        distance[p_x][p_y - 1] = current_distance;
                        visited[p_x][p_y - 1] = 1;
                        new_waiting_list.Add(new GridPoint(p_x, p_y - 1));
                    }
                    else if (p_y >= 1 && visited[p_x][p_y - 1] == 10)
                    {
                        distance[p_x][p_y - 1] = current_distance;
                        visited[p_x][p_y - 1] = 1;
                        new_waiting_wall.Add(new GridPoint(p_x, p_y - 1));
                        count_wall++;
                    }
                    if (p_x <= width - 2 && visited[p_x + 1][p_y] == 0)
                    {
                        distance[p_x + 1][p_y] = current_distance;
                        visited[p_x + 1][p_y] = 1;
                        new_waiting_list.Add(new GridPoint(p_x + 1, p_y));
                    }
                    else if (p_x <= width - 2 && visited[p_x + 1][p_y] == 10)
                    {
                        distance[p_x + 1][p_y] = current_distance;
                        visited[p_x + 1][p_y] = 1;
                        new_waiting_wall.Add(new GridPoint(p_x + 1, p_y));
                        count_wall++;
                    }
                    if (p_y <= height - 2 && visited[p_x][p_y + 1] == 0)
                    {
                        distance[p_x][p_y + 1] = current_distance;
                        visited[p_x][p_y + 1] = 1;
                        new_waiting_list.Add(new GridPoint(p_x, p_y + 1));
                    }
                    else if (p_y <= height - 2 && visited[p_x][p_y + 1] == 10)
                    {
                        distance[p_x][p_y + 1] = current_distance;
                        visited[p_x][p_y + 1] = 1;
                        new_waiting_wall.Add(new GridPoint(p_x, p_y + 1));
                        count_wall++;
                    }
                }

                waiting_list.Clear();
                for (int i = 0; i < new_waiting_list.Count; i++)
                {
                    waiting_list.Add(new_waiting_list[i]);
                }

                //延迟输入
                if (waiting_wall.Count != 0 && waiting_wall[0].Count != 0)
                {
                    for (int i = 0; i < waiting_wall[0].Count; i++)
                    {
                        waiting_list.Add(waiting_wall[0][i]);
                    }
                }

                waiting_wall.Add(new_waiting_wall);

                if (waiting_wall.Count != 0)
                {
                    count_wall -= waiting_wall[0].Count;
                    waiting_wall.RemoveAt(0);
                }
            }
        }

        //原理同计算distance_map，用于寻找与主干的交点，一找到就停止
        public void calculate_distance_to_main(List<List<int>> board_0, ref List<List<int>> distance, List<List<int>> main, ref GridPoint point_in_main, int x, int y)
        {
            int weight = 50;

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (distance[i][j] != -1)
                    {
                        distance[i][j] = 999999;
                    }
                }
            }

            List<List<int>> visited = new List<List<int>>();

            //visited = 1;
            initialize_board(ref visited, width, height, 0);
            copy_board(board_0, ref visited, width, height, 10);

            List<GridPoint> waiting_list = new List<GridPoint>();
            int current_distance = 0;

            waiting_list.Add(new GridPoint(x, y));
            visited[x][y] = 1;
            distance[x][y] = current_distance;

            List<List<GridPoint>> waiting_wall = new List<List<GridPoint>>();
            for (int i = 0; i < weight; i++)
            {
                List<GridPoint> tmp = new List<GridPoint>();
                waiting_wall.Add(tmp);
            }

            int count_wall = 0;

            bool flag = true;
            while ((waiting_list.Count > 0 || count_wall > 0) && flag == true)
            {
                current_distance += 1;
                List<GridPoint> new_waiting_list = new List<GridPoint>();
                List<GridPoint> new_waiting_wall = new List<GridPoint>();

                for (int i = 0; i < waiting_list.Count; i++)
                {

                    int p_x = waiting_list[i].x;
                    int p_y = waiting_list[i].y;

                    //如果找到目标点，则直接退出
                    if (main[p_x][p_y] == 1)
                    {
                        point_in_main = new GridPoint(p_x, p_y);
                        flag = false;
                        break;
                    }

                    if (p_x >= 1 && visited[p_x - 1][p_y] == 0)
                    {
                        distance[p_x - 1][p_y] = current_distance;
                        visited[p_x - 1][p_y] = 1;
                        new_waiting_list.Add(new GridPoint(p_x - 1, p_y));
                    }
                    else if (p_x >= 1 && visited[p_x - 1][p_y] == 10)
                    {
                        distance[p_x - 1][p_y] = current_distance;
                        visited[p_x - 1][p_y] = 1;
                        new_waiting_wall.Add(new GridPoint(p_x - 1, p_y));
                        count_wall++;

                    }
                    if (p_y >= 1 && visited[p_x][p_y - 1] == 0)
                    {
                        distance[p_x][p_y - 1] = current_distance;
                        visited[p_x][p_y - 1] = 1;
                        new_waiting_list.Add(new GridPoint(p_x, p_y - 1));
                    }
                    else if (p_y >= 1 && visited[p_x][p_y - 1] == 10)
                    {
                        distance[p_x][p_y - 1] = current_distance;
                        visited[p_x][p_y - 1] = 1;
                        new_waiting_wall.Add(new GridPoint(p_x, p_y - 1));
                        count_wall++;
                    }
                    if (p_x <= width - 2 && visited[p_x + 1][p_y] == 0)
                    {

                        distance[p_x + 1][p_y] = current_distance;
                        visited[p_x + 1][p_y] = 1;
                        new_waiting_list.Add(new GridPoint(p_x + 1, p_y));
                    }
                    else if (p_x <= width - 2 && visited[p_x + 1][p_y] == 10)
                    {
                        distance[p_x + 1][p_y] = current_distance;
                        visited[p_x + 1][p_y] = 1;
                        new_waiting_wall.Add(new GridPoint(p_x + 1, p_y));
                        count_wall++;
                    }
                    if (p_y <= height - 2 && visited[p_x][p_y + 1] == 0)
                    {
                        distance[p_x][p_y + 1] = current_distance;
                        visited[p_x][p_y + 1] = 1;
                        new_waiting_list.Add(new GridPoint(p_x, p_y + 1));
                    }
                    else if (p_y <= height - 2 && visited[p_x][p_y + 1] == 10)
                    {
                        distance[p_x][p_y + 1] = current_distance;
                        visited[p_x][p_y + 1] = 1;
                        new_waiting_wall.Add(new GridPoint(p_x, p_y + 1));
                        count_wall++;
                    }
                }

                waiting_list.Clear();
                for (int i = 0; i < new_waiting_list.Count; i++)
                {
                    waiting_list.Add(new_waiting_list[i]);
                }

                //延迟输入
                if (waiting_wall.Count != 0 && waiting_wall[0].Count != 0)
                {
                    for (int i = 0; i < waiting_wall[0].Count; i++)
                    {
                        waiting_list.Add(waiting_wall[0][i]);
                    }
                }

                waiting_wall.Add(new_waiting_wall);

                if (waiting_wall.Count != 0)
                {
                    count_wall -= waiting_wall[0].Count;
                    waiting_wall.RemoveAt(0);
                }
            }
        }

        //查看 一/多 个网格点是否在edge上
        public bool on_edge(Edge long_edge, GridPoint point)
        {
            int x1 = long_edge.x1, y1 = long_edge.y1, x2 = long_edge.x2, y2 = long_edge.y2;

            if (x1 > x2)
            {
                Tool0.Swap(ref x1, ref x2);
            }

            if (y1 > y2)
            {
                Tool0.Swap(ref y1, ref y2);
            }

            return point.x >= x1 && point.x <= x2 && point.y >= y1 && point.y <= y2;
        }
        public bool points_on_edge(Edge long_edge, List<GridPoint> points)
        {
            bool flag = false;
            for (int i = 0; i < points.Count; i++)
            {
                if (on_edge(long_edge, points[i]) == true)
                {
                    flag = true;
                    break;
                }
            }
            return flag;
        }
    }
}
