using Linq2Acad;
using DotNetARX;
using Dreambuild.AutoCAD;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore
{
    public static class ThMEPEngineCoreLayerUtils
    {
        public const string BEAM = "AI-梁";
        public const string DOOR = "AI-门";
        public const string WALL = "AI-墙";
        public const string WINDOW = "AI-窗";
        public const string COLUMN = "AI-柱";
        public const string OPENING = "AI-洞";
        public const string RAILING = "AI-栏杆";
        public const string SHEARWALL = "AI-剪力墙";
        public const string ROOMOUTLINE = "AI-房间框线";
        public const string ROOMMARK = "AI-房间名称";
        public const string ROOMSPLITLINE = "AI-房间分割线";
        public const string FIRECOMPARTMENT = "AI-防火分区";
        public const string FRS = "AI-防火卷帘";
        public const string CENTERLINE = "AI-中心线";
        public const string LANECENTERLINE = "E-LANE-CENTER";
        public const string HAVCRoute = "AI-风管路由";
        public const string WaterPipeRoute = "AI-水管路由";        
        public const string Note = "AI-注释";
        public const string ARCHOUTLINE = "AI-建筑轮廓线";
        public static ObjectId CreateAIBeamLayer(this Database database)       
        {
            return database.CreateAILayer(BEAM, 6);
        }

        public static ObjectId CreateAIDoorLayer(this Database database)
        {
            return database.CreateAILayer(DOOR, 3);
        }

        public static ObjectId CreateAIWallLayer(this Database database)
        {
            return database.CreateAILayer(WALL, 7);
        }

        public static ObjectId CreateAIWindowLayer(this Database database)
        {
            return database.CreateAILayer(WINDOW, 6);
        }

        public static ObjectId CreateAIColumnLayer(this Database database)
        {
            return database.CreateAILayer(COLUMN, 2);
        }

        public static ObjectId CreateAIOpeningLayer(this Database database)
        {
            return database.CreateAILayer(OPENING, 5);
        }

        public static ObjectId CreateAIRailingLayer(this Database database)
        {
            return database.CreateAILayer(RAILING, 20);
        }

        public static ObjectId CreateAIShearWallLayer(this Database database)
        {
            return database.CreateAILayer(SHEARWALL, 30);
        }

        public static ObjectId CreateAIRoomOutlineLayer(this Database database)
        {
            return database.CreateAILayer(ROOMOUTLINE, 21);
        }

        public static ObjectId CreateAIRoomSplitlineLayer(this Database database)
        {
            return database.CreateAILayer(ROOMSPLITLINE, 41);
        }

        public static ObjectId CreateAIRoomMarkLayer(this Database database)
        {
            return database.CreateAILayer(ROOMMARK, 4);
        }

        public static ObjectId CreateAIFireCompartmentLayer(this Database database)
        {
            return database.CreateAILayer(FIRECOMPARTMENT, 1);
        }

        public static ObjectId CreateAIFRSLayer(this Database database)
        {
            return database.CreateAILayer(FRS, 1);
        }

        public static ObjectId CreateAICenterLineLayer(this Database database)
        {
            return database.CreateAILayer(CENTERLINE, 2);
        }
        public static ObjectId CreateAILaneCenterLineLayer(this Database database)
        {
            return database.CreateAILayer(LANECENTERLINE, 2);
        }

        public static ObjectId CreateAIHAVCRouteLayer(this Database database)
        {
            return database.CreateAILayer(HAVCRoute, 5);
        }        
        
        public static ObjectId CreateAIWaterPipeRouteLayer(this Database database)
        {
            return database.CreateAILayer(WaterPipeRoute, 3);
        }

        public static ObjectId CreateAINoteLayer(this Database database)
        {
            return database.CreateAILayer(Note, 6);
        }

        public static ObjectId CreateAIArchOutlineLayer(this Database database)
        {
            return database.CreateAILayer(ARCHOUTLINE, 5);
        }

        public static ObjectId CreateAILayer(this Database database, string name, short colorIndex)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                // 创建图层
                var layerId = database.AddLayer(name);

                // 设置图层颜色
                database.SetLayerColor(name, colorIndex);

                // 设置图层状态
                database.OpenAILayer(name);

                // 返回图层
                return layerId;
            }
        }

        /// <summary>
        /// 打开图层设置
        /// (不要锁定、不要关闭、不要隐藏、不要冻结、不要打印)
        /// </summary>
        /// <param name="database"></param>
        /// <param name="layer"></param>
        public static void OpenAILayer(this Database database, string layer)
        {
            using (var acadDb = AcadDatabase.Use(database))
            {
                DbHelper.EnsureLayerOn(layer);
            }
        }
    }
}
