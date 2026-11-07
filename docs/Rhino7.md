# Rhino 7 相容測試版 / Compatibility build

使用 .NET Framework 4.8 / AnyCPU 及 RhinoCommon 7.0.20314.3001。
ViewCube、Focus/Back/Flip 與 C 工作平面正對開關保留。Rhino 7 沒有 Rhino 8 Auto CPlane，C 僅跟隨手動/世界工作平面變更。
macOS 使用 Rhino 7 開啟 macrhi；Windows 使用 Rhino 7 Yak CLI 安裝 rh7_0-win.yak。此為未實機驗證的測試版本。

Targets .NET Framework 4.8 / AnyCPU against RhinoCommon 7.0.20314.3001.
ViewCube, Focus/Back/Flip and C remain available. Rhino 7 has no Rhino 8 Auto CPlane; C follows manual/world plane changes.
Open the macrhi with Rhino 7 on macOS; install the rh7_0-win.yak using Rhino 7 Yak CLI on Windows. Runtime testing has not been performed.

## Build

Download the official RhinoCommon 7.0.20314.3001 NuGet package, extract it into work/rhino7-sdk, then run:

```sh
dotnet build ononSmartView.csproj -c Release -p:RhinoVersion=7 -p:Version=0.1.0-beta.3
```

Alternatively pass -p:RhinoCommonPath=/absolute/path/to/Rhino7/RhinoCommon.dll. Rhino 8 remains the default target.
