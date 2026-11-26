#if LEGATO_BEFORE_MULTIPLAYER
using HMUI;

namespace Legato.UI;

internal abstract class LegacyFlowCoordinator : FlowCoordinator
{
    public sealed override void DidActivate(bool firstActivation, ActivationType activationType) =>
        DidActivate(firstActivation, activationType == ActivationType.AddedToHierarchy, false);

    public sealed override void DidDeactivate(DeactivationType deactivationType) =>
        DidDeactivate(deactivationType == DeactivationType.RemovedFromHierarchy, false);

    public abstract void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling);

    public virtual void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling) =>
        base.DidDeactivate(removedFromHierarchy ? DeactivationType.RemovedFromHierarchy : DeactivationType.NotRemovedFromHierarchy);

    protected void SetTitle(string value) => title = value;
}
#endif
