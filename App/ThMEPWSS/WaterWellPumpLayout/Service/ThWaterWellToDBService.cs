using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeometryExtensions;
using ThMEPWSS.Pipe.Model;
using ThMEPWSS.WaterWellPumpLayout.Model;

namespace ThMEPWSS.WaterWellPumpLayout.Service
{
    public class ThWaterWellToDBService
    {
        public void RemovePumpInDb(ThWaterPumpModel model)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
                model.Geometry.UpgradeOpen();
                model.Geometry.Erase();
                model.Geometry.DowngradeOpen();
            }
        }
        public void InsertPumpToDb(ThWaterWellModel model, int pumpCount, string pumpName, double fontHeight)
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                if (pumpCount==0)
                {
                    return;
                }

                //获取插入的边
                int edgeIndex = model.GetInstalEdge(pumpCount);
                //获取插入水泵的角度
                double angele = model.GetInstalEdgeAngle(edgeIndex);
                //获取插入水泵的位置
                Point3d position = model.GetInstalPosition(edgeIndex, pumpCount, out double space);
                Dictionary<string, string> attNameValues = new Dictionary<string, string>();
                attNameValues.Add("编号", pumpName);
                //文字插入角度ucs
                var blk = InsertBlockReference("W-EQPM", WaterWellBlockNames.DeepWaterPump, position, new Scale3d(1, 1, 1), angele * Math.PI / 180, attNameValues);

                SetPumpCount(blk, pumpCount);
                SetPumpSpace(blk, space);
                SetFontHeight(blk, fontHeight);

                var dump = ThWaterPumpModel.Create(blk);
                model.PumpModel = dump;
               // model.IsHavePump = true;

                //dump.SetPumpCount(pumpCount);
                //dump.SetPumpSpace(space);
                //dump.SetFontHeight(fontHeight);
            }
        }

        private BlockReference InsertBlockReference(string layer, string blkName, Point3d position, Scale3d scale, double angle, Dictionary<string, string> values)
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                var blkId = acadDb.ModelSpace.ObjectId.InsertBlockReference(layer, blkName, position, scale, angle, values);

                var vec = Vector3d.XAxis.TransformBy(Active.Editor.UCS2WCS()).GetNormal();
                var textAngle = Vector3d.XAxis.GetAngleTo(vec, Vector3d.ZAxis);

                blkId.SetDynBlockValue("角度2", textAngle);

                var blk = acadDb.Element<BlockReference>(blkId);
                return blk;
            }
        }


        private void SetPumpSpace(BlockReference blk, double space)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
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
                            property.Value = space * 2;
                        }
                        else if (property.PropertyName == "距离2")
                        {
                            property.Value = space * 3;
                        }
                    }
                }
            }
        }
        public void SetFontHeight(BlockReference blk, double fontHeight)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
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
        public void SetPumpCount(BlockReference blk, int count)
        {
            using (var db = Linq2Acad.AcadDatabase.Active())
            {
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

    }
}
