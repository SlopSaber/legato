using Zenject;

namespace Legato.Injection {
    internal static class ComponentBinding {
        internal static NameTransformScopeConcreteIdArgConditionCopyNonLazyBinder OnNewGameObject(FromBinder binder) {
#if BEAT_SABER_1_29_0 || BEAT_SABER_1_37_1 || BEAT_SABER_1_38_0 || (BEAT_SABER_1_40_0 && !LEGATO_UNITY_6_OBJECT_QUERIES)
            // Match the native deferred provider while retaining context parenting and injection.
            var parameters = new GameObjectCreationParameters();
            BindingUtil.AssertIsComponent(binder.ConcreteTypes);
            BindingUtil.AssertIsNotAbstract(binder.ConcreteTypes);
            binder.BindInfo.RequireExplicitScope = true;
            binder.SubFinalizer = new ScopableBindingFinalizer(binder.BindInfo,
                (container, type) => new AddToNewGameObjectComponentProvider(container, type,
                    binder.BindInfo.Arguments, parameters, binder.BindInfo.ConcreteIdentifier,
                    binder.BindInfo.InstantiatedCallback));
            return new NameTransformScopeConcreteIdArgConditionCopyNonLazyBinder(binder.BindInfo, parameters);
#else
            return binder.FromNewComponentOnNewGameObject();
#endif
        }
    }
}
