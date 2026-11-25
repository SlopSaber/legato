namespace Legato.Injection
{
    using Zenject;

    internal static class ComponentBinding
    {
        internal static NameTransformScopeConcreteIdArgConditionCopyNonLazyBinder OnNewGameObject(FromBinder binder)
        {
#if !LEGATO_NATIVE_ZENJECT_COMPONENT_BINDER
            // Match the native deferred provider while retaining context parenting and injection.
            var parameters = new GameObjectCreationParameters();
            BindingUtil.AssertIsComponent(binder.ConcreteTypes);
            BindingUtil.AssertIsNotAbstract(binder.ConcreteTypes);
            binder.BindInfo.RequireExplicitScope = true;
            binder.SubFinalizer = new ScopableBindingFinalizer(
                binder.BindInfo,
                (container, type) => new AddToNewGameObjectComponentProvider(
                    container,
                    type,
                    binder.BindInfo.Arguments,
                    parameters,
                    binder.BindInfo.ConcreteIdentifier,
                    binder.BindInfo.InstantiatedCallback));
            return new NameTransformScopeConcreteIdArgConditionCopyNonLazyBinder(binder.BindInfo, parameters);
#else
            return binder.FromNewComponentOnNewGameObject();
#endif
        }
    }
}
