using System;
using Linq2Acad;
using DotNetARX;
using System.Linq;
using Dreambuild.AutoCAD;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Service.Hvac;

namespace ThMEPEngineCore.Service.Hvac
{
    public class ThHvacDbModelManager : IDisposable
    {
        private Database HostDb { get; set; }
        public ObjectIdCollection Geometries { get; set; }
        public Dictionary<string, List<int>> Models { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="database"></param>
        public ThHvacDbModelManager(Database database)
        {
            HostDb = database;
            LoadFromDb(database);
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// 从图纸中提取风机图块
        /// </summary>
        /// <param name="database"></param>
        private void LoadFromDb(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                Geometries = new ObjectIdCollection();
                var blkRefs = acadDatabase.ModelSpace
                    .OfType<BlockReference>()
                    .Where(o =>
                    {
                        if (o.GetEffectiveName().Contains(ThHvacCommon.AXIAL_BLOCK_NAME))
                        {
                            return true;
                        }

                        if (o.GetEffectiveName().Contains(ThHvacCommon.HTFC_BLOCK_NAME))
                        {
                            return true;
                        }

                        return false;
                    });
                blkRefs.ForEachDbObject(o => Geometries.Add(o.ObjectId));
                Models = ExtractFromDb(database, Geometries);
            }
        }

        /// <summary>
        /// 是否存在风机
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public bool Contains(string identifier)
        {
            return Models.ContainsKey(identifier);
        }

        /// <summary>
        /// 获取风机图块
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public ObjectIdCollection GetModels(string identifier)
        {
            var objs = new ObjectIdCollection();
            Geometries.Cast<ObjectId>()
                .Where(o => o.IsModel(identifier))
                .ForEach(o => objs.Add(o));
            return objs;
        }

        public void EraseModels(string identifier)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(HostDb))
            {
                GetModels(identifier).Cast<ObjectId>().ForEach(o =>
                {
                    acadDatabase.Element<Entity>(o, true).Erase();
                });
            }
        }

        /// <summary>
        /// 提取块引用中的模型信息（模型标识和模型编号）
        /// </summary>
        /// <param name="objs"></param>
        /// <returns></returns>
        private Dictionary<string, List<int>> ExtractFromDb(Database database, ObjectIdCollection objs)
        {
            var models = new Dictionary<string, List<int>>();
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                foreach (ObjectId obj in objs)
                {
                    TypedValueList valueList = obj.GetXData(ThHvacCommon.RegAppName_FanSelection);
                    if (valueList != null)
                    {
                        // 模型ID
                        string identifier = null;
                        var values = valueList.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataAsciiString);
                        if (values.Any())
                        {
                            identifier = (string)values.ElementAt(0).Value;
                        }

                        // 模型编号
                        int number = 0;
                        values = valueList.Where(o => o.TypeCode == (int)DxfCode.ExtendedDataInteger32);
                        if (values.Any())
                        {
                            number = (int)values.ElementAt(0).Value;
                        }

                        if (!string.IsNullOrEmpty(identifier))
                        {
                            if (models.ContainsKey(identifier))
                            {
                                models[identifier].Add(number);
                            }
                            else
                            {
                                models.Add(identifier, new List<int>()
                                {
                                    number
                                });
                            }
                        }
                    }
                }
            }
            return models;
        }
    }
}
