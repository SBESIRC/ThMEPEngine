using Autodesk.AutoCAD.DatabaseServices;
using Linq2Acad;
using System.Collections.Generic;

namespace Tianhua.Platform3D.UI.PropertyServices.EntityProperties
{
    [PropertyAttribute("楼板线", "")]
    class FloorPLinePropertyTest : ITHProperty
    {
        ObjectId selectId;
        string appName = "";
        public string ShowTypeName => "楼板线";
        public Dictionary<string, object> Properties { get; set; }
        public bool IsVaild { get; set; }
        public void InitObjectId(ObjectId objectId)
        {
            selectId = objectId;
            ClearData();
        }
        public void CheckAndGetData()
        {
            ClearData();
            using (var acadDb = AcadDatabase.Active())
            {
                var entity = acadDb.ModelSpace.Element(selectId);
                if (null == entity || entity.IsErased)
                {
                    return;
                }
                if (entity is Curve polyline)
                {
                    if (polyline.Layer.Contains("楼板"))
                    {
                        IsVaild = true;
                    }
                }
                if (IsVaild)
                {
                    var dbObject = selectId.GetObject(OpenMode.ForRead, true);
                    var valueList = dbObject.GetXDataForApplication(appName);
                    if (valueList == null)
                    {
                        Properties = DefaultProperties();
                    }
                }
            }
        }
        public Dictionary<string, object> DefaultProperties()
        {
            var res = new Dictionary<string, object>();
            res.Add("板厚", 200.0);
            res.Add("偏移", 0.0);
            res.Add("说明", "测试例子");
            return res;
        }
        void ClearData() 
        {
            Properties = new Dictionary<string, object>();
            IsVaild = false;
        }
    }
}
