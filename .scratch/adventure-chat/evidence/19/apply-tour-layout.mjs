import fs from 'node:fs';
import path from 'node:path';

const root = path.resolve('worlds/reference');
const layouts = [
  {
    id: 'tour-map', entry: [140,310], right: [1220,180],
    points: [[56,150],[98,157],[138,170],[285,170],[317,180],[342,170],
      [403,150],[448,121],[478,89],[535,66],[602,58],[672,55],[735,67],
      [772,94],[793,124],[815,148],[856,162],[904,142],[954,128],
      [1012,128],[1083,133],[1158,150],[1248,155],[1270,167],
      [1270,209],[1205,214],[1135,202],[1060,186],[1007,179],
      [960,183],[920,198],[879,216],[843,231],[808,213],[782,192],
      [758,166],[733,141],[697,127],[651,120],[607,124],[558,137],
      [518,154],[481,175],[439,204],[399,232],[355,248],[318,248],
      [314,320],[285,338],[125,338],[125,306],[102,280],[73,270],[54,250],[52,190]],
    obstacles: [{id:'courtyard-table',points:[[146,180],[266,180],[266,256],[146,256]]}],
    notes: 'Inhalt 3.0.0: Hof und gewundener Steinweg. Anschlüsse (140,310)/(1220,180); Baumkronen, Mauern, Blumenbeet und Hoftisch sind nicht begehbar.'
  },
  {
    id: 'tour-depth', entry: [100,300], right: [570,285],
    points: [[40,280],[145,276],[213,281],[240,263],[247,238],
      [274,216],[286,193],[275,174],[263,154],[270,133],[285,116],
      [285,98],[330,89],[357,93],[360,111],[348,133],[329,153],
      [322,178],[339,196],[350,218],[348,240],[361,270],[400,290],
      [500,279],[610,263],[625,291],[568,309],[495,326],[416,342],
      [337,345],[264,337],[192,320],[110,313],[40,307]],
    obstacles: [],
    notes: 'Inhalt 3.0.0: Steinpfad und Treppenanstieg. Anschlüsse (100,300)/(570,285), oberes Prüfziel (320,140); Tiefenwerte unverändert.'
  },
  {
    id: 'tour-combined', entry: [170,290], right: [1130,300],
    points: [[130,280],[158,255],[184,232],[199,206],[204,180],
      [201,154],[210,129],[230,108],[245,87],[274,68],[310,57],
      [347,52],[377,59],[389,78],[361,85],[334,84],[305,91],
      [286,111],[267,135],[258,160],[252,190],[246,215],[250,241],
      [272,261],[314,276],[382,285],[468,291],[555,293],[642,294],
      [725,289],[804,280],[864,265],[892,240],[912,216],[927,188],
      [941,158],[954,132],[965,111],[958,92],[936,82],[902,78],
      [875,85],[850,85],[837,69],[858,55],[894,49],[934,49],
      [977,53],[1001,67],[1014,92],[1011,118],[998,146],[986,175],
      [986,202],[1001,225],[1029,247],[1068,266],[1120,281],
      [1175,291],[1185,314],[1125,326],[1060,336],[970,343],
      [865,347],[770,346],[675,345],[570,345],[465,342],[370,332],
      [290,320],[230,313],[171,307],[135,300]],
    obstacles: [],
    notes: 'Inhalt 3.0.0: zwei Treppenwege und durchgehender unterer Steinpfad. Anschlüsse (170,290)/(1130,300), Prüfziele (225,140)/(600,325)/(990,140); Tal nicht begehbar.'
  }
];

const point = ([x,y]) => ({x,y});
for (const layout of layouts) {
  const file = path.join(root, 'rooms', layout.id + '.yaml');
  const room = JSON.parse(fs.readFileSync(file, 'utf8').replace(/^#.*\r?\n/, ''));
  room.walkable = [{id:'path', points:layout.points.map(point)}];
  room.obstacles = layout.obstacles.map(o => ({id:o.id,points:o.points.map(point)}));
  room.spawns.find(s => s.id === 'entry').position = point(layout.entry);
  room.spawns.find(s => s.id === 'right').position = point(layout.right);
  room.exits.find(e => e.id === 'back').position = point(layout.entry);
  room.exits.find(e => e.id === 'next').position = point(layout.right);
  room.notes = layout.notes;
  fs.writeFileSync(file, '# yaml-language-server: $schema=../../../schemas/world/1.5.0/room.schema.json\n' + JSON.stringify(room, null, 2) + '\n');
}
const manifestFile = path.join(root, 'world.yaml');
const manifest = fs.readFileSync(manifestFile, 'utf8');
if (!/contentVersion: (2\.1\.0|3\.0\.0)/.test(manifest)) throw Error('Unexpected content version; preserved.');
fs.writeFileSync(manifestFile, manifest.replace('contentVersion: 2.1.0', 'contentVersion: 3.0.0'));
