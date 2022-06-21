using System.Collections.Generic;
using DotNetARX;
using Linq2Acad;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.CAD;
using ThMEPEngineCore.IO.SVG;
using ThMEPStructure.ArchiecturePlane.Service;

namespace ThMEPStructure.ArchiecturePlane.Print
{
    internal class ThDoorBlkPrinter: ThComponentBlkPrinter
    {
        protected string DoorLayer { get; set; }
        public ThDoorBlkPrinter()
        {
            DoorLayer = ThArchPrintLayerManager.AEDOORINSD;
        }
        public override ObjectIdCollection Print(Database db, List<ThComponentInfo> infos,double scale=1.0)
        {
            using (var acadDb = AcadDatabase.Use(db))
            {
                var results = new ObjectIdCollection();
                infos.ForEach(o =>
                {
                    var startPt = o.Start.ToPoint3d();
                    var endPt = o.End.ToPoint3d();                  
                    if(startPt.HasValue && endPt.HasValue)
                    {
                        var line = new Line(startPt.Value, endPt.Value);
                        string blkName = GetDoorBlkName(o.BlockName,line.Length);
                        double insertLength = GetDoorLength(line.Length, blkName); 
                        if(!string.IsNullOrEmpty(blkName))
                        {
                            var blkId = InsertBlock(acadDb, blkName, DoorLayer, scale);        
                            var entity = acadDb.Element<BlockReference>(blkId, true);
                            ModifyDoorLength(entity, insertLength);

                            // 把块的中心移动原点
                            var mt1 = Matrix3d.Displacement(new Vector3d(-1.0*line.Length/2.0,0,0));
                            entity.TransformBy(mt1);
                           
                            // 旋转
                            var mt2 = Matrix3d.Rotation(line.Angle,Vector3d.ZAxis,Point3d.Origin);
                            entity.TransformBy(mt2);
                            var blkOwnerDir = Vector3d.YAxis.TransformBy(mt2);

                            // 偏移
                            var midPt = line.StartPoint.GetMidPt(line.EndPoint);
                            var mt3 = Matrix3d.Displacement(midPt - Point3d.Origin);
                            entity.TransformBy(mt3);

                            // 调整门的方向
                            //if (dir.HasValue && dir.Value.Length > 0.0)
                            //{
                            //    // dir 为门的开启方向
                            //    if(blkOwnerDir.DotProduct(dir.Value)<0.0)
                            //    {
                            //        var linear3d = new Line3d(line.StartPoint, line.EndPoint);
                            //        var mt4 =Matrix3d.Mirroring(linear3d);
                            //        entity.TransformBy(mt4);
                            //        linear3d.Dispose();
                            //    }
                            //}
                            results.Add(blkId);
                            line.Dispose();
                        }
                    }
                });
                return results;
            }
        }

        protected virtual double GetDoorLength(double doorLength,string blkName)
        {
            // 后续会根据图块名来调整
            return doorLength - 50.0;
        }

        protected ObjectId InsertBlock(AcadDatabase acadDb, string blkName, string layer,double scale = 1.0)
        {
            return acadDb.ModelSpace.ObjectId.InsertBlockReference(layer, blkName, Point3d.Origin, new Scale3d(scale), 0.0);
        }

        protected void ModifyDoorLength(BlockReference br,double length)
        {
            // 调整长度
            if (br.IsDynamicBlock)
            {
                foreach (DynamicBlockReferenceProperty property in br.DynamicBlockReferencePropertyCollection)
                {
                    if (property.PropertyName == "距离")
                    {
                        property.Value = length;
                        break;
                    }
                }
            }
        }

        protected double? GetRotation(string rotation)
        {
            if(string.IsNullOrEmpty(rotation))
            {
                return null;
            }
            else
            {
                double rad = 0.0;
                if(double.TryParse(rotation,out rad))
                {
                    return rad;
                }
                else
                {
                    return null;
                }
            }     
        }

        protected double GetRotation(Point3d sp,Point3d ep)
        {
            var dir = sp.GetVectorTo(ep);
            return Vector3d.XAxis.GetAngleTo(dir, Vector3d.ZAxis);
        }

        private string GetDoorBlkName(string openMethod, double length)
        {
            // For test
            if(openMethod == "平开门")
            {
                if (length >= 540 && length <= 940)
                {
                    return ThArchPrintBlockManager.ADoor1;
                }
                else if (length >= 940 && length <= 2340)
                {
                    return ThArchPrintBlockManager.ADoor2;
                }
                else
                {
                    return "";
                }
            }
            else if(openMethod == "推拉门")
            {
                if (length >= 1200 && length <= 2400)
                {
                    return ThArchPrintBlockManager.ADoor4;
                }
                else if (length >= 2400 && length <= 3200)
                {
                    return ThArchPrintBlockManager.ADoor5;
                }
                else
                {
                    return "";
                }
            }
            else if(openMethod == "子母门")
            {
                if(length >= 1040 && length <= 1240)
                {
                    return ThArchPrintBlockManager.ADoor3;
                }
                else
                {
                    return "";
                }
            }    
            else
            {
                return "";
            }
        }
    }
}
