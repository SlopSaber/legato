#if LEGATO_BEFORE_MULTIPLAYER
using HMUI;

namespace Legato.UI;

internal abstract class LegacyViewController : ViewController
{
    public sealed override void DidActivate(bool firstActivation, ActivationType activationType) =>
        DidActivate(firstActivation, activationType == ActivationType.AddedToHierarchy, false);

    public sealed override void DidDeactivate(DeactivationType deactivationType) =>
        DidDeactivate(deactivationType == DeactivationType.RemovedFromHierarchy, false);

    public virtual void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling) =>
        base.DidActivate(firstActivation, addedToHierarchy ? ActivationType.AddedToHierarchy : ActivationType.NotAddedToHierarchy);

    public virtual void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling) =>
        base.DidDeactivate(removedFromHierarchy ? DeactivationType.RemovedFromHierarchy : DeactivationType.NotRemovedFromHierarchy);
}
#endif
