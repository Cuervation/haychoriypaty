#!/usr/bin/env python3
"""Rebuild localized hub clean plates and small runtime atlases from checked-in source PNGs.
Requires Pillow only; no runtime image processing is used by the game.
"""
from pathlib import Path
from hashlib import sha256
import json
from PIL import Image, ImageDraw, ImageFilter
ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'tools/hub-characters/source'
RUNTIME = ROOT / 'Assets/Art/Street/Resources/StreetHubCharacters'
GROUPS = {'Seller':'seller.png','Police':'police.png','StoreCharacter':'store.png','Boy':'boy.png','Girl':'girl.png'}
POLYGONS = {
 'Seller': [(555,974),(565,966),(607,962),(654,965),(689,980),(714,1007),(731,1045),(735,1076),(726,1105),(708,1127),(678,1140),(626,1144),(589,1137),(562,1120),(551,1090),(550,1054)],
 'Police': [(509,640),(526,633),(551,637),(576,658),(593,649),(613,638),(644,636),(665,652),(676,655),(695,646),(716,652),(729,679),(732,725),(728,768),(716,793),(694,806),(676,799),(661,781),(639,787),(613,783),(594,767),(580,791),(555,796),(536,779),(524,754),(512,728)],
 'StoreCharacter': [(179,1424),(188,1418),(217,1417),(252,1418),(281,1424),(298,1441),(305,1470),(306,1518),(302,1563),(296,1605),(285,1638),(270,1654),(247,1661),(222,1655),(205,1648),(188,1653),(174,1641),(169,1615),(170,1574),(172,1528),(173,1484)],
 'Boy': [(462,1486),(482,1482),(516,1480),(548,1488),(577,1505),(601,1517),(628,1504),(624,1534),(611,1540),(613,1570),(611,1600),(609,1640),(614,1700),(609,1745),(599,1775),(579,1785),(553,1781),(529,1785),(505,1781),(481,1785),(464,1770),(456,1740),(453,1695),(452,1640),(453,1584)],
 'Girl': [(608,1485),(626,1480),(656,1480),(688,1481),(719,1492),(747,1514),(772,1547),(787,1588),(795,1635),(798,1683),(797,1722),(786,1755),(772,1775),(748,1780),(724,1775),(704,1768),(681,1778),(660,1772),(639,1752),(624,1724),(613,1692),(607,1650),(604,1604),(605,1550)],
}
def guid(path): return sha256(path.as_posix().encode()).hexdigest()[:32]
def write_meta(path, folder=False):
    meta=Path(str(path)+'.meta')
    if meta.exists(): return
    if folder:
        meta.write_text(f'fileFormatVersion: 2\nguid: {guid(path)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
    else:
        meta.write_text(f'''fileFormatVersion: 2
guid: {guid(path)}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
  isReadable: 0
  streamingMipmaps: 0
  textureType: 0
  textureShape: 1
  singleChannelComponent: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteMode: 0
  wrapMode: 1
  filterMode: 1
  aniso: 1
  maxTextureSize: 1024
  textureCompression: 2
  compressionQuality: 50
  userData:
  assetBundleName:
  assetBundleVariant:
''')
def ensure_dirs(path):
    missing=[]; p=path
    while not p.exists() and p != ROOT: missing.append(p); p=p.parent
    path.mkdir(parents=True, exist_ok=True)
    for d in reversed(missing): write_meta(d, True)
# Explicit alpha component bounds measured in the final runtime-sized atlas (top-left pixel origin).
# These are irregular imagegen layouts; a regular grid crops several component tips.
ATLAS_PARTS = {
 'Seller': (720,720, [("head",11,48,257,276),("torso",252,105,481,285),("seated_legs",488,124,715,279),("arm_left",32,327,182,462),("thumb_arm",264,334,453,468),("arm_right",539,327,687,462),("open_hand",20,529,248,660),("thumb_alt",273,523,460,661),("rest_hand",509,550,700,656)]),
 'Police': (720,960, [("head_left",64,34,191,142),("head_center",295,30,428,145),("head_right",529,33,664,146),("torso_left",25,171,208,338),("torso_center",262,170,459,338),("torso_right",511,169,696,339),("legs_left",49,356,200,540),("legs_center",289,357,439,540),("legs_right",525,357,671,539),("whole_left",37,569,201,939),("whole_center",280,568,446,940),("whole_right",527,571,686,940)]),
 'StoreCharacter': (720,720, [("head",16,39,261,244),("torso_bag",263,41,482,266),("paired_legs",515,24,692,264),("arm_left",61,294,180,461),("arm_extended",316,297,459,481),("arm_right",544,296,663,461),("arm_down",60,499,194,692),("arm_open",257,506,460,649),("arm_open_alt",554,507,694,684)]),
 'Boy': (720,620, [("head",35,15,243,207),("torso",244,24,489,217),("shorts",509,45,703,210),("leg_left",79,239,195,400),("arm_left",308,252,416,398),("leg_right",551,235,647,400),("forearm_down",80,421,186,586),("arm_right",315,427,439,573),("arm_raised",544,412,649,597)]),
 'Girl': (720,753, [("hair",10,16,311,284),("backpack",328,71,471,254),("torso",496,73,710,254),("arm_left",81,331,187,474),("arm_pointing",273,285,429,486),("arm_right",553,326,664,483),("arm_down",55,518,167,693),("hips",257,509,435,730),("calves",520,509,686,730)]),
}
def atlas_manifest(name):
    w,h,parts=ATLAS_PARTS[name]
    entries=[]
    for part,x0,y0,x1,y1 in parts:
        entries.append({'name':part,'uv':{'x':x0/w,'y':1-y1/h,'width':(x1-x0)/w,'height':(y1-y0)/h}})
    return {'width':w,'height':h,'parts':entries}
def write_json_meta(path):
    meta=Path(str(path)+'.meta')
    if not meta.exists(): meta.write_text(f'fileFormatVersion: 2\nguid: {guid(path)}\nTextScriptImporter:\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n')
def main():
    original=Image.open(ROOT/'Assets/Art/Street/Resources/street-hub-stadium-v1.png').convert('RGBA')
    cleaned=Image.open(SOURCE/'cleaned-hub.png').convert('RGBA')
    if original.size != cleaned.size or original.size != (841,1870): raise ValueError(f'Expected aligned 841x1870 source pair, got {original.size}/{cleaned.size}')
    for name, filename in GROUPS.items():
        out=RUNTIME/name; ensure_dirs(out)
        mask=Image.new('L',original.size,0); ImageDraw.Draw(mask).polygon(POLYGONS[name],fill=255)
        mask=mask.filter(ImageFilter.GaussianBlur(1.3)); box=mask.getbbox()
        patch=cleaned.crop(box); patch.putalpha(mask.crop(box)); patch.save(out/'BackgroundPatch.png',optimize=True)
        fullatlas=Image.open(SOURCE/filename).convert('RGBA')
        atlas=fullatlas.copy(); atlas.thumbnail((720,960),Image.Resampling.LANCZOS)
        expected=(ATLAS_PARTS[name][0],ATLAS_PARTS[name][1])
        if atlas.size != expected: raise ValueError(f'{name} atlas size {atlas.size} does not match measured part map {expected}')
        atlas.save(out/'PartsAtlas.png',optimize=True)
        manifest=atlas_manifest(name)
        (out/'PartsAtlasBounds.json').write_text(json.dumps(manifest,separators=(',',':')))
        write_meta(out,True); write_meta(out/'BackgroundPatch.png'); write_meta(out/'PartsAtlas.png'); write_json_meta(out/'PartsAtlasBounds.json')
    print('Generated five localized patches and five compact atlases; original master SHA-256 is unchanged.')
if __name__=='__main__': main()
