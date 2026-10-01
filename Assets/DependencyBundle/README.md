# Package Structure dependency installer

Import the unitypackage into a Unity 6000.3 project, then open **Tools > Package Structure > Dependency Installer**. Install Unity MCP, UniTask, VContainer, and DOTween with their individual buttons. Each button fetches a pinned version from the official upstream source. UniTask and VContainer must be installed before enabling the Event Bus button.

The Event Bus button installs `com.kameryurdakull.eventbus` from `https://github.com/kameryurdakull/unity-eventbus.git?path=/Packages/com.kameryurdakull.eventbus#v1.0.0` through Unity Package Manager. No Event Bus source templates are bundled. When updating a project that used the previous installer, remove its old `Assets/Scripts/EventBus` source files and assembly definition before installing the Git package.

After DOTween imports, open **Tools > Demigiant > DOTween Utility Panel** and click **Setup DOTween...** as required by Demigiant.

This installer contains no local UPM tarballs. The online Git packages and DOTween download require network access. Unity MCP's external Python/uv server runtime must be configured separately in its setup window.
