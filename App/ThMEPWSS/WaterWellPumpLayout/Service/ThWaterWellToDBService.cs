using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public void InsertPumpToDb(ThWaterWellModel model,int pumpCount ,string pumpName,double fontHeight)
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                //获取插入的边
                int edgeIndex = model.GetInstalEdge(pumpCount);
                //获取插入水泵的角度
                double angele = model.GetInstalEdgeAngle(edgeIndex);
                //获取插入水泵的位置
                Point3d position = model.GetInstalPosition(edgeIndex, pumpCount, out double space);
                Dictionary<string, string> attNameValues = new Dictionary<string, string>();
                attNameValues.Add("编号", pumpName);
                var blk = InsertBlockReference("W-EQPM", WaterWellBlockNames.DeepWaterPump,position, new Scale3d(1, 1, 1), angele * Math.PI / 180, attNameValues);
                var dump = ThWaterPumpModel.Create(blk);
                dump.SetPumpCount(pumpCount);
                dump.SetPumpSpace(space);
                dump.SetFontHeight(fontHeight);
            }
        }
        public BlockReference InsertBlockReference(string layer,string blkName,Point3d position, Scale3d scale, double angle, Dictionary<string, string> values)
        {
            using (var acadDb = Linq2Acad.AcadDatabase.Active())
            {
                var blkId = acadDb.ModelSpace.ObjectId.InsertBlockReference(layer, blkName, position, scale, angle, values);
                var blk = acadDb.Element<BlockReference>(blkId);
                return blk;
            }
        }
    }
}
