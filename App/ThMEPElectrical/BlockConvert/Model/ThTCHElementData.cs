using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using DotNetARX;
using Linq2Acad;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThCADExtension;

namespace ThMEPElectrical.BlockConvert.Model
{
    public class ThTCHElementData
    {
        public double Rotation { get; set; }
        public Point3d Position { get; set; }
        public Scale3d ScaleFactors { get; set; }
        public string Name { get; set; }
        public Database Database { get; set; }
        public ObjectId ObjId { get; set; }
        public Vector3d Normal { get; set; }
        public Matrix3d OwnerSpace2WCS { get; set; }
        public Matrix3d BlockTransform { get; set; }
        public SortedDictionary<string, string> Attributes { get; set; }
        public DynamicBlockReferencePropertyCollection CustomProperties
        {
            get
            {
                if (ObjId.GetObject(OpenMode.ForWrite) is BlockReference br)
                {
                    return br.DynamicBlockReferencePropertyCollection;
                }
                throw new NotSupportedException();
            }
        }

        public ThTCHElementData(Entity blockRef)
        {
            Init(blockRef);
            OwnerSpace2WCS = Matrix3d.Identity;
        }

        public ThTCHElementData(Entity blockRef, Matrix3d transfrom)
        {
            Init(blockRef);
            OwnerSpace2WCS = transfrom;
        }

        private void Init(Entity blockRef)
        {
            dynamic acadObj = blockRef.AcadObject;
            using (var acadDatabase = AcadDatabase.Use(blockRef.Database))
            {
                ObjId = blockRef.Id;
                Database = blockRef.Database;
                Position = new Point3d(acadObj.Hvac_Pt0[0], acadObj.Hvac_Pt0[1], acadObj.Hvac_Pt0[2]);
                Rotation = acadObj.Hvac_R25;
                //Normal = blockRef.GetBlockNormal();
                //ScaleFactors = blockRef.Id.GetScaleFactors();
                Name = acadObj.Hvac_S5;
                //BlockTransform = blockRef.GetBlockTransform();
                //Attributes = blockRef.GetAttributesInBlockReference();
                OwnerSpace2WCS = Matrix3d.Identity;
            }
        }
    }
}
