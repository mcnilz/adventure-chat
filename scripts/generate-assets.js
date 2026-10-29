import fs from 'node:fs';
import path from 'node:path';
import zlib from 'node:zlib';

function createPng(width, height, getPixel) {
  // RGBA buffer: height rows, each with 1 filter byte (0) + width * 4 bytes
  const rowSize = 1 + width * 4;
  const rawData = Buffer.alloc(height * rowSize);

  let offset = 0;
  for (let y = 0; y < height; y++) {
    rawData[offset++] = 0; // Filter type: None
    for (let x = 0; x < width; x++) {
      const [r, g, b, a] = getPixel(x, y);
      rawData[offset++] = r;
      rawData[offset++] = g;
      rawData[offset++] = b;
      rawData[offset++] = a;
    }
  }

  const compressed = zlib.deflateSync(rawData);

  function crc32(buf) {
    let crc = 0xffffffff;
    for (let i = 0; i < buf.length; i++) {
      const byte = buf[i];
      crc = (crc >>> 8) ^ crcTable[(crc ^ byte) & 0xff];
    }
    return (crc ^ 0xffffffff) >>> 0;
  }

  const crcTable = new Uint32Array(256);
  for (let i = 0; i < 256; i++) {
    let c = i;
    for (let j = 0; j < 8; j++) {
      c = (c & 1) ? (0xedb88320 ^ (c >>> 1)) : (c >>> 1);
    }
    crcTable[i] = c >>> 0;
  }

  function makeChunk(type, data) {
    const len = data.length;
    const chunk = Buffer.alloc(8 + len + 4);
    chunk.writeUInt32BE(len, 0);
    chunk.write(type, 4, 4, 'ascii');
    data.copy(chunk, 8);
    const crc = crc32(chunk.subarray(4, 8 + len));
    chunk.writeUInt32BE(crc, 8 + len);
    return chunk;
  }

  const signature = Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]);

  const ihdrData = Buffer.alloc(13);
  ihdrData.writeUInt32BE(width, 0);
  ihdrData.writeUInt32BE(height, 4);
  ihdrData[8] = 8; // 8 bits per channel
  ihdrData[9] = 6; // RGBA
  ihdrData[10] = 0; // Deflate
  ihdrData[11] = 0; // Filter
  ihdrData[12] = 0; // Interlace

  const ihdrChunk = makeChunk('IHDR', ihdrData);
  const idatChunk = makeChunk('IDAT', compressed);
  const iendChunk = makeChunk('IEND', Buffer.alloc(0));

  return Buffer.concat([signature, ihdrChunk, idatChunk, iendChunk]);
}

// 1. Generate Lounge Background (640x360)
const loungePng = createPng(640, 360, (x, y) => {
  // Upper walls: y: 0 .. 239
  // Walkable floor: y: 240 .. 345, x: 40 .. 600
  // Wall baseboard: y: 235 .. 245
  
  if (y < 235) {
    // Wall area
    // Back wall window: x in 240..400, y in 40..160
    if (x >= 240 && x <= 400 && y >= 40 && y <= 160) {
      // Window frame
      if (x === 240 || x === 400 || y === 40 || y === 160 || x === 320 || y === 100) {
        return [180, 140, 90, 255]; // Wood frame
      }
      // Night sky with stars
      const isStar = ((x * 17 + y * 31) % 113 === 0);
      if (isStar) return [255, 255, 220, 255];
      const skyGradient = Math.floor(20 + (y - 40) * 0.2);
      return [15, 25, skyGradient + 40, 255];
    }

    // Doorway on right: x in 540..600, y in 90..235
    if (x >= 540 && x <= 600 && y >= 90) {
      if (x === 540 || x === 600 || y === 90) {
        return [140, 100, 60, 255]; // Door frame
      }
      return [40, 30, 25, 255]; // Dark inner door
    }

    // Painting on left: x in 80..160, y in 60..130
    if (x >= 80 && x <= 160 && y >= 60 && y <= 130) {
      if (x === 80 || x === 160 || y === 60 || y === 130) {
        return [210, 170, 70, 255]; // Gold frame
      }
      return [70, 110, 130, 255]; // Canvas
    }

    // Cozy wallpaper pattern
    const stripe = (Math.floor(x / 20) % 2 === 0);
    const wallR = stripe ? 45 : 50;
    const wallG = stripe ? 55 : 62;
    const wallB = stripe ? 80 : 90;
    return [wallR, wallG, wallB, 255];
  } else if (y < 245) {
    // Baseboard moulding
    return [110, 75, 45, 255];
  } else {
    // Floor area
    // Walkable region boundary indicators (x: 40..600, y: 245..345)
    const isWalkable = (x >= 40 && x <= 600 && y <= 345);
    
    // Persian style rug in middle: x in 180..460, y in 260..330
    if (x >= 180 && x <= 460 && y >= 260 && y <= 330) {
      const isBorder = (x <= 186 || x >= 454 || y <= 264 || y >= 326);
      if (isBorder) return [200, 160, 80, 255];
      const diamond = (Math.abs((x - 320) / 2) + Math.abs((y - 295))) % 20 < 4;
      if (diamond) return [170, 50, 45, 255];
      return [130, 35, 40, 255];
    }

    // Wooden planks
    const plankX = Math.floor(x / 40);
    const plankLine = (x % 40 === 0) || (y % 18 === 0);
    const shade = (plankX % 2 === 0) ? 10 : 0;
    
    if (plankLine) return [100, 65, 35, 255];
    
    if (isWalkable) {
      return [140 + shade, 95 + shade, 55 + shade, 255];
    } else {
      // Darker non-walkable margins
      return [80, 55, 30, 255];
    }
  }
});

// 2. Generate Character Sprite Sheet (4 frames of 32x48: Total 128x48)
// Frame 0: Idle / Stand
// Frame 1: Walk Step 1
// Frame 2: Walk Stand (Passing)
// Frame 3: Walk Step 2
const charWidth = 128;
const charHeight = 48;

const charPng = createPng(charWidth, charHeight, (x, y) => {
  const frame = Math.floor(x / 32);
  const fx = x % 32; // 0..31
  const fy = y;      // 0..47

  // Character dimensions: Center around fx = 16, height 48
  // Head: y in 4..16, x in 11..21
  // Hair: y in 3..9, x in 10..22
  // Eyes: y = 10, x = 18..19 (facing right)
  // Torso / Shirt: y in 17..31, x in 10..22
  // Belt: y in 31..32, x in 11..21
  // Legs / Pants: y in 33..43
  // Shoes: y in 44..47

  // Hair
  if (fy >= 3 && fy <= 8 && fx >= 10 && fx <= 22) {
    if (fy <= 5 && (fx < 12 || fx > 20)) return [0, 0, 0, 0];
    return [40, 25, 20, 255]; // Dark brown hair
  }

  // Face / Skin
  if (fy >= 9 && fy <= 16 && fx >= 11 && fx <= 21) {
    // Eye
    if (fy === 11 && (fx === 18 || fx === 19)) {
      return [20, 20, 20, 255];
    }
    // Smile
    if (fy === 14 && fx === 18) {
      return [180, 100, 90, 255];
    }
    return [255, 210, 175, 255]; // Skin tone
  }

  // Shirt / Jacket (Adventure Blue with beige vest)
  if (fy >= 17 && fy <= 30 && fx >= 9 && fx <= 23) {
    // Vest highlight
    if (fx >= 13 && fx <= 19 && fy >= 19 && fy <= 28) {
      return [210, 180, 130, 255]; // Beige vest
    }
    // Arm swinging per frame
    if (frame === 1 && fx < 12 && fy > 22) return [0, 0, 0, 0]; // Arm back
    if (frame === 3 && fx > 20 && fy > 22) return [0, 0, 0, 0]; // Arm front
    return [35, 95, 160, 255]; // Blue shirt
  }

  // Belt
  if (fy >= 31 && fy <= 32 && fx >= 10 && fx <= 22) {
    if (fx >= 15 && fx <= 17) return [220, 190, 60, 255]; // Buckle
    return [60, 40, 20, 255]; // Leather
  }

  // Legs / Pants (Brown trousers)
  if (fy >= 33 && fy <= 43) {
    if (frame === 0 || frame === 2) {
      // Standing legs
      if (fx >= 11 && fx <= 21 && fx !== 16) {
        return [90, 60, 40, 255];
      }
    } else if (frame === 1) {
      // Walk 1: Left leg forward, right leg back
      const leg1 = (fx >= 15 && fx <= 22 && (fy - 33) * 0.7 <= (fx - 15));
      const leg2 = (fx >= 9 && fx <= 15 && (fy - 33) * 0.7 <= (15 - fx));
      if (leg1 || leg2) return [90, 60, 40, 255];
    } else if (frame === 3) {
      // Walk 2: Right leg forward, left leg back
      const leg1 = (fx >= 16 && fx <= 23 && (fy - 33) * 0.7 <= (fx - 16));
      const leg2 = (fx >= 8 && fx <= 14 && (fy - 33) * 0.7 <= (14 - fx));
      if (leg1 || leg2) return [90, 60, 40, 255];
    }
  }

  // Shoes
  if (fy >= 44 && fy <= 47) {
    if (frame === 0 || frame === 2) {
      if (fx >= 10 && fx <= 22 && fx !== 16) return [30, 20, 15, 255];
    } else if (frame === 1) {
      if ((fx >= 17 && fx <= 24) || (fx >= 7 && fx <= 13)) return [30, 20, 15, 255];
    } else if (frame === 3) {
      if ((fx >= 18 && fx <= 25) || (fx >= 6 && fx <= 12)) return [30, 20, 15, 255];
    }
  }

  return [0, 0, 0, 0]; // Transparent
});

const targetDir = path.join('src', 'AdventureChat.Server', 'wwwroot', 'assets', 'images');
fs.mkdirSync(targetDir, { recursive: true });

fs.writeFileSync(path.join(targetDir, 'lounge.png'), loungePng);
fs.writeFileSync(path.join(targetDir, 'character.png'), charPng);
console.log('Assets created successfully in ' + targetDir);
