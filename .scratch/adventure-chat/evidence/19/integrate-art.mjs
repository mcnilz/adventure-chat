import {copyFileSync, cpSync, existsSync, mkdirSync, readdirSync, readFileSync, writeFileSync} from 'node:fs';
import {join, resolve} from 'node:path';

const project = 'C:/Users/MartinSchnabel/AppData/Roaming/Open Design/namespaces/release-stable-win/data/projects/taverne-ticket-19-lokale-entw-rfe-0d87';
const production = join(project, 'production');
const world = resolve('worlds/reference');
const archive = join(world, 'design-guide/assets/tour-v3');
if (!existsSync(join(production, 'DELIVERY.md'))) throw new Error('Final delivery is still pending.');
for (const folder of ['backgrounds', 'characters', 'sources', 'qa'])
    cpSync(join(production, folder), join(archive, folder), {recursive: true});
for (const file of readdirSync(production).filter(n => n.endsWith('.json') && !n.endsWith('.artifact.json') && n !== 'preview-server.json'))
    copyFileSync(join(production, file), join(archive, file));
for (const file of ['DELIVERY.md', 'LOCAL-PREVIEW.md', 'studio.js', 'styles.css'])
    copyFileSync(join(production, file), join(archive, file));
mkdirSync(join(archive, 'tools'), {recursive: true});
copyFileSync(join(production, 'tools/verify-and-contact.ps1'), join(archive, 'tools/verify-and-contact.ps1'));
mkdirSync(join(archive, 'references'), {recursive: true});
for (const name of ['demo-map', 'demo-depth', 'demo-combined', 'mara-top-down', 'bruno-top-down', 'mara', 'bruno', 'lounge-runtime', 'garden-runtime'])
    copyFileSync(join(project, `assets/references/${name}.png`), join(archive, `references/${name}.png`));
for (const name of ['kartenweg', 'huegelpfad', 'panorama', 'mara', 'bruno'])
    copyFileSync(join(project, `assets/sources/${name}-generated.png`), join(archive, `sources/${name}-generated.png`));
copyFileSync(join(project, 'generation-prompts.json'), join(archive, 'concept-generation-prompts.json'));
let html = readFileSync(join(production, 'index.html'), 'utf8')
    .replaceAll('../assets/sources/', 'sources/').replaceAll('../assets/references/', 'references/')
    .replaceAll('../index.html', '../../index.html');
html = html.replace('Keine Spieleinbindung oder Ticketänderung durch diese Lieferung.', 'Archivierte Grafiklieferung; Einbindung und Spielprüfung siehe README.md.');
html = html.replace('<strong>Getrennte Lieferung:</strong> Das Spielprojekt und die vorhandene Entwurfsmappe bleiben unverändert. Diese Seite zeigt die tatsächlichen neuen Sheet-Dateien. Spieleinbindung, Laufgeometrie und abschließende menschliche Abnahme folgen beim aufrufenden Agenten.', '<strong>Referenzwelt 3.0.0:</strong> Diese Figuren und Raumgrafiken sind im Spiel eingebunden. Die Wege folgen der sichtbaren Pflasterung. Hier lassen sich einzelne Richtungen und Schrittphasen vergleichen; die abschließende Beurteilung von Stil und Gehgefühl erfolgt im Spiel.');
writeFileSync(join(archive, 'index.html'), html);
for (const [name, target] of [['kartenweg', 'demo-map'], ['huegelpfad', 'demo-depth'], ['panorama', 'demo-combined']])
    copyFileSync(join(archive, `backgrounds/${name}.png`), join(world, `assets/images/${target}.png`));
for (const name of ['mara', 'bruno'])
    copyFileSync(join(archive, `characters/${name}-top-down.png`), join(world, `assets/images/${name}-top-down.png`));

const rooms = [['Kartenweg', 'kartenweg', 'demo-map', true], ['Hügelpfad', 'huegelpfad', 'demo-depth', false], ['Panorama', 'panorama', 'demo-combined', true]];
writeFileSync(join(archive, 'comparison.html'), `<!doctype html><html lang="de"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Tour · Vorher und nachher</title>
<style>body{margin:24px auto;padding:0 24px;max-width:1360px;background:#eef2f3;color:#172733;font:18px/1.6 system-ui}h1,h2{font-family:Georgia,serif;font-weight:normal}.pair,.slices{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:20px}img{width:100%;image-rendering:pixelated}figure{margin:0 0 24px}figcaption{font-size:14px}.slices{grid-template-columns:repeat(3,minmax(0,1fr))}.crop{aspect-ratio:16/9;overflow:hidden}.crop img{width:200%;max-width:none}.middle img{transform:translateX(-25%)}.right img{transform:translateX(-50%)}section{border-top:1px solid #bac8cc;padding:24px 0}@media(max-width:700px){.pair,.slices{grid-template-columns:1fr}}</style>
<h1>Referenzwelt · Tour im Vergleich</h1><p>Entwurfsrichtung bestätigt; abschließende Sichtabnahme im Spiel. <a href="index.html">Figuren in Bewegung</a> · <a href="../../index.html">Design Guide</a></p>
<div class="pair"><figure><img src="references/lounge-runtime.png" alt="Unveränderte Lounge"><figcaption>Lounge · unveränderte Referenz</figcaption></figure><figure><img src="references/garden-runtime.png" alt="Unveränderter Garten"><figcaption>Garten · unveränderte Referenz</figcaption></figure></div>
${rooms.map(([title, name, old, wide]) => `<section><h2>${title}</h2><div class="pair"><figure><img src="references/${old}.png" alt="${title} vorher"><figcaption>Vorher · schematische Demo</figcaption></figure><figure><img src="backgrounds/${name}.png" alt="${title} nachher"><figcaption>Nachher · freigegebenes Motiv</figcaption></figure></div>${wide ? `<div class="slices">${['left', 'middle', 'right'].map((side, i) => `<figure><div class="crop ${side}"><img src="backgrounds/${name}.png" alt="${title} ${['links', 'Mitte', 'rechts'][i]}"></div><figcaption>640px-Ausschnitt · ${['links', 'Mitte', 'rechts'][i]}</figcaption></figure>`).join('')}</div>` : ''}</section>`).join('')}
<section><h2>Figuren vor und nach der Ausarbeitung</h2><p>Die alten Draufsicht-Sheets stehen den vollständigen neuen Posen gegenüber. Für den tatsächlichen Zielmaßstab und einzelne Laufphasen <a href="index.html">die Bewegungsvorschau öffnen</a>.</p>${['mara','bruno'].map(name => `<h3>${name}</h3><div class="pair"><img src="references/${name}-top-down.png" alt="${name} vorher"><img src="characters/${name}-top-down.png" alt="${name} nachher"></div>`).join('')}</section></html>`);
console.log('Integrated five delivered PNGs, sources and portable review pages.');
