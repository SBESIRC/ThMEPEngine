﻿using System;
using Xbim.IO;
using Xbim.Ifc;
using System.Collections.Generic;
using Xbim.Ifc2x3.SharedBldgElements;
using ThMEPTCH.Model;

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
                    var floor_origin = thtchstorey.Origin;
                    var storey = ThTGL2IFC2x3Factory.CreateStorey(Model, building, thtchstorey);
                    foreach (var thtchwall in thtchstorey.Walls)
                    {
                        var wall = ThTGL2IFC2x3Factory.CreateWall(Model, thtchwall, floor_origin);
                        walls.Add(wall);
                        foreach (var thtchdoor in thtchwall.Doors)
                        {
                            var door = ThTGL2IFC2x3Factory.CreateDoor(Model, thtchdoor, wall, thtchwall, floor_origin);
                            doors.Add(door);
                        }
                        foreach (var thtchwindow in thtchwall.Windows)
                        {
                            var window = ThTGL2IFC2x3Factory.CreateWindow(Model, thtchwindow, wall, thtchwall, floor_origin);
                            windows.Add(window);
                        }
                        foreach (var thtchhole in thtchwall.Openings)
                        {
                            var hole = ThTGL2IFC2x3Factory.CreateHole(Model, thtchhole, wall, thtchwall, floor_origin);
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
                    ThTGL2IFC2x3Factory.relContainSlabs2Storey(Model, slabs, storey);
                    ThTGL2IFC2x3Factory.relContainWalls2Storey(Model, walls, storey);
                    ThTGL2IFC2x3Factory.relContainColumns2Storey(Model, columns, storey);
                    ThTGL2IFC2x3Factory.relContainBeams2Storey(Model, beams, storey);
                    ThTGL2IFC2x3Factory.relContainDoors2Storey(Model, doors, storey);
                    ThTGL2IFC2x3Factory.relContainWindows2Storey(Model, windows, storey);
                    ThTGL2IFC2x3Factory.relContainsRailings2Storey(Model, railings, storey);
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
