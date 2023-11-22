using gView.Framework.Geometry;

namespace gView.Blazor.Core.Extensions;

static public class GeometryExtensions
{
    static public IEnvelope? TryUnion(this IEnvelope? envelope, IEnvelope? candidate)
    {
        if (candidate is null)
        {
            return envelope;
        }

        if (envelope is null)
        {
            return candidate;
        }

        var union = new Envelope(envelope);
        union.Union(candidate);

        return union;   
    }
}
