using System;
using MasterBuilderHeeler.Configuration;

namespace MasterBuilderHeeler.Core.Visuals.Gizmo
{
    public interface IGizmoRenderer : IDisposable
    {
        GizmoStyle Style { get; }

        void Render(in GizmoDrawContext context);

        void OnHide();
    }
}
