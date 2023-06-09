﻿using System;
using System.Linq;
using ThCADCore.NTS;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using ThMEPEngineCore.Model;
using ThMEPWSS.Pipe.Model;

namespace ThMEPWSS.Pipe.Service
{
    public  class ThDevicePlatformCondensePipeService
    {
        public List<ThWCondensePipe> CondensePipes { get; private set; }
        private List<ThWCondensePipe> CondensePipeList { get; set; }
        private ThIfcSpace DevicePlatformSpace { get; set; }
        private ThCADCoreNTSSpatialIndex CondensePipeSpatialIndex { get; set; }
        private ThDevicePlatformCondensePipeService(
            List<ThWCondensePipe> condensePipeList,
            ThIfcSpace devicePlatformSpace,
            ThCADCoreNTSSpatialIndex condensePipeSpatialIndex)
        {
            CondensePipeList = condensePipeList;
            DevicePlatformSpace = devicePlatformSpace;
            CondensePipeSpatialIndex = condensePipeSpatialIndex;
            if (CondensePipeSpatialIndex == null)
            {
                DBObjectCollection dbObjs = new DBObjectCollection();
                CondensePipeList.ForEach(o => dbObjs.Add(o.Outline));
                CondensePipeSpatialIndex = new ThCADCoreNTSSpatialIndex(dbObjs);
            }
        }
        public static ThDevicePlatformCondensePipeService Find(
            List<ThWCondensePipe> condensePipeList,
            ThIfcSpace devicePlatformSpace,
            ThCADCoreNTSSpatialIndex condensePipeSpatialIndex = null)
        {
            var instance = new ThDevicePlatformCondensePipeService(condensePipeList, devicePlatformSpace, condensePipeSpatialIndex);
            instance.Find();
            return instance;
        }
        private void Find()
        {           
            var devicePlatformBoundary = DevicePlatformSpace.Boundary.Clone() as Polyline;
            devicePlatformBoundary.Closed = true;
            var crossObjs = CondensePipeSpatialIndex.SelectCrossingPolygon(devicePlatformBoundary);
            var crossCondensePipe = CondensePipeList.Where(o => crossObjs.Contains(o.Outline));
            CondensePipes = crossCondensePipe.Where(o => devicePlatformBoundary.Contains(o.Outline as Curve)).ToList();
        }
    }
}
