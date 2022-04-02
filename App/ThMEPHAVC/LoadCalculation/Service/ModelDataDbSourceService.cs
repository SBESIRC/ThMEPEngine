using System;
using System.IO;
using System.Linq;
using Dreambuild.AutoCAD;
using System.Diagnostics;
using System.IO.Compression;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using Autodesk.AutoCAD.DatabaseServices;
using TianHua.Publics.BaseCode;
using ThMEPHVAC.LoadCalculation.Model;

namespace ThMEPHVAC.LoadCalculation.Service
{
    public class ModelDataDbSourceService
    {
        public const string NOD_THMEP_FAN = "THMEP_SWTF";
        public const string NOD_THMEP_MainUI = "THMEP_THFHJS_MainUI";
        public const int BINARYCHUNKSIZE = 127;

        public OutdoorParameterData dataModel { get; set; }
        public MainUIData mainUIData { get; set; }
        public ModelDataDbSourceService()
        {
            dataModel = new OutdoorParameterData() { Title= "室外通风温度/℃" };
            mainUIData = new MainUIData();
        }
        /// <summary>
        /// 将室外通风配置数据存入图纸NOD中
        /// </summary>
        /// <param name="database"></param>
        public void SaveSWTF(Database database)
        {
            using (OpenCloseTransaction tx = database.TransactionManager.StartOpenCloseTransaction())
            {
                var nod_collection = tx.GetObject(database.NamedObjectsDictionaryId, OpenMode.ForWrite) as DBDictionary;
                if (nod_collection.Contains(NOD_THMEP_FAN))
                {
                    nod_collection.Remove(NOD_THMEP_FAN);
                }
                var dictionary = new DBDictionary();
                nod_collection.SetAt(NOD_THMEP_FAN, dictionary);
                tx.AddNewlyCreatedDBObject(dictionary, true);

                var xres = new Xrecord()
                {
                    Data = ToResultBuffer(CompressBinary(SerializeBinary(dataModel))),
                };
                dictionary.SetAt("outdoorParameterData", xres);
                tx.AddNewlyCreatedDBObject(xres, true);
                tx.Commit();
            }
        }

        /// <summary>
        /// 将室外通风配置数据存入图纸NOD中
        /// </summary>
        /// <param name="database"></param>
        public void SaveFHJS(Database database)
        {
            using (OpenCloseTransaction tx = database.TransactionManager.StartOpenCloseTransaction())
            {
                try
                {
                    var nod_collection = tx.GetObject(database.NamedObjectsDictionaryId, OpenMode.ForWrite) as DBDictionary;
                    if (nod_collection.Contains(NOD_THMEP_MainUI))
                    {
                        nod_collection.Remove(NOD_THMEP_MainUI);
                    }
                    var dictionary = new DBDictionary();
                    nod_collection.SetAt(NOD_THMEP_MainUI, dictionary);
                    tx.AddNewlyCreatedDBObject(dictionary, true);

                    var xres = new Xrecord()
                    {
                        Data = ToResultBuffer(CompressBinary(SerializeBinary(mainUIData))),
                    };
                    dictionary.SetAt("outdoorParameterData", xres);
                    tx.AddNewlyCreatedDBObject(xres, true);
                    tx.Commit();
                }
                catch (Exception ex)
                {
                    //DB有时会有eWasOpenForRead崩溃，暂时先屏蔽这种崩溃
                }
            }
        }

        /// <summary>
        /// 从图纸NOD中读取室外通风配置数据
        /// </summary>
        /// <param name="database"></param>
        public void LoadSWTF(Database database)
        {
            using (OpenCloseTransaction tx = database.TransactionManager.StartOpenCloseTransaction())
            {
                var nod_collection = tx.GetObject(database.NamedObjectsDictionaryId, OpenMode.ForRead) as DBDictionary;
                if (nod_collection.Contains(NOD_THMEP_FAN))
                {
                    ObjectId oid = nod_collection.GetAt(NOD_THMEP_FAN);
                    var dictionary = tx.GetObject(oid, OpenMode.ForRead) as DBDictionary;
                    if (dictionary != null)
                    {
                        using (DbDictionaryEnumerator enumerator = dictionary.GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                var xres = tx.GetObject(enumerator.Current.Value, OpenMode.ForRead) as Xrecord;
                                var data = FromResultBuffer(xres.Data);
                                dataModel = DeserializeBinary(DeCompressBinary(data));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 从图纸NOD中读取室外通风配置数据
        /// </summary>
        /// <param name="database"></param>
        public void LoadFHJS(Database database)
        {
            using (OpenCloseTransaction tx = database.TransactionManager.StartOpenCloseTransaction())
            {
                var nod_collection = tx.GetObject(database.NamedObjectsDictionaryId, OpenMode.ForRead) as DBDictionary;
                if (nod_collection.Contains(NOD_THMEP_MainUI))
                {
                    ObjectId oid = nod_collection.GetAt(NOD_THMEP_MainUI);
                    var dictionary = tx.GetObject(oid, OpenMode.ForRead) as DBDictionary;
                    if (dictionary != null)
                    {
                        using (DbDictionaryEnumerator enumerator = dictionary.GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                var xres = tx.GetObject(enumerator.Current.Value, OpenMode.ForRead) as Xrecord;
                                var data = FromResultBuffer(xres.Data);
                                mainUIData = DeserializeBinaryMainUIData(DeCompressBinary(data));
                            }
                        }
                    }
                }
            }
        }

        #region Binary Serialization
        // https://forums.autodesk.com/t5/net/binary-serialization-to-xrecord/td-p/5601969
        private byte[] SerializeBinary(OutdoorParameterData model)
        {
            var json = FuncJson.Serialize(model);
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, json);
                return ms.ToArray();
            }
        }
        
        private byte[] SerializeBinary(MainUIData model)
        {
            var json = FuncJson.Serialize(model);
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, json);
                return ms.ToArray();
            }
        }
        private OutdoorParameterData DeserializeBinary(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                var formatter = new BinaryFormatter();
                return FuncJson.Deserialize<OutdoorParameterData>((string)formatter.Deserialize(ms));
            }
        }

        private MainUIData DeserializeBinaryMainUIData(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                var formatter = new BinaryFormatter();
                return FuncJson.Deserialize<MainUIData>((string)formatter.Deserialize(ms));
            }
        }
        private byte[] CompressBinary(byte[] data)
        {
            using (var compressedStream = new MemoryStream())
            {
                using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
                {
                    zipStream.Write(data, 0, data.Length);
                    zipStream.Close();
                    return compressedStream.ToArray();
                }
            }
        }
        private byte[] DeCompressBinary(byte[] data)
        {
            using (var compressedStream = new MemoryStream(data))
            {
                using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                {
                    using (var resultStream = new MemoryStream())
                    {
                        zipStream.CopyTo(resultStream);
                        return resultStream.ToArray();
                    }
                }
            }
        }
        private ResultBuffer ToResultBuffer(byte[] bytes)
        {
            int position = 0;
            int remaining = bytes.Length;
            var buff = new ResultBuffer();
            while (remaining > 0)
            {
                if (remaining > BINARYCHUNKSIZE)
                {
                    var chunk = new byte[BINARYCHUNKSIZE];
                    Buffer.BlockCopy(bytes, position, chunk, 0, BINARYCHUNKSIZE);
                    buff.Add(new TypedValue((int)DxfCode.BinaryChunk, chunk));
                    remaining -= BINARYCHUNKSIZE;
                    position += BINARYCHUNKSIZE;
                }
                else
                {
                    var chunk = new byte[remaining];
                    Buffer.BlockCopy(bytes, position, chunk, 0, remaining);
                    buff.Add(new TypedValue((int)DxfCode.BinaryChunk, chunk));
                    remaining = 0;
                }
            }
            return buff;
        }

        private byte[] FromResultBuffer(ResultBuffer buff)
        {
            return buff.AsArray().SelectMany(o => (byte[])o.Value).ToArray();
        }
        #endregion
    }
}
