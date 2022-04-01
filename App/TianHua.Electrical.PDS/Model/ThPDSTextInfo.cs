using System.Collections.Generic;

using Autodesk.AutoCAD.DatabaseServices;

namespace TianHua.Electrical.PDS.Model
{
    public class ThPDSTextInfo
    {
        public List<string> Texts { get; set; }
        public List<ObjectId> ObjectIds { get; set; }

        public ThPDSTextInfo()
        {
            Texts = new List<string>();
            ObjectIds = new List<ObjectId>();
        }
    }
}
