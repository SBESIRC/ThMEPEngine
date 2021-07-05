namespace ThMEPWSS
{
    public static class ThWSSCommon
    {
        public const string SprayUpBlockName = "上喷喷头";                                    //上喷喷头名
        public const string SprayDownBlockName = "下喷喷头";                                  //下喷喷头名
        public const string SprayLayerName = "W-FRPT-SPRL";                                   //喷淋图层名          
        public const string Layout_Line_LayerName = "AI-Sprinkler-喷头布置轴线";                //喷淋轴线图层名
        public const string Layout_BlindArea_LayerName = "AI-Sprinkler-保护盲区";             //喷淋盲区图层
        public const string Layout_Error_Spray_LayerName = "AI-Sprinkler-无法处理";           //有问题的喷淋图层标识图层
        public const string Layout_Origin_Spray_LayerName = "AI-Sprinkler-原始喷头";          //原始的喷淋图层标识图层
        public const string Layout_Area_LayerName = "AI-Sprinkler-可布置区域";                 //可布置区域图层

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

        public const string Layout_FloorDrainBlockWastLayerName = "W-DRAI-FLDR";                       //地漏图层名称 废水地漏（生成使用）
        public const string Layout_FloorDrainBlockRainLayerName = "W-RAIN-EQPM";                       //地漏图层名称 雨水地漏（生成使用）
        public const string Layout_WastWaterPipeLayerName = "W-DRAI-EQPM";                             //废水立管图层
        public const string Layout_PipeRainDrainConnectLayerName = "W-RAIN-PIPE";                      //图层名称 连线图层-雨水地漏连接立管图层
        public const string Layout_PipeWastDrainConnectLayerName = "W-DRAI-WAST-PIPE";                 //图层名称 连线图层-废水地漏连接立管图层
        public const string Layout_PipeRainTextLayerName = "W-RAIN-NOTE";                              //图层名称 连线图层-编号图层
        public const string Layout_PipeWastDrainTextLayerName = "W-DRAI-NOTE";                         //图层名称 连线图层-编号图层
        public const string Layout_PipeCasingBlockName = "套管-AI";
        public const string Layout_PipeCasingLayerName = "W-BUSH";

        public const string Layout_TextStyle = "TH-STYLE3";
    }
}