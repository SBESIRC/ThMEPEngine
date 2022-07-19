using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.Common.Metadata;
using Xbim.Common.Model;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace ThMEPIFC
{
    class IfcStoreToEngineFile
    {
		protected List<IPersistEntity> ifcInstances;
		protected List<XbimShapeGeometry> shapeGeometries;
		protected List<XbimShapeInstance> shapeInstances;
		protected List<IPersistEntity> federatedInstances;
		protected IDictionary<int, List<XbimShapeInstance>> shapeGeoLoopups;
		protected IfcStore ifcModel;
		private ReadTaskInfo readTaskInfo;
		readonly XbimColourMap _colourMap = new XbimColourMap();
		public IfcStoreToEngineFile() { }
		public HashSet<short> DefaultExclusions(IModel model, List<Type> exclude)
		{
			var excludedTypes = new HashSet<short>();
			if (exclude == null)
				exclude = new List<Type>()
				{
					typeof(IIfcSpace),
					typeof(IIfcFeatureElement)
				};
			foreach (var excludedT in exclude)
			{
				ExpressType ifcT;
				if (excludedT.IsInterface && excludedT.Name.StartsWith("IIfc"))
				{
					var concreteTypename = excludedT.Name.Substring(1).ToUpper();
					ifcT = model.Metadata.ExpressType(concreteTypename);
				}
				else
					ifcT = model.Metadata.ExpressType(excludedT);
				if (ifcT == null) // it could be a type that does not belong in the model schema
					continue;
				foreach (var exIfcType in ifcT.NonAbstractSubTypes)
				{
					excludedTypes.Add(exIfcType.TypeId);
				}
			}
			return excludedTypes;
		}
		public List<IfcMeshModel> ReadGeomtry(IfcStore model, out List<PointNormal> allPointNormals)
		{
			ifcModel = model;
			allPointNormals = new List<PointNormal>();
			readTaskInfo = new ReadTaskInfo();
			var excludedTypes = DefaultExclusions(model,null);
			ifcInstances = new List<IPersistEntity>();
			shapeInstances = new List<XbimShapeInstance>();
			shapeGeometries = new List<XbimShapeGeometry>();
			federatedInstances = new List<IPersistEntity>();
			shapeGeoLoopups = new Dictionary<int, List<XbimShapeInstance>>();
			#region 多线程
			using (var geomStore = model.GeometryStore)
			{
				if (geomStore is InMemoryGeometryStore meyGeoStore)
				{
					shapeGeoLoopups = ((InMemoryGeometryStore)geomStore).GeometryShapeLookup;
				}
				using (var geomReader = geomStore.BeginRead())
				{
					var tempIns = GetShapeInstancesToRender(geomReader, excludedTypes);
					var geoCount = geomReader.ShapeGeometries.Count();
					shapeGeometries.AddRange(geomReader.ShapeGeometries);
					shapeInstances.AddRange(tempIns);
				}
			}
			var task = MoreTaskReadAsync();
			bool isContinue = true;
			while (isContinue)
			{
				Thread.Sleep(2000);
				lock (readTaskInfo)
				{
					if (readTaskInfo.AllCount < 1)
					{
						isContinue = false;
						break;
					}
					if (readTaskInfo.TaskCount < 1)
					{
						isContinue = false;
					}
				}
			}
			#endregion;
			allPointNormals.AddRange(readTaskInfo.AllPointVectors);
			return readTaskInfo.AllModels;
		}
		private async Task MoreTaskReadAsync()
		{
			List<Task> tasks = new List<Task>();

			int size = 100;
			int count = shapeGeometries.Count;
			int taskCount = (int)Math.Ceiling((double)count / size);
			readTaskInfo.AllCount = count;
			readTaskInfo.TaskCount = taskCount;
			readTaskInfo.AllTaskCount = taskCount;

			for (int j = 0; j < taskCount; j++)
			{
				var tempi = j;
				var t = Task.Run(() =>
				{
					var targetShapes = new List<XbimShapeGeometry>();

					int start = tempi * size,
						end = (tempi + 1) * size,
						thisSize = size;
					lock (shapeGeometries)
					{
						if (end > shapeGeometries.Count)
						{
							thisSize = shapeGeometries.Count - start;
							end = shapeGeometries.Count;
						}
					}
					targetShapes.AddRange(shapeGeometries.GetRange(start, thisSize));
					ReadGeometries(targetShapes, tempi, start, end);

				});
				tasks.Add(t);
			}
			await Task.WhenAll(tasks);
		}
		private void ReadGeometries(List<XbimShapeGeometry> targetShapes, int taskNum, int start, int end)
		{
			int thisCount = end - start;
			int pIndex = 0;
			var thisPointVectors = new List<PointNormal>();
			var thisModels = new List<IfcMeshModel>();
			var intGeoCount = 0;
			foreach (var item in targetShapes)
			{
				var insModel = shapeInstances.Find(c => c.ShapeGeometryLabel == item.ShapeLabel);
				if (insModel == null)
				{
					continue;
				}
				var material = GetMeshModelMaterial(insModel, item.IfcShapeLabel, item.ShapeLabel, out string typeStr);
				if (typeStr.Contains("open"))
					continue;
				var allPts = item.Vertices.ToList();
				var allFace = item.Faces.ToList();
				if (allFace.Count < 1)
					continue;
				if (shapeGeoLoopups.ContainsKey(item.ShapeLabel))
				{
					var allValues = shapeGeoLoopups[item.ShapeLabel];
					int tempCount = 1;
					foreach (var copyModel in allValues)
					{
						var transform = copyModel.Transformation;
						var mesh = new IfcMeshModel(intGeoCount + tempCount, copyModel.IfcProductLabel);
						foreach (var face in allFace.ToList())
						{
							var ptIndexs = face.Indices.ToArray();
							for (int i = 0; i < face.TriangleCount; i++)
							{
								var triangle = new FaceTriangle();
								triangle.TriangleMaterial = material;
								var pt1Index = ptIndexs[i * 3];
								var pt2Index = ptIndexs[i * 3 + 1];
								var pt3Index = ptIndexs[i * 3 + 2];
								var pt1 = TransPoint(allPts[pt1Index], transform);
								var pt1Normal = face.Normals.Last();
								if (pt1Index < face.Normals.Count())
									pt1Normal = face.NormalAt(pt1Index);
								pIndex += 1;
								pt1Normal = TransVector(pt1Normal, transform);
								triangle.PtIndexs.Add(pIndex);
								thisPointVectors.Add(GetPointNormal(pIndex, pt1, pt1Normal));
								var pt2 = TransPoint(allPts[pt2Index], transform);
								var pt2Normal = face.Normals.Last();
								if (pt2Index < face.Normals.Count())
									pt2Normal = face.NormalAt(pt2Index);
								pIndex += 1;
								pt2Normal = TransVector(pt2Normal, transform);
								triangle.PtIndexs.Add(pIndex);
								thisPointVectors.Add(GetPointNormal(pIndex, pt2, pt2Normal));
								var pt3 = TransPoint(allPts[pt3Index], transform);
								var pt3Normal = face.Normals.Last();
								if (pt3Index < face.Normals.Count())
									pt3Normal = face.NormalAt(pt3Index); //face.Normals[pt3Index].Normal;
								pIndex += 1;
								pt3Normal = TransVector(pt3Normal, transform);
								triangle.PtIndexs.Add(pIndex);
								thisPointVectors.Add(GetPointNormal(pIndex, pt3, pt3Normal));
								mesh.FaceTriangles.Add(triangle);
							}
						}
						thisModels.Add(mesh);
					}
				}
				else
				{
					var transform = insModel.Transformation;
					var mesh = new IfcMeshModel(intGeoCount, insModel.IfcProductLabel);
					foreach (var face in allFace.ToList())
					{
						var ptIndexs = face.Indices.ToArray();
						for (int i = 0; i < face.TriangleCount; i++)
						{
							var triangle = new FaceTriangle();
							triangle.TriangleMaterial = material;
							var pt1Index = ptIndexs[i * 3];
							var pt2Index = ptIndexs[i * 3 + 1];
							var pt3Index = ptIndexs[i * 3 + 2];
							var pt1 = TransPoint(allPts[pt1Index], transform);
							var pt1Normal = face.Normals.Last();
							if (pt1Index < face.Normals.Count())
								pt1Normal = face.NormalAt(pt1Index);
							pIndex += 1;
							pt1Normal = TransVector(pt1Normal, transform);
							triangle.PtIndexs.Add(pIndex);
							thisPointVectors.Add(GetPointNormal(pIndex, pt1, pt1Normal));
							var pt2 = TransPoint(allPts[pt2Index], transform);
							var pt2Normal = face.Normals.Last();
							if (pt2Index < face.Normals.Count())
								pt2Normal = face.NormalAt(pt2Index);
							pIndex += 1;
							pt2Normal = TransVector(pt2Normal, transform);
							triangle.PtIndexs.Add(pIndex);
							thisPointVectors.Add(GetPointNormal(pIndex, pt2, pt2Normal));
							var pt3 = TransPoint(allPts[pt3Index], transform);
							var pt3Normal = face.Normals.Last();
							if (pt3Index < face.Normals.Count())
								pt3Normal = face.NormalAt(pt3Index);
							pIndex += 1;
							pt3Normal = TransVector(pt3Normal, transform);
							triangle.PtIndexs.Add(pIndex);
							thisPointVectors.Add(GetPointNormal(pIndex, pt3, pt3Normal));
							mesh.FaceTriangles.Add(triangle);
						}
					}
					thisModels.Add(mesh);
				}

				intGeoCount += 1;
			}
			lock (readTaskInfo)
			{
				readTaskInfo.ReadCount += thisCount;
				var ptOffSet = readTaskInfo.AllPointVectors.Count;
				foreach (var item in thisPointVectors)
				{
					item.PointIndex += ptOffSet;
				}
				foreach (var item in thisModels)
				{
					item.CIndex += ptOffSet;
					foreach (var tr in item.FaceTriangles)
					{
						for (int i = 0; i < tr.PtIndexs.Count; i++)
							tr.PtIndexs[i] += ptOffSet;
					}
				}
				readTaskInfo.AllPointVectors.AddRange(thisPointVectors);
				readTaskInfo.AllModels.AddRange(thisModels);
				readTaskInfo.TaskCount -= 1;
			}
		}
		private PointNormal GetPointNormal(int pIndex, XbimPoint3D point, XbimVector3D normal)
		{
			return new PointNormal
			{
				PointIndex = pIndex,
				Point = new PointVector() { X = -(float)point.X, Y = (float)point.Z, Z = (float)point.Y },
				Normal = new PointVector() { X = -(float)normal.X, Y = (float)normal.Z, Z = (float)normal.Y },
			};
		}
		private XbimPoint3D TransPoint(XbimPoint3D xbimPoint, XbimMatrix3D xbimMatrix)
		{
			return xbimMatrix.Transform(xbimPoint);
		}
		private XbimVector3D TransVector(XbimVector3D xbimVector, XbimMatrix3D xbimMatrix)
		{
			return xbimMatrix.Transform(xbimVector);
		}
		protected IEnumerable<XbimShapeInstance> GetShapeInstancesToRender(IGeometryStoreReader geomReader, HashSet<short> excludedTypes)
		{
			var shapeInstances = geomReader.ShapeInstances
				.Where(s => s.RepresentationType == XbimGeometryRepresentationType.OpeningsAndAdditionsIncluded
							&&
							!excludedTypes.Contains(s.IfcTypeId));
			return shapeInstances;
		}
		private IfcMaterial GetMeshModelMaterial(XbimShapeInstance insModel, int ifcLable, int shapeLable, out string typeStr)
		{
			typeStr = "";
			var defalutMaterial = new IfcMaterial
			{
				Kd_R = 169 / 255f,
				Kd_G = 179 / 255f,
				Kd_B = 218 / 255f,
				Ks_R = 0,
				Ks_B = 0,
				Ks_G = 0,
				K = 0.5f,
				NS = 12,
			};
			//var ifcModel = ifcInstances[ifcLable];
			//var insModel = shapeInstances.Find(c => c.ShapeGeometryLabel == shapeLable);
			var type = this.ifcModel.Metadata.ExpressType((short)insModel.IfcTypeId);
			typeStr = type.ExpressName.ToLower();
			var v = _colourMap[type.Name];
			if (typeStr.Contains("window"))
			{

			}
			else if (typeStr.Contains("open"))
			{

			}
			/*
			defalutMaterial = new IfcMaterial
			{
				Kd_R = v.Red,
				Kd_G = v.Green,
				Kd_B = v.Blue,
				Ks_R = v.DiffuseFactor,
				Ks_G = v.SpecularFactor,
				Ks_B = v.DiffuseTransmissionFactor,
				K = v.Alpha,
				NS = 12,
			};
			return defalutMaterial;*/

			//testListSting.Add(ifcModel.GetType().ToString().ToLower());
			//testTypeStr.Add(typeStr);
			if (typeStr.Contains("wall"))
			{
				defalutMaterial = new IfcMaterial
				{
					Kd_R = 226 / 255f,
					Kd_G = 212 / 255f,
					Kd_B = 190 / 255f,
					Ks_R = 0,
					Ks_B = 0,
					Ks_G = 0,
					K = 1f,
					NS = 12,
				};
			}
			else if (typeStr.Contains("beam"))
			{
				defalutMaterial = new IfcMaterial
				{
					Kd_R = 194 / 255f,
					Kd_G = 178 / 255f,
					Kd_B = 152 / 255f,
					Ks_R = 0,
					Ks_B = 0,
					Ks_G = 0,
					K = 1f,
					NS = 12,
				};
			}
			else if (typeStr.Contains("door"))
			{
				defalutMaterial = new IfcMaterial
				{
					Kd_R = 167 / 255f,
					Kd_G = 182 / 255f,
					Kd_B = 199 / 255f,
					Ks_R = 0,
					Ks_B = 0,
					Ks_G = 0,
					K = 1f,
					NS = 12,
				};
			}
			else if (typeStr.Contains("slab"))
			{
				defalutMaterial = new IfcMaterial
				{
					Kd_R = 167 / 255f,
					Kd_G = 182 / 255f,
					Kd_B = 199 / 255f,
					Ks_R = 0,
					Ks_B = 0,
					Ks_G = 0,
					K = 1f,
					NS = 12,
				};
			}
			else if (typeStr.Contains("window"))
			{
				defalutMaterial = new IfcMaterial
				{
					Kd_R = 116 / 255f,
					Kd_G = 195 / 255f,
					Kd_B = 219 / 255f,
					Ks_R = 0,
					Ks_B = 0,
					Ks_G = 0,
					K = 0.5f,
					NS = 12,
				};
			}
			else if (typeStr.Contains("column"))
			{
				defalutMaterial = new IfcMaterial
				{
					Kd_R = 171 / 255f,
					Kd_G = 157 / 255f,
					Kd_B = 135 / 255f,
					Ks_R = 0,
					Ks_B = 0,
					Ks_G = 0,
					K = 1f,
					NS = 12,
				};
			}
			else if (typeStr.Contains("railing"))
			{
				defalutMaterial = new IfcMaterial { Kd_R = 136 / 255f, Kd_G = 211 / 255f, Kd_B = 198 / 255f, Ks_R = 0, Ks_B = 0, Ks_G = 0, K = 0.5f, NS = 12, };
			}
			else if (typeStr.Contains("open"))
			{

			}
			else if (typeStr.Contains("ifcmaterial"))
			{

			}
			else
			{

			}
			return defalutMaterial;
		}
	}
	class ReadTaskInfo
	{
		public int AllCount { get; set; }
		public int ReadCount { get; set; }
		public int TaskCount { get; set; }
		public int AllTaskCount { get; set; }
		public List<PointNormal> AllPointVectors { get; } = new List<PointNormal>();
		public List<IfcMeshModel> AllModels { get; } = new List<IfcMeshModel>();
	}
}
