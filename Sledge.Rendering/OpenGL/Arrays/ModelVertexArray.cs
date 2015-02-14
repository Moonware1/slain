﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Sledge.Rendering.Cameras;
using Sledge.Rendering.DataStructures.Models;
using Sledge.Rendering.Interfaces;
using Sledge.Rendering.OpenGL.Shaders;
using Sledge.Rendering.OpenGL.Vertices;

namespace Sledge.Rendering.OpenGL.Arrays
{
    public class ModelVertexArray : VertexArray<Model, ModelVertex>
    {
        private const int FacePolygons = 0;
        private const int FaceWireframe = 1;

        private Model _model;

        public ModelVertexArray(IEnumerable<Model> data) : base(data)
        {
        }

        private int frame = 0;

        // todo model shader
        public void Render(IRenderer renderer, ModelShader shader, IViewport viewport, Matrix4 modelView)
        {
            var camera = viewport.Camera;

            var vpMatrix = camera.GetViewportMatrix(viewport.Control.Width, viewport.Control.Height);
            var camMatrix = camera.GetCameraMatrix();

            var eye = camera.EyeLocation;
            var options = camera.RenderOptions;

            shader.Bind();
            shader.SelectionTransform = Matrix4.Identity;
            shader.ModelMatrix = modelView;
            shader.CameraMatrix = camMatrix;
            shader.ViewportMatrix = vpMatrix;
            shader.Orthographic = camera.Flags.HasFlag(CameraFlags.Orthographic);
            shader.UseAccentColor = !options.RenderFacePolygonTextures;

            shader.AnimationTransforms = _model.GetCurrentTransforms();
            //shader.AnimationTransforms = frame;

            shader.ModelMatrix = Matrix4.CreateRotationZ(frame++ / 100f);

            // todo
            // options.RenderFacePolygonLighting

            // Render non-transparent polygons
            string last = null;
            foreach (var subset in GetSubsets<string>(FacePolygons).Where(x => x.Instance != null).OrderBy(x => (string)x.Instance))
            {
                var mat = (string)subset.Instance;
                if (mat != last) renderer.Materials.Bind(mat);
                last = mat;

                Render(PrimitiveType.Triangles, subset);
            }

            shader.UseAccentColor = true;

            // Render wireframe
            foreach (var subset in GetSubsets(FaceWireframe))
            {
                Render(PrimitiveType.Lines, subset);
            }

            shader.Unbind();
        }

        protected override void CreateArray(IEnumerable<Model> data)
        {
            StartSubset(FaceWireframe);

            foreach (var model in data)
            {
                _model = model;
                foreach (var g in model.Meshes.GroupBy(x => x.Material.UniqueIdentifier))
                {
                    StartSubset(FacePolygons);

                    foreach (var mesh in g)
                    {
                        var index = PushData(mesh.Vertices.Select((vertex, i) => Convert(vertex, i, model)));
                        PushIndex(FacePolygons, index, Enumerable.Range(0, mesh.Vertices.Count).Select(System.Convert.ToUInt32));
                    }

                    PushSubset(FacePolygons, g.Key);
                }
            }

            PushSubset(FaceWireframe, (object) null);
        }
        
        private ModelVertex Convert(MeshVertex vert, int i, Model mdl)
        {
            var weights = vert.Weightings.ToList();
            var w1 = weights.Count > 0 ? weights[0] : new KeyValuePair<int, float>(0, 1);
            var w2 = weights.Count > 1 ? weights[1] : new KeyValuePair<int, float>(0, 0);
            var w3 = weights.Count > 2 ? weights[2] : new KeyValuePair<int, float>(0, 0);

            return new ModelVertex
            {
                Position = vert.Location,
                Normal = vert.Normal,
                Texture = new Vector2(vert.TextureU, vert.TextureV),
                AccentColor = Color.Red.ToAbgr(),
                TintColor = Color.White.ToAbgr(),
                Flags = VertexFlags.None,

                WeightingIndex1 = (byte) w1.Key,
                WeightingIndex2 = (byte) w2.Key,
                WeightingIndex3 = (byte) w3.Key,
                //
                WeightingValue1 = w1.Value,
                WeightingValue2 = w2.Value,
                WeightingValue3 = w3.Value
            };
        }
    }
}
