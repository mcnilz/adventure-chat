import {readFileSync, writeFileSync} from 'node:fs';
import {createHash} from 'node:crypto';
const base = '.scratch/adventure-chat/evidence/19/';
const before = JSON.parse(readFileSync(base + 'protected-files-before.json', 'utf8').replace(/^\uFEFF/, ''));
const failures = before.filter(file => createHash('sha256').update(readFileSync(file.path)).digest('hex').toUpperCase() !== file.sha256);
writeFileSync(base + 'protected-files-result.json', JSON.stringify({checked: before.length, changed: failures, passed: failures.length === 0}, null, 2) + '\n');
if (failures.length) throw new Error('Protected files changed: ' + failures.map(x => x.path).join(', '));
console.log(`${before.length} protected files unchanged (SHA-256).`);
