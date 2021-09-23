using Linq2Acad;
using DotNetARX;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore
{
    public static class ThMEPEngineCoreLayerUtils
    {
        public static ObjectId CreateAIBeamLayer(this Database database)
        {
            return database.CreateAILayer("AI-梁", 6);
        }

        public static ObjectId CreateAIDoorLayer(this Database database)
        {
            return database.CreateAILayer("AI-门", 3);
        }

        public static ObjectId CreateAIWindowLayer(this Database database)
        {
            return database.CreateAILayer("AI-窗", 6);
        }

        public static ObjectId CreateAIColumnLayer(this Database database)
        {
            return database.CreateAILayer("AI-柱", 2);
        }

        public static ObjectId CreateAIOpeningLayer(this Database database)
        {
            return database.CreateAILayer("AI-洞", 5);
        }

        public static ObjectId CreateAIRailingLayer(this Database database)
        {
            return database.CreateAILayer("AI-栏杆", 20);
        }

        public static ObjectId CreateAIShearWallLayer(this Database database)
        {
            return database.CreateAILayer("AI-剪力墙", 30);
        }

        public static ObjectId CreateAIRoomOutlineLayer(this Database database)
        {
            return database.CreateAILayer("AI-房间框线", 21);
        }

        public static ObjectId CreateAIRoomMarkLayer(this Database database)
        {
            return database.CreateAILayer("AI-房间名称", 4);
        }

        public static ObjectId CreateAIFireCompartmentLayer(this Database database)
        {
            return database.CreateAILayer("AI-防火分区", 1);
        }

        public static ObjectId CreateAIFRSLayer(this Database database)
        {
            return database.CreateAILayer("AI-防火卷帘", 1);
        }

        public static ObjectId CreateAILayer(this Database database, string name, short colorIndex)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var layerId = database.AddLayer(name);
                database.UnOffLayer(name);
                database.UnLockLayer(name);
                database.UnPrintLayer(name);
                database.UnFrozenLayer(name);
                database.SetLayerColor(name, colorIndex);
                return layerId;
            }
        }
    }
}
