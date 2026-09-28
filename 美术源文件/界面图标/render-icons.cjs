// npm install sharp, then node render-icons.cjs (from any directory).
const fs = require('node:fs/promises');
const path = require('node:path');
const sharp = require('sharp');
const keys = ['letter', 'album', 'map', 'help', 'brand', 'write', 'livelihood'];
(async () => {
  const output = path.resolve(__dirname, '../../Assets/Resources/UI/Icons');
  await fs.mkdir(output, {recursive:true});
  for (const key of keys) {
    await sharp(path.join(__dirname, key + '.svg'), {density:192})
      .resize(256,256).png().toFile(path.join(output,key+'.png'));
  }
  process.stdout.write('Rendered ' + keys.length + ' transparent UI icons.\n');
})().catch(e => { console.error(e); process.exitCode=1; });
