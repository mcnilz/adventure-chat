import {copyFileSync, mkdirSync, readdirSync} from 'node:fs';
const source = 'tests/AdventureChat.E2ETests/bin/Debug/net10.0/';
const target = '.scratch/adventure-chat/evidence/19/screenshots/';
mkdirSync(target, {recursive: true});
const names = readdirSync(source).filter(n => n.startsWith('reference-tour-') && n.endsWith('.png'));
for (const name of names) copyFileSync(source + name, target + name);
console.log(`Saved ${names.length} current browser screenshots.`);
