"""Package successful Rhino 7/8/9 builds without copying Rhino SDK binaries."""
from pathlib import Path
import hashlib
import shutil
import subprocess
import xml.etree.ElementTree as ET
import zipfile

ROOT = Path(__file__).resolve().parent.parent
VERSION = ET.parse(ROOT/'ononSmartView.csproj').getroot().findtext('PropertyGroup/Version')
OUT = ROOT/'dist/releases'/f'v{VERSION}'
OUT.mkdir(parents=True, exist_ok=True)
YAK = Path('/Applications/Rhino 9.app/Contents/Resources/bin/yak')
if not YAK.exists():
    YAK = Path('/Applications/Rhino 8.app/Contents/Resources/bin/yak')
if not YAK.exists():
    raise SystemExit('Install Rhino with Yak CLI before packaging.')

def archive(path, files):
    with zipfile.ZipFile(path,'w',zipfile.ZIP_DEFLATED) as z:
        for source, dest in files:
            z.write(source, dest)

for major, tf in [(7,'net48'),(8,'net8.0'),(9,'net10.0')]:
    dll = ROOT/'bin/Release'/tf/'ononSmartView.dll'
    if not dll.exists():
        raise SystemExit(f'Missing build: {dll}')
    stage = OUT/f'stage-rhino{major}'
    stage.mkdir(exist_ok=True)
    # Remove only earlier outputs in this generated staging folder.
    for p in stage.glob('*.yak'):
        p.unlink()
    shutil.copy2(dll, stage/'ononSmartView.rhp')
    shutil.copy2(ROOT/'ui/ononSmartView.rui', stage/'ononSmartView.rui')
    deps = dll.with_suffix('.deps.json')
    if major != 7 and deps.exists():
        shutil.copy2(deps, stage/deps.name)
    (stage/'manifest.yml').write_text(f'''name: ononsmartview
version: {VERSION}
authors:
  - ZionW
description: >
  Experimental Rhino {major} viewport navigation, selected-object orbit,
  face focus and ViewCube. Test release; runtime verification pending.
url: https://github.com/ZionW/ononSmartView
keywords:
  - viewport
  - viewcube
''')
    subprocess.run([str(YAK),'build','--platform','win'],cwd=stage,check=True)
    yak = list(stage.glob('*.yak'))
    if len(yak) != 1:
        raise SystemExit(f'Expected one Yak in {stage}')
    shutil.copy2(yak[0], OUT/yak[0].name)
    files = [(stage/'ononSmartView.rhp','ononSmartView.rhp'),(stage/'ononSmartView.rui','ononSmartView.rui')]
    if major != 7 and deps.exists():
        files.append((stage/deps.name,deps.name))
    readme = OUT/f'INSTALL-rhino{major}.txt'
    readme.write_text(f'''ononSmartView {VERSION} — Rhino {major} / Windows

繁體中文
解壓縮全部檔案到固定資料夾。若 Windows 提供「解除封鎖」，請先解除封鎖。
在 Rhino {major} 執行 PlugInManager → 安裝 → 選擇 ononSmartView.rhp。
安裝後不要移動資料夾。請先移除其他相同外掛副本並重新啟動 Rhino。
開啟 ononSmartView.rui 並顯示 SmartView 工具列可使用圖示按鈕。
指令 SmartViewCube 顯示／隱藏 ViewCube。本測試版本仍有疏漏，Windows 尚未實機驗證。

English
Extract all files to a permanent folder. Unblock the downloaded archive/files if Windows offers that option.
In Rhino {major}, run PlugInManager > Install > ononSmartView.rhp.
Keep all files together. Remove previous copies and restart Rhino before installation.
Open ononSmartView.rui and show the SmartView toolbar for the icon button.
Run SmartViewCube to show/hide ViewCube. Experimental release; Windows runtime remains untested.
''')
    archive(OUT/f'ononSmartView-{VERSION}-rhino{major}-windows-rhp.zip',files+[(readme,'INSTALL.txt')])
    macfiles = [(dll,'ononSmartView.rhp/ononSmartView.dll'),(ROOT/'ui/ononSmartView.rui','ononSmartView.rhp/ononSmartView.rui')]
    if major != 7 and deps.exists():
        macfiles.append((deps,'ononSmartView.rhp/'+deps.name))
    archive(OUT/f'ononSmartView-{VERSION}-rhino{major}-mac.macrhi',macfiles)

sourcefiles = []
for pattern in ['*.cs','*.csproj','README*.md','.gitignore','scripts/*.py','scripts/*.ps1','packaging/**/*.yml','ui/**/*','docs/*.md']:
    for p in ROOT.glob(pattern):
        if p.is_file():
            sourcefiles.append((p,str(p.relative_to(ROOT))))
archive(OUT/f'ononSmartView-{VERSION}-source.zip',sorted(set(sourcefiles)))
archive(OUT/'SmartViewCube-toolbar.zip',[(p,str(p.relative_to(ROOT/'ui'))) for p in (ROOT/'ui').rglob('*') if p.is_file()])
for name in ['SmartViewCube-20s.gif','SmartViewCube-20s.mp4']:
    shutil.copy2(ROOT/'dist/demos'/name, OUT/name)
# All package archives must be readable; SDK DLLs must never be bundled.
for p in list(OUT.glob('*.zip'))+list(OUT.glob('*.macrhi'))+list(OUT.glob('*.yak')):
    with zipfile.ZipFile(p) as z:
        if z.testzip() is not None:
            raise SystemExit(f'Corrupt archive: {p}')
        if any(Path(n).name.lower() in ['rhinocommon.dll','rhino.ui.dll','eto.dll'] for n in z.namelist()):
            raise SystemExit(f'SDK assembly leaked into {p}')
assets=sorted(p for p in OUT.iterdir() if p.is_file() and p.suffix in ['.zip','.macrhi','.yak','.gif','.mp4'])
(OUT/'SHA256SUMS.txt').write_text(''.join(f'{hashlib.sha256(p.read_bytes()).hexdigest()}  {p.name}\n' for p in assets))
print('Release assets:',len(assets))
for p in assets:print(p.name,p.stat().st_size)
