﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using LogicAndTrick.Oy;
using Sledge.BspEditor.Modification;
using Sledge.BspEditor.Modification.Operations.Tree;
using Sledge.BspEditor.Primitives;
using Sledge.BspEditor.Primitives.MapObjects;
using Sledge.BspEditor.Rendering.Viewport;
using Sledge.BspEditor.Tools.Properties;
using Sledge.Common.Shell.Components;
using Sledge.Rendering.Cameras;
using Sledge.DataStructures.Geometric;
using Sledge.Rendering.Pipelines;
using Sledge.Rendering.Primitives;
using Sledge.Rendering.Resources;
using Sledge.Rendering.Viewports;
using Sledge.Shell.Input;
using Plane = Sledge.DataStructures.Geometric.Plane;

namespace Sledge.BspEditor.Tools.Clip
{
    [Export(typeof(ITool))]
    [OrderHint("N")]
    public class ClipTool : BaseTool
    {
        public enum ClipState
        {
            None,
            Drawing,
            Drawn,
            MovingPoint1,
            MovingPoint2,
            MovingPoint3
        }

        public enum ClipSide
        {
            Both,
            Front,
            Back
        }

        private Vector3? _clipPlanePoint1;
        private Vector3? _clipPlanePoint2;
        private Vector3? _clipPlanePoint3;
        private Vector3? _drawingPoint;
        private ClipState _prevState;
        private ClipState _state;
        private ClipSide _side;

        public ClipTool()
        {
            Usage = ToolUsage.Both;
            _clipPlanePoint1 = _clipPlanePoint2 = _clipPlanePoint3 = _drawingPoint = null;
            _state = _prevState = ClipState.None;
            _side = ClipSide.Both;

            UseValidation = true;
        }

        public override Image GetIcon()
        {
            return Resources.Tool_Clip;
        }

        public override string GetName()
        {
            return "Clip Tool";
        }

        protected override IEnumerable<Subscription> Subscribe()
        {
            yield return Oy.Subscribe<ClipTool>("Tool:Activated", t => CycleClipSide());
            yield return Oy.Subscribe<string>("ClipTool:SetClipSide", v => SetClipSide(v));
        }

        private void SetClipSide(string visiblePoints)
        {
            if (Enum.TryParse(visiblePoints, true, out ClipSide s) && s != _side)
            {
                _side = s;
                Invalidate();
            }
        }
        
        private void CycleClipSide()
        {
            var side = (int) _side;
            side = (side + 1) % Enum.GetValues(typeof (ClipSide)).Length;
            _side = (ClipSide) side;
            Invalidate();
        }

        private ClipState GetStateAtPoint(int x, int y, MapViewport viewport)
        {
            if (_clipPlanePoint1 == null || _clipPlanePoint2 == null || _clipPlanePoint3 == null) return ClipState.None;

            var p = viewport.Flatten(viewport.ScreenToWorld(x, y));
            var p1 = viewport.Flatten(_clipPlanePoint1.Value);
            var p2 = viewport.Flatten(_clipPlanePoint2.Value);
            var p3 = viewport.Flatten(_clipPlanePoint3.Value);

            var d = 5 / viewport.Zoom;

            if (p.X >= p1.X - d && p.X <= p1.X + d && p.Y >= p1.Y - d && p.Y <= p1.Y + d) return ClipState.MovingPoint1;
            if (p.X >= p2.X - d && p.X <= p2.X + d && p.Y >= p2.Y - d && p.Y <= p2.Y + d) return ClipState.MovingPoint2;
            if (p.X >= p3.X - d && p.X <= p3.X + d && p.Y >= p3.Y - d && p.Y <= p3.Y + d) return ClipState.MovingPoint3;

            return ClipState.None;
        }

        protected override void MouseDown(MapViewport vp, OrthographicCamera camera, ViewportEvent e)
        {
            var viewport = vp;
            _prevState = _state;

            var point = SnapIfNeeded(viewport.ScreenToWorld(e.X, e.Y));
            var st = GetStateAtPoint(e.X, e.Y, viewport);
            if (_state == ClipState.None || st == ClipState.None)
            {
                _state = ClipState.Drawing;
                _drawingPoint = point;
            }
            else if (_state == ClipState.Drawn)
            {
                _state = st;
            }
            Invalidate();
        }

        protected override void MouseUp(MapViewport vp, OrthographicCamera camera, ViewportEvent e)
        {
            var viewport = vp;

            var point = SnapIfNeeded(viewport.ScreenToWorld(e.X, e.Y));
            if (_state == ClipState.Drawing)
            {
                // Do nothing
                _state = _prevState;
            }
            else
            {
                _state = ClipState.Drawn;
            }

            // todo
            // Editor.Instance.CaptureAltPresses = false;

            Invalidate();
        }

        protected override void MouseMove(MapViewport vp, OrthographicCamera camera, ViewportEvent e)
        {
            var viewport = vp;

            var point = SnapIfNeeded(viewport.ScreenToWorld(e.X, e.Y));
            var st = GetStateAtPoint(e.X, e.Y, viewport);
            if (_state == ClipState.Drawing)
            {
                _state = ClipState.MovingPoint2;
                _clipPlanePoint1 = _drawingPoint;
                _clipPlanePoint2 = point;
                _clipPlanePoint3 = _clipPlanePoint1 + SnapIfNeeded(viewport.GetUnusedCoordinate(new Vector3(128, 128, 128)));
            }
            else if (_state == ClipState.MovingPoint1)
            {
                // Move point 1
                var cp1 = viewport.GetUnusedCoordinate(_clipPlanePoint1.Value) + point;
                if (KeyboardState.Ctrl)
                {
                    var diff = _clipPlanePoint1 - cp1;
                    _clipPlanePoint2 -= diff;
                    _clipPlanePoint3 -= diff;
                }
                _clipPlanePoint1 = cp1;
            }
            else if (_state == ClipState.MovingPoint2)
            {
                // Move point 2
                var cp2 = viewport.GetUnusedCoordinate(_clipPlanePoint2.Value) + point;
                if (KeyboardState.Ctrl)
                {
                    var diff = _clipPlanePoint2 - cp2;
                    _clipPlanePoint1 -= diff;
                    _clipPlanePoint3 -= diff;
                }
                _clipPlanePoint2 = cp2;
            }
            else if (_state == ClipState.MovingPoint3)
            {
                // Move point 3
                var cp3 = viewport.GetUnusedCoordinate(_clipPlanePoint3.Value) + point;
                if (KeyboardState.Ctrl)
                {
                    var diff = _clipPlanePoint3 - cp3;
                    _clipPlanePoint1 -= diff;
                    _clipPlanePoint2 -= diff;
                }
                _clipPlanePoint3 = cp3;
            }

            // todo?
            // Editor.Instance.CaptureAltPresses = _state != ClipState.None && _state != ClipState.Drawn;

            if (st != ClipState.None || _state != ClipState.None && _state != ClipState.Drawn)
            {
                viewport.Control.Cursor = Cursors.Cross;
            }
            else
            {
                viewport.Control.Cursor = Cursors.Default;
            }

            Invalidate();
        }

        public override void KeyDown(MapViewport viewport, ViewportEvent e)
        {
            if (e.KeyCode == Keys.Enter && _state != ClipState.None)
            {
                if (!_clipPlanePoint1.Value.EquivalentTo(_clipPlanePoint2.Value)
                    && !_clipPlanePoint2.Value.EquivalentTo(_clipPlanePoint3.Value)
                    && !_clipPlanePoint1.Value.EquivalentTo(_clipPlanePoint3.Value)) // Don't clip if the points are too close together
                {
                    PerformClip();
                }
            }
            if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.Enter) // Escape cancels, Enter commits and resets
            {
                _clipPlanePoint1 = _clipPlanePoint2 = _clipPlanePoint3 = _drawingPoint = null;
                _state = _prevState = ClipState.None;
            }

            Invalidate();

            base.KeyDown(viewport, e);
        }

        private void PerformClip()
        {
            var objects = Document.Selection.OfType<Solid>().ToList();
            if (!objects.Any()) return;

            var plane = new Plane(_clipPlanePoint1.Value, _clipPlanePoint2.Value, _clipPlanePoint3.Value);
            var clip = new Transaction();
            var found = false;
            foreach (var solid in objects)
            {
                solid.Split(Document.Map.NumberGenerator, plane, out var backSolid, out var frontSolid);
                found = true;
                
                // Remove the clipped solid
                clip.Add(new Detatch(solid.Hierarchy.Parent.ID, solid));
                
                if (_side != ClipSide.Back && frontSolid != null)
                {
                    // Add front solid
                    clip.Add(new Attach(solid.Hierarchy.Parent.ID, frontSolid));
                }
                
                if (_side != ClipSide.Front && backSolid != null)
                {
                    // Add back solid
                    clip.Add(new Attach(solid.Hierarchy.Parent.ID, backSolid));
                }
            }
            if (found)
            {
                MapDocumentOperation.Perform(Document, clip);
            }
        }

        public override void Render(BufferBuilder builder)
        {
            base.Render(builder);

            if (_state != ClipState.None && _clipPlanePoint1 != null && _clipPlanePoint2 != null && _clipPlanePoint3 != null)
            {
                // Draw the lines
                var p1 = _clipPlanePoint1.Value;
                var p2 = _clipPlanePoint2.Value;
                var p3 = _clipPlanePoint3.Value;

                builder.Append(
                    new []
                    {
                        new VertexStandard { Position = p1, Colour = Vector4.One, Tint = Vector4.One },
                        new VertexStandard { Position = p2, Colour = Vector4.One, Tint = Vector4.One },
                        new VertexStandard { Position = p3, Colour = Vector4.One, Tint = Vector4.One },
                    },
                    new uint [] { 0, 1, 1, 2, 2, 0 },
                    new []
                    {
                        new BufferGroup(PipelineType.WireframeGeneric, CameraType.Both, false, p1, 0, 6)
                    }
                );

                if (!p1.EquivalentTo(p2)
                    && !p2.EquivalentTo(p3)
                    && !p1.EquivalentTo(p3)
                    && !Document.Selection.IsEmpty)
                {
                    var plane = new Plane(p1, p2, p3).ToPrecisionPlane();

                    // Draw the clipped solids
                    var faces = new List<Polygon>();
                    foreach (var solid in Document.Selection.OfType<Solid>().ToList())
                    {
                        var s = solid.ToPolyhedron().ToPrecisionPolyhedron();
                        s.Split(plane, out var back, out var front);

                        if (_side != ClipSide.Front && back != null) faces.AddRange(back.Polygons.Select(x => x.ToStandardPolygon()));
                        if (_side != ClipSide.Back && front != null) faces.AddRange(front.Polygons.Select(x => x.ToStandardPolygon()));
                    }

                    var verts = new List<VertexStandard>();
                    var indices = new List<int>();

                    foreach (var polygon in faces)
                    {
                        var c = verts.Count;
                        verts.AddRange(polygon.Vertices.Select(x => new VertexStandard { Position = x, Colour = Vector4.One, Tint = Vector4.One }));
                        for (var i = 0; i < polygon.Vertices.Count; i++)
                        {
                            indices.Add(c + i);
                            indices.Add(c + (i + 1) % polygon.Vertices.Count);
                        }
                    }

                    builder.Append(
                        verts, indices.Select(x => (uint) x),
                        new[] { new BufferGroup(PipelineType.WireframeGeneric, CameraType.Both, false, p1, 0, (uint) indices.Count) }
                    );

                    // var lines = faces.Select(x => new Line(Color.White, x.Vertices.Select(v => v).ToArray()) {Width = 2});
                    // list.AddRange(lines);
                    // 
                    // // Draw the clipping plane
                    // var poly = new Polygon(plane);
                    // var bbox = Document.Selection.GetSelectionBoundingBox();
                    // var point = bbox.Center;
                    // foreach (var boxPlane in bbox.GetBoxPlanes())
                    // {
                    //     var proj = boxPlane.Project(point);
                    //     var dist = (point - proj).VectorMagnitude() * 0.1m;
                    //     poly.Split(new Plane(boxPlane.Normal, proj + boxPlane.Normal * Math.Max(dist, 100)));
                    // }
                    // 
                    // // Add the face in both directions so it renders on both sides
                    // list.Add(new Face(
                    //     Material.Flat(Color.FromArgb(100, Color.Turquoise)),
                    //     poly.Vertices.Select(x => new Sledge.Rendering.Scenes.Renderables.Vertex(x.ToVector3(), 0, 0)).ToList())
                    // {
                    //     CameraFlags = CameraFlags.Perspective
                    // });
                    // list.Add(new Face(
                    //     Material.Flat(Color.FromArgb(100, Color.Turquoise)),
                    //     poly.Vertices.Select(x => new Sledge.Rendering.Scenes.Renderables.Vertex(x.ToVector3(), 0, 0)).Reverse().ToList())
                    // {
                    //     CameraFlags = CameraFlags.Perspective
                    // });
                }
            }
        }

        public override void Render(IViewport viewport, OrthographicCamera camera, Vector3 worldMin, Vector3 worldMax, Graphics graphics)
        {
            base.Render(viewport, camera, worldMin, worldMax, graphics);

            if (_state != ClipState.None && _clipPlanePoint1 != null && _clipPlanePoint2 != null && _clipPlanePoint3 != null)
            {
                var p1 = _clipPlanePoint1.Value;
                var p2 = _clipPlanePoint2.Value;
                var p3 = _clipPlanePoint3.Value;
                var points = new[] {p1, p2, p3};

                foreach (var p in points)
                {
                    const int size = 8;
                    var spos = camera.WorldToScreen(p);
                    var rect = new Rectangle((int)spos.X - size / 2, (int)spos.Y - size / 2, size, size);

                    graphics.FillRectangle(Brushes.White, rect);
                    graphics.DrawRectangle(Pens.Black, rect);
                }
            }
        }
    }
}
