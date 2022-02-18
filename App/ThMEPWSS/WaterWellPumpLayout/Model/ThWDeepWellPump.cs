using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Model;

namespace ThMEPWSS.Pipe.Model
{
    public class ThWDeepWellPump //: ThIfcBuildingElement
    {
        private double Angle = 0.0;//泵角度
        public ObjectId PumpObjectID { get; private set; }        

        public void SetPumpSpace(double space)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var blk = db.Element<BlockReference>(PumpObjectID);
                if (blk.IsDynamicBlock)
                {
                    foreach (DynamicBlockReferenceProperty property in blk.DynamicBlockReferencePropertyCollection)
                    {
                        if (property.PropertyName == "距离")
                        {
                            property.Value = space;
                        }
                        else if (property.PropertyName == "距离1")
                        {
                            property.Value = space*2;
                        }
                        else if (property.PropertyName == "距离2")
                        {
                            property.Value = space*3;
                        }
                    }
                }
            }

        }
        public void SetFontHeight(double fontHeight)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var blk = db.Element<BlockReference>(PumpObjectID);
                if (blk.IsDynamicBlock)
                {
                    foreach (DynamicBlockReferenceProperty property in blk.DynamicBlockReferencePropertyCollection)
                    {
                        if (property.PropertyName == "字高")
                        {
                            property.Value = fontHeight;
                        }
                    }
                }
            }
        }
        public void SetLocPoint(Point3d point)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var blk = db.Element<BlockReference>(PumpObjectID);
                blk.Position = point;
            }
        }
        public void SetRotation(double rotation)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var blk = db.Element<BlockReference>(PumpObjectID);
                blk.Rotation = rotation - Angle;
            }
        }
        public void SetLayer(string layer)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                PumpObjectID.SetLayer(layer);
                //var blk = db.Element<BlockReference>(PumpObjectID);
                //blk.Layer = layer;
            }
        }
        public void SetPumpCount(int count)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var blk = db.Element<BlockReference>(PumpObjectID);
                if (blk.IsDynamicBlock)
                {
                    foreach (DynamicBlockReferenceProperty property in blk.DynamicBlockReferencePropertyCollection)
                    {
                        if (property.PropertyName == "可见性")
                        {
                            string strCount = "单台";
                            switch (count)
                            {
                                case 1:
                                    strCount = "单台";
                                    break;
                                case 2:
                                    strCount = "两台";
                                    break;
                                case 3:
                                    strCount = "三台";
                                    break;
                                case 4:
                                    strCount = "四台";
                                    break;
                                default:
                                    break;
                            }
                            property.Value = strCount;
                        }
                    }
                }
            }
        }
        public string GetName()
        {
            string strName = "";
            SortedDictionary<string, string> attributes = PumpObjectID.GetAttributesInBlockReference();
            foreach (var property in attributes)
            {
                if(property.Key == "编号")
                {
                    strName = property.Value;
                }
            }
            return strName;
        }
        public Point3d GetPosition()
        {
            Point3d pos;
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                var blk = db.Element<BlockReference>(PumpObjectID);
                pos = blk.Position;
            }
            return pos;
        }
        public int GetPumpCount()
        {
            int count = 0;
            using (var db = Linq2Acad.AcadDatabase.Active())
            { 
                var blk = db.Element<BlockReference>(PumpObjectID);
                if (blk.IsDynamicBlock)
                {
                    foreach (DynamicBlockReferenceProperty property in blk.DynamicBlockReferencePropertyCollection)
                    {
                        if (property.PropertyName == "可见性")
                        {
                            string strCount = (string)property.Value;
                            if (strCount == "单台")
                            {
                                count = 1;
                            }
                            else if (strCount == "两台")
                            {
                                count = 2;
                            }
                            else if (strCount == "三台")
                            {
                                count = 3;
                            }
                            else if (strCount == "四台")
                            {
                                count = 4;
                            }
                            break;
                        }
                    }
                }
            }
            
            return count;
        }
        public static ThWDeepWellPump Create(string layer, string blockName,string pumpName, Point3d position, Scale3d scale, double rotateAngle)
        {
            //var objectID = Draw.Insert("潜水泵-AI", pt,angle*Math.PI/180.0);
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                Dictionary<string, string> attNameValues = new Dictionary<string, string>();
                attNameValues.Add("编号", pumpName);
                var blkId= acadDb.ModelSpace.ObjectId.InsertBlockReference(layer, blockName, position, scale, rotateAngle * Math.PI/180, attNameValues);
                return new ThWDeepWellPump()
                {
                    PumpObjectID = blkId
                    //Outline = entity,
                    //Uuid = Guid.NewGuid().ToString()
                };
            }
        }
        public static ThWDeepWellPump Create(ObjectId id)
        {
            return new ThWDeepWellPump()
            {
                PumpObjectID = id
            };
        }
    }
}
