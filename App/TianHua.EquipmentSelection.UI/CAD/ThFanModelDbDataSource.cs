using System;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using Linq2Acad;
using AcHelper.Collections;
using Autodesk.AutoCAD.DatabaseServices;
using TianHua.Publics.BaseCode;

namespace TianHua.FanSelection.UI.CAD
{
    public class ThFanModelDbDataSource
    {
        public const int BINARYCHUNKSIZE = 127;
        public const string NOD_THMEP_FAN = "THMEP_FAN";

        public List<FanDataModel> Models { get; set; }

        public ThFanModelDbDataSource()
        {
            Models = new List<FanDataModel>();
        }

        public void Save(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var nod_collection = acadDatabase.Element<DBDictionary>(database.NamedObjectsDictionaryId, true);
                if (nod_collection.Contains(NOD_THMEP_FAN))
                {
                    nod_collection.Remove(NOD_THMEP_FAN);
                }
                var dictionary = new DBDictionary();
                nod_collection.SetAt(NOD_THMEP_FAN, dictionary);
                acadDatabase.AddNewlyCreatedDBObject(dictionary);
                Models.ForEach(m =>
                {
                    var xres = new Xrecord()
                    {
                        Data = ToResultBuffer(CompressBinary(SerializeBinary(m))),
                    };
                    dictionary.SetAt(m.ID, xres);
                    acadDatabase.AddNewlyCreatedDBObject(xres);
                });
            }
        }
        public void Load(Database database)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                var transaction = database.TransactionManager.TopTransaction;
                var dictionary = Dictionaries.GetNamedObjectsDictionary(NOD_THMEP_FAN);
                if (dictionary != null)
                {
                    using (DbDictionaryEnumerator enumerator = dictionary.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            var xres = acadDatabase.Element<Xrecord>(enumerator.Current.Value);
                            var data = FromResultBuffer(xres.Data);
                            Models.Add(DeserializeBinary(DeCompressBinary(data)));
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
