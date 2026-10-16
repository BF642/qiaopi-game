"""Pack native glTF metallic/roughness into Unity Standard metallic/smoothness.

Usage: python tools/pack_native_metallic.py Assets/Resources/Models/ROLEMeshyMotion
Requires Pillow. The original image remains alongside the derived Unity texture.
"""
import sys
from pathlib import Path
from PIL import Image, ImageOps

folder = Path(sys.argv[1])
with Image.open(folder / 'NativeMetalRough.jpg') as source:
    _, roughness, metallic = source.convert('RGB').split()
    Image.merge('RGBA', (metallic, metallic, metallic, ImageOps.invert(roughness))).save(
        folder / 'MetalSmooth.png')
print('Native pixels packed without resizing:', folder)
