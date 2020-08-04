using Linq2Acad;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;

namespace ThMEPEngineCore.IO
{
    public class ThDwgReader
    {
        public Database HostDb { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="database"></param>
        public ThDwgReader(Database database)
        {
            HostDb = database;
        }

        /// <summary>
        /// 读取结构梁
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public ThIfcBeamStandardCase ReadBeam(ObjectId obj)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(HostDb))
            {
                return new ThIfcBeamStandardCase();
            }
        }

        /// <summary>
        /// 读取结构柱
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public ThIfcColumnStandardCase ReadColumn(ObjectId obj)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(HostDb))
            {
                return new ThIfcColumnStandardCase();
            }
        }
    }
}
