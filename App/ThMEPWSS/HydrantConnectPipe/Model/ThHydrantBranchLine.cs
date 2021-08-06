using AcHelper;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Dreambuild.AutoCAD;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADCore.NTS;
using ThMEPEngineCore.CAD;

namespace ThMEPWSS.HydrantConnectPipe.Model
{
    public class ThHydrantBranchLine
    {
        public Polyline BranchPolylineObb { set; get; }
        public Polyline BranchPolyline { set; get; }
        public ThHydrantBranchLine()
        {
            BranchPolyline = new Polyline();
        }

        public static ThHydrantBranchLine Create(Entity data)
        {
            var branchLine = new ThHydrantBranchLine();
            if (data is Polyline)
            {
                var polyline = data.Clone() as Polyline;
                branchLine.BranchPolyline = polyline;
                var objcets = polyline.BufferPL(50);
                var obb = objcets[0] as Polyline;
                branchLine.BranchPolylineObb = obb;

            }
            return branchLine;
        }
        public void Draw(AcadDatabase acadDatabase)
        {
            var lines = BranchPolyline.ToLines();
            foreach (var line in lines)
            {
                line.LayerId = DbHelper.GetLayerId("W-FRPT-HYDT-PIPE-AI");
                acadDatabase.CurrentSpace.Add(line);
            }
        }
        public void InsertValve(AcadDatabase acadDatabase)
        {
            List<Line> lines = BranchPolyline.ToLines();
            while (!InsertValve(acadDatabase, lines))
            {
                if (lines.Count != 0)
                {
                    lines.RemoveAt(lines.Count - 1);
                }
                else
                {
                    return;
                }
            }
        }
        public void InsertPipeMark(AcadDatabase acadDatabase, string strMapScale)
        {
            string riserName = "";
            switch (strMapScale)
            {
                case "1:100":
                    riserName = "消火栓管线管径";
                    break;
                case "1:150":
                    riserName = "消火栓管径150";
                    break;
                default:
                    break;
            }
            var lines = BranchPolyline.ToLines();
            if (lines.Count != 0)
            {
                var line = lines.Last();
                var position = line.GetCenter();
                var vector = line.EndPoint.GetVectorTo(line.StartPoint).GetNormal();
                var refVector = new Vector3d(0, 0, 1);
                var basVector = new Vector3d(1, 0, 0);
                double angle = basVector.GetAngleTo(vector, refVector);
                if (angle > Math.PI / 2.0 && angle <= Math.PI)
                {
                    angle = angle + Math.PI;
                }
                else if (angle > Math.PI && angle <= Math.PI * 3.0 / 2.0)
                {
                    angle = angle - Math.PI;
                }
                var blkId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-DIMS", riserName, position, new Scale3d(1, 1, 1), angle);
                var blk = acadDatabase.Element<BlockReference>(blkId);
                if (blk.IsDynamicBlock)
                {
                    foreach (DynamicBlockReferenceProperty property in blk.DynamicBlockReferencePropertyCollection)
                    {
                        if (property.PropertyName == "可见性")
                        {
                            property.Value = "DN65";
                            break;
                        }
                        if (property.PropertyName == "可见性1")
                        {
                            property.Value = "DN65";
                            break;
                        }
                    }
                }
            }

        }
        private bool InsertValve(AcadDatabase acadDatabase, List<Line> lines)
        {
            if (lines.Count != 0)
            {
                var line = lines.Last();
                var normal = line.EndPoint.GetVectorTo(line.StartPoint).GetNormal();
                var vector = normal * 500;
                if (line.Length < 1000 && line.Length >= 560)
                {
                    vector = normal * (line.Length / 2.0);
                }
                else if (line.Length < 560)
                {
                    return false;
                }
                var postion = line.EndPoint + vector;
                var refVector = new Vector3d(0, 0, 1);
                var basVector = new Vector3d(1, 0, 0);
                double angle = basVector.GetAngleTo(vector, refVector);
                if (angle > Math.PI / 2.0 && angle <= Math.PI)
                {
                    angle = angle + Math.PI;
                }
                else if (angle > Math.PI && angle <= Math.PI * 3.0 / 2.0)
                {
                    angle = angle - Math.PI;
                }
                var blkId = acadDatabase.ModelSpace.ObjectId.InsertBlockReference("W-FRPT-HYDT-EQPM", "蝶阀", postion, new Scale3d(1, 1, 1), angle);
                var blk = acadDatabase.Element<BlockReference>(blkId);
                if (blk.IsDynamicBlock)
                {
                    foreach (DynamicBlockReferenceProperty property in blk.DynamicBlockReferencePropertyCollection)
                    {
                        if (property.PropertyName == "可见性")
                        {
                            property.Value = "蝶阀";
                            break;
                        }
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
