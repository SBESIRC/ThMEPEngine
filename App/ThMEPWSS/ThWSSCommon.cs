namespace ThMEPWSS
{
    public static class ThWSSCommon
    {
        public const string SprayUpBlockName = "上喷喷头";                                     //上喷喷头名
        public const string SprayDownBlockName = "下喷喷头";                                   //下喷喷头名
        public const string SprayLayerName = "W-FRPT-SPRL";                                    //喷淋图层名          
        public const string Layout_Line_LayerName = "AI-Sprinkler-喷头布置轴线";               //喷淋轴线图层名
        public const string Layout_Error_Spray_LayerName = "AI-Sprinkler-无法处理";            //有问题的喷淋图层标识图层
        public const string Layout_Origin_Spray_LayerName = "AI-Sprinkler-原始喷头";           //原始的喷淋图层标识图层

        public const string MopPoolBlockName = "A-Kitchen-9";                                   //拖把池图块名
        public const string WashingMachineBlockName = "A-Toilet-9";                             //洗衣机图块名
        public const string KitchenBasinBlockName = "A-Kitchen-4";                              //厨房台盆图块名
        public const string FloorDrainBlockName_1 = "W-drain-3";                                //地漏图块名-1
        public const string FloorDrainBlockName_2 = "W-drain-4";                                //地漏图块名-2                            
        public const string FloorDrainBlockName_3 = "$TwtSys$00000141";                         //地漏图块名-3（天正地漏炸开后块名称）
        public const string FloorDrainBlockName_4 = "$TwtSys$00000137";                         //地漏图块名-4（天正地漏炸开后块名称）
        public const string FloorDrainBlockName_5 = "$TwtSys$00000329";                         //地漏图块名-5（天正地漏炸开后块名称）
        public const string FloorDrainBlockName_6 = "$TwtSys$00000327";                         //地漏图块名-6（天正地漏炸开后块名称）
        public const string FloorDrainBlockName_7 = "$TwtSys$00000328";                         //地漏图块名-7（天正地漏炸开后块名称）
        public const string FloorDrainBlockName_8 = "$TwtSys$00000543";                         //地漏图块名-8（天正地漏炸开后块名称）
        public const string FloorDrainBlockName_9 = "$TwtSys$00000571";                         //地漏图块名-9（天正地漏炸开后块名称）
        public const string GravityFlowRainBucketBlockName = "W-drain-1";                       //重力流雨水斗图块名称
        public const string GravityFlowRainBucketBlockName_Contain = "87型";                    //重力流雨水斗图块名称（包含）
        public const string SideRainBucketBlockName_1 = "W-drain-2";                            //侧入式雨水斗图块名称
        public const string SideRainBucketBlockName_2 = "W-drain-5";                            //侧入式雨水斗图块名称
        public const string RoofRainwaterRiserBlockName = "W-pipe-1";                           //屋面雨水立管图块名称
        public const string BalconyRiserBlockName = "W-pipe-2";                                 //阳台立管图块名称
        public const string CondensateRiserBlockName_1 = "h-p-s";                               //冷凝水立管图块名称-1
        public const string CondensateRiserBlockName_2 = "W-pipe-3";                            //冷凝水立管图块名称-2
        public const string WaterPipeWellBlockName = "A-hole-5";                                //水管径留洞图块名称
        public const string FlueShaftBlockName = "A-hole-4";                                    //烟道留洞图块名称


        public const string Layout_FloorDrainBlockName = "地漏平面";                                   //地漏块名称--生成时用
        public const string Layout_PositionRiserBlockName = "带定位立管";                                       //带定位立管 （图纸比例 1:50  1:100使用）
        public const string Layout_PositionRiser150BlockName = "带定位立管150";                                 //带定位立管 （图纸比例 1:150 使用）
        public const string Layout_CleanoutBlockName = "清扫口系统";
        public const string Layout_FloorDrainBlockWastLayerName = "W-DRAI-FLDR";                       //地漏图层名称 废水地漏（生成使用）
        public const string Layout_FloorDrainBlockRainLayerName = "W-RAIN-EQPM";                       //地漏图层名称 雨水地漏（生成使用）
        public const string Layout_WastWaterPipeLayerName = "W-DRAI-EQPM";                             //废水立管图层
        public const string Layout_PipeRainDrainConnectLayerName = "W-RAIN-PIPE";                      //图层名称 连线图层-雨水地漏连接立管图层
        public const string Layout_PipeWastDrainConnectLayerName = "W-DRAI-WAST-PIPE";                 //图层名称 连线图层-废水地漏连接立管图层
        public const string Layout_PipeRainTextLayerName = "W-RAIN-NOTE";                              //图层名称 连线图层-编号图层
        public const string Layout_PipeWastDrainTextLayerName = "W-DRAI-NOTE";                         //图层名称 连线图层-编号图层
        public const string Layout_PipeCasingTextLayerName = "W-BUSH-NOTE";                            //图层名称 套管标注图层
        public const string Layout_PipeCasingBlockName = "套管-AI";
        public const string Layout_PipeCasingLayerName = "W-BUSH";

        public const string Layout_TextStyle = "TH-STYLE3";

        public const string Layout_FireHydrantPipeLineLayerName = "W-FRPT-HYDT-PIPE";                  //消火栓管线图层名称
        public const string Layout_FireHydrantTextLayerName = "W-FRPT-HYDT-DIMS";                      //消火栓文字图层名称
        public const string Layout_FireHydrantDescriptionLayerName = "W-WSUP-NOTE";
        public const string Layout_FireHydrantEqumLayerName = "W-FRPT-HYDT-EQPM";
        public const string Layout_FireHydrantBlockName = "室内消火栓系统1";
        public const string Layout_FireHydrantLayerName = "W-FRPT-HYDT";
        public const string Layout_ButterflyValveBlcokName = "蝶阀";
        public const string Layout_ExhaustValveSystemBlockName = "自动排气阀系统1";
        public const string Layout_LevelBlockName = "标高";
        public const string Layout_CheckValveBlockName = "止回阀";
        public const string Layout_ShutOffValve = "截止阀";
        public const string Layout_SafetyValve = "安全阀";
        public const string Layout_LevelLayerName = "W-NOTE";
        public const string Layout_ConnectionReserveBlcokName = "接驳预留";

        public const string Blind_Zone_LayerName = "AI-喷头校核-盲区检测";
        public const string From_Boundary_So_Far_LayerName = "AI-喷头校核-喷头距边是否过大";
        public const string Room_Checker_LayerName = "AI-喷头校核-房间是否布置喷头";
        public const string Parking_Stall_Checker_LayerName = "AI-喷头校核-车位上方喷头";
        public const string Mechanical_Parking_Stall_Checker_LayerName = "AI-喷头校核-机械车位侧喷";
        public const string Sprinkler_Distance_LayerName = "AI-喷头校核-喷头间距是否过小";
        public const string From_Boundary_So_Close_LayerName = "AI-喷头校核-喷头距边是否过小";
        public const string Distance_Form_Beam_LayerName = "AI-喷头校核-喷头距梁是否过小";
        public const string Layout_Area_LayerName = "AI-喷头校核-可布置区域";
        public const string Beam_Checker_LayerName = "AI-喷头校核-较高的梁";
        public const string Pipe_Checker_LayerName = "AI-喷头校核-喷头是否连管";
        public const string Duct_Checker_LayerName = "AI-喷头校核-宽度大于1200的风管";
        public const string Duct_Blind_Zone_LayerName = "AI-喷头校核-风管下喷盲区";
        public const string Sprinkler_So_Dense_LayerName = "AI-喷头校核-区域喷头过密";

        public static string Sprinkler_Connect_MainPipe = "W-喷淋-不接支管主管";
        public static string Sprinkler_Connect_SubMainPipe = "W-喷淋-连接支管主管";
        public static string Sprinkler_Connect_Pipe = "W-FRPT-SPRL-PIPE";

        //一层给排水平面系统
        //立管名称
        public static string VerticalPipe_BlockName1 = "污废水管+通气管";
        public static string VerticalPipe_BlockName2 = "带定位立管";
        public static string VerticalPipe_BlockName3 = "带定位立管150";
        //出乎框线图层
        public static string OutFrameLayerName = "AI-出户框线";
        //室外主管图层
        public static string OutdoorSewagePipeLayerName = "AI-室外污水主管";
        public static string OutdoorRainPipeLayerName = "AI-室外雨水主管";
        //其他管线图层
        public static string DraiSewageLayerName = "W-DRAI-SEWA-PIPE";      //污水管
        public static string DraiWasteLayerName = "W-DRAI-WAST-PIPE";       //废水管
        public static string DraiLayerName = "W-RAIN-PIPE";                 //雨水、冷凝水管
        //室外雨污水井
        public static string OutdoorWasteWellLayerName = "W-DRAI-EQPM";         //室外污水井图层名
        public static string OutdoorWasteWellBlockName = "污废合流井编号";      //室外污水井块名
        public static string OutdoorRainWellLayerName = "W-RAIN-EQPM";          //室外雨水井图层名
        public static string OutdoorRainWellBlockName = "重力流雨水井编号";     //室外雨水井块名
        //堵头
        public static string ReservedPlugBlockName = "清扫口系统";     //堵头块名
        //处理冷凝水管
        public static string DisconnectionLayerName = "W-WSUP-EQPM";
        public static string DisconnectionBlockName = "断线";
        public static string RainwaterInletLayerName = "W-RAIN-EQPM";
        public static string RainwaterInletBlockName = "13#雨水口";
        public static string SealedWellLayerName = "W-RAIN-EQPM";
        public static string SealedWellBlockName = "水封井";
        //管径标注
        public static string RainDimsLayerName = "W-RAIN-DIMS";
        public static string DraiDimsLayerName = "W-DRAI-DIMS";
        public static string DimsBlockName = "给水管径100";
        //套管标注
        public static string DrivepipeBlockName = "套管";
        public static string DrivepipeLayerName = "W-BUSH";
        public static string DrivepipeNoteLayerName = "W-BUSH-NOTE";
    }
}