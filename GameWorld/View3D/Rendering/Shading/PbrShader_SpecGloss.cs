﻿using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework.Graphics;
using Shared.GameFormats.RigidModel.Types;

namespace GameWorld.Core.Rendering.Shading
{
    public class PbrShader_SpecGloss : PbrShader
    {
        public override RenderFormats RenderFormat { get => RenderFormats.SpecGloss; }
        public override Effect Effect { get; protected set; }

        public PbrShader_SpecGloss(ResourceLibrary resourceLibrary) : base(resourceLibrary)
        {
            Effect = resourceLibrary.GetEffect(ShaderTypes.Pbr_SpecGloss);

            _textureEffectParams.Add(TextureType.Diffuse, Effect.Parameters["DiffuseTexture"]);
            _textureEffectParams.Add(TextureType.Specular, Effect.Parameters["SpecularTexture"]);
            _textureEffectParams.Add(TextureType.Normal, Effect.Parameters["NormalTexture"]);
            _textureEffectParams.Add(TextureType.Gloss, Effect.Parameters["GlossTexture"]);

            _useTextureParams.Add(TextureType.Diffuse, Effect.Parameters["UseDiffuse"]);
            _useTextureParams.Add(TextureType.Specular, Effect.Parameters["UseSpecular"]);
            _useTextureParams.Add(TextureType.Normal, Effect.Parameters["UseNormal"]);
            _useTextureParams.Add(TextureType.Gloss, Effect.Parameters["UseGloss"]);

            Effect.Parameters["tex_cube_diffuse"]?.SetValue(resourceLibrary.PbrDiffuse);
            Effect.Parameters["tex_cube_specular"]?.SetValue(resourceLibrary.PbrSpecular);
            Effect.Parameters["specularBRDF_LUT"]?.SetValue(resourceLibrary.PbrLut);
        }

        public override IShader Clone()
        {
            var newShader = new PbrShader_SpecGloss(_resourceLibrary);

            newShader.Effect.Parameters["DiffuseTexture"].SetValue(Effect.Parameters["DiffuseTexture"].GetValueTexture2D());
            newShader.Effect.Parameters["SpecularTexture"].SetValue(Effect.Parameters["SpecularTexture"].GetValueTexture2D());
            newShader.Effect.Parameters["NormalTexture"].SetValue(Effect.Parameters["NormalTexture"].GetValueTexture2D());
            newShader.Effect.Parameters["GlossTexture"].SetValue(Effect.Parameters["GlossTexture"].GetValueTexture2D());

            return newShader;
        }
    }
}