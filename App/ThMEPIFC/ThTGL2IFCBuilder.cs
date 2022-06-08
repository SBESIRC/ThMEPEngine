using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThMEPTCH.Model;
using Xbim.Ifc;
using Xbim.IO;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.SharedBldgElements;

namespace ThMEPIFC
{
    public class ThTGL2IFCBuilder
    {
        static public void BuildIfcModel(IfcStore Model, ThTCHProject project)
        {
            if (Model != null)
            {
                var Site = ThTGL2IFCFactory.CreateSite(Model, project.Site);
                var thtchbuilding = project.Site.Building;
                var Building = ThTGL2IFCFactory.CreateBuilding(Model, Site, thtchbuilding);
                List<IfcBuildingStorey> storeys = new List<IfcBuildingStorey>();
                List<IfcDoor> doors = new List<IfcDoor>();
                List<IfcWindow> windows = new List<IfcWindow>();
                List<IfcWallStandardCase> walls = new List<IfcWallStandardCase>();
                List<IfcSlab> slabs = new List<IfcSlab>();
                foreach (var thtchstorey in thtchbuilding.Storeys)
                {

                    var floor_origin = thtchstorey.FloorOrigin;
                    var Storey = ThTGL2IFCFactory.CreateStorey(Model, Building, thtchstorey);
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
                        foreach(var thtchhole in thtchwall.Openings)
                        {
                            var hole = ThTGL2IFCFactory.CreateHole(Model, thtchhole, wall, thtchwall, floor_origin);
                        }
                    }
                    foreach(var thtchslab in thtchstorey.ThTCHSlabs)
                    {
                        var slab = ThTGL2IFCFactory.CreateSlab(Model, thtchslab, floor_origin);
                        slabs.Add(slab);
                    }
                    ThTGL2IFCFactory.relContainWalls2Storey(Model, walls, Storey);
                    ThTGL2IFCFactory.relContainDoors2Storey(Model, doors, Storey);
                    ThTGL2IFCFactory.relContainWindows2Storey(Model, windows, Storey);
                    ThTGL2IFCFactory.relContainSlabs2Storey(Model, slabs, Storey);
                    walls.Clear();
                    doors.Clear();
                    windows.Clear();
                    slabs.Clear();
                }
            }
            else
            {
                Console.WriteLine("ifcstore to build is empty!");
                Console.ReadKey();
            }
        }
        static public void SaveIfcModel(IfcStore Model,string filepath)
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
            else
            {
                Console.WriteLine("ifcstore to save is empty!");
                Console.ReadKey();
            }
        }
    }
}
