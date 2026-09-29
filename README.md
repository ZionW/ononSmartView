# ononSmartView

**Rhino 8 viewport navigation plug-in / Rhino 8 視圖導覽外掛**

![ononSmartView ViewCube in Rhino 8](ononSmartViewCube.png)

ononSmartView is an experimental C# / RhinoCommon plug-in for focusing on Brep faces, facing Rhino's active construction plane, and navigating with a lightweight ViewCube. It targets .NET 8 and AnyCPU and does not use WPF or Windows Forms.

ononSmartView 是一個實驗中的 C# / RhinoCommon 外掛，可正視 Brep 曲面、跟隨 Rhino 目前的工作平面，並透過輕量 ViewCube 導覽。專案以 .NET 8 和 AnyCPU 為目標，不使用 WPF 或 Windows Forms。

> **Experimental beta:** Back up important models before use. The plug-in has been tested on macOS with Rhino 8.35. The Windows package is built, but Windows loading and behavior have not been verified. The latest face-axis alignment changes still need additional in-Rhino verification.
>
> **測試版：** 使用前請先備份重要模型。外掛曾在 macOS Rhino 8.35 測試；Windows 套件已建置，但尚未在 Windows Rhino 驗證載入與執行情況。最新的曲面軸向校準仍需進一步在 Rhino 內確認。

## Features / 功能

- Select a Brep face to set the construction plane and animate the camera to a face-on parallel view. Planar faces are preferred; curved faces use a tangent frame at the picked point.
- 選取 Brep 面後設定工作平面，並以動畫切換至平行正視。優先使用平面；曲面會依選取點建立切線座標框架。
- `ononSmartViewBack` restores the camera, projection, target, lens, and construction plane captured before focus. `ononSmartViewFlip` turns the focused view around.
- `ononSmartViewBack` 還原聚焦前記錄的相機、投影、目標點、鏡頭與工作平面；`ononSmartViewFlip` 翻轉正視方向。
- The ViewCube supports face, edge, and corner snaps, drag rotation, quarter-turn buttons, and a reset button. In Perspective, reset returns to the view recorded immediately before the current ViewCube session; in standard views, it restores that view's default direction and frames the model.
- ViewCube 支援點選面、邊、角切換視角、拖曳旋轉、水平轉 90°，以及重設按鈕。Perspective 重設會回到本次 ViewCube 操作前記錄的視角；標準視圖則回復預設方向並重新取景。
- Click the circular **C** button below the ViewCube to turn automatic work-plane facing on or off. The button only changes the setting: clicking it does not move or restore the camera. When on, the camera follows the next change to the active construction plane, including Object and World CPlanes. A green button means on. The Perspective viewport stays in perspective for gumball move or push/pull operations. The choice is saved; the plug-in does not change Rhino's Auto CPlane preference.
- 點擊 ViewCube 下方的圓形 **C** 按鈕，即可開關自動正對工作平面。按鈕只切換設定，點擊當下不移動或還原相機。開啟後，從下一次工作平面變動開始跟隨，包含物件與世界工作平面；綠色表示開啟。Perspective 視窗維持透視投影，方便使用操作軸移動或推拉。選擇會被保存，外掛不會更改 Rhino 的自動工作平面偏好設定。

## Commands / 指令

| Command | Description |
| --- | --- |
| `ononSmartView` | Select a Brep face and focus it. / 選取 Brep 面並正視。 |
| `ononSmartViewBack` | Restore the camera and construction plane before focus. / 還原聚焦前的相機與工作平面。 |
| `ononSmartViewFlip` | Flip the focused view. / 翻轉正視方向。 |
| `ononSmartViewCube` | Toggle the ViewCube. / 顯示或隱藏 ViewCube。 |
| `ononSmartViewAutoFace` | Toggle camera alignment to the active construction plane; same as the circular C button. / 開關相機正對目前工作平面；與圓形 C 按鈕相同。 |
| `ononSmartViewFocusFace` | Scriptable face-focus entry point; prompts for Brep GUID, face index, and viewport name. / 腳本用正視指令，依序輸入 Brep GUID、面索引與視窗名稱。 |

## Install on macOS / macOS 安裝

1. Download [`ononSmartView.macrhi`](ononSmartView.macrhi).
2. Open the `.macrhi` package with Rhino 8 and restart Rhino if prompted.
3. If an older `SmartView` plug-in is installed, remove it first to avoid duplicate ViewCubes or Auto CPlane listeners.

1. 下載 [`ononSmartView.macrhi`](ononSmartView.macrhi)。
2. 使用 Rhino 8 開啟 `.macrhi` 安裝套件；若有提示，請重新啟動 Rhino。
3. 若已安裝舊版 `SmartView`，請先移除，以免重複顯示 ViewCube 或同時監聽自動工作平面。

If the camera still faces the construction plane while **C** is off, check Rhino's plug-in list for the older `SmartView`. It has its own Auto CPlane camera listener. Disable it and restart Rhino; only `ononSmartView` should be loaded.

如果 **C** 已關閉，相機仍會正對工作平面，請檢查 Rhino 外掛清單是否仍載入舊版 `SmartView`。它有獨立的 Auto CPlane 相機跟隨器。停用舊版並重新啟動 Rhino，讓 `ononSmartView` 單獨載入。

The package is unsigned. Depending on macOS security settings, Rhino may ask you to confirm loading it.

此安裝套件未簽署。macOS 安全性設定可能會要求你確認是否載入。

## Install on Windows / Windows 安裝

The Windows build is an AnyCPU .NET 8 Rhino plug-in. Download either the [`.rhi` installer](ononSmartView.rhi) or the [`.rhp` assembly with its dependency file](ononSmartView-windows-manual.zip). If the Rhino Installer Engine does not handle `.rhi` on your system, extract both files from `manual-load` into the same folder, then use Rhino's `PlugInManager` command to install/load `ononSmartView.rhp`. Restart Rhino after loading.

Windows 版本是 AnyCPU 的 .NET 8 Rhino 外掛。可下載 [`.rhi` 安裝套件](ononSmartView.rhi)，或下載[`.rhp` 外掛與相依檔](ononSmartView-windows-manual.zip)。若系統上的 Rhino Installer Engine 無法處理 `.rhi`，請將 `manual-load` 中兩個檔案解壓縮到同一資料夾，再使用 Rhino 的 `PlugInManager` 指令安裝／載入 `ononSmartView.rhp`。載入後請重新啟動 Rhino。

The `.rhi` installer uses Rhino's legacy installer format, which McNeel no longer actively develops. The manual `PlugInManager` route is included as a fallback. See McNeel's [Windows plug-in installer guide](https://developer.rhino3d.com/guides/rhinocommon/plugin-installers-windows/) and [Rhino Installer Engine guide](https://developer.rhino3d.com/guides/general/rhino-installer-engine/).

`.rhi` 安裝套件採用 Rhino 舊式安裝格式，McNeel 已不再積極維護；因此也提供 `PlugInManager` 手動載入方式。請參考 McNeel 的[Windows 外掛安裝說明](https://developer.rhino3d.com/guides/rhinocommon/plugin-installers-windows/)與 [Rhino Installer Engine 說明](https://developer.rhino3d.com/guides/general/rhino-installer-engine/)。

## Build / 編譯

Requirements: Rhino 8 with RhinoCommon, and the .NET 8 SDK.

需求：安裝 Rhino 8（含 RhinoCommon）與 .NET 8 SDK。

```sh
dotnet build ononSmartView.csproj -c Release
```

The project defaults to RhinoCommon and the .NET runtime bundled with Rhino 8 on macOS. Override `RhinoCommonPath` and `RhinoRuntimePath` when building with a different Rhino installation. On Windows, build with the .NET 8 SDK and the Rhino 8 `System/RhinoCommon.dll` reference. `scripts/build-windows.ps1` builds the plug-in and creates the `.rhi` and `.rhp` files. Windows runtime behavior has not been verified.

專案預設使用 macOS Rhino 8 隨附的 RhinoCommon 與 .NET runtime。若使用其他 Rhino 安裝位置，請覆寫 `RhinoCommonPath` 和 `RhinoRuntimePath`。Windows 編譯請使用 .NET 8 SDK 與 Rhino 8 的 `System/RhinoCommon.dll`。`scripts/build-windows.ps1` 會編譯外掛並產生 `.rhi` 與 `.rhp` 套件；Windows 執行情況尚未驗證。

```powershell
.\scripts\build-windows.ps1
# Optional custom RhinoCommon path:
.\scripts\build-windows.ps1 -RhinoCommonPath "C:\Program Files\Rhino 8\System\RhinoCommon.dll"
```

## Testing / 測試

`scripts/ononSmartViewSmokeTest.py` creates a box and a slanted planar face in Rhino for manual command checks. It does not automate face picking or camera assertions.

`scripts/ononSmartViewSmokeTest.py` 會在 Rhino 建立方盒與斜面，供手動檢查指令使用；它不會自動選面或驗證相機狀態。

On macOS Rhino 8.35, Rhino MCP verified the C switch with the legacy plug-in disabled: picking a planar surface and a circle changed the Auto CPlane while C was off, but the camera location, target, and up vector stayed unchanged. Turning C on did not immediately move the camera; the next plane change aligned it; turning C off preserved that view.

在 macOS Rhino 8.35 中停用舊版外掛後，已用 Rhino MCP 驗證 C 開關：C 關閉時點選平面曲面與圓曲線，Auto CPlane 會改變工作平面，但相機位置、目標與上方向不變。開啟 C 的當下相機不移動；下一次切換工作平面才正對；再次關閉 C 會保留當下視角。

## License / 授權

No license has been selected yet. All rights are reserved by default. Contact the author before redistributing or modifying this project.

目前尚未指定開源授權，預設保留所有權利。重新散布或修改前請先聯絡作者。
