using System;
using Xbim.IO;
using Xbim.Ifc;
using System.Collections.Generic;
using Xbim.Ifc4.SharedBldgElements;
using ThMEPTCH.Model;

namespace ThMEPIFC
{
    public class ThTGL2IFCBuilder
    {
        static public void BuildIfcModel(IfcStore Model, ThTCHProject project)
        {
            if (Model != null)
            {
                var site = ThTGL2IFCFactory.CreateSite(Model, project.Site);
                var building = ThTGL2IFCFactory.CreateBuilding(Model, site, project.Site.Building);
                foreach (var thtchstorey in project.Site.Building.Storeys)
                {
                    var walls = new List<IfcWall>();
                    var slabs = new List<IfcSlab>();
                    var doors = new List<IfcDoor>();
                    var windows = new List<IfcWindow>();
                    var floor_origin = thtchstorey.FloorOrigin;
                    var storey = ThTGL2IFCFactory.CreateStorey(Model, building, thtchstorey);
                    foreach (var thtchwall in thtchstorey.ThTCHWalls)
                    {
                        var wall = ThTGL2IFCFactory.CreateWall(Model, thtchwall, floor_origin);
                        walls.Add(wall);
                        foreach (var thtchdoor in thtchwall.Doors)
                        {
                            var door = ThTGL2IFCFactory.CreateDoor(Model, thtchdoor, wall, thtchwall, floor_origin);
                            doors.Add(door);
                        }
                        foreach (var thtchwindow in thtchwall.Windows)
                        {
                            var window = ThTGL2IFCFactory.CreateWindow(Model, thtchwindow, wall, thtchwall, floor_origin);
                            windows.Add(window);
                        }
                        foreach (var thtchhole in thtchwall.Openings)
                        {
                            var hole = ThTGL2IFCFactory.CreateHole(Model, thtchhole, wall, thtchwall, floor_origin);
                        }
                    }
                    foreach (var thtchslab in thtchstorey.ThTCHSlabs)
                    {
                        var slab = ThTGL2IFCFactory.CreateSlab(Model, thtchslab, floor_origin);
                        slabs.Add(slab);
                    }
                    ThTGL2IFCFactory.relContainWalls2Storey(Model, walls, storey);
                    ThTGL2IFCFactory.relContainDoors2Storey(Model, doors, storey);
                    ThTGL2IFCFactory.relContainWindows2Storey(Model, windows, storey);
                    ThTGL2IFCFactory.relContainSlabs2Storey(Model, slabs, storey);
                }
            }
        }
        static public void SaveIfcModel(IfcStore Model, string filepath)
        {
            if (Model != null)
            {
                using (var txn = Model.BeginTransaction("save ifc file"))
                {
                    try
                    {
                        Model.SaveAs(filepath, StorageType.Ifc);
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
