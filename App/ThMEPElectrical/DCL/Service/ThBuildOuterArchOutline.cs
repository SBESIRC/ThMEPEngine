﻿using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPEngineCore.Service;

namespace ThMEPElectrical.DCL.Service
{
    public class ThBuildOuterArchOutline: ThBuildOuterOutline
    {
        public ThBuildOuterArchOutline()
        {
        }
        public override void Extract(Database db,Point3dCollection pts)
        {
            var data = new Model1Data(db, pts);
            ModelData = data;
            var outlineBuilder = new ThArchitectureOutlineBuilder(data.MergeData());
            outlineBuilder.Build();
            OuterOutlineList = outlineBuilder.Results;
        }
    }
}
