# SmartView toolbar / 工具列

按鈕：顯示／隱藏 ViewCube。指令：`_SmartViewCube`。
圖示為灰色圓框與立方體，提供淺色／深色 SVG 及透明 PNG。

在 Rhino 9 的工具列設定中開啟 `ononSmartView.rui`，顯示 SmartView 工具列，即可點擊圖示切換 ViewCube。

Open `ononSmartView.rui` in Rhino 9 toolbar settings and show the SmartView toolbar. Click its cube icon to toggle ViewCube.
The icon includes light/dark SVG variants and transparent PNG exports. Requires the plug-in version with the `SmartViewCube` command (beta.6+).

Rebuild UI assets with `python3 scripts/build-toolbar.py`.
