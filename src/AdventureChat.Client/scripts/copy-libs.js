import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const targetDir = path.resolve(__dirname, '../../AdventureChat.Server/wwwroot/js/lib');
fs.mkdirSync(targetDir, { recursive: true });

const signalrSource = path.resolve(__dirname, '../node_modules/@microsoft/signalr/dist/browser/signalr.min.js');
const signalrDest = path.join(targetDir, 'signalr.min.js');

if (fs.existsSync(signalrSource)) {
  fs.copyFileSync(signalrSource, signalrDest);
  console.log('Copied signalr.min.js to wwwroot/js/lib');
} else {
  console.warn('Warning: signalr.min.js not found in node_modules');
}
