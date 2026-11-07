"""Build the SmartViewCube native Rhino toolbar and vector icon assets."""
from pathlib import Path
import xml.etree.ElementTree as ET
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
UI = ROOT / 'ui'
ICONS = UI / 'icons'
ICONS.mkdir(parents=True, exist_ok=True)
IDS = {'file':'acc35bcc-253f-42ab-80b1-9f004f059d95', 'toolbar':'ea760a50-03ea-435a-967c-6be8141d65e5',
       'macro':'de7248ac-f7cc-439f-9379-16716fc149cc', 'icon':'c9fd8687-e27f-451b-ad65-249579464b76',
       'button':'b58ce4bf-0492-4264-a498-42826b73ad41', 'group':'012b2ed2-5b9b-4a2e-9070-ed01be113b95',
       'groupitem':'e5ad0f81-d91b-4af6-a5a4-9b290b70f16b'}

def svg(dark=False):
    edge, ring, top, left, right = ('#E4E9EE','#88939D','#263D51','#26313B','#1B2630') if dark else ('#475968','#9BA6AE','#DDEDF8','#F8FAFC','#E9EEF2')
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 32 32">
<circle cx="16" cy="16" r="14" fill="none" stroke="{ring}" stroke-width="1.4"/>
<g stroke="{edge}" stroke-width="1.45" stroke-linejoin="round">
<path d="M16 6.8 24 11.4 16 16 8 11.4Z" fill="{top}"/>
<path d="M8 11.4 16 16 16 25.2 8 20.6Z" fill="{left}"/>
<path d="M16 16 24 11.4 24 20.6 16 25.2Z" fill="{right}"/>
</g></svg>'''

r = ET.Element('RhinoUI', major_ver='4', minor_ver='0', guid=IDS['file'], localize='False', default_language_id='1033', dpi_scale='100', plug_in_guid='8c981dce-6e31-4c7a-a9bc-7f25701b9d34')
def text(parent, tag, en, zh):
    e=ET.SubElement(parent,tag)
    ET.SubElement(e,'locale_1033').text=en
    ET.SubElement(e,'locale_1028').text=zh
    return e
ET.SubElement(r,'extend_rhino_menus');ET.SubElement(r,'menus')
groups=ET.SubElement(r,'tool_bar_groups')
group=ET.SubElement(groups,'tool_bar_group',guid=IDS['group'],hide_single_tab='True',single_file='False',point_floating='240,180',active_tool_bar_group=IDS['toolbar'])
text(group,'text','SmartView','SmartView')
gitem=ET.SubElement(group,'tool_bar_group_item',guid=IDS['groupitem'],major_version='1',minor_version='1')
text(gitem,'text','SmartView','SmartView');ET.SubElement(gitem,'tool_bar_id').text=IDS['toolbar']
bars=ET.SubElement(r,'tool_bars');bar=ET.SubElement(bars,'tool_bar',guid=IDS['toolbar'],bitmap_id=IDS['icon'],item_display_style='bitmap')
text(bar,'text','SmartView','SmartView')
button=ET.SubElement(bar,'tool_bar_item',guid=IDS['button']);text(button,'text','Show / hide ViewCube','顯示／隱藏 ViewCube')
ET.SubElement(button,'left_macro_id').text=IDS['macro']
macros=ET.SubElement(r,'macros');macro=ET.SubElement(macros,'macro_item',guid=IDS['macro'],bitmap_id=IDS['icon'])
for tag in ['text','button_text','menu_text','tooltip']:
    text(macro,tag,'Show / hide ViewCube','顯示／隱藏 ViewCube')
# Do not cancel an active modeling command merely to toggle the overlay.
ET.SubElement(macro,'script').text='_SmartViewCube'
icons=ET.SubElement(r,'icons');icon=ET.SubElement(icons,'icon',guid=IDS['icon'])
for dark in [False,True]:
    value=svg(dark);(ICONS/('SmartViewCube-dark.svg' if dark else 'SmartViewCube.svg')).write_text(value)
    ET.SubElement(icon,'dark' if dark else 'light').text=value
bitmaps=ET.SubElement(r,'bitmaps')
for name,size in [('small',16),('normal',24),('large',32)]:ET.SubElement(bitmaps,name+'_bitmap',item_width=str(size),item_height=str(size))
ET.SubElement(r,'scripts');ET.indent(r)
ET.ElementTree(r).write(UI/'ononSmartView.rui',encoding='utf-8',xml_declaration=True)
# Transparent raster exports for manual toolbar editors and a preview.
for size in [16,24,32,64,256]:
    scale=8;im=Image.new('RGBA',(size*scale,size*scale));d=ImageDraw.Draw(im)
    k=size*scale/32
    def pts(v):return [(round(x*k),round(y*k)) for x,y in v]
    d.ellipse((2*k,2*k,30*k,30*k),outline='#9BA6AE',width=max(1,round(1.4*k)))
    for points,color in [([(16,6.8),(24,11.4),(16,16),(8,11.4)],'#DDEDF8'), ([(8,11.4),(16,16),(16,25.2),(8,20.6)],'#F8FAFC'), ([(16,16),(24,11.4),(24,20.6),(16,25.2)],'#E9EEF2')]:
        p=pts(points);d.polygon(p,fill=color);d.line(p+[p[0]],fill='#475968',width=max(1,round(1.45*k)),joint='curve')
    im.resize((size,size),Image.Resampling.LANCZOS).save(ICONS/f'SmartViewCube-{size}.png')
print(UI/'ononSmartView.rui')
