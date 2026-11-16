# Tweaks55 native providers

These sources derive from the MIT-licensed maintained Tweaks55 source at signed commit 0ee135c. Original patch files remain in the consumer repository and are excluded only when the provider is selected. Shared sources select native analytics, haptic and focus hooks while retaining mod-owned settings and OpenVR pose handling. Legacy focus handling is limited to controller objects and retains native event subscription ownership. Compilation and metadata inspection do not establish runtime compatibility.
