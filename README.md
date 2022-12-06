# AsketchR Prototype

Mobile AR App for creating drawings in 3D space. Create drawings using multiple different interaction methods. 
Your creations can be persistently anchored in the real world and reloaded at any time by creating Cloud Anchors with Azure Spatial Anchors (ASA).

Tested with iOS only.

## Examples

![Examples](examples.png)


## Used plugins

- VRSketchingGeometry (see repo [here](https://github.com/tterpi/VRSketchingGeometry))


## Install

To use any of the anchoring functionality, you need to install some additional packages.

1. Download the ASA SDK Core and your platform specific package from the provided links
2. In Unity, open the Package Manager (Window>Package Manager)
3. Add the packages via "Add package from tarball" by selecting the downloaded ```.tgz``` file

SDK Core:
- com.microsoft.azure.spatial-anchors-sdk.core-2.12.0 ([download](https://dev.azure.com/aipmr/MixedReality-Unity-Packages/_artifacts/feed/Unity-packages/Npm/com.microsoft.azure.spatial-anchors-sdk.core/overview/2.12.0))

Platform-specific:
- com.microsoft.azure.spatial-anchors-sdk.ios-2.12.0 ([download](https://dev.azure.com/aipmr/MixedReality-Unity-Packages/_artifacts/feed/Unity-packages/Npm/com.microsoft.azure.spatial-anchors-sdk.ios/overview/2.12.0))
- com.microsoft.azure.spatial-anchors-sdk.android-2.12.0 ([download](https://dev.azure.com/aipmr/MixedReality-Unity-Packages/_artifacts/feed/Unity-packages/Npm/com.microsoft.azure.spatial-anchors-sdk.android/overview/2.12.0))
