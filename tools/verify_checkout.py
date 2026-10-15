"""Verify downloaded resources, without modifying the project."""
from pathlib import Path
import json,hashlib,sys
root=Path(__file__).resolve().parent.parent
manifest=json.loads((root/'docs/resource-transfer-manifest.json').read_text())
errors=[]
for item in manifest['files']:
 p=root/item['path']
 if not p.is_file():errors.append(item['path']+': missing');continue
 if p.stat().st_size!=item['bytes']:errors.append(item['path']+': wrong size (run git lfs pull)');continue
 if '--sha256' in sys.argv:
  h=hashlib.sha256()
  with p.open('rb') as f:
   for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
  if h.hexdigest()!=item['sha256']:errors.append(item['path']+': hash mismatch')
if errors:print('\n'.join(errors));sys.exit(1)
print('Resource transfer OK:',len(manifest['files']),'files. Open this folder in Unity 6000.6.0f1.')
