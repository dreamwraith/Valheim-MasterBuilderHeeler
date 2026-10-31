using System;
using MasterBuilderHeeler.Configuration;

namespace MasterBuilderHeeler.Core.Visuals.Gizmo
{
    // Note: This interface is not fully fleshed out - right now lots of stuff is just going direct into the one theme I have built.
    // So its mostly pointless placeholder for now - but eventually i will refactor to be more integrated and theme-agnostic. For now just a stub.
    public interface IGizmoRenderer : IDisposable
    {
        GizmoStyle Style { get; }

        float Scale { get; set; }

        void Render(in GizmoDrawContext context);

        void OnHide();
    }
}
