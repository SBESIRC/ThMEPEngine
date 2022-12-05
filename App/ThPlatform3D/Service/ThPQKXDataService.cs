using Autodesk.AutoCAD.DatabaseServices;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThPlatform3D.Service
{
    public class ThPQKXDataService
    {
        public const string RegAppName_PQK = "THBM_PQK";

        public static void AddXData(ObjectId objId, TypedValueList tvs)
        {
            objId.AddXData(RegAppName_PQK, tvs);
        }

        public static TypedValueList GetXData(ObjectId objId)
        {
            return objId.GetXData(RegAppName_PQK);
        }

        public static TypedValueList Create(List<string> values)
        {
            var tvs = new TypedValueList();
            values.ForEach(v =>
            {
                tvs.Add(DxfCode.ExtendedDataAsciiString, v);
            });           
            return tvs;
        }       
    }
}
