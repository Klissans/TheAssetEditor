﻿using System.IO;
using System.Numerics;
using Editors.ImportExport.Common;
using Editors.ImportExport.Exporting.Exporters.DdsToMaterialPng;
using Editors.ImportExport.Exporting.Exporters.DdsToNormalPng;
using Editors.ImportExport.Exporting.Exporters.DdsToPng;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf.Helpers;
using Editors.ImportExport.Misc;
using MeshImportExport;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.Animation;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.Types;
using Shared.GameFormats.RigidModel.Vertex;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Schema2;

namespace Editors.ImportExport.Exporting.Exporters.RmvToGltf
{
    public record RmvToGltfExporterSettings(

        PackFile InputModelFile,
        List<PackFile> InputAnimationFiles,
        PackFile InputSkeletonFile,
        string OutputPath,
        bool ExportTextures,
        bool ConvertMaterialTextureToBlender,
        bool ConvertNormalTextureToBlue,
        bool ExportAnimations        
    );

    public class RmvToGltfExporter
    {
        private readonly PackFileService _packFileService;
        private readonly DdsToNormalPngExporter _ddsToNormalPngExporter;
        private readonly DdsToPngExporter _ddsToPngExporter;
        private readonly DdsToMaterialPngExporter _ddsToMaterialPngExporter;

        public RmvToGltfExporter(PackFileService packFileSerivce, DdsToNormalPngExporter ddsToNormalPngExporter, DdsToPngExporter ddsToPngExporter, DdsToMaterialPngExporter ddsToMaterialPngExporter)
        {
            _packFileService = packFileSerivce;
            _ddsToNormalPngExporter = ddsToNormalPngExporter;
            _ddsToPngExporter = ddsToPngExporter;
            _ddsToMaterialPngExporter = ddsToMaterialPngExporter;
        }

        internal ExportSupportEnum CanExportFile(PackFile file)
        {
            if (FileExtensionHelper.IsRmvFile(file.Name))
                return ExportSupportEnum.HighPriority;
            if (FileExtensionHelper.IsWsModelFile(file.Name))
                return ExportSupportEnum.HighPriority;
            return ExportSupportEnum.NotSupported;
        }

        private AnimationFile FetchAnimSkeleton(RmvFile rmv2)
        {
            var skeletonName = rmv2.Header.SkeletonName + ".anim";
            var skeletonSearchList = _packFileService.SearchForFile(skeletonName);
            var skeletonPath = _packFileService.GetFullPath(_packFileService.FindFile(skeletonSearchList[0]));
            var skeletonPackFile = _packFileService.FindFile(skeletonPath);

            var animSkeletonFile = AnimationFile.Create(skeletonPackFile);
            return animSkeletonFile;
        }

        private void GenerateAnimations(RmvToGltfExporterSettings settings, List<(Node, Matrix4x4)> gltfSkeleton, ModelRoot outputScene, AnimationFile animSkeletonFile, bool doMirror)
        {
            //for (int iAnim = 0; iAnim < settings.InputAnimationFiles.Count; iAnim++)
            {
                var animAnimationFile = AnimationFile.Create(settings.InputAnimationFiles[0]);

                var animBuilder = new GltfAnimationCreator(gltfSkeleton, animSkeletonFile);
                animBuilder.CreateFromTWAnim(outputScene, animAnimationFile, doMirror);
            }
        }

        public void Export(RmvToGltfExporterSettings settings)
        {
            const bool doMirror = true; // TODO: put in view (CheckBox) -> settomgs

            var rmv2 = new ModelFactory().Load(settings.InputModelFile.DataSource.ReadData());
            var lodLevel = rmv2.ModelList.First();
            var hasSkeleton = string.IsNullOrWhiteSpace(rmv2.Header.SkeletonName) == false;
            var gltfSkeleton = new List<(Node, Matrix4x4)>();
            
            var outputScene = ModelRoot.CreateModel();

            if (hasSkeleton)
            {
                var animSkeletonFile = FetchAnimSkeleton(rmv2);
                gltfSkeleton = GenerateSkeleton(rmv2, outputScene, animSkeletonFile, doMirror);

                if (settings.ExportAnimations && settings.InputAnimationFiles.Count != 0)
                {
                    GenerateAnimations(settings, gltfSkeleton, outputScene, animSkeletonFile, doMirror);
                }
            }       

            var meshes = new List<Mesh>();
            foreach (var rmvMesh in lodLevel)
            {
                var gltfMaterial = new MaterialBuilder(rmvMesh.Material.ModelName + "_Material")
                    .WithDoubleSide(true)
                    .WithMetallicRoughness()
                    .WithAlpha(SharpGLTF.Materials.AlphaMode.MASK);

                if (settings.ExportTextures == true)
                {
                    gltfMaterial = GenerateMaterial(settings, rmvMesh, gltfMaterial);
                }
                else
                {
                    gltfMaterial = BuildFakeMaterialPerMesh(rmvMesh, settings.InputModelFile);
                }
                var mesh = outputScene.CreateMesh(GenerateMesh(rmvMesh, gltfMaterial, hasSkeleton, doMirror));
                meshes.Add(mesh);
            }
            BuildGltf(meshes, gltfSkeleton, settings, outputScene);
        }

        public void BuildGltf(List<Mesh> meshes, List<(Node, Matrix4x4)> gltfSkeleton, RmvToGltfExporterSettings settings, ModelRoot model)
        {
            var scene = model.UseScene("default");
            foreach (var mesh in meshes)
            {
                if (gltfSkeleton.Count != 0)
                    scene.CreateNode(mesh.Name).WithSkinnedMesh(mesh, gltfSkeleton.ToArray());
                else
                    scene.CreateNode(mesh.Name).WithMesh(mesh);
            }
            model.SaveGLTF(settings.OutputPath + Path.GetFileNameWithoutExtension(settings.InputModelFile.Name) + ".gltf");
        }


        internal List<(Node, Matrix4x4)> GenerateSkeleton(RmvFile rmv2, ModelRoot model, AnimationFile animSkeletonFile, bool doMIrror)
        {
            var gltfSkeletonBindings = GltfSkeletonCreator.Create(model, animSkeletonFile, doMIrror);

            return gltfSkeletonBindings;
        }

        public static MeshBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4> GenerateMesh(RmvModel rmvMesh, MaterialBuilder material, bool hasSkeleton, bool doMirror)
        {
            var mesh = new MeshBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4>(rmvMesh.Material.ModelName);
            if (hasSkeleton)
            {
                mesh.VertexPreprocessor.SetValidationPreprocessors(); 
            }

            var prim = mesh.UsePrimitive(material);

            var vertexList = new List<VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4>>();
            foreach (var vertex in rmvMesh.Mesh.VertexList)
            {
                var glTfvertex = new VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4>();
                glTfvertex.Geometry.Position = new Vector3(vertex.Position.X, vertex.Position.Y, vertex.Position.Z);
                glTfvertex.Geometry.Normal = new Vector3(vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z);
                glTfvertex.Geometry.Tangent = new Vector4(vertex.Tangent.X, vertex.Tangent.Y, vertex.Tangent.Z, 1);
                glTfvertex.Material.TexCoord = new Vector2(vertex.Uv.X, vertex.Uv.Y);

                glTfvertex.Geometry.Position = VecConv.GetSys(GlobalSceneTransforms.FlipVector(VecConv.GetXna(glTfvertex.Geometry.Position), doMirror));
                glTfvertex.Geometry.Normal = VecConv.GetSys(GlobalSceneTransforms.FlipVector(VecConv.GetXna(glTfvertex.Geometry.Normal), doMirror));
                glTfvertex.Geometry.Tangent = VecConv.GetSys(GlobalSceneTransforms.FlipVector(VecConv.GetXna(glTfvertex.Geometry.Tangent), doMirror));

                if (hasSkeleton)
                {
                    glTfvertex = SetVertexInfluences(vertex, glTfvertex);
                }
                else
                {
                    glTfvertex.Skinning.SetBindings((0, 1));                    
                }
                vertexList.Add(glTfvertex);
            }

            var triangleCount = rmvMesh.Mesh.IndexList.Length;
            for (var i = 0; i < triangleCount; i += 3)
            {

                ushort i0, i1, i2;
                if (doMirror) // if mirrored, flip the winding order
                {
                    i0 = rmvMesh.Mesh.IndexList[i + 0];
                    i1 = rmvMesh.Mesh.IndexList[i + 2];
                    i2 = rmvMesh.Mesh.IndexList[i + 1];
                }
                else
                {
                    i0 = rmvMesh.Mesh.IndexList[i + 0];
                    i1 = rmvMesh.Mesh.IndexList[i + 1];
                    i2 = rmvMesh.Mesh.IndexList[i + 2];
                }
                
                prim.AddTriangle(vertexList[i0], vertexList[i1], vertexList[i2]);
            }
            return mesh;
        }


        private static VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4> SetVertexInfluences(CommonVertex vertex, VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4> glTfvertex)
        {
            if (vertex.WeightCount == 2)
            {
                var rigging = new (int, float)[2] {
                        (vertex.BoneIndex[0], vertex.BoneWeight[0]),
                        (vertex.BoneIndex[1], 1.0f - vertex.BoneWeight[0])
                        };

                glTfvertex.Skinning.SetBindings(rigging);

            }
            else if (vertex.WeightCount == 4)
            {
                var rigging = new (int, float)[4] {
                        (vertex.BoneIndex[0], vertex.BoneWeight[0]),
                        (vertex.BoneIndex[1], vertex.BoneWeight[1]),
                        (vertex.BoneIndex[2], vertex.BoneWeight[2]),
                        (vertex.BoneIndex[3], 1.0f - (vertex.BoneWeight[0] + vertex.BoneWeight[1] + vertex.BoneWeight[2]))
                        };

                glTfvertex.Skinning.SetBindings(rigging);
            }

            return glTfvertex;
        }



        internal static VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4> BonesAndWeight(CommonVertex vertex,
            VertexBuilder<VertexPositionNormalTangent, VertexTexture1, VertexJoints4> glTfvertex)
        {
            if (vertex.WeightCount == 2)
            {
                if (vertex.BoneWeight[0] == 0)
                    vertex.BoneIndex[0] = 0;

                if (vertex.BoneWeight[1] == 0)
                    vertex.BoneIndex[1] = 0;

                var sum = vertex.BoneWeight[0] + vertex.BoneWeight[1];
                if (Math.Abs(sum - 1) > 4E-07)
                {

                    vertex.BoneWeight[0] += Math.Abs(sum - 1);
                    // Values are within specified tolerance of each other....
                }

                glTfvertex.Skinning.Weights = new Vector4(vertex.BoneWeight[0], vertex.BoneWeight[1], 0, 0);
                glTfvertex.Skinning.Joints = new Vector4(vertex.BoneIndex[0], vertex.BoneIndex[1], 0, 0);
            }
            else if (vertex.WeightCount == 4)
            {
                if (vertex.BoneWeight[0] == 0)
                    vertex.BoneIndex[0] = 0;

                if (vertex.BoneWeight[1] == 0)
                    vertex.BoneIndex[1] = 0;

                if (vertex.BoneWeight[2] == 0)
                    vertex.BoneIndex[2] = 0;

                if (vertex.BoneWeight[3] == 0)
                    vertex.BoneIndex[3] = 0;

                var sum = vertex.BoneWeight[0] + vertex.BoneWeight[1] + vertex.BoneWeight[2] + vertex.BoneWeight[3];
                if (Math.Abs(sum - 1) > 4E-07)
                {

                    vertex.BoneWeight[0] += Math.Abs(sum - 1);
                    // Values are within specified tolerance of each other....
                }

                glTfvertex.Skinning.Weights = new Vector4(vertex.BoneWeight[0], vertex.BoneWeight[1], vertex.BoneWeight[2], vertex.BoneWeight[3]);
                glTfvertex.Skinning.Joints = new Vector4(vertex.BoneIndex[0], vertex.BoneIndex[1], vertex.BoneIndex[2], vertex.BoneIndex[3]);
            }
            else
            {
                //throw new Exception("Woops");
                glTfvertex.Skinning.Weights = new Vector4(0, 1, 0, 0);
                glTfvertex.Skinning.Joints = new Vector4(0, 1, 0, 0);
            }
            return glTfvertex;
        }

        internal MaterialBuilder BuildFakeMaterialPerMesh(RmvModel rmvMesh, PackFile file)
        {
            var material = TextureHelper.BuildFakeMaterial(_packFileService, rmvMesh);
            return material;
        }

        internal MaterialBuilder GenerateMaterial(RmvToGltfExporterSettings settings, RmvModel rmv2Mesh, MaterialBuilder material)
        {
            var textures = rmv2Mesh.Material.GetAllTextures();
            var systemPath = "";

            var normalMapTexture = textures.FirstOrDefault(t => t.TexureType == TextureType.Normal);
            if (normalMapTexture.Path != null)
            {
                systemPath = _ddsToNormalPngExporter.Export(normalMapTexture.Path, settings.OutputPath, settings.ConvertNormalTextureToBlue);
                material.WithChannelImage(KnownChannel.Normal, systemPath);
            }

            var materialTexture = textures.FirstOrDefault(t => t.TexureType == TextureType.MaterialMap);
            if (materialTexture.Path != null)
            {
                systemPath = _ddsToMaterialPngExporter.Export(materialTexture.Path, settings.OutputPath, settings.ConvertMaterialTextureToBlender);
                material.WithChannelImage(KnownChannel.MetallicRoughness, systemPath);
            }

            var baseColourTexture = textures.FirstOrDefault(t => t.TexureType == TextureType.BaseColour);
            if (baseColourTexture.Path != null)
            {
                systemPath = _ddsToPngExporter.GenericExportNoConversion(settings.OutputPath, baseColourTexture);
                material.WithChannelImage(KnownChannel.BaseColor, systemPath);
            }
            return material;
        }
    }
}
