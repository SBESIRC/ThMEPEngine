using Autodesk.AutoCAD.Runtime;
using System.IO;
using System.Collections;
using System.Web.Script.Serialization;
using System;

namespace ThMEPEngineCore
{
    public class ThMEPEngineCoreApp : IExtensionApplication
    {
        public void Initialize()
        {
            //
        }

        public void Terminate()
        {
            //
        }

        [CommandMethod("TIANHUACAD", "THEXTRACTMODEL", CommandFlags.Modal)]
        public void ThExtractModel()
        {
            JavaScriptSerializer _JavaScriptSerializer = new JavaScriptSerializer();
            var _JsonBlocks = ReadTxt(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Blocks.json"));
            ArrayList _ListBlocks = _JavaScriptSerializer.Deserialize<ArrayList>(_JsonBlocks);


            var _JsonElementTypes = ReadTxt(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ElementTypes.json"));
            ArrayList _ListElementTypes = _JavaScriptSerializer.Deserialize<ArrayList>(_JsonElementTypes);
        }

        private string ReadTxt(string _Path)
        {
            try
            {
                using (StreamReader _StreamReader = File.OpenText(_Path))
                {
                    return _StreamReader.ReadToEnd();
                }
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
