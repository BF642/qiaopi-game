"""Pack native glTF metallic/roughness into Unity Standard metallic/smoothness.

Usage: python tools/pack_native_metallic.py Assets/Resources/Models/ROLEMeshyMotion
Requires Pillow. The original image remains alongside the derived Unity texture.
"""
import sys
from pathlib import Path
from PIL import Image, ImageOps

folder = Path(sys.argv[1])
native = next((folder / ('NativeMetalRough' + ext) for ext in ('.jpg', '.png')
               if (folder / ('NativeMetalRough' + ext)).is_file()), None)
if native is None:
    raise FileNotFoundError('Native metallic/roughness texture missing: ' + str(folder))
with Image.open(native) as source:
    _, roughness, metallic = source.convert('RGB').split()
    Image.merge('RGBA', (metallic, metallic, metallic, ImageOps.invert(roughness))).save(
        folder / 'MetalSmooth.png')
print('Native pixels packed without resizing:', folder)
