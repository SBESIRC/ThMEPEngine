﻿using System.Linq;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Xbim.IO;
using Xbim.Ifc;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.Interfaces;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.ProfileResource;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.RepresentationResource;
using Xbim.Ifc2x3.GeometricConstraintResource;
using ThMEPTCH.Model;

namespace ThMEPIFC.Ifc2x3
{
    public partial class ThTGL2IFC2x3Factory
    {
        static int epsilon = 0;

        static public IfcStore CreateAndInitModel(string projectName,string projectId = "")
        {
            var model = CreateModel();
            using (var txn = model.BeginTransaction("Initialize Model"))
            {
                //there should always be one project in the model
                var project = model.Instances.New<IfcProject>(p => p.Name = projectName);
                if (!string.IsNullOrEmpty(projectId))
                    project.GlobalId = new Xbim.Ifc2x3.UtilityResource.IfcGloballyUniqueId(projectId);
                //set the units to SI (mm and metres)
                project.Initialize(ProjectUnits.SIUnitsUK);
                //set GeometricRepresentationContext
                project.RepresentationContexts.Add(CreateGeometricRepresentationContext(model));
                //now commit the changes, else they will be rolled back at the end of the scope of the using statement
                txn.Commit();
            }
            return model;
        }

        private static IfcStore CreateModel()
        {
            return IfcStore.Create(XbimSchemaVersion.Ifc2X3, XbimStoreType.InMemoryModel);
        }

        private static IfcGeometricRepresentationContext CreateGeometricRepresentationContext(IfcStore model)
        {
            return model.Instances.New<IfcGeometricRepresentationContext>(c =>
            {
                c.Precision = 1E-5;
                c.CoordinateSpaceDimension = new IfcDimensionCount(3);
                c.WorldCoordinateSystem = model.ToIfcAxis2Placement3D(Point3d.Origin);
            });
        }

        public static IfcSite CreateSite(IfcStore model, ThTCHSite site)
        {
            using (var txn = model.BeginTransaction("Initialise Site"))
            {
                var ret = model.Instances.New<IfcSite>(s =>
                {
                    s.ObjectPlacement = model.ToIfcLocalPlacement(WCS());
                });
                if (!string.IsNullOrEmpty(site.Uuid))
                    ret.GlobalId = new Xbim.Ifc2x3.UtilityResource.IfcGloballyUniqueId(site.Uuid);
                //get the project there should only be one and it should exist
                var project = model.Instances.OfType<IfcProject>().FirstOrDefault();
                project.AddSite(ret);
                txn.Commit();
                return ret;
            }
        }

        private static CoordinateSystem3d WCS()
        {
            return Matrix3d.Identity.CoordinateSystem3d;
        }

        public static IfcBuilding CreateBuilding(IfcStore model, IfcSite site, ThTCHBuilding building)
        {
            using (var txn = model.BeginTransaction("Initialise Building"))
            {
                var ret = model.Instances.New<IfcBuilding>(b =>
                {
                    b.Name = building.BuildingName;
                    b.CompositionType = IfcElementCompositionEnum.ELEMENT;
                    b.ObjectPlacement = model.ToIfcLocalPlacement(WCS(), site.ObjectPlacement);
                });
                if (!string.IsNullOrEmpty(building.Uuid))
                    ret.GlobalId = new Xbim.Ifc2x3.UtilityResource.IfcGloballyUniqueId(building.Uuid);
                model.Instances.New<IfcRelDefinesByProperties>(rel =>
                {
                    rel.Name = "THifc properties";
                    rel.RelatedObjects.Add(ret);
                    rel.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pset =>
                    {
                        pset.Name = "Basic set of THifc properties";
                        foreach (var item in building.Properties)
                        {
                            pset.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                            {
                                p.Name = item.Key;
                                p.NominalValue = new IfcText(item.Value.ToString());
                            }));
                        }
                    });
                });
                site.AddBuilding(ret);
                txn.Commit();
                return ret;
            }
        }

        static public IfcBuildingStorey CreateStorey(IfcStore model, IfcBuilding building, ThTCHBuildingStorey storey)
        {
            using (var txn = model.BeginTransaction("Create Storey"))
            {
                var ret = model.Instances.New<IfcBuildingStorey>(s =>
                {
                    s.Name = storey.Number;
                    s.ObjectPlacement = model.ToIfcLocalPlacement(WCS(), building.ObjectPlacement);
                });
                if (!string.IsNullOrEmpty(storey.Uuid))
                    ret.GlobalId = new Xbim.Ifc2x3.UtilityResource.IfcGloballyUniqueId(storey.Uuid);
                // setup aggregation relationship
                var ifcRel = model.Instances.New<IfcRelAggregates>();
                ifcRel.RelatingObject = building;
                ifcRel.RelatedObjects.Add(ret);

                // add properties
                model.Instances.New<IfcRelDefinesByProperties>(rel =>
                {
                    rel.Name = "THifc properties";
                    rel.RelatedObjects.Add(ret);
                    rel.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pset =>
                    {
                        pset.Name = "Basic set of THifc properties";
                        foreach (var item in storey.Properties)
                        {
                            pset.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                            {
                                p.Name = item.Key;
                                p.NominalValue = new IfcText(item.Value.ToString());
                            }));
                        }
                    });
                });
                txn.Commit();
                return ret;
            }
        }

        private static IfcProductDefinitionShape CreateProductDefinitionShape(IfcStore model, IfcExtrudedAreaSolid solid)
        {
            //Create a Definition shape to hold the geometry
            var shape = model.Instances.New<IfcShapeRepresentation>();
            var modelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
            shape.ContextOfItems = modelContext;
            shape.RepresentationType = "SweptSolid";
            shape.RepresentationIdentifier = "Body";
            shape.Items.Add(solid);

            //Create a Product Definition and add the model geometry to the wall
            var rep = model.Instances.New<IfcProductDefinitionShape>();
            rep.Representations.Add(shape);
            return rep;
        }

        static public IfcWall CreateWall(IfcStore model, ThTCHWall wall, Point3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Wall"))
            {
                var ret = model.Instances.New<IfcWall>();
                ret.Name = "A Standard rectangular wall";
                if (!string.IsNullOrEmpty(wall.Uuid))
                    ret.GlobalId = new Xbim.Ifc2x3.UtilityResource.IfcGloballyUniqueId(wall.Uuid);
                //model as a swept area solid
                IfcProfileDef profile = null;
                var moveVector = floor_origin.GetAsVector();
                if (wall.Outline is Polyline pline)
                {
                    profile = model.ToIfcArbitraryClosedProfileDef(pline);
                    moveVector = moveVector + wall.ExtrudedDirection.MultiplyBy(pline.Elevation);
                }
                else
                {
                    profile = model.ToIfcRectangleProfileDef(wall.Length, wall.Width);
                }
                var body = model.ToIfcExtrudedAreaSolid(profile, wall.ExtrudedDirection, wall.Height);

                //Create a Definition shape to hold the geometry
                var shape = model.Instances.New<IfcShapeRepresentation>();
                var modelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                shape.ContextOfItems = modelContext;
                shape.RepresentationType = "SweptSolid";
                shape.RepresentationIdentifier = "Body";
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                var rep = model.Instances.New<IfcProductDefinitionShape>();
                rep.Representations.Add(shape);
                ret.Representation = rep;

                //now place the wall into the model
                var lp = model.Instances.New<IfcLocalPlacement>();
                
                var ax3D = model.Instances.New<IfcAxis2Placement3D>(p =>
                {
                    p.Axis = model.ToIfcDirection(Vector3d.ZAxis);
                    p.RefDirection = model.ToIfcDirection(wall.XVector);
                    p.Location = model.ToIfcCartesianPoint(wall.Origin + moveVector);
                });
                lp.RelativePlacement = ax3D;
                ret.ObjectPlacement = lp;

                // add properties
                model.Instances.New<IfcRelDefinesByProperties>(rel =>
                {
                    rel.Name = "THifc properties";
                    rel.RelatedObjects.Add(ret);
                    rel.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pset =>
                    {
                        pset.Name = "Basic set of THifc properties";
                        //pset.HasProperties.AddRange(new[] {
                        //    model.Instances.New<IfcPropertySingleValue>(p=>{
                        //        p.Name = "OpenDirection";
                        //        p.NominalValue=new IfcText(window.OpenDirection);
                        //    }),
                        //     model.Instances.New<IfcPropertySingleValue>(p=>{
                        //        p.Name = "Test property";
                        //        p.NominalValue=new IfcText("nothing");
                        //    })
                        //});
                        foreach (var item in wall.Properties)
                        {
                            pset.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                            {
                                p.Name = item.Key;
                                p.NominalValue = new IfcText(item.Value.ToString());
                            }));
                        }
                    });
                });
                /*
                // Where Clause: The IfcWallStandard relies on the provision of an IfcMaterialLayerSetUsage 
                var ifcMaterialLayerSetUsage = model.Instances.New<IfcMaterialLayerSetUsage>();
                var ifcMaterialLayerSet = model.Instances.New<IfcMaterialLayerSet>();
                var ifcMaterialLayer = model.Instances.New<IfcMaterialLayer>();
                ifcMaterialLayer.LayerThickness = 10;
                ifcMaterialLayerSet.MaterialLayers.Add(ifcMaterialLayer);
                ifcMaterialLayerSetUsage.ForLayerSet = ifcMaterialLayerSet;
                ifcMaterialLayerSetUsage.LayerSetDirection = IfcLayerSetDirectionEnum.AXIS2;
                ifcMaterialLayerSetUsage.DirectionSense = IfcDirectionSenseEnum.NEGATIVE;
                ifcMaterialLayerSetUsage.OffsetFromReferenceLine = 150;

                // Add material to wall
                var material = model.Instances.New<IfcMaterial>();
                material.Name = "some material";
                var ifcRelAssociatesMaterial = model.Instances.New<IfcRelAssociatesMaterial>();
                ifcRelAssociatesMaterial.RelatingMaterial = material;
                ifcRelAssociatesMaterial.RelatedObjects.Add(ret);

                ifcRelAssociatesMaterial.RelatingMaterial = ifcMaterialLayerSetUsage;

                // IfcPresentationLayerAssignment is required for CAD presentation in IfcWall or IfcWallStandardCase
                var ifcPresentationLayerAssignment = model.Instances.New<IfcPresentationLayerAssignment>();
                ifcPresentationLayerAssignment.Name = "some ifcPresentationLayerAssignment";
                ifcPresentationLayerAssignment.AssignedItems.Add(shape);


                // linear segment as IfcPolyline with two points is required for IfcWall
                var ifcPolyline = model.Instances.New<IfcPolyline>();
                var startPoint = model.Instances.New<IfcCartesianPoint>();
                startPoint.SetXY(0, 0);
                var endPoint = model.Instances.New<IfcCartesianPoint>();
                endPoint.SetXY(4000, 0);
                ifcPolyline.Points.Add(startPoint);
                ifcPolyline.Points.Add(endPoint);

                var shape2D = model.Instances.New<IfcShapeRepresentation>();
                shape2D.ContextOfItems = modelContext;
                shape2D.RepresentationIdentifier = "Axis";
                shape2D.RepresentationType = "Curve2D";
                shape2D.Items.Add(ifcPolyline);
                rep.Representations.Add(shape2D);*/
                txn.Commit();
                return ret;
            }
        }

        static public IfcColumn CreateColumn(IfcStore model, ThTCHColumn column, Point3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Column"))
            {
                var ret = model.Instances.New<IfcColumn>();
                ret.Name = "A Standard rectangular column";
                if (!string.IsNullOrEmpty(column.Uuid))
                    ret.GlobalId = new Xbim.Ifc2x3.UtilityResource.IfcGloballyUniqueId(column.Uuid);
                //model as a swept area solid
                IfcProfileDef profile = null;
                var moveVector = floor_origin.GetAsVector();
                if (column.Outline is Polyline pline)
                {
                    profile = model.ToIfcArbitraryClosedProfileDef(pline);
                    moveVector = moveVector + column.ExtrudedDirection.MultiplyBy(pline.Elevation);
                }
                else
                {
                    profile = model.ToIfcRectangleProfileDef(column.Length, column.Width);
                }
                var body = model.ToIfcExtrudedAreaSolid(profile, column.ExtrudedDirection, column.Height);

                //Create a Definition shape to hold the geometry
                var shape = model.Instances.New<IfcShapeRepresentation>();
                var modelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                shape.ContextOfItems = modelContext;
                shape.RepresentationType = "SweptSolid";
                shape.RepresentationIdentifier = "Body";
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                var rep = model.Instances.New<IfcProductDefinitionShape>();
                rep.Representations.Add(shape);
                ret.Representation = rep;

                //now place the wall into the model
                var lp = model.Instances.New<IfcLocalPlacement>();

                var ax3D = model.Instances.New<IfcAxis2Placement3D>(p =>
                {
                    p.Axis = model.ToIfcDirection(Vector3d.ZAxis);
                    p.RefDirection = model.ToIfcDirection(column.XVector);
                    p.Location = model.ToIfcCartesianPoint(column.Origin + moveVector);
                });
                lp.RelativePlacement = ax3D;
                ret.ObjectPlacement = lp;

                // add properties
                model.Instances.New<IfcRelDefinesByProperties>(rel =>
                {
                    rel.Name = "THifc properties";
                    rel.RelatedObjects.Add(ret);
                    rel.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pset =>
                    {
                        pset.Name = "Basic set of THifc properties";
                        //pset.HasProperties.AddRange(new[] {
                        //    model.Instances.New<IfcPropertySingleValue>(p=>{
                        //        p.Name = "OpenDirection";
                        //        p.NominalValue=new IfcText(window.OpenDirection);
                        //    }),
                        //     model.Instances.New<IfcPropertySingleValue>(p=>{
                        //        p.Name = "Test property";
                        //        p.NominalValue=new IfcText("nothing");
                        //    })
                        //});
                        foreach (var item in column.Properties)
                        {
                            pset.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                            {
                                p.Name = item.Key;
                                p.NominalValue = new IfcText(item.Value.ToString());
                            }));
                        }
                    });
                });
                /*
                // Where Clause: The IfcWallStandard relies on the provision of an IfcMaterialLayerSetUsage 
                var ifcMaterialLayerSetUsage = model.Instances.New<IfcMaterialLayerSetUsage>();
                var ifcMaterialLayerSet = model.Instances.New<IfcMaterialLayerSet>();
                var ifcMaterialLayer = model.Instances.New<IfcMaterialLayer>();
                ifcMaterialLayer.LayerThickness = 10;
                ifcMaterialLayerSet.MaterialLayers.Add(ifcMaterialLayer);
                ifcMaterialLayerSetUsage.ForLayerSet = ifcMaterialLayerSet;
                ifcMaterialLayerSetUsage.LayerSetDirection = IfcLayerSetDirectionEnum.AXIS2;
                ifcMaterialLayerSetUsage.DirectionSense = IfcDirectionSenseEnum.NEGATIVE;
                ifcMaterialLayerSetUsage.OffsetFromReferenceLine = 150;

                // Add material to wall
                var material = model.Instances.New<IfcMaterial>();
                material.Name = "some material";
                var ifcRelAssociatesMaterial = model.Instances.New<IfcRelAssociatesMaterial>();
                ifcRelAssociatesMaterial.RelatingMaterial = material;
                ifcRelAssociatesMaterial.RelatedObjects.Add(ret);

                ifcRelAssociatesMaterial.RelatingMaterial = ifcMaterialLayerSetUsage;

                // IfcPresentationLayerAssignment is required for CAD presentation in IfcWall or IfcWallStandardCase
                var ifcPresentationLayerAssignment = model.Instances.New<IfcPresentationLayerAssignment>();
                ifcPresentationLayerAssignment.Name = "some ifcPresentationLayerAssignment";
                ifcPresentationLayerAssignment.AssignedItems.Add(shape);


                // linear segment as IfcPolyline with two points is required for IfcWall
                var ifcPolyline = model.Instances.New<IfcPolyline>();
                var startPoint = model.Instances.New<IfcCartesianPoint>();
                startPoint.SetXY(0, 0);
                var endPoint = model.Instances.New<IfcCartesianPoint>();
                endPoint.SetXY(4000, 0);
                ifcPolyline.Points.Add(startPoint);
                ifcPolyline.Points.Add(endPoint);

                var shape2D = model.Instances.New<IfcShapeRepresentation>();
                shape2D.ContextOfItems = modelContext;
                shape2D.RepresentationIdentifier = "Axis";
                shape2D.RepresentationType = "Curve2D";
                shape2D.Items.Add(ifcPolyline);
                rep.Representations.Add(shape2D);*/
                txn.Commit();
                return ret;
            }
        }

        static public IfcBeam CreateBeam(IfcStore model, ThTCHBeam beam, Point3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Beam"))
            {
                var ret = model.Instances.New<IfcBeam>();
                ret.Name = "A Standard rectangular beam";
                if (!string.IsNullOrEmpty(beam.Uuid))
                    ret.GlobalId = new Xbim.Ifc2x3.UtilityResource.IfcGloballyUniqueId(beam.Uuid);
                //model as a swept area solid
                IfcProfileDef profile = null;
                var moveVector = floor_origin.GetAsVector();
                if (beam.Outline is Polyline pline)
                {
                    profile = model.ToIfcArbitraryClosedProfileDef(pline);
                    moveVector = moveVector + beam.ExtrudedDirection.MultiplyBy(pline.Elevation);
                }
                else
                {
                    profile = model.ToIfcRectangleProfileDef(beam.Length, beam.Width);
                }
                var body = model.ToIfcExtrudedAreaSolid(profile, beam.ExtrudedDirection, beam.Height);

                //Create a Definition shape to hold the geometry
                var shape = model.Instances.New<IfcShapeRepresentation>();
                var modelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                shape.ContextOfItems = modelContext;
                shape.RepresentationType = "SweptSolid";
                shape.RepresentationIdentifier = "Body";
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                var rep = model.Instances.New<IfcProductDefinitionShape>();
                rep.Representations.Add(shape);
                ret.Representation = rep;

                //now place the wall into the model
                var lp = model.Instances.New<IfcLocalPlacement>();

                var ax3D = model.Instances.New<IfcAxis2Placement3D>(p =>
                {
                    p.Axis = model.ToIfcDirection(Vector3d.ZAxis);
                    p.RefDirection = model.ToIfcDirection(beam.XVector);
                    p.Location = model.ToIfcCartesianPoint(beam.Origin + moveVector);
                });
                lp.RelativePlacement = ax3D;
                ret.ObjectPlacement = lp;

                // add properties
                model.Instances.New<IfcRelDefinesByProperties>(rel =>
                {
                    rel.Name = "THifc properties";
                    rel.RelatedObjects.Add(ret);
                    rel.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pset =>
                    {
                        pset.Name = "Basic set of THifc properties";
                        //pset.HasProperties.AddRange(new[] {
                        //    model.Instances.New<IfcPropertySingleValue>(p=>{
                        //        p.Name = "OpenDirection";
                        //        p.NominalValue=new IfcText(window.OpenDirection);
                        //    }),
                        //     model.Instances.New<IfcPropertySingleValue>(p=>{
                        //        p.Name = "Test property";
                        //        p.NominalValue=new IfcText("nothing");
                        //    })
                        //});
                        foreach (var item in beam.Properties)
                        {
                            pset.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                            {
                                p.Name = item.Key;
                                p.NominalValue = new IfcText(item.Value.ToString());
                            }));
                        }
                    });
                });
                /*
                // Where Clause: The IfcWallStandard relies on the provision of an IfcMaterialLayerSetUsage 
                var ifcMaterialLayerSetUsage = model.Instances.New<IfcMaterialLayerSetUsage>();
                var ifcMaterialLayerSet = model.Instances.New<IfcMaterialLayerSet>();
                var ifcMaterialLayer = model.Instances.New<IfcMaterialLayer>();
                ifcMaterialLayer.LayerThickness = 10;
                ifcMaterialLayerSet.MaterialLayers.Add(ifcMaterialLayer);
                ifcMaterialLayerSetUsage.ForLayerSet = ifcMaterialLayerSet;
                ifcMaterialLayerSetUsage.LayerSetDirection = IfcLayerSetDirectionEnum.AXIS2;
                ifcMaterialLayerSetUsage.DirectionSense = IfcDirectionSenseEnum.NEGATIVE;
                ifcMaterialLayerSetUsage.OffsetFromReferenceLine = 150;

                // Add material to wall
                var material = model.Instances.New<IfcMaterial>();
                material.Name = "some material";
                var ifcRelAssociatesMaterial = model.Instances.New<IfcRelAssociatesMaterial>();
                ifcRelAssociatesMaterial.RelatingMaterial = material;
                ifcRelAssociatesMaterial.RelatedObjects.Add(ret);

                ifcRelAssociatesMaterial.RelatingMaterial = ifcMaterialLayerSetUsage;

                // IfcPresentationLayerAssignment is required for CAD presentation in IfcWall or IfcWallStandardCase
                var ifcPresentationLayerAssignment = model.Instances.New<IfcPresentationLayerAssignment>();
                ifcPresentationLayerAssignment.Name = "some ifcPresentationLayerAssignment";
                ifcPresentationLayerAssignment.AssignedItems.Add(shape);


                // linear segment as IfcPolyline with two points is required for IfcWall
                var ifcPolyline = model.Instances.New<IfcPolyline>();
                var startPoint = model.Instances.New<IfcCartesianPoint>();
                startPoint.SetXY(0, 0);
                var endPoint = model.Instances.New<IfcCartesianPoint>();
                endPoint.SetXY(4000, 0);
                ifcPolyline.Points.Add(startPoint);
                ifcPolyline.Points.Add(endPoint);

                var shape2D = model.Instances.New<IfcShapeRepresentation>();
                shape2D.ContextOfItems = modelContext;
                shape2D.RepresentationIdentifier = "Axis";
                shape2D.RepresentationType = "Curve2D";
                shape2D.Items.Add(ifcPolyline);
                rep.Representations.Add(shape2D);*/
                txn.Commit();
                return ret;
            }
        }

        static public void relContainWalls2Storey(IfcStore model, List<IfcWall> walls, IfcBuildingStorey Storey)
        {
            using (var txn = model.BeginTransaction("relContainWalls2Storey"))
            {
                //for ifc2x3
                var relContainedIn = model.Instances.New<IfcRelContainedInSpatialStructure>();
                Storey.ContainsElements.Append<IIfcRelContainedInSpatialStructure>(relContainedIn);
                foreach (var wall in walls)
                {
                    relContainedIn.RelatedElements.Add(wall);
                    //Storey.AddElement(wall);
                }
                relContainedIn.RelatingStructure = Storey;

                txn.Commit();
            }
        }

        static public void relContainColumns2Storey(IfcStore model, List<IfcColumn> columns, IfcBuildingStorey Storey)
        {
            using (var txn = model.BeginTransaction("relContainColumns2Storey"))
            {
                //for ifc2x3
                var relContainedIn = model.Instances.New<IfcRelContainedInSpatialStructure>();
                Storey.ContainsElements.Append<IIfcRelContainedInSpatialStructure>(relContainedIn);
                foreach (var wall in columns)
                {
                    relContainedIn.RelatedElements.Add(wall);
                    //Storey.AddElement(wall);
                }
                relContainedIn.RelatingStructure = Storey;

                txn.Commit();
            }
        }

        static public void relContainBeams2Storey(IfcStore model, List<IfcBeam> beams, IfcBuildingStorey Storey)
        {
            using (var txn = model.BeginTransaction("relContainColumns2Storey"))
            {
                //for ifc2x3
                var relContainedIn = model.Instances.New<IfcRelContainedInSpatialStructure>();
                Storey.ContainsElements.Append<IIfcRelContainedInSpatialStructure>(relContainedIn);
                foreach (var wall in beams)
                {
                    relContainedIn.RelatedElements.Add(wall);
                    //Storey.AddElement(wall);
                }
                relContainedIn.RelatingStructure = Storey;

                txn.Commit();
            }
        }
        static public IfcDoor CreateDoor(IfcStore model, ThTCHDoor door, IfcElement openedElement, ThTCHWall thwall, Point3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Door"))
            {
                var ret = model.Instances.New<IfcDoor>();
                ret.Name = "door";
                if (!string.IsNullOrEmpty(door.Uuid))
                    ret.GlobalId = new Xbim.Ifc2x3.UtilityResource.IfcGloballyUniqueId(door.Uuid);
                //model as a swept area solid
                var profile = model.ToIfcRectangleProfileDef(door.Length, door.Width - epsilon);
                var body = model.ToIfcExtrudedAreaSolid(profile, door.ExtrudedDirection, door.Height);

                //Create a Definition shape to hold the geometry
                var shape = model.Instances.New<IfcShapeRepresentation>();
                var modelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                shape.ContextOfItems = modelContext;
                shape.RepresentationType = "SweptSolid";
                shape.RepresentationIdentifier = "Body";
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                var rep = model.Instances.New<IfcProductDefinitionShape>();
                rep.Representations.Add(shape);
                ret.Representation = rep;

                //now place the wall into the model
                var lp = model.Instances.New<IfcLocalPlacement>();
                var ax3D = model.Instances.New<IfcAxis2Placement3D>(p =>
                {
                    p.Axis = model.ToIfcDirection(Vector3d.ZAxis);
                    p.RefDirection = model.ToIfcDirection(door.XVector);
                    p.Location = model.ToIfcCartesianPoint(door.Origin + floor_origin.GetAsVector());
                });
                lp.RelativePlacement = ax3D;
                ret.ObjectPlacement = lp;

                // add properties
                model.Instances.New<IfcRelDefinesByProperties>(rel =>
                {
                    rel.Name = "THifc properties";
                    rel.RelatedObjects.Add(ret);
                    rel.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pset =>
                    {
                        pset.Name = "Basic set of THifc properties";
                        //pset.HasProperties.AddRange(new[] {
                        //    model.Instances.New<IfcPropertySingleValue>(p=>{
                        //        p.Name = "OpenDirection";
                        //        p.NominalValue=new IfcText(window.OpenDirection);
                        //    }),
                        //     model.Instances.New<IfcPropertySingleValue>(p=>{
                        //        p.Name = "Test property";
                        //        p.NominalValue=new IfcText("nothing");
                        //    })
                        //});
                        foreach (var item in door.Properties)
                        {
                            pset.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                            {
                                p.Name = item.Key;
                                p.NominalValue = new IfcText(item.Value.ToString());
                            }));
                        }
                    });
                });

                //create opening element
                var hole = model.Instances.New<IfcOpeningElement>();
                hole.Name = "hole";
                if (!string.IsNullOrEmpty(door.Uuid))
                    hole.GlobalId = new Xbim.Ifc2x3.UtilityResource.IfcGloballyUniqueId(door.Uuid+"hole");
                //todo: describe hole's geometry

                var hole_rectProf = model.Instances.New<IfcRectangleProfileDef>(p =>
                {
                    p.XDim = door.Length;
                    p.YDim = thwall.Width;
                    p.ProfileType = IfcProfileTypeEnum.AREA;
                    p.Position = model.ToIfcAxis2Placement2D(default);
                });

                var hole_body = model.Instances.New<IfcExtrudedAreaSolid>(s =>
                {
                    s.Depth = door.Height;
                    s.SweptArea = hole_rectProf;
                    s.ExtrudedDirection = model.ToIfcDirection(door.ExtrudedDirection);
                });
                hole_body.Position = model.ToIfcAxis2Placement3D(Point3d.Origin);

                var hole_shape = model.Instances.New<IfcShapeRepresentation>();
                var hole_modelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                hole_shape.ContextOfItems = modelContext;
                hole_shape.RepresentationType = "SweptSolid";
                hole_shape.RepresentationIdentifier = "Body";
                hole_shape.Items.Add(hole_body);

                var hole_rep = model.Instances.New<IfcProductDefinitionShape>();
                hole_rep.Representations.Add(hole_shape);
                hole.Representation = hole_rep;

                hole.ObjectPlacement = lp;

                //create relVoidsElement
                var relVoidsElement = model.Instances.New<IfcRelVoidsElement>();
                relVoidsElement.RelatedOpeningElement = hole;
                relVoidsElement.RelatingBuildingElement = openedElement;

                //create relFillsElement
                var relFillsElement = model.Instances.New<IfcRelFillsElement>();
                relFillsElement.RelatingOpeningElement = hole;
                relFillsElement.RelatedBuildingElement = ret;

                txn.Commit();
                return ret;
            }
        }

        static public void relContainDoors2Storey(IfcStore model, List<IfcDoor> doors, IfcBuildingStorey Storey)
        {
            using (var txn = model.BeginTransaction("relContainDoors2Storey"))
            {
                //for ifc2x3
                var relContainedIn = model.Instances.New<IfcRelContainedInSpatialStructure>();
                Storey.ContainsElements.Append<IIfcRelContainedInSpatialStructure>(relContainedIn);
                foreach (var door in doors)
                {
                    relContainedIn.RelatedElements.Add(door);
                    //Storey.AddElement(door);
                }
                relContainedIn.RelatingStructure = Storey;
                txn.Commit();
            }
        }
        static public IfcWindow CreateWindow(IfcStore model, ThTCHWindow window, IfcElement openedElement, ThTCHWall thwall, Point3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Window"))
            {
                var ret = model.Instances.New<IfcWindow>();
                ret.Name = "window";
                if (!string.IsNullOrEmpty(window.Uuid))
                    ret.GlobalId = new Xbim.Ifc2x3.UtilityResource.IfcGloballyUniqueId(window.Uuid);
                //represent wall as a rectangular profile
                var rectProf = model.Instances.New<IfcRectangleProfileDef>(p =>
                {
                    p.XDim = window.Length;
                    p.YDim = window.Width - epsilon;
                    p.ProfileType = IfcProfileTypeEnum.AREA;
                    p.Position = model.ToIfcAxis2Placement2D(default);
                });

                //model as a swept area solid
                var body = model.Instances.New<IfcExtrudedAreaSolid>(s =>
                {
                    s.Depth = window.Height;
                    s.SweptArea = rectProf;
                    s.ExtrudedDirection = model.ToIfcDirection(window.ExtrudedDirection);
                });

                //parameters to insert the geometry in the model
                body.Position = model.ToIfcAxis2Placement3D(Point3d.Origin);

                //Create a Definition shape to hold the geometry
                var shape = model.Instances.New<IfcShapeRepresentation>();
                var modelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                shape.ContextOfItems = modelContext;
                shape.RepresentationType = "SweptSolid";
                shape.RepresentationIdentifier = "Body";
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                var rep = model.Instances.New<IfcProductDefinitionShape>();
                rep.Representations.Add(shape);
                ret.Representation = rep;

                //now place the wall into the model
                var lp = model.Instances.New<IfcLocalPlacement>();
                var ax3D = model.Instances.New<IfcAxis2Placement3D>(p =>
                {
                    p.Axis = model.ToIfcDirection(Vector3d.ZAxis);
                    p.RefDirection = model.ToIfcDirection(window.XVector);
                    p.Location = model.ToIfcCartesianPoint(window.Origin + floor_origin.GetAsVector());
                });
                lp.RelativePlacement = ax3D;
                ret.ObjectPlacement = lp;

                // add properties
                model.Instances.New<IfcRelDefinesByProperties>(rel =>
                {
                    rel.Name = "THifc properties";
                    rel.RelatedObjects.Add(ret);
                    rel.RelatingPropertyDefinition = model.Instances.New<IfcPropertySet>(pset =>
                    {
                        pset.Name = "Basic set of THifc properties";
                        foreach (var item in window.Properties)
                        {
                            pset.HasProperties.Add(model.Instances.New<IfcPropertySingleValue>(p =>
                            {
                                p.Name = item.Key;
                                p.NominalValue = new IfcText(item.Value.ToString());
                            }));
                        }
                    });
                });

                //create opening element
                var hole = model.Instances.New<IfcOpeningElement>();
                hole.Name = "hole";
                if (!string.IsNullOrEmpty(window.Uuid))
                    hole.GlobalId = new Xbim.Ifc2x3.UtilityResource.IfcGloballyUniqueId(window.Uuid + "hole");
                //todo: describe hole's geometry

                var hole_rectProf = model.Instances.New<IfcRectangleProfileDef>(p =>
                {
                    p.XDim = window.Length;
                    p.YDim = thwall.Width;//todo;
                    p.ProfileType = IfcProfileTypeEnum.AREA;
                    p.Position = model.ToIfcAxis2Placement2D(default);
                });

                var hole_body = model.Instances.New<IfcExtrudedAreaSolid>(s =>
                {
                    s.Depth = window.Height;
                    s.SweptArea = hole_rectProf;
                    s.ExtrudedDirection = model.ToIfcDirection(window.ExtrudedDirection);
                });

                hole_body.Position = model.ToIfcAxis2Placement3D(Point3d.Origin);

                var hole_shape = model.Instances.New<IfcShapeRepresentation>();
                var hole_modelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                hole_shape.ContextOfItems = modelContext;
                hole_shape.RepresentationType = "SweptSolid";
                hole_shape.RepresentationIdentifier = "Body";
                hole_shape.Items.Add(hole_body);

                var hole_rep = model.Instances.New<IfcProductDefinitionShape>();
                hole_rep.Representations.Add(hole_shape);
                hole.Representation = hole_rep;

                hole.ObjectPlacement = lp;

                //create relVoidsElement
                var relVoidsElement = model.Instances.New<IfcRelVoidsElement>();
                relVoidsElement.RelatedOpeningElement = hole;
                relVoidsElement.RelatingBuildingElement = openedElement;

                //create relFillsElement
                var relFillsElement = model.Instances.New<IfcRelFillsElement>();
                relFillsElement.RelatingOpeningElement = hole;
                relFillsElement.RelatedBuildingElement = ret;

                txn.Commit();
                return ret;
            }
        }

        static public void relContainWindows2Storey(IfcStore model, List<IfcWindow> windows, IfcBuildingStorey Storey)
        {
            using (var txn = model.BeginTransaction("relContainWindows2Storey"))
            {
                //for ifc2x3
                var relContainedIn = model.Instances.New<IfcRelContainedInSpatialStructure>();
                Storey.ContainsElements.Append<IIfcRelContainedInSpatialStructure>(relContainedIn);
                foreach (var window in windows)
                {
                    relContainedIn.RelatedElements.Add(window);
                    //Storey.AddElement(window);
                }
                relContainedIn.RelatingStructure = Storey;
                txn.Commit();
            }
        }
        static public IfcOpeningElement CreateHole(IfcStore model, ThTCHOpening hole, IfcElement openedElement, ThTCHWall thwall, Point3d floor_origin)
        {
            using (var txn = model.BeginTransaction("Create Window"))
            {
                var ret = model.Instances.New<IfcOpeningElement>();
                ret.Name = "window";
                if (!string.IsNullOrEmpty(hole.Uuid))
                    ret.GlobalId = new Xbim.Ifc2x3.UtilityResource.IfcGloballyUniqueId(hole.Uuid);
                //represent wall as a rectangular profile
                var rectProf = model.Instances.New<IfcRectangleProfileDef>(p =>
                {
                    p.XDim = hole.Length;
                    p.YDim = hole.Width - epsilon;
                    p.ProfileType = IfcProfileTypeEnum.AREA;
                    p.Position = model.ToIfcAxis2Placement2D(default);
                });

                //model as a swept area solid
                var body = model.Instances.New<IfcExtrudedAreaSolid>(s =>
                {
                    s.Depth = hole.Height;
                    s.SweptArea = rectProf;
                    s.ExtrudedDirection = model.ToIfcDirection(hole.ExtrudedDirection);
                });

                //parameters to insert the geometry in the model
                body.Position = model.ToIfcAxis2Placement3D(Point3d.Origin);

                //Create a Definition shape to hold the geometry
                var shape = model.Instances.New<IfcShapeRepresentation>();
                var modelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                shape.ContextOfItems = modelContext;
                shape.RepresentationType = "SweptSolid";
                shape.RepresentationIdentifier = "Body";
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                var rep = model.Instances.New<IfcProductDefinitionShape>();
                rep.Representations.Add(shape);
                ret.Representation = rep;

                //now place the wall into the model
                var lp = model.Instances.New<IfcLocalPlacement>();
                var ax3D = model.Instances.New<IfcAxis2Placement3D>(p =>
                {
                    p.Axis = model.ToIfcDirection(Vector3d.ZAxis);
                    p.RefDirection = model.ToIfcDirection(hole.XVector);
                    p.Location = model.ToIfcCartesianPoint(hole.Origin + floor_origin.GetAsVector());
                });
                lp.RelativePlacement = ax3D;
                ret.ObjectPlacement = lp;

                //create relVoidsElement
                var relVoidsElement = model.Instances.New<IfcRelVoidsElement>();
                relVoidsElement.RelatedOpeningElement = ret;
                relVoidsElement.RelatingBuildingElement = openedElement;

                txn.Commit();
                return ret;
            }
        }

        static public void relContainSlabs2Storey(IfcStore model, List<IfcSlab> slabs, IfcBuildingStorey Storey)
        {
            using (var txn = model.BeginTransaction("relContainSlabs2Storey"))
            {
                var relContainedIn = model.Instances.New<IfcRelContainedInSpatialStructure>();
                Storey.ContainsElements.Append<IIfcRelContainedInSpatialStructure>(relContainedIn);
                foreach (var slab in slabs)
                {
                    relContainedIn.RelatedElements.Add(slab);
                }
                relContainedIn.RelatingStructure = Storey;
                txn.Commit();
            }
        }

        static public void relContainsRailings2Storey(IfcStore model, List<IfcRailing> railings, IfcBuildingStorey storey)
        {
            using (var txn = model.BeginTransaction("relContainsRailings2Storey"))
            {
                var relContainedIn = model.Instances.New<IfcRelContainedInSpatialStructure>();
                storey.ContainsElements.Append<IIfcRelContainedInSpatialStructure>(relContainedIn);
                foreach (var railing in railings)
                {
                    relContainedIn.RelatedElements.Add(railing);
                }
                relContainedIn.RelatingStructure = storey;
                txn.Commit();
            }
        }
    }
}
