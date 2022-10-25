## MARS and AR Foundation

The MARS AR Foundation providers package supports the MARS package for creating applications on smartphones and AR HMDs. Within this package you will find a set of wrapper classes for AR Foundation to manage an AR Session and update it accordingly in the MARS database with data received from the device.

With this package you can make MARS's simulation system be available as an [AR Subsystem](https://docs.unity3d.com/Packages/com.unity.xr.arsubsystems@3.1/manual/index.html), allowing you to run AR Foundation projects in the Editor using simulated data. For more information about this, you can check the [Using MARS Simulation with AR Foundation based applications](MarsSimulation.md) section.

Check the [Mars AR Foundation providers](MarsARFoundationProviders.md) section to understand this package's compatibility and plugins.

## Installation

This package is intended to be installed during [MARS](https://docs.unity3d.com/Packages/com.unity.mars@latest) installation, and shouldn't be installed individually from Package Manager.

## Package contents

The following table describes the package folder structure under `Packages/MARS AR Foundation Providers/`:

|**Location**|**Description**|
|---|---|
|Default Island.asset|A [functionality island](https://docs.unity3d.com/Packages/com.unity.mars@latest/index.html?subfolder=/manual/SoftwareDevelopmentGuide.html%23functionality-islands) that contains all of the AR Foundation providers as defaults. Use this to ensure that these providers are used if multiple packages provide overlapping functionality.|
|Editor|Contains Unity.ARFoundationProviders.Editor assembly definition and related Editor code.|
|Runtime|Contains Unity.ARFoundationProviders assembly definition and related runtime code.|

## Requirements

This version of MARS AR Foundation Providers is compatible with the following versions of the Unity Editor:

* 2019.4 and later (recommended)

## Known limitations

MARS AR Foundation Providers version 1.4.0 includes the following known limitations:

* HoloLens support is limited to camera tracking.
* Camera facing direction is not implemented for AR Foundation 4.x.
* The `Force Multipass` option is required Magic Leap to work around an issue where content is only drawn in one eye. Enable this from the **Project Settings** window (menu: **Project Settings &gt; XR Plugin Management &gt; Magic Leap Settings**), which is available when the Magic Leap XR plug-in is installed.
