using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TianHua.AutoCAD.Utility.ExtensionTools
{
    public static class ObjectIDTool
    {
        public static T GetObjectByID<T>(this ObjectId id, OpenCloseTransaction tr) where T : DBObject
        {
            return tr.GetObject(id, OpenMode.ForRead) as T;
        }

        public static T GetObjectByID<T>(this ObjectId id, OpenCloseTransaction tr,OpenMode openMode) where T : DBObject
        {
            return tr.GetObject(id, openMode) as T;
        }

        public static T GetObjectByID<T>(this ObjectId id) where T : DBObject
        {
            return id.GetObject(OpenMode.ForWrite) as T;
        }

        public static T GetObjectByID<T>(this ObjectId id, OpenMode openMode) where T : DBObject
        {
            return id.GetObject(openMode) as T;
        }
    }
}
