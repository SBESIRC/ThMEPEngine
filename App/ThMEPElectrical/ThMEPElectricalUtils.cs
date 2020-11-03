using System.IO;
using Linq2Acad;
using AcHelper;
using DotNetARX;
using ThCADExtension;
using GeometryExtensions;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.DatabaseServices;
//using Autodesk.AutoCAD.GraphicsInterface;

namespace ThMEPElectrical
{
    //public class ThCompassDrawJig : DrawJig
    //{
    //    #region Fields
    //    private Point3d mBase;
    //    private Point3d mLocation;
    //    List<Entity> mEntities;
    //    #endregion
    //    public ThCompassDrawJig(Point3d basePt)
    //    {
    //        mBase = basePt;
    //        mEntities = new List<Entity>();
    //    }
    //    #region Properties

    //    public Point3d Location
    //    {
    //        get { return mLocation; }
    //        set { mLocation = value; }
    //    }

    //    #endregion
    //    #region Methods

    //    public void AddEntity(Entity ent)
    //    {
    //        mEntities.Add(ent);
    //    }

    //    public void TransformEntities()
    //    {
    //        Matrix3d mat = Matrix3d.Displacement(mBase.GetVectorTo(mLocation));

    //        foreach (Entity ent in mEntities)
    //        {
    //            ent.TransformBy(mat);
    //        }
    //    }

    //    #endregion
    //    #region Overrides

    //    protected override bool WorldDraw(Autodesk.AutoCAD.GraphicsInterface.WorldDraw draw)
    //    {
    //        Matrix3d mat = Matrix3d.Displacement(mBase.GetVectorTo(mLocation));

    //        WorldGeometry geo = draw.Geometry;
    //        if (geo != null)
    //        {
    //            geo.PushModelTransform(mat);

    //            foreach (Entity ent in mEntities)
    //            {
    //                geo.Draw(ent);
    //            }

    //            geo.PopModelTransform();
    //        }

    //        return true;
    //    }

    //    protected override SamplerStatus Sampler(JigPrompts prompts)
    //    {
    //        JigPromptPointOptions prOptions1 = new JigPromptPointOptions("\n请指定插入点");
    //        prOptions1.UseBasePoint = false;

    //        PromptPointResult prResult1 = prompts.AcquirePoint(prOptions1);
    //        if (prResult1.Status == PromptStatus.Cancel || prResult1.Status == PromptStatus.Error)
    //            return SamplerStatus.Cancel;

    //        if (!mLocation.IsEqualTo(prResult1.Value, new Tolerance(10e-10, 10e-10)))
    //        {
    //            mLocation = prResult1.Value;
    //            return SamplerStatus.OK;
    //        }
    //        else
    //            return SamplerStatus.NoChange;
    //    }
    //    #endregion
    //}


    public static class ThMEPElectricalUtils
    {
        public static string BlockDwgPath()
        {
            return Path.Combine(ThCADCommon.SupportPath(), ThMEPCommon.SENSORDWGNAME);
        }

        public static ObjectId InsertCompassBlock(this Database database, string name, string layer, Dictionary<string, string> attNameValues)
        {
            using (AcadDatabase acadDatabase = AcadDatabase.Use(database))
            {
                return acadDatabase.ModelSpace.ObjectId.InsertBlockReference(
                    layer,
                    name,
                    Point3d.Origin,
                    new Scale3d(1.0),
                    0.0,
                    attNameValues);
            }
        }

        public static void ImportCompassBlock(this Database database, string name, string layer)
        {
            using (AcadDatabase currentDb = AcadDatabase.Use(database))
            using (AcadDatabase blockDb = AcadDatabase.Open(BlockDwgPath(), DwgOpenMode.ReadOnly, false))
            {
                currentDb.Blocks.Import(blockDb.Blocks.ElementOrDefault(name), false);
                currentDb.Layers.Import(blockDb.Layers.ElementOrDefault(layer), false);
            }
        }
    }
}
