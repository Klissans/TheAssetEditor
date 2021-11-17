﻿using Filetypes.RigidModel;
using Filetypes.RigidModel.Transforms;
using Filetypes.RigidModel.Vertex;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Rendering;
using View3D.Rendering.Geometry;

namespace View3D.Services
{
    public class MeshBuilderService
    {
        public static MeshObject BuildMeshFromRmvModel(RmvModel modelPart, string skeletonName, IGraphicsCardGeometry context)
        {
            var output = new MeshObject(context, skeletonName);
            output.VertexArray = new VertexPositionNormalTextureCustom[modelPart.Mesh.VertexList.Length];
            output.IndexArray = (ushort[])modelPart.Mesh.IndexList.Clone();
            output.ChangeVertexType(modelPart.Material.VertexType, skeletonName);

            for (int i = 0; i < modelPart.Mesh.VertexList.Length; i++)
            {
                var vertex = modelPart.Mesh.VertexList[i];
                output.VertexArray[i].Position = vertex.Position;
                output.VertexArray[i].Normal = vertex.Normal;
                output.VertexArray[i].BiNormal = vertex.BiNormal;
                output.VertexArray[i].Tangent = vertex.Tangent;
                output.VertexArray[i].TextureCoordinate = vertex.Uv;

                if (output.VertexFormat == VertexFormat.Static)
                {
                    output.VertexArray[i].BlendIndices = Vector4.Zero;
                    output.VertexArray[i].BlendWeights = Vector4.Zero;
                }
                else if (output.VertexFormat == VertexFormat.Weighted)
                {
                    output.VertexArray[i].BlendIndices = new Vector4(vertex.BoneIndex[0], vertex.BoneIndex[1], 0, 0);
                    output.VertexArray[i].BlendWeights = new Vector4(vertex.BoneWeight[0], vertex.BoneWeight[1], 0, 0);
                }
                else if (output.VertexFormat == VertexFormat.Cinematic)
                {
                    output.VertexArray[i].BlendIndices = new Vector4(vertex.BoneIndex[0], vertex.BoneIndex[1], vertex.BoneIndex[2], vertex.BoneIndex[3]);
                    output.VertexArray[i].BlendWeights = new Vector4(vertex.BoneWeight[0], vertex.BoneWeight[1], vertex.BoneWeight[2], vertex.BoneWeight[3]);
                }
                else
                    throw new Exception("Unkown vertex format");
            }

            output.RebuildVertexBuffer();
            output.RebuildIndexBuffer();
            output.BuildBoundingBox();
            return output;
        }

        public static RmvModel CreateRmvSubModel(RmvModel baseModel, MeshObject geometry, RmvVersionEnum version)
        {
            var newSubModel = baseModel.Clone();
            newSubModel.Mesh = CreateRmvFileMesh(geometry);
            return newSubModel;
        }


        public static RmvMesh CreateRmvFileMesh(MeshObject geometry)
        {
            // Ensure normalized
            for (int i = 0; i < geometry.VertexArray.Length; i++)
            {
                geometry.VertexArray[i].Normal = Vector3.Normalize(geometry.VertexArray[i].Normal);
                geometry.VertexArray[i].BiNormal = Vector3.Normalize(geometry.VertexArray[i].BiNormal);
                geometry.VertexArray[i].Tangent = Vector3.Normalize(geometry.VertexArray[i].Tangent);
            }

            RmvMesh mesh = new RmvMesh();
            mesh.IndexList = geometry.GetIndexBuffer().ToArray();
            mesh.VertexList = geometry.VertexArray.
                Select(x => new CommonVertex()
                {
                    Position = x.Position,
                    Normal = x.Normal,
                    BiNormal = x.BiNormal,
                    Tangent = x.Tangent,

                    Colour = new Vector4(0,0,0,1),
                    Uv = x.TextureCoordinate,

                    BoneIndex = x.GetBoneIndexs().Take(geometry.WeightCount).Select(x=>(byte)x).ToArray(),
                    BoneWeight = x.GetBoneWeights().Take(geometry.WeightCount).ToArray(),
                    WeightCount = geometry.WeightCount
                }).ToArray();

            return mesh;
        }
    }
}