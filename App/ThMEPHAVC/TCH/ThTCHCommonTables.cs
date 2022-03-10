namespace ThMEPHVAC.TCH
{
    public static class ThTCHCommonTables
    {
        public const double flgThickness = 0.2;
        public const string ductTableName = "Ducts";
        public const string elbowTableName = "DuctElbows";
        public const string reducingTableName = "DuctReducers";
        public const string teeTableName = "Duct3Ts";
        public const string crossTableName = "Duct4Ts";
        public const string interfaceTableName = "MepInterfaces";
        public const string flangesTableName = "Flanges";
        public const string materialsTableName = "Materials";
        public const string subSystemTypeTableName = "SubSystemTypes";
        public const string ductDimContents = "DuctDimContents";
        public const string ductDimensions = "DuctDimensions";
        public const string ductOffsets = "DuctOffsets";
        public static string[] subSystems = { "消防加压",
                                              "消防排烟",
                                              "消防补风",
                                              "排风兼排烟",
                                              "送风兼补风",
                                              "排风",
                                              "送风",
                                              "空调送风",
                                              "空调回风",
                                              "空调新风",
                                              "排油烟",
                                              "厨房补风",
                                              "事故排风",
                                              "烟风管",};
        public static string[] materials = { "镀锌钢板" };
    }
}