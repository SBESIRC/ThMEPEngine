using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThMEPWSS.DrainageSystemDiagram
{
    public static class ThDrainageADCommon
    {
        public static int length_stack_WM = 400;
        public static int length_stack_end = 1000;
        public static int length_pipe_end = 270;
        public static int length_stack_end_break = 70;

        public static int diameterDim_move_x = 50;
        public static int diameterDim_move_y = 30;
        public static string diameterDN_visi_pre = "DN";

        public static int tol_diaDim = 350;
        public static int tol_pipe_end = 500;
        public static int tol_StackR = 100;

        public static string visiName_valve = "可见性";
        public static string visiName1_valve = "可见性1";

        public static string blkName_stack = "带定位立管";
        public static string blkName_angleValve = "给水角阀平面";
        public static string blkName_dim = "给水管径50";
        public static double blk_scale_end = 1;

        public static List<string> LayerFilter = new List<string>()
                                                    {
                                                        "W-WSUP-EQPM",
                                                        "W-WSUP-COOL-PIPE-AI",
                                                        "W-WSUP-COOL-PIPE",
                                                        "W-WSUP-DIMS"
                                                    };

        public static List<string> toiBlkNames = new List<string>() { "A-Toilet-1", "A-Toilet-2", "A-Toilet-3",
                                                    "A-Toilet-4", "A-Kitchen-3", "A-Kitchen-4",
                                                    "小便器", "A-Toilet-5", "蹲便器",
                                                    "A-Kitchen-9", "A-Toilet-6", "A-Toilet-7",
                                                    "A-Toilet-8", "A-Toilet-9", "儿童坐便器",
                                                    "儿童洗脸盆", "儿童小便器"  };

        public static List<string> valveBlkName = new List<string>() {"$VALVE$00000333","截止阀",
                                                    "给水角阀平面",
                                                    "水表1","进户水表","室内水表详图",
                                                    "给水管径50","带定位立管"};

        public static Dictionary<string, string> curveType = new Dictionary<string, string>() {
                                                                { "Line", "Pipe" },
                                                                { "Circle", "Stack" }
                                                            };


        public static Dictionary<string, double> blkSize_stack = new Dictionary<string, double>(){
                                                                {"DN15",25 },
                                                                {"DN20",25 },
                                                                {"DN25",25 },
                                                                {"DN32",25 },
                                                                {"DN40",50 },
                                                                {"DN50",50 },
                                                                {"DN65",50 },
                                                                {"DN80",50 },
                                                                {"DN100",50 },
                                                                {"DN125",75 },
                                                                {"DN150",75 },
                                                                {"DN200",100 },
                                                                {"DN250",125 },
                                                                {"DN300",150 },
                                                              };

        public static Dictionary<string, int> blk_WM_SV = new Dictionary<string, int>() {
                                                            {"进户水表",400 },
                                                            {"水表",675 },
                                                            {"水表带止回阀1",1050 },
                                                            {"水表带止回阀2",1245 },
                                                            {"水表仅带过滤器1",1050 },
                                                            {"水表仅带过滤器2",1425 },
                                                            {"水表带倒流防止器",2175 },
                                                            {"水表带电动阀1",1425 },
                                                            {"水表带电动阀2",1800 },
                                                        };

        public static Dictionary<string, string> toi_end_name = new Dictionary<string, string>() {
                                                            {"A-Toilet-1","给水角阀" },
                                                            {"A-Toilet-2","给水角阀" },
                                                            {"A-Toilet-3","给水角阀" },
                                                            {"A-Toilet-4","给水角阀" },
                                                            {"A-Kitchen-3","给水角阀" },
                                                            {"A-Kitchen-4","给水角阀" },
                                                            {"小便器","感应式冲洗阀" },
                                                            {"A-Toilet-5","给水角阀" },
                                                            {"蹲便器","延时自闭阀" },
                                                            {"A-Kitchen-9","水龙头1" },
                                                            {"A-Toilet-6","淋浴器系统" },
                                                            {"A-Toilet-7","淋浴器系统" },
                                                            {"A-Toilet-8","浴缸系统" },
                                                            {"A-Toilet-9","给水角阀" },
                                                            {"儿童坐便器","给水角阀" },
                                                            {"儿童洗脸盆","给水角阀" },
                                                            {"儿童小便器","感应式冲洗阀" },
                                                        };

        public static Dictionary<string, List<string>> endValve_dir_name = new Dictionary<string, List<string>>  {
                                                            {"给水角阀",new List<string>() {"向右","向前","向左","向后"}},
                                                            {"感应式冲洗阀",new List<string>() {"向右水平", "向后水平", "向左水平", "向前水平" } }, //前后和其他反着的
                                                            {"延时自闭阀" ,new List<string>() {"向右水平", "向前水平", "向左水平", "向后水平"} },
                                                            {"水龙头1", new List<string>() { "向右","向前","向左","向后" } },
                                                            {"淋浴器系统", new List<string>() {"向右","向前","向左","向后" } },
                                                            {"浴缸系统", new List<string>() {"向右","向前","向左","向后" } },
                                                        };

        public static Dictionary<string, double> cool_supply_equivalent = new Dictionary<string, double>
                                                        {
                                                            {"A-Toilet-1",0.75 },
                                                            {"A-Toilet-2",0.75 },
                                                            {"A-Toilet-3",0.75 },
                                                            {"A-Toilet-4",0.75 },
                                                            {"A-Kitchen-3",1},
                                                            {"A-Kitchen-4",1},
                                                            {"小便器",0.5},
                                                            {"A-Toilet-5",0.5 },
                                                            {"蹲便器",0.5 },
                                                            {"A-Kitchen-9",1 },
                                                            {"A-Toilet-6",0.75 },
                                                            {"A-Toilet-7",0.75 },
                                                            {"A-Toilet-8",1.2 },
                                                            {"A-Toilet-9",1 },
                                                            {"儿童坐便器",0.5 },
                                                            {"儿童洗脸盆",0.75 },
                                                            {"儿童小便器",0.5 },
                                                        };

        public static Dictionary<string, double> cool_supply_flow = new Dictionary<string, double>
                                                        {
                                                            {"A-Toilet-1",0.15 },
                                                            {"A-Toilet-2",0.15 },
                                                            {"A-Toilet-3",0.15 },
                                                            {"A-Toilet-4",0.25 },
                                                            {"A-Kitchen-3",0.2},
                                                            {"A-Kitchen-4",0.2},
                                                            {"小便器",0.1},
                                                            {"A-Toilet-5",0.1 },
                                                            {"蹲便器",1.2 },
                                                            {"A-Kitchen-9",0.2 },
                                                            {"A-Toilet-6",0.15 },
                                                            {"A-Toilet-7",0.15 },
                                                            {"A-Toilet-8",0.24 },
                                                            {"A-Toilet-9",0.2 },
                                                            {"儿童坐便器",0.1 },
                                                            {"儿童洗脸盆",0.15 },
                                                            {"儿童小便器",0.1 },
                                                        };

        public static Dictionary<int, double> cool_supply_flow_diam = new Dictionary<int, double>() {
                                                            {20,0.0213},
                                                            {25,0.0273},
                                                            {32,0.0354},
                                                            {40,0.0413},
                                                            {50,0.0527},
                                                            {65,0.0681},
                                                            {80,0.0809},
                                                            {100,0.1063},
                                                            {125,0.131},
                                                            {150,0.1593},
                                                            {200,0.2071}
                                                        };

        public static Dictionary<int, (double, double)> cool_supply_diamFlowRange = new Dictionary<int, (double, double)>{
                                                            {20,(0,0.8)},
                                                            {25,(0,1)},
                                                            {32,(0,1)},
                                                            {40,(0,1)},
                                                            {50,(0,1.2)},
                                                            {65,(0,1.2)},
                                                            {80,(0,1.5)},
                                                            {100,(0,1.5)},
                                                            {125,(0,1.5)},
                                                            {150,(0,1.5)},
                                                            {200,(0,1.5)}
                                                        };
    }
}
