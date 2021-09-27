using System;
using Linq2Acad;
using System.IO;
using System.Linq;
using System.IO.Compression;
using TianHua.Publics.BaseCode;
using System.Runtime.Serialization.Formatters.Binary;
using Autodesk.AutoCAD.DatabaseServices;
using TianHua.FanSelection;
using TianHua.FanSelection.Service;
using ThMEPEngineCore.Service.Hvac;

namespace ThMEPHVAC.CAD
{
    public class ThFanModelDataService
    {
        public const int BINARYCHUNKSIZE = 127;
        public const string NOD_THMEP_FAN = "THMEP_FAN";
        public double CalcAirVolume(ObjectId objId)
        {
            using (var db = AcadDatabase.Use(objId.Database))
            {
                var identifier = objId.GetModelIdentifier();
                if (!string.IsNullOrEmpty(identifier))
                {
                    return 0.0;
                }

                var model = DataModel(objId.Database, identifier);
                if (model != null)
                {
                    return FanAirVolumeService.CalcAirVolume(model);
                }

                return 0.0;
            }
        }
        private FanDataModel DataModel(Database database, string identifier)
        {
            using (var db = AcadDatabase.Use(database))
            {
                var nod_collection = db.Element<DBDictionary>(db.Database.NamedObjectsDictionaryId);
                if (nod_collection.Contains(NOD_THMEP_FAN))
                {
                    ObjectId oid = nod_collection.GetAt(NOD_THMEP_FAN);
                    var dictionary = db.Element<DBDictionary>(oid);
                    if (dictionary != null)
                    {
                        using (DbDictionaryEnumerator enumerator = dictionary.GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                var xres = db.Element<Xrecord>(enumerator.Current.Value);
                                var data = FromResultBuffer(xres.Data);
                                var model = DeserializeBinary(DeCompressBinary(data));
                                if (model.ID == identifier)
                                {
                                    return model;
                                }
                            }
                        }
                    }
                }
                return null;
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
