using System;
using System.Collections.Generic;

using Xbim.IO;
using Xbim.Ifc;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.SharedBldgElements;

namespace ThMEPIFC.Ifc2x3
{
    public class ThProtoBuf2IFC2x3Builder
    {
        static public void BuildIfcModel(IfcStore Model, ThTCHProjectData project)
        {
            if (Model != null)
            {
                var site = ThProtoBuf2IFC2x3Factory.CreateSite(Model);
                var building = ThProtoBuf2IFC2x3Factory.CreateBuilding(Model, site, project.Site.Buildings[0]);
                foreach (var thtchstorey in project.Site.Buildings[0].Storeys)
                {
                    var walls = new List<IfcWall>();
                    var columns = new List<IfcColumn>();
                    var beams = new List<IfcBeam>();
                    var slabs = new List<IfcSlab>();
                    var doors = new List<IfcDoor>();
                    var windows = new List<IfcWindow>();
                    var railings = new List<IfcRailing>();
                    var rooms = new List<IfcSpace>();
                    var floor_origin = thtchstorey.Origin;
                    var storey = ThProtoBuf2IFC2x3Factory.CreateStorey(Model, building, thtchstorey);
                    foreach (var thtchwall in thtchstorey.Walls)
                    {
                        var wall = ThProtoBuf2IFC2x3Factory.CreateWall(Model, thtchwall, floor_origin);
                        walls.Add(wall);
                        foreach (var thtchdoor in thtchwall.Doors)
                        {
                            doors.Add(SetupDoor(Model, wall, thtchwall, thtchdoor, floor_origin));
                        }
                        foreach (var thtchwindow in thtchwall.Windows)
                        {
                            windows.Add(SetupWindow(Model, wall, thtchwall, thtchwindow, floor_origin));
                        }
                        foreach (var thtchhole in thtchwall.Openings)
                        {
                            SetupHole(Model, wall, thtchhole, floor_origin);
                        }
                    }
                    //暂不支持梁和柱
                    //foreach (var thtchcolumn in thtchstorey.Columns)
                    //{
                    //    var column = ThProtoBuf2IFC2x3Factory.CreateColumn(Model, thtchcolumn, floor_origin);
                    //    columns.Add(column);
                    //}
                    //foreach (var thtchbeam in thtchstorey.Beams)
                    //{
                    //    var beam = ThProtoBuf2IFC2x3Factory.CreateBeam(Model, thtchbeam, floor_origin);
                    //    beams.Add(beam);
                    //}
                    ThXbimSlabEngine slabxbimEngine = new ThXbimSlabEngine();
                    foreach (var thtchslab in thtchstorey.Slabs)
                    {
                        var slab = ThProtoBuf2IFC2x3Factory.CreateBrepSlab(Model, thtchslab, floor_origin, slabxbimEngine);
                        if (null != slab)
                            slabs.Add(slab);
                    }
                    foreach (var thtchrailing in thtchstorey.Railings)
                    {
                        var railing = ThProtoBuf2IFC2x3Factory.CreateRailing(Model, thtchrailing, floor_origin);
                        railings.Add(railing);
                    }
                    //遍历，造房间
                    foreach (var thtchRoom in thtchstorey.Rooms)
                    {
                        var room = ThProtoBuf2IFC2x3Factory.CreateRoom(Model, thtchRoom, floor_origin);
                        rooms.Add(room);
                    }
                    ThProtoBuf2IFC2x3Factory.relContainSlabs2Storey(Model, slabs, storey);
                    ThProtoBuf2IFC2x3Factory.relContainWalls2Storey(Model, walls, storey);
                    ThProtoBuf2IFC2x3Factory.relContainColumns2Storey(Model, columns, storey);
                    ThProtoBuf2IFC2x3Factory.relContainBeams2Storey(Model, beams, storey);
                    ThProtoBuf2IFC2x3Factory.relContainDoors2Storey(Model, doors, storey);
                    ThProtoBuf2IFC2x3Factory.relContainWindows2Storey(Model, windows, storey);
                    ThProtoBuf2IFC2x3Factory.relContainsRailings2Storey(Model, railings, storey);
                    ThProtoBuf2IFC2x3Factory.relContainsRooms2Storey(Model, rooms, storey);
                }
            }
        }

        static public void BuildIfcModel(IfcStore Model, ThSUProjectData project)
        {
            if (Model != null)
            {
                // 虚拟set
                var site = ThProtoBuf2IFC2x3Factory.CreateSite(Model);
                var building = ThProtoBuf2IFC2x3Factory.CreateBuilding(Model, site, project.Root.Name + "Building");
                var storey = ThProtoBuf2IFC2x3Factory.CreateStorey(Model, building, "1F");
                var definitions = project.Definitions;
                var suElements = new List<IfcBuildingElementProxy>();
                foreach (var element in project.Buildings)
                {
                    var def = definitions[element.Component.DefinitionIndex];
                    var trans = element.Component.Transformations;
                    var ifcBuildingElement = ThProtoBuf2IFC2x3Factory.CreatedSUElement(Model, def, trans);
                    suElements.Add(ifcBuildingElement);
                }
                ThProtoBuf2IFC2x3Factory.relContainsSUElements2Storey(Model, suElements, storey);
            }
        }

        static public IfcDoor SetupDoor(IfcStore model, IfcWall ifcWall, ThTCHWallData wall, ThTCHDoorData door, ThTCHPoint3d floor_origin)
        {
            var ifcDoor = ThProtoBuf2IFC2x3Factory.CreateDoor(model, door, floor_origin);
            var ifcHole = ThProtoBuf2IFC2x3Factory.CreateHole(model, wall, door, floor_origin);
            ThProtoBuf2IFC2x3Factory.BuildRelationship(model, ifcWall, ifcDoor, ifcHole);
            return ifcDoor;
        }

        static public IfcWindow SetupWindow(IfcStore model, IfcWall ifcWall, ThTCHWallData wall, ThTCHWindowData window, ThTCHPoint3d floor_origin)
        {
            var ifcWindow = ThProtoBuf2IFC2x3Factory.CreateWindow(model, window, floor_origin);
            var ifcHole = ThProtoBuf2IFC2x3Factory.CreateHole(model, wall, window, floor_origin);
            ThProtoBuf2IFC2x3Factory.BuildRelationship(model, ifcWall, ifcWindow, ifcHole);
            return ifcWindow;
        }

        static public IfcOpeningElement SetupHole(IfcStore model, IfcWall ifcWall, ThTCHOpeningData hole, ThTCHPoint3d floor_origin)
        {
            var ifcHole = ThProtoBuf2IFC2x3Factory.CreateHole(model, hole, floor_origin);
            ThProtoBuf2IFC2x3Factory.BuildRelationship(model, ifcWall, ifcHole);
            return ifcHole;
        }

        static public void SaveIfcModel(IfcStore Model, string filepath)
        {
            if (Model != null)
            {
                using (var txn = Model.BeginTransaction("save ifc file"))
                {
                    try
                    {
                        Model.SaveAs(filepath, IfcStorageType.Ifc);
                    }
                    catch (System.Exception e)
                    {
                        Console.WriteLine("Failed to save HelloWall.ifc");
                        Console.WriteLine(e.Message);
                    }
                    txn.Commit();
                }
            }
        }
    }
}
