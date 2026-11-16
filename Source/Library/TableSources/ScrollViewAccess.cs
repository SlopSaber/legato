using HMUI;
using IPA.Utilities;

namespace Legato.UI {
    internal static class ScrollViewAccess {
        internal static void RefreshButtons(ScrollView view) =>
            view.InvokeMethod<object, ScrollView>("RefreshButtons");
    }
}
