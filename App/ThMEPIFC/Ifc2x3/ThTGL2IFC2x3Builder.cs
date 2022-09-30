using System;
using Xbim.IO;
using Xbim.Ifc;
using ThMEPTCH.Model;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.SharedBldgElements;

namespace ThMEPIFC.Ifc2x3
{
    public class ThTGL2IFC2x3Builder
    {
        static public void BuildIfcModel(IfcStore Model, ThTCHProject project)
        {
            if (Model != null)
            {
                var site = ThTGL2IFC2x3Factory.CreateSite(Model, project.Site);
                var building = ThTGL2IFC2x3Factory.CreateBuilding(Model, site, project.Site.Building);
                foreach (var thtchstorey in project.Site.Building.Storeys)
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
                    var storey = ThTGL2IFC2x3Factory.CreateStorey(Model, building, thtchstorey);
                    foreach (var thtchwall in thtchstorey.Walls)
                    {
                        var wall = ThTGL2IFC2x3Factory.CreateWall(Model, thtchwall, floor_origin);
                        walls.Add(wall);
                        foreach (var thtchdoor in thtchwall.Doors)
                        {
                            doors.Add(SetupDoor(Model, wall, thtchwall, thtchdoor, floor_origin));
                        }
                        foreach (var thtchwindow in thtchwall.Windows)
                        {
                            windows.Add(SetupWindow(Model, wall, thtchwall,thtchwindow, floor_origin));
                        }
                        foreach (var thtchhole in thtchwall.Openings)
                        {
                            SetupHole(Model, wall, thtchhole, floor_origin);
                        }
                    }
                    foreach (var thtchcolumn in thtchstorey.Columns)
                    {
                        var column = ThTGL2IFC2x3Factory.CreateColumn(Model, thtchcolumn, floor_origin);
                        columns.Add(column);
                    }
                    foreach (var thtchbeam in thtchstorey.Beams)
                    {
                        var beam = ThTGL2IFC2x3Factory.CreateBeam(Model, thtchbeam, floor_origin);
                        beams.Add(beam);
                    }
                    foreach (var thtchslab in thtchstorey.Slabs)
                    {
                        var slab = ThTGL2IFC2x3Factory.CreateMeshSlab(Model, thtchslab, floor_origin);
                        if(null !=slab)
                            slabs.Add(slab);
                    }
                    foreach (var thtchrailing in thtchstorey.Railings)
                    {
                        var railing = ThTGL2IFC2x3Factory.CreateRailing(Model, thtchrailing, floor_origin);
                        railings.Add(railing);
                    }
                    //遍历，造房间
                    foreach (var thtchRoom in thtchstorey.Rooms)
                    {
                        var room = ThTGL2IFC2x3Factory.CreateRoom(Model, thtchRoom, floor_origin);
                        rooms.Add(room);
                    }
                    ThTGL2IFC2x3Factory.relContainSlabs2Storey(Model, slabs, storey);
                    ThTGL2IFC2x3Factory.relContainWalls2Storey(Model, walls, storey);
                    ThTGL2IFC2x3Factory.relContainColumns2Storey(Model, columns, storey);
                    ThTGL2IFC2x3Factory.relContainBeams2Storey(Model, beams, storey);
                    ThTGL2IFC2x3Factory.relContainDoors2Storey(Model, doors, storey);
                    ThTGL2IFC2x3Factory.relContainWindows2Storey(Model, windows, storey);
                    ThTGL2IFC2x3Factory.relContainsRailings2Storey(Model, railings, storey);
                    ThTGL2IFC2x3Factory.relContainsRooms2Storey(Model, rooms, storey);
                }
            }
        }

        static public IfcDoor SetupDoor(IfcStore model, IfcWall ifcWall, ThTCHWall wall, ThTCHDoor door, Point3d floor_origin)
        {
            var ifcDoor = ThTGL2IFC2x3Factory.CreateDoor(model, door, floor_origin);
            var ifcHole = ThTGL2IFC2x3Factory.CreateHole(model, wall, door, floor_origin);
            ThTGL2IFC2x3Factory.BuildRelationship(model, ifcWall, ifcDoor, ifcHole);
            return ifcDoor;
        }

        static public IfcWindow SetupWindow(IfcStore model, IfcWall ifcWall, ThTCHWall wall, ThTCHWindow window, Point3d floor_origin)
        {
            var ifcWindow = ThTGL2IFC2x3Factory.CreateWindow(model, window, floor_origin);
            var ifcHole = ThTGL2IFC2x3Factory.CreateHole(model, wall, window, floor_origin);
            ThTGL2IFC2x3Factory.BuildRelationship(model, ifcWall, ifcWindow, ifcHole);
            return ifcWindow;
        }

        static public IfcOpeningElement SetupHole(IfcStore model, IfcWall ifcWall, ThTCHOpening hole, Point3d floor_origin)
        {
            var ifcHole = ThTGL2IFC2x3Factory.CreateHole(model, hole, floor_origin);
            ThTGL2IFC2x3Factory.BuildRelationship(model, ifcWall, ifcHole);
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
