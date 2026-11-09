import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';

const base = path.resolve('.scratch/adventure-chat/evidence/19');
const root = fs.mkdtempSync(path.join(base, 'generator-check-'));
try {
  fs.mkdirSync(path.join(root, 'scripts'));
  for (const file of ['generate-depth-art.js', 'png.js'])
    fs.copyFileSync(path.resolve('scripts', file), path.join(root, 'scripts', file));
  fs.writeFileSync(path.join(root, 'package.json'), '{"type":"module"}\n');
  for (const world of ['reference', 'map', 'scroll'])
    fs.mkdirSync(path.join(root, 'worlds', world, 'assets/images'), {recursive:true});
  const protectedNames = ['demo-map', 'demo-depth', 'demo-combined', 'mara-top-down', 'bruno-top-down'];
  for (const name of protectedNames)
    fs.writeFileSync(path.join(root, 'worlds/reference/assets/images', name + '.png'), 'preserve-' + name);
  const run = spawnSync(process.execPath, ['scripts/generate-depth-art.js'], {cwd:root,encoding:'utf8'});
  assert.equal(run.status, 0, run.stderr);
  for (const name of protectedNames)
    assert.equal(fs.readFileSync(path.join(root, 'worlds/reference/assets/images', name + '.png'), 'utf8'), 'preserve-' + name);
  for (const world of ['map', 'scroll']) for (const name of ['demo-map', 'demo-depth', 'demo-combined']) {
    const png = fs.readFileSync(path.join(root, 'worlds', world, 'assets/images', name + '.png'));
    assert.deepEqual([...png.subarray(0,8)], [137,80,78,71,13,10,26,10]);
  }
  console.log('Generator boundary verified: five reference sentinels preserved; six demo PNGs produced in isolation.');
} finally {
  const relative = path.relative(base, path.resolve(root));
  if (relative.startsWith('generator-check-') && !relative.includes(path.sep) && !path.isAbsolute(relative))
    fs.rmSync(root, {recursive:true});
  else throw Error('Cleanup target outside the intended evidence directory; preserved.');
}
