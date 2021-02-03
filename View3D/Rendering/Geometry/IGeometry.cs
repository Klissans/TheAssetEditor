﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components.Component;

namespace View3D.Rendering.Geometry
{
    public interface IGeometry : IDisposable
    {
        float? Intersect(Ray ray, Matrix modelMatrix);
        public bool IntersectFace(Ray ray, Matrix modelMatrix, out FaceSelection face);


        public void ApplyMesh(Effect effect, GraphicsDevice device);

        public void ApplyMeshPart(Effect effect, GraphicsDevice device, FaceSelection faceSelection);
    }
}
