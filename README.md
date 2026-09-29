# ononSmartView
**Experimental Rhino 8 viewport navigation plug-in** · [繁體中文版](README.zh-TW.md)

![ononSmartView running in Rhino 8](ononSmartViewCube.png)

ononSmartView is a C# / RhinoCommon plug-in for focusing on Brep faces, aligning the camera to Rhino’s active construction plane, and navigating with a lightweight ViewCube. It targets .NET 8 and AnyCPU; it does not use WPF or Windows Forms.

> **TEST VERSION — incomplete and subject to change.** This plug-in still has known gaps and unverified behavior. It has been tested on macOS with Rhino 8.35; the Windows package has been built but not tested in Windows Rhino. Do not rely on it for production work, and back up important models.

## Features

- Select a Brep face to set the construction plane and animate the camera to a face-on parallel view. Planar faces are preferred; curved faces use a tangent frame at the picked point.
- `ononSmartViewBack` restores the camera, projection, target, lens, and construction plane saved before focus. `ononSmartViewFlip` turns the focused view around.
- The ViewCube supports face, edge, and corner snaps, drag rotation, quarter-turn buttons, and reset. Perspective reset returns to the view saved just before the current ViewCube session; standard views return to their default direction and frame the model.
- The circular **C** button toggles automatic camera alignment to the active construction plane. It only changes the setting; clicking it does not move the camera. When enabled, the camera follows the next plane change, including Object and World CPlanes. Perspective remains perspective for gumball operations. The plug-in does not change Rhino’s Auto CPlane preference.

## Commands

| Command | Description |
| --- | --- |
| `ononSmartView` | Select a Brep face and focus it. |
| `ononSmartViewBack` | Restore the camera and construction plane from before focus. |
| `ononSmartViewFlip` | Flip the focused view. |
| `ononSmartViewCube` | Toggle the ViewCube. |
| `ononSmartViewAutoFace` | Toggle camera alignment to the active construction plane; same as the C button. |
| `ononSmartViewFocusFace` | Scriptable entry point; prompts for Brep GUID, face index, and viewport name. |

## Install on macOS

1. Download [ononSmartView.macrhi](ononSmartView.macrhi).
2. Open the `.macrhi` installer with Rhino 8 and restart Rhino if prompted.
3. Remove any older `SmartView` plug-in first to avoid duplicate ViewCubes or Auto CPlane listeners.

The installer is unsigned, so macOS security settings may ask you to confirm loading it.

## Install on Windows

The Windows package targets AnyCPU and .NET 8. Download [ononSmartView.rhi](ononSmartView.rhi). If Rhino’s installer engine does not open it, download [the manual-load archive](ononSmartView-windows-manual.zip), extract both files into one folder, then install/load `ononSmartView.rhp` with Rhino’s `PlugInManager` command. Restart Rhino after loading.

The `.rhi` installer uses Rhino’s legacy installer format. See McNeel’s [Windows plug-in installer guide](https://developer.rhino3d.com/guides/rhinocommon/plugin-installers-windows/) and [Rhino Installer Engine guide](https://developer.rhino3d.com/guides/general/rhino-installer-engine/).

## Build

Requirements: Rhino 8 with RhinoCommon and the .NET 8 SDK.

```sh
dotnet build ononSmartView.csproj -c Release
```

On Windows, use Rhino 8’s `System/RhinoCommon.dll` reference. `scripts/build-windows.ps1` builds the plug-in and creates the `.rhi` and `.rhp` packages.

## Testing and limitations

The smoke-test script creates a box and a slanted face for manual checks; it does not automate face picking or camera assertions. Rhino MCP testing on macOS Rhino 8.35 verified the C toggle with the legacy plug-in disabled. Windows behavior and the latest face-axis alignment still need in-Rhino verification. This is a test version: some features and documentation may be incomplete or inaccurate, and may change as issues are found.

## License

No license has been selected. All rights are reserved by default. Contact the author before redistributing or modifying this project.
