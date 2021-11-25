using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using NFox.Cad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.Engine;
using ThMEPHVAC.FanConnect.Model;

namespace ThMEPHVAC.FanConnect.Engine
{
    public class ThFanCURecognitionEngine
    {
        public List<ThFanCUModel> Extract(Database database)
        {
            using (var acadDatabase = AcadDatabase.Use(database))
            {
                var retModel = new List<ThFanCUModel>();
                var Results = acadDatabase
                   .ModelSpace
                   .OfType<BlockReference>()
                   .Where(o => IsHYDTPipeLayer(o.Layer) && IsValveBlock(o));
                foreach(var blk in Results)
                {
                    var tmpFan = new ThFanCUModel();
                    var offset1x = Convert.ToDouble(blk.ObjectId.GetDynBlockValue("水管连接点1 X"));
                    var offset1y = Convert.ToDouble(blk.ObjectId.GetDynBlockValue("水管连接点1 Y"));

                    var offset1 = new Point3d(offset1x, offset1y, 0);

                    var dbcollection = new DBObjectCollection();
                    blk.Explode(dbcollection);
                    dbcollection = dbcollection.OfType<Entity>().Where(O => O is Curve).ToCollection();

                    tmpFan.FanPoint = offset1.TransformBy(blk.BlockTransform);
                    tmpFan.FanObb = dbcollection.GetMinimumRectangle();
                    retModel.Add(tmpFan);
                }
                return retModel;
            }
        }

        private bool IsHYDTPipeLayer(string layer)
        {
            if(layer.ToUpper() == "H-EQUP-FC" || layer.ToUpper() == "H-EQUP-AHU")
            {
                return true;
            }

            return false;
        }

        private bool IsValveBlock(BlockReference blockReference)
        {
            var blkName = blockReference.GetEffectiveName().ToUpper();
            if(blkName.Contains("AI-FCU"))
            {
                return true;
            }
            return false;
        }
    }
}
