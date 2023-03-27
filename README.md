# LivingWorld - AR Foundation

AR project that uses [*AR Foundation 5.0*](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@5.0/manual/index.html) for an image tracking application with an accompanying server to add and remove image targets at runtime across multiple devices.

This application relies on three Unity packages:

* Google ARCore XR Plug-in ([documentation](https://docs.unity3d.com/Packages/com.unity.xr.arcore@5.0/manual/index.html))
* Apple ARKit XR Plug-in ([documentation](https://docs.unity3d.com/Packages/com.unity.xr.arkit@5.0/manual/index.html))
* ARFoundation ([documentation](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@5.0/manual/index.html))

A remote REST API server to access and download the image targets is required. The code to set up your own Node.js API server is available at:

https://github.com/yamgo2/LivingWorld-Server

## Features

This project uses AR Foundation's [*MutableRuntimeReferenceImageLibrary*](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@5.0/api/UnityEngine.XR.ARSubsystems.MutableRuntimeReferenceImageLibrary.html) to construct a reference library from remote image targets at runtime.

* New image targets can be added via the smartphone camera or from the gallery
* Image targets become available to all other users
* Exisiting image targets can be linked with Youtube links or direct links to video files
* Videos can be played on an image target's surface in the AR space
* Videos adjust their format to the image target's size
* A list of all available content can be accessed for each image target
* Users and libraries can be managed via the REST API and accompanying MySQL database
* A local mode for only the device can be accessed if the server is not reachable
* Comes with a responsive and modern UI based on established AR design principles
* Compatible with iOS and Android devices

![UI example 1](https://i.imgur.com/P5zpAXg.jpg)
![UI example 2](https://i.imgur.com/auIaA5o.jpg)

## How to use

1. Install Unity 2022.2 or later and clone this repository

2. Open the Unity project at the root of this repository

3. Wait for the dependencies to finish downloading

4. Open the LivingWorld scene in the Scenes folder

5. Set the REST API URL in the DataManager object

6. Go to [Build Settings](https://docs.unity3d.com/Manual/BuildSettings.html), select your target platform, and build the scene

## Used Resources

* UnityImageCropper ([GitHub](https://github.com/yasirkula/UnityImageCropper), [Unity Asset Store](https://assetstore.unity.com/packages/tools/gui/image-cropper-116650))
* NativeGallery ([GitHub](https://github.com/yasirkula/UnityNativeGallery), [Unity Asset Store](https://assetstore.unity.com/packages/tools/integration/native-gallery-for-android-ios-112630))
* HologramShader ([GitHub](https://github.com/andydbc/HologramShader))

## Community and Feedback

This project can be freely modified and used in any context. It is meant to be added upon and serve as a foundation for further development. The ultimate goal is a centralized platform that enables users to create and upload their own AR tracking targets and associate them with virtual content. The platform would act as a single entry point, providing a server and database that allows for easy access to all user-generated AR content. This centralized approach would facilitate the discovery and sharing of AR experiences, as users can access all available content from one hub.

If you you have a question, find a bug, or would like to request a new feature, please [submit a GitHub issue](https://github.com/yamgo2/LivingWorld-ARFoundation/issues).
