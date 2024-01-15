# Remote Rendering For Godot 4
A Remote Rendering（Cloud Rendering）solution for Godot4, based on C#. It is similar to [Unity Render Streaming]([Unity-Technologies/UnityRenderStreaming: Streaming server for Unity (github.com)](https://github.com/Unity-Technologies/UnityRenderStreaming)), which provides high quality rendering abilities via browser.



## Demo

<video src="demo/demo.mp4"></video>



## Description

This project aims to provide a **remote cloud rendering solution** for Godot 4.0 and above versions. Developers can use this project to dynamically deploy Godot Engine projects to web browsers in real-time. Furthermore, the project allows receiving user inputs (such as keyboard, mouse, and touch gestures) from the web interface. Data transmission is accomplished using the WebRTC protocol, and audio-video stream encoding utilizes the FFmpeg.AutoGen wrapper.

**Key Features:**

+ **Lightweight Design:** The project integrates a lightweight WebRTC library, [libdatachannel]([paullouisageneau/libdatachannel: C/C++ WebRTC network library featuring Data Channels, Media Transport, and WebSockets (github.com)](https://github.com/paullouisageneau/libdatachannel)), introduced as a DLL file into the C# project. This design choice avoids the use of Google's extensive WebRTC library. For encoding and decoding, FFmpeg encoder is employed, with calls facilitated through the [FFmpeg.AutoGen]([Ruslan-B/FFmpeg.AutoGen: FFmpeg auto generated unsafe bindings for C#/.NET and Core (Linux, MacOS and Mono). (github.com)](https://github.com/Ruslan-B/FFmpeg.AutoGen)) library.
+ **Simplicity:** The project architecture follows the paradigm of `Unity Render Streaming` (with significant simplifications due to limitations in my capabilities and time). However, it encompasses essential cloud rendering logic, serving as an excellent example for understanding cloud rendering and streaming transfer.



## Requirements

To run this project, you need to prepare the following components.

#### libdatachannel

[paullouisageneau/libdatachannel: C/C++ WebRTC network library featuring Data Channels, Media Transport, and WebSockets (github.com)](https://github.com/paullouisageneau/libdatachannel)

Build this repository and put the .dll file into out root directory (the directory of Godot project). The C# file `/WebRTC/WebRTC.cs` will import it (use `[DllImport(WebRTC.Lib)]`).

#### FFmpeg.AutoGen

[Ruslan-B/FFmpeg.AutoGen: FFmpeg auto generated unsafe bindings for C#/.NET and Core (Linux, MacOS and Mono). (github.com)](https://github.com/Ruslan-B/FFmpeg.AutoGen)

For convenient package management, you can use NuGet to manage external packages by installing the following two packages:

```
FFmpeg.AutoGen.Abstractions
FFmpeg.AutoGen.Bindings.DynamicallyLoaded
```

Secondly, you should put [.dll files](https://github.com/Ruslan-B/FFmpeg.AutoGen/tree/master/FFmpeg/bin/x64) in directory `/WebRTC/Codec/FFmpegLibraries` so FFmpeg.AutoGen can load FFmpeg functions properly.

#### Godot 4

The Godot Engine for this project should support C#. You can directly download the .NET version of Godot from [Download for Windows - Godot Engine](https://godotengine.org/download/windows/) or manually compile the source code with the Mono module enabled.

#### Project Settings

After creating your Godot project, you should set `AllowUnsafeBlocks` as true, cause `FFmpeg.AutoGen` utilizes pointer operations for memory manipulation, and since C# by default does not allow the use of pointers, it's necessary to enable unsafe blocks.

An example of the `*.csproj` file is as follows:

```xml
<Project Sdk="Godot.NET.Sdk/4.1.2">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <RootNamespace>Project</RootNamespace>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="FFmpeg.AutoGen.Abstractions" Version="6.1.0" />
    <PackageReference Include="FFmpeg.AutoGen.Bindings.DynamicallyLoaded" Version="6.1.0" />
  </ItemGroup>
</Project>
```

#### Notice

+ Theoretically, this project supports multiple platforms, but currently, due to limitations with dynamic linking libraries, development has been focused exclusively on the **Windows platform**.
+ Please ensure that the aforementioned preparations are done **using the SAME 32-bit or 64-bit architecture**. This repository is 64-bit.



## Structure

```
<root>
├── RemoteRendering				// C# files for RemoteRendering module
	├── InputSystem				// C# files for input-bindings from browser
├── WebRTC						// C# files for WebRTC-bindings
	├── Codec					// C# files for Codecs
		├── FFmpegLibraries		// .dll files for FFmpeg library
├── datachannel.dll				// .dll file compiled from the libdatachannel library
└── WebApp						// Web application for signaling
```







