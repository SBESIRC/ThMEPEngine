using System;
using System.Linq;
using Xbim.Ifc;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.ProfileResource;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.TopologyResource;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.RepresentationResource;
using Xbim.Ifc2x3.GeometricConstraintResource;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.Ifc2x3.MeasureResource;

namespace ThMEPIFC.Ifc2x3
{
    public static class ThTGL2IFC2x3CloneExtension
    {
        public static IfcBuildingStorey CloneAndCreateNew(this IfcBuildingStorey storey, IfcStore model, IfcBuilding building, string storeyName)
        {
            using (var txn = model.BeginTransaction("Create Storey"))
            {
                var ret = model.Instances.New<IfcBuildingStorey>(s =>
                {
                    s.Name = storeyName;
                    s.ObjectPlacement = model.ToIfcLocalPlacement(WCS(), building.ObjectPlacement);
                });

                // setup aggregation relationship
                var ifcRel = model.Instances.New<IfcRelAggregates>();
                ifcRel.RelatingObject = building;
                ifcRel.RelatedObjects.Add(ret);

                // add properties
                var property = storey.Model.Instances.OfType<IfcRelDefinesByProperties>().FirstOrDefault(o => o.RelatedObjects.Contains(storey));
                if (!property.IsNull())
                {
                    var ifcRelDefinesByProperties = property.CloneAndCreateNew(model);
                    ifcRelDefinesByProperties.RelatedObjects.Add(ret);
                }
                txn.Commit();
                return ret;
            }
        }

        public static IfcWall CloneAndCreateNew(this IfcWall sourceWall, IfcStore model)
        {
            using (var txn = model.BeginTransaction("Create Wall"))
            {
                var ret = model.Instances.New<IfcWall>();
                ret.Name = sourceWall.Name.ToString();
                ret.Description = sourceWall.Description.ToString();

                //model as a swept area solid
                IfcRepresentationItem body = sourceWall.Representation.Representations.FirstOrDefault().Items[0].CloneAndCreateNew(model);

                //Create a Definition shape to hold the geometry
                var modelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                var shape = model.Instances.New<IfcShapeRepresentation>();
                shape.ContextOfItems = modelContext;
                shape.RepresentationType = "SurfaceModel";
                shape.RepresentationIdentifier = "Body";
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                var rep = model.Instances.New<IfcProductDefinitionShape>();
                rep.Representations.Add(shape);
                ret.Representation = rep;

                //now place the wall into the model
                var lp = model.Instances.New<IfcLocalPlacement>();
                var ax3D = (sourceWall.ObjectPlacement as IfcLocalPlacement).RelativePlacement.CloneAndCreateNew(model);
                lp.RelativePlacement = ax3D;
                ret.ObjectPlacement = lp;

                // add properties
                var property = sourceWall.Model.Instances.OfType<IfcRelDefinesByProperties>().FirstOrDefault(o => o.RelatedObjects.Contains(sourceWall));
                if (!property.IsNull())
                {
                    var ifcRelDefinesByProperties = property.CloneAndCreateNew(model);
                    ifcRelDefinesByProperties.RelatedObjects.Add(ret);
                }
                txn.Commit();
                return ret;
            }
        }

        public static IfcSlab CloneAndCreateNew(this IfcSlab sourceSlab, IfcStore model)
        {
            using (var txn = model.BeginTransaction("Create Slab"))
            {
                var ret = model.Instances.New<IfcSlab>();
                ret.Name = sourceSlab.Name.ToString();
                ret.Description = sourceSlab.Description.ToString();

                //model as a swept area solid
                IfcRepresentationItem body = sourceSlab.Representation.Representations.FirstOrDefault().Items[0].CloneAndCreateNew(model);

                //Create a Definition shape to hold the geometry
                var modelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                var shape = model.Instances.New<IfcShapeRepresentation>();
                shape.ContextOfItems = modelContext;
                shape.RepresentationType = "SurfaceModel";
                shape.RepresentationIdentifier = "Body";
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                var rep = model.Instances.New<IfcProductDefinitionShape>();
                rep.Representations.Add(shape);
                ret.Representation = rep;

                //now place the wall into the model
                var lp = model.Instances.New<IfcLocalPlacement>();
                var ax3D = (sourceSlab.ObjectPlacement as IfcLocalPlacement).RelativePlacement.CloneAndCreateNew(model);
                lp.RelativePlacement = ax3D;
                ret.ObjectPlacement = lp;

                // add properties
                var property = sourceSlab.Model.Instances.OfType<IfcRelDefinesByProperties>().FirstOrDefault(o => o.RelatedObjects.Contains(sourceSlab));
                if (!property.IsNull())
                {
                    var ifcRelDefinesByProperties = property.CloneAndCreateNew(model);
                    ifcRelDefinesByProperties.RelatedObjects.Add(ret);
                }
                txn.Commit();
                return ret;
            }
        }

        public static IfcBeam CloneAndCreateNew(this IfcBeam sourceBeam, IfcStore model)
        {
            using (var txn = model.BeginTransaction("Create Beam"))
            {
                var ret = model.Instances.New<IfcBeam>();
                ret.Name = sourceBeam.Name.ToString();
                ret.Description = sourceBeam.Description.ToString();

                //model as a swept area solid
                IfcRepresentationItem body = sourceBeam.Representation.Representations.FirstOrDefault().Items[0].CloneAndCreateNew(model);

                //Create a Definition shape to hold the geometry
                var modelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                var shape = model.Instances.New<IfcShapeRepresentation>();
                shape.ContextOfItems = modelContext;
                shape.RepresentationType = "SurfaceModel";
                shape.RepresentationIdentifier = "Body";
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                var rep = model.Instances.New<IfcProductDefinitionShape>();
                rep.Representations.Add(shape);
                ret.Representation = rep;

                //now place the wall into the model
                var lp = model.Instances.New<IfcLocalPlacement>();
                var ax3D = (sourceBeam.ObjectPlacement as IfcLocalPlacement).RelativePlacement.CloneAndCreateNew(model);
                lp.RelativePlacement = ax3D;
                ret.ObjectPlacement = lp;

                // add properties
                var property = sourceBeam.Model.Instances.OfType<IfcRelDefinesByProperties>().FirstOrDefault(o => o.RelatedObjects.Contains(sourceBeam));
                if (!property.IsNull())
                {
                    var ifcRelDefinesByProperties = property.CloneAndCreateNew(model);
                    ifcRelDefinesByProperties.RelatedObjects.Add(ret);
                }
                txn.Commit();
                return ret;
            }
        }

        public static IfcColumn CloneAndCreateNew(this IfcColumn sourceColumn, IfcStore model)
        {
            using (var txn = model.BeginTransaction("Create Column"))
            {
                var ret = model.Instances.New<IfcColumn>();
                ret.Name = sourceColumn.Name.ToString();
                ret.Description = sourceColumn.Description.ToString();

                //model as a swept area solid
                IfcRepresentationItem body = sourceColumn.Representation.Representations.FirstOrDefault().Items[0].CloneAndCreateNew(model);

                //Create a Definition shape to hold the geometry
                var modelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                var shape = model.Instances.New<IfcShapeRepresentation>();
                shape.ContextOfItems = modelContext;
                shape.RepresentationType = "SurfaceModel";
                shape.RepresentationIdentifier = "Body";
                shape.Items.Add(body);

                //Create a Product Definition and add the model geometry to the wall
                var rep = model.Instances.New<IfcProductDefinitionShape>();
                rep.Representations.Add(shape);
                ret.Representation = rep;

                //now place the wall into the model
                var lp = model.Instances.New<IfcLocalPlacement>();
                var ax3D = (sourceColumn.ObjectPlacement as IfcLocalPlacement).RelativePlacement.CloneAndCreateNew(model);
                lp.RelativePlacement = ax3D;
                ret.ObjectPlacement = lp;

                // add properties
                var property = sourceColumn.Model.Instances.OfType<IfcRelDefinesByProperties>().FirstOrDefault(o => o.RelatedObjects.Contains(sourceColumn));
                if (!property.IsNull())
                {
                    var ifcRelDefinesByProperties = property.CloneAndCreateNew(model);
                    ifcRelDefinesByProperties.RelatedObjects.Add(ret);
                }
                txn.Commit();
                return ret;
            }
        }

        #region private Clone Method
        private static IfcRepresentationItem CloneAndCreateNew(this IfcRepresentationItem body, IfcStore model)
        {
            IfcRepresentationItem result = body;
            if (body is IfcExtrudedAreaSolid solid)
            {
                result = model.Instances.New<IfcExtrudedAreaSolid>(s =>
                {
                    s.Depth = double.Parse(solid.Depth.Value.ToString());
                    s.SweptArea = solid.SweptArea.CloneAndCreateNew(model);
                    s.ExtrudedDirection = solid.ExtrudedDirection.CloneAndCreateNew(model);
                    s.Position = solid.Position.CloneAndCreateNew(model);
                });
            }
            else if (body is IfcFacetedBrep brep)
            {
                var NewBrep = model.Instances.New<IfcFacetedBrep>();
                NewBrep.Outer = brep.Outer.CloneAndCreateNew(model);
                result = NewBrep;
            }
            else if (body is IfcFaceBasedSurfaceModel surfaceModel)
            {
                var newSurface = model.Instances.New<IfcFaceBasedSurfaceModel>();
                var faceSet = surfaceModel.FbsmFaces.FirstOrDefault();
                newSurface.FbsmFaces.Add(faceSet.CloneAndCreateNew(model));
                result = newSurface;
            }
            else
            {
                throw new NotSupportedException();
            }
            return result;
        }

        private static IfcDirection CloneAndCreateNew(this IfcDirection direction, IfcStore model)
        {
            var result = model.Instances.New<IfcDirection>();
            result.SetXYZ(direction.X, direction.Y, direction.Z);
            return result;
        }

        private static IfcAxis2Placement CloneAndCreateNew(this IfcAxis2Placement axis2Placement, IfcStore model)
        {
            IfcAxis2Placement result;
            if (axis2Placement is IfcAxis2Placement3D axis2Placement3D)
            {
                result = axis2Placement3D.CloneAndCreateNew(model);
            }
            else
            {
                throw new NotSupportedException();
            }
            return result;
        }

        private static IfcAxis2Placement3D CloneAndCreateNew(this IfcAxis2Placement3D axis2Placement, IfcStore model)
        {
            var result = model.Instances.New<IfcAxis2Placement3D>(p =>
            {
                p.Axis = axis2Placement.Axis?.CloneAndCreateNew(model);
                p.RefDirection = axis2Placement.RefDirection?.CloneAndCreateNew(model);
                p.Location = axis2Placement.Location.CloneAndCreateNewXYZ(model);
            });
            return result;
        }

        private static IfcClosedShell CloneAndCreateNew(this IfcClosedShell closedShell, IfcStore model)
        {
            var result = model.Instances.New<IfcClosedShell>();
            result.CfsFaces.AddRange(closedShell.CfsFaces.Select(o => o.CloneAndCreateNew(model)));
            return result;
        }

        private static IfcFace CloneAndCreateNew(this IfcFace face, IfcStore model)
        {
            var result = model.Instances.New<IfcFace>();
            result.Bounds.Add(face.Bounds.FirstOrDefault().CloneAndCreateNew(model));
            return result;
        }

        private static IfcFaceBound CloneAndCreateNew(this IfcFaceBound faceBound, IfcStore model)
        {
            var result = model.Instances.New<IfcFaceBound>();
            result.Bound = faceBound.Bound.CloneAndCreateNew(model);
            return result;
        }

        private static IfcLoop CloneAndCreateNew(this IfcLoop loop, IfcStore model)
        {
            IfcLoop result;
            if (loop is IfcPolyLoop polyLoop)
            {
                var NewPolyLoop = model.Instances.New<IfcPolyLoop>();
                var polygon = polyLoop.Polygon;
                foreach (var pt in polygon)
                {
                    NewPolyLoop.Polygon.Add(pt.CloneAndCreateNewXYZ(model));
                }
                result = NewPolyLoop;
            }
            else
            {
                throw new NotSupportedException();
            }
            return result;
        }

        private static IfcCartesianPoint CloneAndCreateNewXYZ(this IfcCartesianPoint point, IfcStore model)
        {
            var Newpt = model.Instances.New<IfcCartesianPoint>();
            Newpt.SetXYZ(point.X, point.Y, point.Z);
            return Newpt;
        }

        private static IfcCartesianPoint CloneAndCreateNewXY(this IfcCartesianPoint point, IfcStore model)
        {
            var Newpt = model.Instances.New<IfcCartesianPoint>();
            Newpt.SetXY(point.X, point.Y);
            return Newpt;
        }

        private static IfcRelDefinesByProperties CloneAndCreateNew(this IfcRelDefinesByProperties property, IfcStore model)
        {
            var result = model.Instances.New<IfcRelDefinesByProperties>(rel =>
            {
                rel.Name = property.Name.ToString();
                rel.RelatingPropertyDefinition = property.RelatingPropertyDefinition.CloneAndCreateNew(model);
            });
            return result;
        }

        private static IfcPropertySetDefinition CloneAndCreateNew(this IfcPropertySetDefinition propertySetDefinition, IfcStore model)
        {
            IfcPropertySetDefinition result;
            if (propertySetDefinition is IfcPropertySet propertySet)
            {
                result = model.Instances.New<IfcPropertySet>(pset =>
                {
                    pset.Name = propertySet.Name.ToString();
                    foreach (var item in propertySet.HasProperties)
                    {
                        pset.HasProperties.Add(item.CloneAndCreateNew(model));
                    }
                });
            }
            else
            {
                throw new NotSupportedException();
            }
            return result;
        }

        private static IfcConnectedFaceSet CloneAndCreateNew(this IfcConnectedFaceSet faceSet, IfcStore model)
        {
            var result = model.Instances.New<IfcConnectedFaceSet>();
            foreach (var face in faceSet.CfsFaces)
            {
                result.CfsFaces.Add(face.CloneAndCreateNew(model));
            }
            return result;
        }

        private static IfcProfileDef CloneAndCreateNew(this IfcProfileDef profileDef, IfcStore model)
        {
            IfcProfileDef result;
            if (profileDef is IfcRectangleProfileDef rectangleProfileDef)
            {
                result = model.Instances.New<IfcRectangleProfileDef>(d =>
                {
                    d.XDim = double.Parse(rectangleProfileDef.XDim.Value.ToString());
                    d.YDim = double.Parse(rectangleProfileDef.YDim.Value.ToString());
                    d.ProfileType = IfcProfileTypeEnum.AREA;
                    //d.Position = rectangleProfileDef.Position.CloneAndCreateNew(model);
                });
            }
            else if (profileDef is IfcArbitraryClosedProfileDef arbitraryClosedProfileDef)
            {
                result = model.Instances.New<IfcArbitraryClosedProfileDef>(d =>
                {
                    d.ProfileType = IfcProfileTypeEnum.AREA;
                    d.OuterCurve = (arbitraryClosedProfileDef.OuterCurve as IfcCompositeCurve).CloneAndCreateNew(model);
                });
            }
            else
            {
                throw new NotSupportedException();
            }
            return result;
        }

        private static IfcCompositeCurve CloneAndCreateNew(this IfcCompositeCurve curve, IfcStore model)
        {
            var compositeCurve = model.Instances.New<IfcCompositeCurve>();
            foreach (var segment in curve.Segments)
            {
                compositeCurve.Segments.Add(segment.CloneAndCreateNew(model));
            }
            return compositeCurve;
        }

        private static IfcCompositeCurveSegment CloneAndCreateNew(this IfcCompositeCurveSegment segment, IfcStore model)
        {
            var result = model.Instances.New<IfcCompositeCurveSegment>(s =>
            {
                s.SameSense = true;
            });
            result.ParentCurve = segment.ParentCurve.CloneAndCreateNew(model);
            return result;
        }

        private static IfcCurve CloneAndCreateNew(this IfcCurve ifcCurve, IfcStore model)
        {
            IfcCurve result;
            if (ifcCurve is IfcPolyline ifcPolyline)
            {
                var poly = model.Instances.New<IfcPolyline>();
                foreach (var point in ifcPolyline.Points)
                {
                    poly.Points.Add(point.CloneAndCreateNewXY(model));
                }
                result = poly;
            }
            else
            {
                throw new NotSupportedException();
            }
            return result;
        }

        private static IfcProperty CloneAndCreateNew(this IfcProperty property, IfcStore model)
        {
            IfcProperty result;
            if (property is IfcPropertySingleValue propertySingleValue)
            {
                result = model.Instances.New<IfcPropertySingleValue>(p =>
                {
                    p.Name = propertySingleValue.Name.ToString();
                    p.NominalValue = propertySingleValue.NominalValue.CloneAndCreateNew();
                });
            }
            else
            {
                throw new NotSupportedException();
            }
            return result;
        }

        private static IfcValue CloneAndCreateNew(this IfcValue value)
        {
            if (value is IfcText ifcText)
            {
                return new IfcText(ifcText.ToString());
            }
            else if (value is IfcLengthMeasure ifcLengthMeasure)
            {
                return new IfcLengthMeasure(double.Parse(ifcLengthMeasure.Value.ToString()));
            }
            else
            {
                return new IfcText(value.Value.ToString());
            }
        }

        private static Autodesk.AutoCAD.Geometry.CoordinateSystem3d WCS()
        {
            return Autodesk.AutoCAD.Geometry.Matrix3d.Identity.CoordinateSystem3d;
        }
        #endregion
    }
}
