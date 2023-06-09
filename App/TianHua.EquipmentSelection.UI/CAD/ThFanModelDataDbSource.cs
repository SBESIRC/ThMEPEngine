﻿using System;
using System.IO;
using System.Linq;
using Dreambuild.AutoCAD;
using System.Diagnostics;
using System.IO.Compression;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using Autodesk.AutoCAD.DatabaseServices;
using TianHua.Publics.BaseCode;
using TianHua.FanSelection.Function;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThFanModelDataDbSource
    {
        public const int BINARYCHUNKSIZE = 127;
        public const string NOD_THMEP_FAN = "THMEP_FAN";

        public List<FanDataModel> Models { get; set; }

        public ThFanModelDataDbSource()
        {
            Models = new List<FanDataModel>();
        }

        public static ThFanModelDataDbSource Create(Database database)
        {
            var ds = new ThFanModelDataDbSource();
            ds.Load(database);
            return ds;
        }

        /// <summary>
        /// 将风机模型数据存入图纸NOD中
        /// </summary>
        /// <param name="database"></param>
        public void Save(Database database)
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
                Models.Where(m => m.IsSubModel() || m.IsValid()).ForEach(m =>
                {
                    var xres = new Xrecord()
                    {
                        Data = ToResultBuffer(CompressBinary(SerializeBinary(m))),
                    };
                    dictionary.SetAt(m.ID, xres);
                    tx.AddNewlyCreatedDBObject(xres, true);
                });
                tx.Commit();
            }
        }

        /// <summary>
        /// 删除风机模型
        /// </summary>
        /// <param name="database"></param>
        /// <param name="identifier"></param>
        public void Erase(Database database, string identifier)
        {
            using (OpenCloseTransaction tx = database.TransactionManager.StartOpenCloseTransaction())
            {
                var nod_collection = tx.GetObject(database.NamedObjectsDictionaryId, OpenMode.ForRead) as DBDictionary;
                if (nod_collection.Contains(NOD_THMEP_FAN))
                {
                    ObjectId oid = nod_collection.GetAt(NOD_THMEP_FAN);
                    var dictionary = tx.GetObject(oid, OpenMode.ForWrite) as DBDictionary;
                    if (dictionary != null)
                    {
                        using (DbDictionaryEnumerator enumerator = dictionary.GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                var xres = tx.GetObject(enumerator.Current.Value, OpenMode.ForWrite) as Xrecord;
                                var data = FromResultBuffer(xres.Data);
                                var model = DeserializeBinary(DeCompressBinary(data));
                                if (model.ID == identifier)
                                {
                                    xres.Erase();
                                }
                                else if (model.PID == identifier)
                                {
                                    xres.Erase();
                                }
                            }
                        }
                    }
                }
                tx.Commit();
            }
        }

        /// <summary>
        /// 从图纸NOD中读取风机模型数据
        /// </summary>
        /// <param name="database"></param>
        public void Load(Database database)
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
                                Models.Add(DeserializeBinary(DeCompressBinary(data)));
                            }
                        }
                    }
                }
            }
        }

        #region Binary Serialization
        // https://forums.autodesk.com/t5/net/binary-serialization-to-xrecord/td-p/5601969
        private byte[] SerializeBinary(FanDataModel model)
        {
            var json = FuncJson.Serialize(model);
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, json);
                return ms.ToArray();
            }
        }
        private FanDataModel DeserializeBinary(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                var formatter = new BinaryFormatter();
                return FuncJson.Deserialize<FanDataModel>((string)formatter.Deserialize(ms));
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
