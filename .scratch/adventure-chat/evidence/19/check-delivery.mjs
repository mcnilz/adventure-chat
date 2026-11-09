import {readFileSync, existsSync, writeFileSync} from 'node:fs';
import {createHash} from 'node:crypto';
import {resolve, dirname, join} from 'node:path';
import assert from 'node:assert/strict';
const root = resolve('worlds/reference/design-guide/assets/tour-v3');
const json = file => JSON.parse(readFileSync(join(root, file), 'utf8').replace(/^\uFEFF/, ''));
const hash = file => createHash('sha256').update(readFileSync(file)).digest('hex');
const sources = json('sources/all-generations.json');
for (const source of sources) assert.equal(hash(join(root, source.file)), source.sha256);
for (const [name, runtime] of [['kartenweg', 'demo-map'], ['huegelpfad', 'demo-depth'], ['panorama', 'demo-combined']])
    assert.equal(hash(join(root, `backgrounds/${name}.png`)), hash(`worlds/reference/assets/images/${runtime}.png`));
for (const name of ['mara', 'bruno'])
    assert.equal(hash(join(root, `characters/${name}-top-down.png`)), hash(`worlds/reference/assets/images/${name}-top-down.png`));
let links = 0;
for (const file of ['index.html', 'comparison.html']) {
    const path = join(root, file);
    for (const [, target] of readFileSync(path, 'utf8').matchAll(/(?:href|src)="([^"]+)"/g)) {
        if (target.startsWith('#') || /^https?:/.test(target)) continue;
        assert.ok(existsSync(resolve(dirname(path), target)), `${file}: missing ${target}`);
        links++;
    }
}
const result = {generationOriginalsHashChecked:sources.length, runtimeCopiesHashChecked:5, localPageLinksChecked:links, passed:true};
writeFileSync('.scratch/adventure-chat/evidence/19/delivery-check-result.json', JSON.stringify(result, null, 2) + '\n');
console.log(result);
