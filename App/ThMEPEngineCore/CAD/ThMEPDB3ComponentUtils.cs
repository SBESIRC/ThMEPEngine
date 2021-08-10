using System;
using System.IO;
using System.Text;
using BuildingModelData;
using System.IO.Compression;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPEngineCore.CAD
{
    public class ThMEPDB3ComponentUtils
    {
        public static bool IsStair(Entity e)
        {
            string ComponentType = null;
            var info = GetObjectExtProp(e, "DXMXComponentInfo");
            if (!string.IsNullOrEmpty(info))//判断是否存在扩展参数，存在才有可能是楼梯或者部件
            {
                info = DecompressString(info);//解压字符串，防止楼梯存储的扩展参数过长的情况
                var nvSet = new BmdNameValueString(info);
                if (nvSet != null && nvSet.ContainsName("ComponentType"))
                {
                    ComponentType = nvSet["ComponentType"];
                }
            }
            return ComponentType == "Stair";
        }

        /// <summary>
        /// 解压字符串
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string DecompressString(string str)
        {
            var compressBeforeByte = Convert.FromBase64String(str);
            var compressAfterByte = Decompress(compressBeforeByte);
            string compressString = Encoding.GetEncoding("UTF-8").GetString(compressAfterByte);
            return compressString;
        }

        /// <summary>
        /// Decompress
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static byte[] Decompress(byte[] data)
        {
            try
            {
                var ms = new MemoryStream(data);
                var zip = new GZipStream(ms, CompressionMode.Decompress, true);
                var msreader = new MemoryStream();
                var buffer = new byte[0x1000];
                while (true)
                {
                    var reader = zip.Read(buffer, 0, buffer.Length);
                    if (reader <= 0)
                    {
                        break;
                    }
                    msreader.Write(buffer, 0, reader);
                }
                zip.Close();
                ms.Close();
                msreader.Position = 0;
                buffer = msreader.ToArray();
                msreader.Close();
                return buffer;
            }
            catch (System.Exception e)
            {
                throw new System.Exception(e.Message);
            }
        }

        /// <summary>
        /// 读取元素的扩展参数
        /// </summary>
        /// <param name="Obj"></param>
        /// <param name="propName"></param>
        /// <returns></returns>
        public static string GetNewObjectExtProp(DBObject Obj, string propName)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                var newPropName = "DXMX_" + propName + "_0";
                var result = Obj.GetXDataForApplication(newPropName);
                if (result != null)
                {
                    var txt = result.AsArray()[1].Value.ToString();
                    if (txt.Length >= 18 && txt.Substring(15, 3) == "|&|")
                    {
                        var hashcode = txt.Substring(0, 15);
                        var value = txt.Substring(18);
                        return value;
                    }

                    var values = result.AsArray();
                    for (int i = 1; i < values.Length; i++)
                    {
                        sb.Append(values[i].Value.ToString());
                    }
                    return sb.ToString();
                }
                else
                {
                    #region 旧的属性读取方式

                    result = Obj.GetXDataForApplication(propName);
                    if (result != null)
                    {
                        var txt = result.AsArray()[1].Value.ToString();
                        if (txt.Length >= 18 && txt.Substring(15, 3) == "|&|")
                        {
                            var hashcode = txt.Substring(0, 15);
                            var value = txt.Substring(18);

                            try
                            {
                                //if (value.GetHashCode() == Convert.ToInt32(hashcode))
                                //{
                                //    //将旧的属性写入方法替换未新的
                                //    SetObjectExtProp(Obj, propName, value);
                                //    return value;
                                //}
                                return value;
                            }
                            catch { return ""; }
                        }
                        else
                        {
                            //将旧的属性写入方法替换未新的
                            //SetObjectExtProp(Obj, propName, txt);

                            return txt;
                        }
                    }
                    else
                        return "";

                    #endregion 旧的属性读取方式
                }
            }
            catch (System.Exception)
            {
            }

            return "";
        }

        /// <summary>
        /// 读取元素的扩展参数(分批读取)
        /// </summary>
        /// <param name="Obj"></param>
        /// <param name="propName"></param>
        /// <param name="bmdEleType"></param>
        /// <returns></returns>
        public static string GetObjectExtProp(DBObject Obj, string propName)
        {
            var hatchEdges = string.Empty;
            var IsTrue = true;
            var index = 0;
            while (IsTrue)
            {
                string edgsString = GetNewObjectExtProp(Obj, propName + (index.ToString() == "0" ? "" : index.ToString()));
                if (!string.IsNullOrEmpty(edgsString))
                {
                    hatchEdges += edgsString;
                }
                else
                {
                    IsTrue = false;
                }
                index++;
            }

            return hatchEdges;
        }


        /// <summary>
        /// 检查对象是否存在扩展属性
        /// </summary>
        /// <param name="Obj"></param>
        /// <param name="propName"></param>
        /// <returns></returns>
        public static bool CheckExistObjectExtProp(DBObject Obj, string propName)
        {
            var newPropName = "DXMX_" + propName + "_0";
            var result = Obj.GetXDataForApplication(newPropName);
            if (result == null)
            {
                result = Obj.GetXDataForApplication(propName);
                if (result == null)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
