using System;
using System.Collections.Generic;
using Sledge.BspEditor.Rendering.Viewport;
using Sledge.DataStructures.Geometric;
using Sledge.Rendering.Cameras;
using Sledge.Rendering.Scenes;
using Sledge.Rendering.Scenes.Elements;

namespace Sledge.BspEditor.Tools.Draggable
{
    public abstract class BaseDraggable : IDraggable
    {
        public event EventHandler DragStarted;
        public event EventHandler DragMoved;
        public event EventHandler DragEnded;

        protected virtual void OnDragStarted()
        {
            DragStarted?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnDragMoved()
        {
            DragMoved?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnDragEnded()
        {
            DragEnded?.Invoke(this, EventArgs.Empty);
        }

        public virtual void StartDrag(MapViewport viewport, ViewportEvent e, Coordinate position)
        {
            OnDragStarted();
        }

        public virtual void Drag(MapViewport viewport, ViewportEvent e, Coordinate lastPosition, Coordinate position)
        {
            OnDragMoved();
        }

        public virtual void EndDrag(MapViewport viewport, ViewportEvent e, Coordinate position)
        {
            OnDragEnded();
        }


        public virtual void MouseDown(MapViewport viewport, ViewportEvent e, Coordinate position)
        {

        }

        public virtual void MouseUp(MapViewport viewport, ViewportEvent e, Coordinate position)
        {
            
        }

        public abstract void Click(MapViewport viewport, ViewportEvent e, Coordinate position);
        public abstract bool CanDrag(MapViewport viewport, ViewportEvent e, Coordinate position);
        public abstract void Highlight(MapViewport viewport);
        public abstract void Unhighlight(MapViewport viewport);
        public abstract IEnumerable<SceneObject> GetSceneObjects();
        public abstract IEnumerable<Element> GetViewportElements(MapViewport viewport, PerspectiveCamera camera);
        public abstract IEnumerable<Element> GetViewportElements(MapViewport viewport, OrthographicCamera camera);
    }
}