using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Sledge.BspEditor.Rendering;
using Sledge.BspEditor.Rendering.Viewport;
using Sledge.BspEditor.Tools.State;
using Sledge.DataStructures.Geometric;
using Sledge.Rendering.Cameras;
using Sledge.Rendering.Scenes;
using Sledge.Rendering.Scenes.Elements;
using Line = Sledge.Rendering.Scenes.Renderables.Line;

namespace Sledge.BspEditor.Tools.ExampleStateTool
{
    public class FirstPointDrawnState : IState
    {
        public StateTool Owner { get; set; }
        public Coordinate FirstPoint { get; set; }
        public Coordinate SecondPoint { get; set; }

        public FirstPointDrawnState(StateTool owner, Coordinate firstPoint)
        {
            Owner = owner;
            FirstPoint = SecondPoint = firstPoint;
        }

        public IState GetNextState(StateEvent ev)
        {
            if (ev.Action == StateAction.MouseMove)
            {
                SecondPoint = ev.Viewport.ProperScreenToWorld(ev.ViewportEvent.Location);
            }
            else if (ev.Action == StateAction.MouseClick && ev.ViewportEvent.Button == MouseButtons.Left)
            {
                return new LineDrawnState(Owner, FirstPoint, ev.Viewport.ProperScreenToWorld(ev.ViewportEvent.Location));
            }
            return this;
        }

        public IEnumerable<SceneObject> GetSceneObjects()
        {
            yield return new Line(Color.Red, FirstPoint.ToVector3(), SecondPoint.ToVector3());
        }

        public IEnumerable<Element> GetViewportElements(MapViewport viewport, PerspectiveCamera camera)
        {
            yield break;
        }

        public IEnumerable<Element> GetViewportElements(MapViewport viewport, OrthographicCamera camera)
        {
            yield break;
        }
    }
}