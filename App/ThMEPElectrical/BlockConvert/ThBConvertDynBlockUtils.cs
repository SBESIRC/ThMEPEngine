using Linq2Acad;
using ThCADExtension;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace ThMEPElectrical.BlockConvert
{
    public static class ThBConvertDynBlockUtils
    {       
        /// <summary>
        /// 提取动态块中当前可见性下可见的文字的内容
        /// </summary>
        /// <param name="blockReference"></param>
        /// <returns></returns>
        public static List<string> VisibleTexts(this ThBlockReferenceData blockReference)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(blockReference.HostDatabase))
            {
                var texts = new List<string>();
                foreach (ObjectId obj in blockReference.VisibleEntities())
                {
                    var entity = acadDatabase.Element<Entity>(obj);
                    if (entity is DBText dBText)
                    {
                        texts.Add(dBText.TextString);
                    }
                }
                return texts;
            }
        }
    }
}
