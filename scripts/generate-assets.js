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

// 1. Generate Classic Lucasfilm Adventure Pirate Tavern Background (640x360)
// Inspired by iconic Lucasfilm SCUMM aesthetics (The Secret of Monkey Island / Scumm Bar / Woodtick)
const loungePng = createPng(640, 360, (x, y) => {
  // Upper walls: y: 0 .. 235
  // Baseboard: y: 235 .. 242
  // Walkable floor: y: 240 .. 345, x: 40 .. 600
  // Non-walkable lower/side margins: y > 345 or x < 40 or x > 600

  // Helper distance
  const dist = (x1, y1, x2, y2) => Math.hypot(x1 - x2, y1 - y2);

  if (y < 235) {
    // -------------------------------------------------------------
    // CEILING BEAMS (y: 0 .. 26)
    // -------------------------------------------------------------
    if (y <= 24) {
      const woodShade = (x % 32 === 0 || y === 0 || y === 24) ? -30 : 0;
      const grain = (Math.sin(x * 0.15 + y * 0.4) > 0.6) ? 12 : 0;
      return [
        Math.max(0, Math.min(255, 45 + woodShade + grain)),
        Math.max(0, Math.min(255, 26 + woodShade + grain)),
        Math.max(0, Math.min(255, 14 + woodShade + grain)),
        255
      ];
    }

    // -------------------------------------------------------------
    // LEFT & RIGHT SUPPORT PILLARS (x: 0..38, x: 602..640)
    // -------------------------------------------------------------
    if (x <= 38 || x >= 602) {
      const isEdge = x === 0 || x === 38 || x === 602 || x === 639 || (y % 40 === 0);
      const pillarShade = isEdge ? -25 : 0;
      const pillarGrain = (x % 6 === 0) ? -10 : 5;
      return [
        Math.max(0, Math.min(255, 50 + pillarShade + pillarGrain)),
        Math.max(0, Math.min(255, 30 + pillarShade + pillarGrain)),
        Math.max(0, Math.min(255, 18 + pillarShade + pillarGrain)),
        255
      ];
    }

    // -------------------------------------------------------------
    // ARCHED WINDOW OVERLOOKING CARIBBEAN NIGHT (x in 230..410, y in 35..175)
    // -------------------------------------------------------------
    const winCenterX = 320;
    const winTopY = 85; // Center of arch
    const winRadius = 88;
    const isInsideArch = (y <= winTopY && dist(x, y, winCenterX, winTopY) <= winRadius) ||
                         (y > winTopY && y <= 175 && x >= 232 && x <= 408);
    const isArchFrame = (y <= winTopY && Math.abs(dist(x, y, winCenterX, winTopY) - winRadius) <= 4) ||
                        (y > winTopY && y <= 175 && (Math.abs(x - 232) <= 3 || Math.abs(x - 408) <= 3 || y >= 172));
    const isMullion = isInsideArch && (Math.abs(x - 320) <= 2 || Math.abs(y - 105) <= 2 || Math.abs(y - 145) <= 2);

    if (isArchFrame || isMullion) {
      return [90, 55, 30, 255]; // Dark polished oak window frame
    }

    if (isInsideArch) {
      // Night Sky Gradient (Deep sapphire to purple-night)
      const skyGrad = (y - 35) / 140; // 0..1
      const skyR = Math.floor(10 + skyGrad * 12);
      const skyG = Math.floor(18 + skyGrad * 20);
      const skyB = Math.floor(45 + skyGrad * 40);

      // Crescent Moon (Center: x=365, y=65, radius=14)
      const moonDist = dist(x, y, 365, 65);
      const cutDist = dist(x, y, 360, 61);
      if (moonDist <= 14 && cutDist > 11) {
        if (moonDist <= 12) return [255, 250, 210, 255]; // Bright moon core
        return [235, 215, 140, 255]; // Moon edge
      }
      // Moon soft glow halo
      if (moonDist <= 22) {
        return [skyR + 25, skyG + 25, skyB + 20, 255];
      }

      // Stars
      const starHash = (x * 47 + y * 89) % 223;
      if (starHash === 0 && y < 130) {
        return [255, 255, 230, 255]; // Bright star
      }
      if (starHash === 17 && y < 120) {
        return [180, 210, 255, 220]; // Faint blue star
      }

      // Distant Island Mountains & Sea Horizon at y >= 140
      if (y >= 148) {
        // Sea water
        const wave = ((x + y * 3) % 8 === 0) ? 15 : 0;
        return [8, 20 + wave, 40 + wave, 255];
      } else if (y >= 135) {
        // Distant hills / coastline
        const hillY = 148 - Math.sin((x - 230) * 0.04) * 8;
        if (y >= hillY) {
          return [12, 18, 30, 255]; // Dark hill silhouette
        }
      }

      // Distant Pirate Ship Silhouette (at x: 260..295, y: 122..148)
      if (x >= 265 && x <= 290 && y >= 122 && y <= 148) {
        // Main Mast (x=278, y: 122..148)
        if (x === 278) return [5, 10, 20, 255];
        // Yardarms / Sails (y=128, y=136)
        if ((y === 128 && x >= 270 && x <= 286) || (y === 136 && x >= 268 && x <= 288)) {
          return [5, 10, 20, 255];
        }
        // Hull (y: 142..147, x: 266..290)
        if (y >= 142 && y <= 147 && x >= (266 + (147 - y))) {
          return [5, 10, 20, 255];
        }
      }

      // Palm Tree silhouette on left window edge (x: 238..255, y: 110..148)
      if (x >= 238 && x <= 252 && y >= 110 && y <= 148) {
        // Trunk
        if (x === 242 + Math.floor((148 - y) * 0.1)) return [8, 14, 22, 255];
        // Leaves
        if (y <= 122 && (Math.abs(x - 245) + Math.abs(y - 116) <= 8)) return [8, 14, 22, 255];
      }

      return [skyR, skyG, skyB, 255];
    }

    // -------------------------------------------------------------
    // STONE FIREPLACE (x in 52..168, y in 80..235)
    // -------------------------------------------------------------
    if (x >= 52 && x <= 168 && y >= 80) {
      // Hearth Mantlepiece (y: 80..92, x: 48..172)
      if (y <= 92) {
        const isMantleEdge = y === 80 || y === 92 || x === 52 || x === 168;
        return isMantleEdge ? [65, 40, 22, 255] : [110, 70, 40, 255];
      }

      // Pirate Skull on Mantlepiece (x: 100..120, y: 64..80) - handled below if y < 80

      // Fireplace Cavity (Arch opening: x in 76..144, y in 125..235)
      const fireArchTopY = 145;
      const fireCenterX = 110;
      const isFireCavity = (y >= fireArchTopY && x >= 76 && x <= 144) ||
                           (y < fireArchTopY && dist(x, y, fireCenterX, fireArchTopY) <= 34);

      if (isFireCavity) {
        // Fire & Logs (x: 90..130, y: 185..235)
        if (x >= 88 && x <= 132 && y >= 180) {
          // Fire burning logs
          if (y >= 218) {
            return [40, 20, 10, 255]; // Dark burning wood
          }
          // Roaring Flames
          const flameDist = dist(x, y, 110, 218);
          const flicker = ((x * 13 + y * 7) % 5);
          if (flameDist < 16 + flicker) {
            return [255, 240, 80, 255]; // Bright yellow center
          } else if (flameDist < 26 + flicker) {
            return [255, 130, 20, 255]; // Orange flame
          } else if (flameDist < 34 + flicker) {
            return [210, 40, 10, 255];  // Crimson flame tips
          }
        }
        // Fire soot cavity background
        const glow = Math.max(0, 40 - Math.floor(dist(x, y, 110, 210) * 0.6));
        return [25 + glow, 15 + Math.floor(glow * 0.6), 12, 255];
      }

      // Stone Bricks of Fireplace
      const brickH = 14;
      const brickW = 28;
      const row = Math.floor((y - 92) / brickH);
      const colOffset = (row % 2 === 1) ? 14 : 0;
      const col = Math.floor((x - 52 + colOffset) / brickW);
      const isMortar = ((y - 92) % brickH === 0) || ((x - 52 + colOffset) % brickW === 0);

      if (isMortar) return [45, 40, 38, 255];
      const brickTone = ((row * 7 + col * 13) % 4) * 8;
      return [105 + brickTone, 95 + brickTone, 85 + brickTone, 255];
    }

    // Pirate Skull on Mantlepiece (x: 98..122, y: 62..80)
    if (x >= 98 && x <= 122 && y >= 62 && y < 80) {
      // Skull bandana (y: 62..68)
      if (y <= 68) return [190, 30, 30, 255]; // Red pirate bandana
      // Skull bone (y: 69..79, x: 102..118)
      if (x >= 102 && x <= 118) {
        // Eye sockets (y=72..74, x=106, 114)
        if (y >= 72 && y <= 74 && (x === 106 || x === 107 || x === 113 || x === 114)) {
          return [20, 20, 20, 255];
        }
        return [230, 225, 205, 255]; // White skull bone
      }
    }

    // -------------------------------------------------------------
    // PIRATE CAPTAIN PORTRAIT (x in 178..222, y in 60..120)
    // -------------------------------------------------------------
    if (x >= 178 && x <= 222 && y >= 60 && y <= 120) {
      const isGoldFrame = x === 178 || x === 179 || x === 221 || x === 222 ||
                          y === 60 || y === 61 || y === 119 || y === 120;
      if (isGoldFrame) return [220, 180, 60, 255]; // Gold frame

      // Portrait background
      if (y >= 62 && y <= 80) {
        // Pirate Tricorn Hat
        if (y <= 74 && Math.abs(x - 200) <= 16) return [20, 20, 25, 255];
        // Face & Beard
        if (y > 74 && Math.abs(x - 200) <= 8) {
          if (y >= 78) return [40, 25, 15, 255]; // Black beard
          return [230, 180, 140, 255]; // Face
        }
      }
      // Red Captain Coat
      if (y > 80) return [140, 30, 35, 255];
      return [35, 55, 65, 255];
    }

    // -------------------------------------------------------------
    // GROG / RUM BARREL WITH BRASS TAP (x in 460..525, y in 145..235)
    // -------------------------------------------------------------
    if (x >= 460 && x <= 525 && y >= 145) {
      // Wooden barrel body with bulging curve
      const barrelMidX = 492;
      const barrelMidY = 188;
      const barrelRx = 30;
      const barrelRy = 42;
      const dx = (x - barrelMidX) / barrelRx;
      const dy = (y - barrelMidY) / barrelRy;
      if (dx * dx + dy * dy <= 1.0) {
        // Iron hoops (y around 155, 172, 204, 222)
        if (Math.abs(y - 155) <= 2 || Math.abs(y - 172) <= 2 || Math.abs(y - 204) <= 2 || Math.abs(y - 222) <= 2) {
          return [60, 65, 70, 255]; // Iron bands
        }
        // Wood staves vertical lines
        const isStave = (Math.abs(x - 492) % 10 === 0);
        if (isStave) return [70, 40, 20, 255];

        // Brass Tap (x: 488..498, y: 194..202)
        if (x >= 488 && x <= 498 && y >= 194 && y <= 202) {
          return [220, 185, 50, 255]; // Shiny brass spigot
        }

        return [120, 75, 42, 255]; // Warm wood staves
      }
      // Barrel wooden stand legs
      if (y >= 225 && (Math.abs(x - 470) <= 4 || Math.abs(x - 515) <= 4)) {
        return [70, 42, 20, 255];
      }
    }

    // -------------------------------------------------------------
    // ARCHED DOORWAY TO DOCKS / HARBOR ON RIGHT (x in 542..598, y in 80..235)
    // -------------------------------------------------------------
    if (x >= 542 && x <= 598 && y >= 80) {
      const doorArchY = 120;
      const doorCenterX = 570;
      const isDoorArch = (y >= doorArchY) || (dist(x, y, doorCenterX, doorArchY) <= 28);
      const isDoorFrame = (Math.abs(x - 542) <= 3 || Math.abs(x - 598) <= 3 || (y <= doorArchY && Math.abs(dist(x, y, doorCenterX, doorArchY) - 28) <= 3));

      if (isDoorFrame) return [100, 60, 30, 255]; // Oak doorframe
      if (isDoorArch) {
        // Deep warm lantern glow from outside tavern
        const glow = Math.floor((y - 80) * 0.3);
        return [30 + glow, 22 + Math.floor(glow * 0.8), 18, 255];
      }
    }

    // -------------------------------------------------------------
    // HANGING BRASS SHIP LANTERN / CHANDELIER (x: 310..330, y: 20..68)
    // -------------------------------------------------------------
    if (x >= 310 && x <= 330 && y >= 20 && y <= 68) {
      // Chain
      if (x === 320 && y <= 42) return [160, 130, 50, 255];
      // Lantern body (y: 42..68, x: 312..328)
      if (y >= 42) {
        const isFrame = x === 312 || x === 328 || y === 42 || y === 68 || x === 320;
        if (isFrame) return [190, 150, 50, 255]; // Brass frame
        // Glowing Amber Flame Core
        return [255, 220, 100, 255];
      }
    }

    // -------------------------------------------------------------
    // TAVERN WALLPAPER / WOOD PANELLING
    // -------------------------------------------------------------
    // Ambient fire glow on left wall
    const fireGlow = Math.max(0, 50 - Math.floor(dist(x, y, 110, 180) * 0.25));

    // Wainscoting lower wall (y: 160..235)
    if (y >= 160) {
      const panel = (Math.floor(x / 30) % 2 === 0);
      const panelBorder = (x % 30 === 0) || (y === 160) || (y === 234);
      if (panelBorder) return [50 + fireGlow, 30 + Math.floor(fireGlow * 0.7), 18, 255];
      const pR = (panel ? 75 : 85) + fireGlow;
      const pG = (panel ? 48 : 55) + Math.floor(fireGlow * 0.7);
      const pB = (panel ? 30 : 35);
      return [pR, pG, pB, 255];
    }

    // Upper Wall Nautical Wallpaper (Deep teal with vintage diamond pattern)
    const diamond = (Math.abs((x % 32) - 16) + Math.abs((y % 32) - 16)) <= 3;
    const baseR = (diamond ? 45 : 35) + fireGlow;
    const baseG = (diamond ? 65 : 55) + Math.floor(fireGlow * 0.7);
    const baseB = (diamond ? 85 : 75);
    return [baseR, baseG, baseB, 255];

  } else if (y < 242) {
    // -------------------------------------------------------------
    // BASEBOARD MOULDING TRIM (y: 235 .. 242)
    // -------------------------------------------------------------
    const isTrimLine = (y === 235 || y === 241);
    return isTrimLine ? [65, 40, 20, 255] : [110, 72, 40, 255];

  } else {
    // -------------------------------------------------------------
    // TAVERN FLOOR AREA (y: 242 .. 360)
    // Walkable floor region: x in 40..600, y in 240..345
    // -------------------------------------------------------------
    const isWalkable = (x >= 40 && x <= 600 && y <= 345);

    // Warm radial lighting from Fireplace (left) and Chandelier (center)
    const fireLight = Math.max(0, 45 - Math.floor(dist(x, y, 110, 240) * 0.18));
    const centerLight = Math.max(0, 30 - Math.floor(dist(x, y, 320, 280) * 0.15));
    const totalLight = fireLight + centerLight;

    // ORNATE PERSIAN / PIRATE CAPTAIN RUG (x in 170..470, y in 255..335)
    if (x >= 170 && x <= 470 && y >= 255 && y <= 335) {
      // Rug Fringe (left x: 170..174, right x: 466..470)
      if (x <= 174 || x >= 466) {
        return [220, 200, 140, 255]; // Gold/ivory fringe tassels
      }

      // Outer Gold / Navy Border (within 8px of rug bounds)
      const isOuterBorder = (x <= 184 || x >= 456 || y <= 263 || y >= 327);
      if (isOuterBorder) {
        const borderStripe = ((x + y) % 8 < 4);
        return borderStripe ? [215 + totalLight, 175 + totalLight, 60, 255] : [25, 45, 80, 255];
      }

      // Inner Medallions & Persian Patterns
      const medallion1 = dist(x, y, 240, 295) <= 22;
      const medallion2 = dist(x, y, 320, 295) <= 26;
      const medallion3 = dist(x, y, 400, 295) <= 22;

      if (medallion1 || medallion2 || medallion3) {
        const medCenter = medallion1 ? 240 : (medallion2 ? 320 : 400);
        const medD = dist(x, y, medCenter, 295);
        if (medD < 6) return [235 + totalLight, 200 + totalLight, 80, 255]; // Gold center
        if (medD < 14) return [25, 60, 110, 255];  // Royal Navy blue ring
        return [210 + totalLight, 160 + totalLight, 55, 255]; // Gold outer ring
      }

      // Rich Crimson / Ruby field with floral micro-diamonds
      const diamondPat = (Math.abs((x - 320) % 20) + Math.abs((y - 295) % 16)) <= 3;
      if (diamondPat) return [190 + totalLight, 70 + totalLight, 50, 255];

      return [145 + totalLight, 35 + Math.floor(totalLight * 0.5), 38, 255]; // Deep velvet red
    }

    // WOODEN FLOOR PLANKS
    const plankWidth = 48;
    const plankX = Math.floor(x / plankWidth);
    const rowY = Math.floor((y - 242) / 20);
    const plankStagger = (rowY % 2 === 1) ? 24 : 0;
    const plankIndex = Math.floor((x + plankStagger) / plankWidth);

    // Plank Lines & Nails
    const isPlankLineX = ((x + plankStagger) % plankWidth === 0);
    const isPlankLineY = ((y - 242) % 20 === 0);
    const isNail = isPlankLineX && (y % 20 === 3 || y % 20 === 17);

    if (isNail) return [40, 40, 45, 255]; // Iron nail head
    if (isPlankLineX || isPlankLineY) return [60, 38, 20, 255]; // Dark plank seam

    // Wood Grain Texture & Shading
    const woodTone = ((plankIndex * 13 + rowY * 7) % 5) * 8;
    const grainNoise = (Math.sin(x * 0.2 + y * 0.6) > 0.5) ? 8 : 0;

    if (isWalkable) {
      // Warm, welcoming tavern floorboards
      const baseR = 135 + woodTone + totalLight + grainNoise;
      const baseG = 88 + Math.floor((woodTone + totalLight) * 0.8) + grainNoise;
      const baseB = 52 + Math.floor(totalLight * 0.4);
      return [
        Math.max(0, Math.min(255, baseR)),
        Math.max(0, Math.min(255, baseG)),
        Math.max(0, Math.min(255, baseB)),
        255
      ];
    } else {
      // Darker shadows in non-walkable margins
      const darkR = 75 + woodTone;
      const darkG = 48 + Math.floor(woodTone * 0.7);
      const darkB = 28;
      return [darkR, darkG, darkB, 255];
    }
  }
});

// 2. Generate Iconic Lucasfilm Adventure Protagonist Sprite Sheet (Guybrush Threepwood inspired)
// 4 frames of 32x48: Total 128x48
// Frame 0: Idle / Stand
// Frame 1: Walk Step 1 (Left forward, right back)
// Frame 2: Walk Stand (Passing pose with slight vertical bob)
// Frame 3: Walk Step 2 (Right forward, left back)
const charWidth = 128;
const charHeight = 48;

const charPng = createPng(charWidth, charHeight, (x, y) => {
  const frame = Math.floor(x / 32);
  const fx = x % 32; // 0..31
  let fy = y;        // 0..47

  // Slight 1px vertical bob on passing frame (frame 2) for authentic bouncy SCUMM walk cycle
  if (frame === 2 && fy >= 2) {
    fy = fy + 1;
  }

  // -----------------------------------------------------------------
  // HAIR: Blonde / Golden with ponytail (Guybrush signature)
  // y in 3..14, x in 10..22
  // -----------------------------------------------------------------
  // Ponytail tied behind (x: 8..11, y: 8..15)
  if (fx >= 8 && fx <= 11 && fy >= 8 && fy <= 15) {
    if (fy >= 9 && fy <= 10 && fx === 11) return [40, 20, 20, 255]; // Ponytail band
    return (fy % 2 === 0) ? [235, 195, 65, 255] : [195, 150, 40, 255]; // Blonde hair
  }

  // Top hair volume & bangs (y: 3..8, x: 11..22)
  if (fy >= 3 && fy <= 8 && fx >= 11 && fx <= 22) {
    if (fy === 3 && (fx < 13 || fx > 20)) return [0, 0, 0, 0]; // Rounded top
    // Blonde highlight
    if (fy <= 5 && fx >= 14 && fx <= 19) return [255, 225, 95, 255]; // Bright blonde
    return [220, 180, 50, 255]; // Golden blonde
  }

  // Back hair & sideburns (y: 9..14, x: 10..13)
  if (fy >= 9 && fy <= 14 && fx >= 10 && fx <= 13) {
    return [180, 140, 35, 255];
  }

  // -----------------------------------------------------------------
  // FACE & SKIN (y in 8..16, x in 13..22)
  // -----------------------------------------------------------------
  if (fy >= 8 && fy <= 16 && fx >= 13 && fx <= 22) {
    // Expressive Eye (y=11, fx=18..19)
    if (fy === 11 && fx === 19) return [20, 20, 20, 255]; // Dark pupil
    if (fy === 11 && fx === 18) return [255, 255, 255, 255]; // White eye catchlight
    // Eyebrow (y=10, fx=18..20)
    if (fy === 10 && fx >= 18 && fx <= 20) return [140, 100, 30, 255]; // Blonde eyebrow

    // Pirate Smirk / Mouth (y=14, fx=18..19)
    if (fy === 14 && (fx === 18 || fx === 19)) return [180, 85, 75, 255];

    // Nose bridge (y=12, fx=21)
    if (fy === 12 && fx === 21) return [240, 175, 140, 255];

    // Peach Skin Tone with shading
    if (fx <= 14 || fy === 16) return [235, 180, 145, 255]; // Shadow
    return [255, 212, 178, 255]; // Warm skin highlight
  }

  // -----------------------------------------------------------------
  // RUFFLED PIRATE SHIRT & ROYAL BLUE VEST (y in 17..30, x in 8..24)
  // -----------------------------------------------------------------
  if (fy >= 17 && fy <= 30 && fx >= 8 && fx <= 24) {
    // Open Vest & Puffy White Shirt Layout:
    // Center chest: White linen pirate shirt (fx in 14..18)
    // Left/Right: Royal blue / dark navy vest (fx in 10..13 and fx in 19..22)

    // Center Ruffled Pirate Shirt
    const isShirtChest = (fx >= 14 && fx <= 18 && fy <= 28);
    if (isShirtChest) {
      if (fy === 17 || fy === 18) return [255, 255, 255, 255]; // Ruffled collar
      const shirtFold = (fy % 3 === 0);
      return shirtFold ? [225, 222, 215, 255] : [250, 248, 242, 255]; // Pure linen white
    }

    // Arm animation per frame (Puffy white sleeves)
    if (frame === 1) {
      // Walk 1: Left arm forward (fx > 20), Right arm back (fx < 11)
      if (fx >= 20 && fy >= 19 && fy <= 27) return [245, 240, 235, 255]; // Front puffy sleeve
      if (fx <= 11 && fy >= 19 && fy <= 26) return [210, 205, 195, 255]; // Back sleeve shadow
      // Hands
      if (fx >= 21 && fy >= 28 && fy <= 29) return [255, 212, 178, 255];
    } else if (frame === 3) {
      // Walk 2: Right arm forward (fx < 12), Left arm back (fx > 19)
      if (fx <= 12 && fy >= 19 && fy <= 27) return [245, 240, 235, 255]; // Front sleeve
      if (fx >= 20 && fy >= 19 && fy <= 26) return [210, 205, 195, 255]; // Back sleeve
      // Hands
      if (fx <= 11 && fy >= 28 && fy <= 29) return [255, 212, 178, 255];
    } else {
      // Stand / Passing arms at sides
      if ((fx === 9 || fx === 23) && fy >= 20 && fy <= 27) return [240, 235, 230, 255];
      // Hands
      if ((fx === 9 || fx === 23) && fy >= 28 && fy <= 29) return [255, 212, 178, 255];
    }

    // Vest Body (Royal Blue with gold trim buttons)
    if (fx >= 10 && fx <= 22) {
      // Gold button / trim along vest edge
      if ((fx === 13 || fx === 19) && (fy === 21 || fy === 25)) {
        return [235, 195, 50, 255]; // Gold button
      }
      // Vest shading
      if (fx <= 11 || fx >= 21) return [20, 45, 85, 255]; // Dark blue shadow
      return [35, 75, 140, 255]; // Classic Lucasfilm Royal Blue vest
    }
  }

  // -----------------------------------------------------------------
  // PIRATE SASH & BELT (y in 29..32, x in 10..22)
  // -----------------------------------------------------------------
  if (fy >= 29 && fy <= 32 && fx >= 10 && fx <= 22) {
    // Golden Belt Buckle (center: fx in 15..17, fy in 30..31)
    if (fx >= 15 && fx <= 17 && fy >= 30 && fy <= 31) {
      return [255, 215, 0, 255]; // Bright Gold buckle
    }
    // Crimson Pirate Sash
    const sashFold = (fx % 2 === 0);
    return sashFold ? [220, 35, 35, 255] : [175, 20, 20, 255]; // Red pirate sash
  }

  // -----------------------------------------------------------------
  // PANTS / BREECHES (y in 33..40)
  // -----------------------------------------------------------------
  if (fy >= 33 && fy <= 40) {
    if (frame === 0 || frame === 2) {
      // Standing legs
      if (fx >= 12 && fx <= 20 && fx !== 16) {
        return (fx === 12 || fx === 20) ? [25, 30, 42, 255] : [38, 48, 65, 255]; // Navy breeches
      }
    } else if (frame === 1) {
      // Walk 1: Left leg forward (fx: 16..22), Right leg back (fx: 10..15)
      const legFwd = (fx >= 15 && fx <= 22 && (fy - 33) * 0.8 <= (fx - 14));
      const legBwd = (fx >= 10 && fx <= 15 && (fy - 33) * 0.8 <= (16 - fx));
      if (legFwd || legBwd) {
        return (legBwd) ? [25, 30, 42, 255] : [42, 52, 70, 255];
      }
    } else if (frame === 3) {
      // Walk 2: Right leg forward (fx: 16..23), Left leg back (fx: 9..14)
      const legFwd = (fx >= 16 && fx <= 23 && (fy - 33) * 0.8 <= (fx - 15));
      const legBwd = (fx >= 9 && fx <= 14 && (fy - 33) * 0.8 <= (15 - fx));
      if (legFwd || legBwd) {
        return (legBwd) ? [25, 30, 42, 255] : [42, 52, 70, 255];
      }
    }
  }

  // -----------------------------------------------------------------
  // TALL PIRATE LEATHER BOOTS WITH CUFFS (y in 41..47)
  // -----------------------------------------------------------------
  if (fy >= 41 && fy <= 47) {
    if (frame === 0 || frame === 2) {
      // Standing boots
      if (fx >= 11 && fx <= 21 && fx !== 16) {
        if (fy === 41 || fy === 42) return [110, 68, 35, 255]; // Turned-down leather boot cuff
        if (fy >= 46) return [30, 18, 10, 255]; // Sole / Heel
        return [70, 42, 22, 255]; // Dark leather boot
      }
    } else if (frame === 1) {
      // Walk 1 Stride Boots
      const bootFwd = (fx >= 17 && fx <= 24);
      const bootBwd = (fx >= 8 && fx <= 13);
      if (bootFwd || bootBwd) {
        if (fy <= 42) return [110, 68, 35, 255]; // Boot cuff
        if (fy >= 46) return [30, 18, 10, 255]; // Boot sole
        return bootBwd ? [50, 30, 15, 255] : [75, 45, 24, 255];
      }
    } else if (frame === 3) {
      // Walk 2 Stride Boots
      const bootFwd = (fx >= 18 && fx <= 25);
      const bootBwd = (fx >= 7 && fx <= 12);
      if (bootFwd || bootBwd) {
        if (fy <= 42) return [110, 68, 35, 255]; // Boot cuff
        if (fy >= 46) return [30, 18, 10, 255]; // Boot sole
        return bootBwd ? [50, 30, 15, 255] : [75, 45, 24, 255];
      }
    }
  }

  return [0, 0, 0, 0]; // Transparent
});

const targetDir = path.join('src', 'AdventureChat.Server', 'wwwroot', 'assets', 'images');
fs.mkdirSync(targetDir, { recursive: true });

fs.writeFileSync(path.join(targetDir, 'lounge.png'), loungePng);
fs.writeFileSync(path.join(targetDir, 'character.png'), charPng);
console.log('Assets created successfully in ' + targetDir);
