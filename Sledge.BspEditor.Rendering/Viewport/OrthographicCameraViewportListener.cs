﻿using System;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using Sledge.Common;
using Sledge.DataStructures.Geometric;
using Sledge.Rendering;
using Sledge.Rendering.Cameras;
using Sledge.Shell.Input;

namespace Sledge.BspEditor.Rendering.Viewport
{
    public class OrthographicCameraViewportListener : IViewportEventListener
    {
        public MapViewport Viewport { get; set; }

        private OrthographicCamera Camera => Viewport.Viewport.Camera as OrthographicCamera;

        public OrthographicCameraViewportListener(MapViewport viewport)
        {
            Viewport = viewport;
        }

        private const float ScrollStart = 1;
        private const float ScrollIncrement = 0.025f;
        private const int ScrollMaximum = 200;
        private const int ScrollPadding = 40;

        public void UpdateFrame(long frame)
        {
            return;//todo cross thread etc
            if (/*Viewport.Viewport.IsFocused &&*/ _mouseDown != null && Control.MouseButtons.HasFlag(MouseButtons.Left) && !KeyboardState.IsKeyDown(Keys.Space))
            {
                var pt = Viewport.Control.PointToClient(Control.MousePosition);
                var pos = Camera.Position;
                if (pt.X < ScrollPadding)
                {
                    var mx = ScrollStart + ScrollIncrement * Math.Min(ScrollMaximum, ScrollPadding - pt.X);
                    mx = mx * mx + ScrollStart;
                    pos.X -= mx / (float)Camera.Zoom;
                }
                else if (pt.X > Viewport.Width - ScrollPadding)
                {
                    var mx = ScrollStart + ScrollIncrement * Math.Min(ScrollMaximum, pt.X - (Viewport.Width - ScrollPadding));
                    mx = mx * mx + ScrollStart;
                    pos.X += mx / (float)Camera.Zoom;
                }
                if (pt.Y < ScrollPadding)
                {
                    var my = ScrollStart + ScrollIncrement * Math.Min(ScrollMaximum, ScrollPadding - pt.Y);
                    my = my * my + ScrollStart;
                    pos.Y += my / (float)Camera.Zoom;
                }
                else if (pt.Y > Viewport.Height - ScrollPadding)
                {
                    var my = ScrollStart + ScrollIncrement * Math.Min(ScrollMaximum, pt.Y - (Viewport.Height - ScrollPadding));
                    my = my * my + ScrollStart;
                    pos.Y -= my / (float)Camera.Zoom;
                }
                Camera.Position = pos;
            }
        }

        public bool IsActive()
        {
            return Viewport != null && Camera != null;
        }

        public void KeyUp(ViewportEvent e)
        {
            if (e.KeyCode == Keys.Space)
            {
                Viewport.Control.Cursor = Cursors.Default;
                Viewport.Control.Capture = false;
                e.Handled = true;
            }
        }

        public void KeyDown(ViewportEvent e)
        {
            if (e.KeyCode == Keys.Space)
            {
                Viewport.Control.Cursor = Cursors.SizeAll;
                if (false) //!Sledge.Settings.View.Camera2DPanRequiresMouseClick)
                {
                    Viewport.Control.Capture = true;
                    var p = e.Sender.Control.PointToClient(Cursor.Position);
                    _mouseDown = new Vector3(p.X, Viewport.Height - p.Y, 0);
                }
                e.Handled = true;
            }

            var moveAllowed = false; //DocumentManager.CurrentDocument != null && (DocumentManager.CurrentDocument.Selection.IsEmpty() || !Sledge.Settings.Select.ArrowKeysNudgeSelection);
            if (moveAllowed)
            {
                var shift = new Vector3(0, 0, 0);

                switch (e.KeyCode)
                {
                    case Keys.Left:
                        shift.X = (float) (-Viewport.Width / Camera.Zoom / 4);
                        break;
                    case Keys.Right:
                        shift.X = (float)(Viewport.Width / Camera.Zoom / 4);
                        break;
                    case Keys.Up:
                        shift.Y = (float)(Viewport.Height / Camera.Zoom / 4);
                        break;
                    case Keys.Down:
                        shift.Y = (float)(-Viewport.Height / Camera.Zoom / 4);
                        break;
                }

                Camera.Position += shift;
            }

            var str = e.KeyCode.ToString();
            if (str.StartsWith("NumPad") || str.StartsWith("D"))
            {
                var last = str.Last();
                if (Char.IsDigit(last))
                {
                    var press = (int) Char.GetNumericValue(last);
                    if (press >= 0 && press <= 9)
                    {
                        if (press == 0) press = 10;
                        var num = Math.Max(press - 6, 6 - press);
                        var pow = (float) Math.Pow(2, num);
                        var zoom = press < 6 ? 1 / pow : pow;
                        Camera.Zoom = (float) zoom;
                        // Mediator.Publish(EditorMediator.ViewZoomChanged, Camera.Zoom);
                    }
                }
            }
        }

        public void MouseMove(ViewportEvent e)
        {
            var lmouse = Control.MouseButtons.HasFlag(MouseButtons.Left);
            var mmouse = Control.MouseButtons.HasFlag(MouseButtons.Middle);
            var space = KeyboardState.IsKeyDown(Keys.Space);
            if (space || mmouse)
            {
                Viewport.Control.Cursor = Cursors.SizeAll;
                if (lmouse || mmouse /*|| !Sledge.Settings.View.Camera2DPanRequiresMouseClick*/)
                {
                    var point = new Vector3(e.X, Viewport.Height - e.Y, 0);
                    if (_mouseDown != null)
                    {
                        var difference = _mouseDown - point;
                        Camera.Position += (difference / (float)Camera.Zoom);
                    }
                    _mouseDown = point;
                    e.Handled = true;
                }
            }

            //var pt = Viewport.ProperScreenToWorld(new Vector3(e.X, e.Y, 0));
            //Mediator.Publish(EditorMediator.MouseVector3sChanged, pt);
        }

        public void MouseWheel(ViewportEvent e)
        {
            var before = Camera.Flatten(Camera.ScreenToWorld(new Vector3(e.X, e.Y, 0)));
            Camera.Zoom *= (float) DMath.Pow(/*Sledge.Settings.View.ScrollWheelZoomMultiplier*/ 1.4m, (e.Delta < 0 ? -1 : 1));
            var after = Camera.Flatten(Camera.ScreenToWorld(new Vector3(e.X, e.Y, 0)));
            Camera.Position -= (after - before);

            //Mediator.Publish(EditorMediator.ViewZoomChanged, Camera.Zoom);
            if (KeyboardState.IsKeyDown(Keys.ControlKey))
            {
                //Mediator.Publish(EditorMediator.SetZoomValue, Camera.Zoom);
            }
        }

        public void MouseUp(ViewportEvent e)
        {
            var space = KeyboardState.IsKeyDown(Keys.Space);
            var req = true;// Sledge.Settings.View.Camera2DPanRequiresMouseClick;
            if (space && (!req || e.Button == MouseButtons.Left))
            {
                e.Handled = true;
            }
            else if (e.Button == MouseButtons.Middle)
            {
                e.Handled = true;
                Viewport.Control.Cursor = Cursors.Default;
            }
        }

        private Vector3 _mouseDown;

        public void MouseDown(ViewportEvent e)
        {
            var space = KeyboardState.IsKeyDown(Keys.Space);
            var req = true; // Sledge.Settings.View.Camera2DPanRequiresMouseClick;
            if (space && (!req || e.Button == MouseButtons.Left))
            {
                e.Handled = true;
            }
            else if (e.Button == MouseButtons.Middle)
            {
                e.Handled = true;
                Viewport.Control.Cursor = Cursors.SizeAll;
            }
            _mouseDown = new Vector3(e.X, Viewport.Height - e.Y, 0);
        }

        public void MouseClick(ViewportEvent e)
        {
            
        }

        public void MouseDoubleClick(ViewportEvent e)
        {
            
        }

        public void DragStart(ViewportEvent e)
        {
            
        }

        public void DragMove(ViewportEvent e)
        {

        }

        public void DragEnd(ViewportEvent e)
        {

        }

        public void MouseEnter(ViewportEvent e)
        {
            if (KeyboardState.IsKeyDown(Keys.Space))
            {
                Viewport.Control.Cursor = Cursors.SizeAll;
            }
            //Mediator.Publish(EditorMediator.ViewFocused);
            //Mediator.Publish(EditorMediator.ViewZoomChanged, Camera.Zoom);
        }

        public void MouseLeave(ViewportEvent e)
        {
            Viewport.Control.Cursor = Cursors.Default;
            //Mediator.Publish(EditorMediator.ViewUnfocused);
        }

        public void ZoomChanged(ViewportEvent e)
        {

        }

        public void PositionChanged(ViewportEvent e)
        {

        }
    }
}
