### MARS AR Foundation Providers
Use MARS AR Foundation Providers to support MARS applications on smartphones and AR HMDs. This package contains a set of wrapper classes for AR Foundation to manage the AR Session and update the MARS database with device data.

This package integrates with [AR Foundation](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@latest) to provide MARS support for [ARKit (iOS)](https://docs.unity3d.com/Packages/com.unity.xr.arkit@latest), [ARCore (Android)](https://docs.unity3d.com/Packages/com.unity.xr.arcore@latest), [Magic Leap (Lumin OS)](https://docs.unity3d.com/Packages/com.unity.xr.magicleap@latest) and [Hololens (WSA)](https://docs.unity3d.com/Packages/com.unity.xr.windowsmr@latest)

The following features are available on each platform:
- ARKit
  - Camera tracking
  - Plane finding
  - Point cloud
  - Raycasting
  - Image markers
  - Face tracking
  - Light estimation
  - Meshing


- ARCore
  - Camera tracking
  - Plane finding
  - Point cloud
  - Raycasting
  - Image markers
  - Face tracking
  - Light estimation


- Magic Leap
  - Camera tracking
  - Plane finding
  - Raycasting


- Hololens
  - Camera tracking
  - Meshing

### AR Foundation compatibility
This package is built to work with most versions of AR Foundation, allowing users to select the best option based on their project needs. Any minor version of AR Foundation 2.x, 3.x or 4.x should work. This package has been tested specifically with the following versions and their matching XR plug-ins:
- 2.1.8
- 3.0.1
- 3.1.3
- 4.0.0-preview.3

**Note:** As of version 1.1.0, this package no longer directly depends on AR Foundation. The MARS Installer will automatically install AR Foundation for new installs, but users upgrading existing projects must add the desired AR Foundation version to the project manifest to maintain AR functionality.

### Platform plug-ins
To function on devices in Player builds, AR Foundation requires a platform plug-in (for example, `com.unity.xr.arcore` for AR Core on Android). For ARKit support on iOS, use `com.unity.xr.arkit`. Face tracking on ARKit requires an additional package: `com.unity.xr.arkit-face-tracking`. For the most part, there is an XR plug-in version that matches the AR Foundation version, but there are some exceptions. Unity recommends using the verified version in most cases. This package was tested with the following versions in Unity 2019.4.15f1:

- `com.unity.xr.arfoundation@2.1.10`
  - `com.unity.xr.arcore@2.1.8`
  - `com.unity.xr.arkit@2.1.9`
  - `com.unity.xr.arkit-face-tracking@1.0.7`
  - `com.unity.xr.windowsmr@2.0.3`
  - `com.unity.xr.magicleap@4.1.3`

Note that 2019-compatible Magic Leap and Windows Mixed Reality plugins only exist for AR Foundation 2.x. You will not be able to make HoloLens or Magic Leap builds if you are using AR Foundation 3.0+ and Unity 2019.

Remember to check your project settings for permissions and required player settings based on what your Project needs. For example, plane detection on Magic Leap requires the `WorldReconstruction` permission.

**Note:** The version of the AR Foundation package you're using has to match with the version of ARKit, ARCore, Magic Leap or Hololens unless noted otherwise. If the versions don't match, this leads to compiler errors. For example, if you are using AR Foundation 4.0.0 preview 3 and ARKit, you must ensure that ARKit is set to version 4.0.0 preview 3 as well.
