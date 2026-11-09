import fs from 'node:fs';
import { createPng } from './png.js';

// Original geometric pixel landscapes. No network assets or extra packages.
function landscape(width, map) {
  const height = 360;
  const pixels = new Uint8Array(width * height * 4);
  function rect(x, y, w, h, color) {
    for (let py = Math.max(0, y); py < Math.min(height, y + h); py++)
      for (let px = Math.max(0, x); px < Math.min(width, x + w); px++)
        pixels.set([...color, 255], (py * width + px) * 4);
  }
  function polygon(points, color) {
    for (let y = 0; y < height; y++) {
      const intersections = [];
      for (let i = 0; i < points.length; i++) {
        const a = points[i], b = points[(i + 1) % points.length];
        if ((a[1] > y) !== (b[1] > y)) intersections.push(a[0] + (y - a[1]) * (b[0] - a[0]) / (b[1] - a[1]));
      }
      intersections.sort((a, b) => a - b);
      for (let i = 0; i + 1 < intersections.length; i += 2)
        rect(Math.ceil(intersections[i]), y, Math.floor(intersections[i + 1]) - Math.ceil(intersections[i]) + 1, 1, color);
    }
  }
  rect(0, 0, width, height, map ? [63, 100, 65] : [104, 153, 170]);
  if (!map) {
    for (let x = -120; x < width; x += 220) {
      polygon([[x, 150], [x + 120, 40], [x + 260, 150]], [79, 113, 120]);
      polygon([[x, 170], [x + 140, 85], [x + 300, 170]], [74, 123, 98]);
    }
    rect(0, 155, width, 205, [72, 112, 66]);
  }
  // Each 640px section has an accessible narrowing path to the distant ridge.
  for (let x = 0; x < width; x += 640) {
    if (map) {
      rect(x + 20, 235, 620, 95, [171, 151, 101]);
      rect(x + 270, 60, 90, 270, [171, 151, 101]);
      for (let i = 0; i < 7; i++) {
        const tx = x + 60 + i * 80;
        rect(tx, 130, 7, 30, [83, 60, 39]);
        polygon([[tx - 18, 145], [tx + 3, 100], [tx + 25, 145]], [36, 76, 54]);
      }
    } else {
      polygon([[x + 270, 120], [x + 370, 120], [x + 610, 340], [x + 30, 340]], [190, 160, 105]);
      polygon([[x + 280, 125], [x + 360, 125], [x + 580, 340], [x + 60, 340]], [205, 180, 125]);
      for (const [y, size] of [[145, 8], [200, 16], [280, 28]]) {
        const offset = Math.round((y - 120) * 1.1);
        for (const sign of [-1, 1]) {
          const tx = x + 320 + sign * (65 + offset);
          rect(tx, y - size, 4, size, [92, 67, 42]);
          polygon([[tx - size, y - size], [tx, y - size * 3], [tx + size, y - size]], [33, 77, 56]);
          rect(tx - 5, y, 14, 4, [50, 80, 48]);
        }
      }
      for (const [y, size] of [[145, 4], [200, 7], [280, 12], [320, 16]]) {
        rect(x + 315, y, size, 2, [155, 130, 91]);
      }
    }
  }
  // Bottom connection remains broad enough for horizontal travel.
  rect(20, 310, width - 40, 30, [190, 160, 105]);
  for (const x of [60, width - 60]) {
    rect(x - 2, 277, 5, 33, [83, 60, 39]);
    rect(x - 20, 272, 40, 15, [117, 83, 47]);
    rect(x - 15, 278, 29, 3, [233, 208, 147]);
  }
  return createPng(width, height, (x, y) => pixels.subarray((y * width + x) * 4, (y * width + x) * 4 + 4));
}

for (const world of ['reference', 'map', 'scroll']) {
  for (const [name, width, map] of [['demo-map', 1280, true], ['demo-depth', 640, false], ['demo-combined', 1280, false]]) {
    fs.writeFileSync(new URL('../worlds/' + world + '/assets/images/' + name + '.png', import.meta.url), landscape(width, map));
  }
}

// Small schematic overhead versions distinguish the two reference identities.
// Same 128px grid and right/back/front rows as the supplied adventure sheets.
for (const [id, shirt, hair] of [['mara', [84, 126, 155], [99, 52, 35]], ['bruno', [91, 115, 70], [53, 39, 32]]]) {
  const png = createPng(512, 384, (x, y) => {
    const frame = Math.floor(x / 128), row = Math.floor(y / 128);
    let u = x % 128 - 64, v = y % 128 - 64;
    if (row === 0) [u, v] = [v, -u]; // right-facing frame
    if (row === 2) [u, v] = [-u, -v];
    const step = frame % 2 === 0 ? -3 : 3;
    if ((u >= -18 && u < -5 && v >= 18 + step && v < 34 + step) ||
        (u >= 5 && u < 18 && v >= 18 - step && v < 34 - step)) return [30, 40, 51, 255];
    if (Math.abs(u) < 27 && v >= -7 && v < 24) return [...shirt, 255];
    if (Math.abs(u) >= 27 && Math.abs(u) < 34 && v >= -12 && v < 15) return [221, 171, 128, 255];
    if (Math.abs(u) < 18 && v >= -35 && v < -4) return [...(v < -26 ? [232, 184, 143] : hair), 255];
    return [0, 0, 0, 0];
  });
  fs.writeFileSync(new URL('../worlds/reference/assets/images/' + id + '-top-down.png', import.meta.url), png);
}
