﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components.Component;
using View3D.Components.Rendering;
using View3D.SceneNodes;

namespace View3D.Rendering.RenderItems
{
    public class VertexRenderItem : IRenderItem
    {
        public VertexInstanceMesh VertexRenderer { get; set; }

        public MeshNode Node { get; set; }
        public Matrix World { get; set; }
        public List<int> SelectedVertices { get; set; }

        public void Draw(GraphicsDevice device, CommonShaderParameters parameters)
        {
            VertexRenderer.Update(Node.Geometry, Node.ModelMatrix, Node.Orientation, parameters.CameraPosition, SelectedVertices);
            VertexRenderer.Draw(parameters.View, parameters.Projection, device, new Vector3(0, 1, 0));
        }
    }
}
