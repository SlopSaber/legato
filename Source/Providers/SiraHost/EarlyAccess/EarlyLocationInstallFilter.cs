#if LEGATO_SIRA_EARLY_CONTEXTS
using System;
using System.Collections.Generic;
using Legato.Sira;
using Zenject;

namespace SiraUtil.Zenject.Internal.Filters
{
    internal sealed class EarlyLocationInstallFilter : IInstallFilter
    {
        private readonly Location _locations;

        internal EarlyLocationInstallFilter(Location locations)
        {
            _locations = locations;
        }

        public bool ShouldInstall(Context context, IEnumerable<Type> bindings)
        {
            return context is EarlySceneContext early && (_locations & early.Location) != Location.None;
        }
    }
}
#endif
