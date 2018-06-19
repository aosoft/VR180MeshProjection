VR180 Mesh Projection Box Parser
====

Copyright (c) Yasuhiro Taniuchi  

## About

This is a program to read Metadata for VR180 Video File. 

The VR180 camera records "Mesh Projection Box" Metadata for mapping the video to the 3D Mesh. This Unity Project also includes processing to convert Metadata to Unity Mesh for visualization.

* Source code list
    * MeshProjeectionBox.cs - Parsing binary data of Mesh Projection Box.
    * BoxHeader.cs - Analyze the MP4 file.
    * VR180Mesh.cs - Convert the Mesh Projection Box to Mesh and play video.
* This Unity Project targets 2017.4.3f1. 
    * MeshProjectionBox.cs, BoxHeader.cs are not dependent on Unity.
    * This project may work on older versions of Unity.

## How to use

1. Set the local source path to URL property of VideoPlayer that is attached at "VR180 Video Player" GameObject.
2. Run.

### Notes

* Implementation is insufficient. (error handling etc.)
* StereoView.shader is implemented so that it can be seen stereoscopically if it is set to use VR HMD with XR Setting. 

## License

zlib/libpng License

