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
    // ARCHED DOORWAY TO GARTEN ON RIGHT (x in 535..605, y in 55..235)
    // -------------------------------------------------------------
    // 1. Hanging Wooden Sign above Doorway (x: 540..600, y: 55..75)
    if (x >= 540 && x <= 600 && y >= 55 && y <= 75) {
      if ((x === 548 || x === 592) && y <= 61) {
        return [140, 110, 40, 255]; // Brass/iron chain link
      }
      if (y >= 61) {
        const isBorder = (y === 61 || y === 75 || x === 540 || x === 600);
        if (isBorder) return [200, 160, 60, 255]; // Gold frame

        // Carved arrow pointing right
        const isArrow = (y === 68 && x >= 582 && x <= 594) ||
                        (Math.abs(x - 592) === Math.abs(y - 68) && x <= 593 && x >= 589);
        if (isArrow) return [255, 235, 120, 255]; // Bright gold arrow

        return [85, 45, 22, 255]; // Dark polished oak sign
      }
    }

    // 2. Doorway Arch, Lantern & Outdoor Garden View (x: 540..600, y: 78..235)
    if (x >= 540 && x <= 600 && y >= 78) {
      const doorArchY = 120;
      const doorCenterX = 570;
      const isDoorArch = (y >= doorArchY && x >= 544 && x <= 596) || (dist(x, y, doorCenterX, doorArchY) <= 26);
      const isDoorFrame = (Math.abs(x - 542) <= 3 || Math.abs(x - 598) <= 3 || (y <= doorArchY && Math.abs(dist(x, y, doorCenterX, doorArchY) - 28) <= 3));

      // Brass lantern on left side of frame (x: 530..538, y: 105..125)
      if (x >= 530 && x <= 538 && y >= 105 && y <= 125) {
        const isLanternFrame = x === 530 || x === 538 || y === 105 || y === 125;
        if (isLanternFrame) return [180, 140, 40, 255];
        return [255, 225, 120, 255]; // Glowing lantern
      }

      if (isDoorFrame) return [120, 75, 38, 255]; // Sturdy Oak doorframe
      if (isDoorArch) {
        // Moonlit garden view through open doorway
        if (y < 155) {
          const skyGrad = (y - 85) / 70;
          const sR = Math.floor(12 + skyGrad * 10);
          const sG = Math.floor(22 + skyGrad * 18);
          const sB = Math.floor(48 + skyGrad * 24);
          if ((x * 31 + y * 67) % 73 === 0) return [255, 255, 220, 255]; // Stars
          return [sR, sG, sB, 255];
        }
        if (y < 185) {
          const treeH = 165 + Math.sin(x * 0.15) * 6;
          if (y >= treeH) return [18, 42, 28, 255]; // Garden bushes
          return [18, 30, 55, 255];
        }
        // Cobblestones
        const isStone = ((x + y * 2) % 10 < 2);
        if (isStone) return [38, 48, 44, 255];
        return [65, 75, 70, 255];
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

// 2. Generate Iconic Lucasfilm Adventure Big-Head Character Sprite Sheet
// Inspired by Maniac Mansion & Zak McKracken (classic oversized head proportion, distinct features)
// Dimensions: 4 frames of 48x64 per row, 3 rows (Side, Back, Front): Total 192x192
// Row 0 (y: 0..63): Side View (facing Right; flipped horizontally for Left)
// Row 1 (y: 64..127): Back View (facing Up / towards back of room, Rückenansicht)
// Row 2 (y: 128..191): Front View (facing Down / towards front of room / camera)
const charWidth = 192;
const charHeight = 192;
const frameWidth = 48;
const frameHeight = 64;

const charPng = createPng(charWidth, charHeight, (x, y) => {
  const row = Math.floor(y / frameHeight); // 0: Side, 1: Back, 2: Front
  const frame = Math.floor(x / frameWidth); // 0..3: Idle, Walk1, Pass, Walk2
  const fx = x % frameWidth; // 0..47 (center at fx = 24)
  let fy = y % frameHeight; // 0..63 (feet at fy = 63)

  // Subtle 1px vertical bob on passing frame (frame 2) for authentic bouncy SCUMM walk cycle
  if (frame === 2 && fy >= 4) {
    fy = fy + 1;
  }

  // Helper distance
  const dist = (x1, y1, x2, y2) => Math.hypot(x1 - x2, y1 - y2);

  // =================================================================
  // ROW 0: SIDE VIEW (Facing Right)
  // =================================================================
  if (row === 0) {
    // ---------------------------------------------------------------
    // 1. BIG HEAD & HAIR (y in 4..32, x in 12..38) - Maniac Mansion / Zak Proportion
    // ---------------------------------------------------------------
    // Top & Back Hair Volume (fx: 12..34, fy: 4..16)
    const isTopHair = (fy >= 4 && fy <= 13 && fx >= 14 && fx <= 34);
    const isBackHair = (fy >= 10 && fy <= 26 && fx >= 12 && fx <= 18);
    const isSideburn = (fy >= 17 && fy <= 23 && fx >= 18 && fx <= 21);

    if (isTopHair || isBackHair || isSideburn) {
      if (fy === 4 && (fx < 18 || fx > 30)) return [0, 0, 0, 0]; // Rounded top
      if (fy <= 7 && fx >= 20 && fx <= 28) return [75, 45, 30, 255]; // Hair highlight (Classic brown)
      return [42, 24, 16, 255]; // Rich dark brown hair
    }

    // Ear (Side view: fx: 18..21, fy: 18..22)
    if (fx >= 18 && fx <= 21 && fy >= 18 && fy <= 22) {
      if (fx === 20 && fy === 20) return [215, 145, 115, 255]; // Ear inner shadow
      return [255, 205, 170, 255]; // Ear lobe
    }

    // Face / Skin area (fx: 18..36, fy: 12..31)
    const isFace = (fy >= 12 && fy <= 31 && fx >= 18 && fx <= 36);
    if (isFace) {
      // Rounded jaw/chin boundary
      if (fy >= 28 && fx < 22) return [0, 0, 0, 0];
      if (fy >= 30 && (fx < 24 || fx > 32)) return [0, 0, 0, 0];

      // Large Expressive Maniac Mansion / Zak Eye (fx: 27..32, fy: 14..18)
      // Eyebrow (fy: 13..14, fx: 26..33)
      if (fy <= 14 && fx >= 26 && fx <= 33) return [30, 18, 12, 255]; // Thick expressive eyebrow

      if (fy >= 15 && fy <= 18 && fx >= 27 && fx <= 32) {
        if (fx >= 30 && fx <= 31 && fy >= 16 && fy <= 17) return [15, 15, 20, 255]; // Pupil
        if (fx === 30 && fy === 15) return [255, 255, 255, 255]; // Eye catchlight
        return [245, 245, 248, 255]; // Large white sclera
      }

      // Iconic Prominent Nose (fx: 33..37, fy: 19..22)
      if (fy >= 19 && fy <= 22 && fx >= 33 && fx <= 37) {
        if (fy === 22 && fx >= 35) return [220, 150, 120, 255]; // Nose bottom shadow
        return [255, 208, 175, 255]; // Nose tip
      }

      // Mouth / Smile (fx: 28..33, fy: 24..26)
      if (fy >= 24 && fy <= 26 && fx >= 28 && fx <= 33) {
        if (fy === 25 && fx >= 29 && fx <= 32) return [150, 50, 45, 255]; // Smile line
        if (fy === 24 && fx >= 29 && fx <= 31) return [255, 255, 255, 255]; // Teeth grin
      }

      // Shading / Skin Tone
      if (fx <= 22 || fy >= 29) return [230, 175, 140, 255]; // Jaw/neck shadow
      return [255, 212, 180, 255]; // Main warm peach skin
    }

    // ---------------------------------------------------------------
    // 2. TORSO & CLOTHING (fy in 32..47, fx in 14..34)
    // ---------------------------------------------------------------
    if (fy >= 32 && fy <= 47 && fx >= 14 && fx <= 34) {
      // White Collar / Neckline (fy: 32..34, fx: 22..28)
      if (fy <= 34 && fx >= 22 && fx <= 28) {
        return [250, 250, 255, 255]; // Clean white collar
      }

      // Tie / Center Placket (fy: 34..42, fx: 25..27)
      if (fy >= 34 && fy <= 42 && fx >= 25 && fx <= 27) {
        return (fy % 2 === 0) ? [195, 30, 35, 255] : [155, 20, 25, 255]; // Crimson red tie
      }

      // Arms & Swing animation
      if (frame === 1) {
        // Walk 1: Front arm swinging forward (fx: 29..34, fy: 35..45)
        if (fx >= 29 && fx <= 34 && fy >= 35 && fy <= 45) {
          if (fy >= 43) return [255, 212, 180, 255]; // Hand
          return [30, 65, 120, 255]; // Front blue sleeve
        }
        // Back arm swinging backward (fx: 14..18, fy: 35..44)
        if (fx >= 14 && fx <= 18 && fy >= 35 && fy <= 44) {
          if (fy >= 42) return [225, 175, 140, 255]; // Hand shadow
          return [18, 40, 80, 255]; // Back sleeve shadow
        }
      } else if (frame === 3) {
        // Walk 2: Front arm swinging backward (fx: 15..20, fy: 35..45)
        if (fx >= 15 && fx <= 20 && fy >= 35 && fy <= 45) {
          if (fy >= 43) return [255, 212, 180, 255]; // Hand
          return [30, 65, 120, 255];
        }
        // Back arm swinging forward (fx: 28..33, fy: 35..44)
        if (fx >= 28 && fx <= 33 && fy >= 35 && fy <= 44) {
          if (fy >= 42) return [225, 175, 140, 255];
          return [18, 40, 80, 255];
        }
      } else {
        // Stand / Passing arms at sides
        if ((fx <= 17 || fx >= 31) && fy >= 35 && fy <= 45) {
          if (fy >= 43) return [255, 212, 180, 255]; // Hand
          return (fx >= 31) ? [30, 65, 120, 255] : [18, 40, 80, 255];
        }
      }

      // Torso Vest / Jacket Body (Royal Blue)
      if (fx >= 18 && fx <= 30) {
        if (fx <= 19 || fx >= 29) return [20, 45, 85, 255]; // Jacket shadow
        return [35, 75, 140, 255]; // Classic Royal Blue Jacket
      }
    }

    // ---------------------------------------------------------------
    // 3. BELT & WAIST (fy in 47..49, fx in 18..30)
    // ---------------------------------------------------------------
    if (fy >= 47 && fy <= 49 && fx >= 18 && fx <= 30) {
      if (fx >= 25 && fx <= 28 && fy === 48) return [255, 215, 0, 255]; // Gold buckle
      return [35, 20, 15, 255]; // Dark leather belt
    }

    // ---------------------------------------------------------------
    // 4. PANTS / LEGS (fy in 50..57)
    // ---------------------------------------------------------------
    if (fy >= 50 && fy <= 57) {
      if (frame === 0 || frame === 2) {
        // Standing legs
        if (fx >= 18 && fx <= 30 && fx !== 24) {
          return (fx <= 20 || fx >= 28) ? [30, 36, 48, 255] : [45, 55, 72, 255]; // Slate pants
        }
      } else if (frame === 1) {
        // Walk 1: Front leg forward (fx: 23..32), Back leg back (fx: 16..22)
        if (fx >= 23 && fx <= 32 && (fy - 50) <= (fx - 22) * 1.2) return [48, 58, 76, 255];
        if (fx >= 16 && fx <= 22 && (fy - 50) <= (23 - fx) * 1.2) return [28, 34, 45, 255];
      } else if (frame === 3) {
        // Walk 2: Back leg forward (fx: 24..33), Front leg back (fx: 15..21)
        if (fx >= 24 && fx <= 33 && (fy - 50) <= (fx - 23) * 1.2) return [48, 58, 76, 255];
        if (fx >= 15 && fx <= 21 && (fy - 50) <= (22 - fx) * 1.2) return [28, 34, 45, 255];
      }
    }

    // ---------------------------------------------------------------
    // 5. SHOES (fy in 58..63)
    // ---------------------------------------------------------------
    if (fy >= 58 && fy <= 63) {
      if (frame === 0 || frame === 2) {
        if (fx >= 17 && fx <= 31 && fx !== 24) {
          if (fy === 63) return [20, 12, 8, 255]; // Sole
          return [75, 45, 25, 255]; // Brown shoes
        }
      } else if (frame === 1) {
        if (fx >= 25 && fx <= 35) return (fy === 63) ? [20, 12, 8, 255] : [80, 48, 28, 255];
        if (fx >= 13 && fx <= 20) return (fy === 63) ? [20, 12, 8, 255] : [55, 32, 18, 255];
      } else if (frame === 3) {
        if (fx >= 26 && fx <= 36) return (fy === 63) ? [20, 12, 8, 255] : [80, 48, 28, 255];
        if (fx >= 12 && fx <= 19) return (fy === 63) ? [20, 12, 8, 255] : [55, 32, 18, 255];
      }
    }

    return [0, 0, 0, 0];
  }

  // =================================================================
  // ROW 1: BACK VIEW (Facing Up / Towards Back Wall - Rückenansicht)
  // =================================================================
  if (row === 1) {
    // ---------------------------------------------------------------
    // 1. BIG HEAD FROM BEHIND (y in 4..32, x in 12..36)
    // ---------------------------------------------------------------
    const isHeadBack = (fy >= 4 && fy <= 31 && fx >= 13 && fx <= 35);
    if (isHeadBack) {
      // Rounded top and bottom head shape
      if (fy === 4 && (fx < 18 || fx > 30)) return [0, 0, 0, 0];
      if (fy === 5 && (fx < 16 || fx > 32)) return [0, 0, 0, 0];
      if (fy >= 30 && (fx < 18 || fx > 30)) return [0, 0, 0, 0];

      // Ears on left and right sides (fx: 12..14 and fx: 34..36, fy: 18..23)
      if ((fx <= 14 || fx >= 34) && fy >= 18 && fy <= 23) {
        return [235, 180, 145, 255]; // Back of ear lobes
      }

      // Neck nape (fy: 28..31, fx: 20..28)
      if (fy >= 28 && fx >= 20 && fx <= 28) {
        return [225, 170, 135, 255]; // Skin nape
      }

      // Full Volume of Textured Hair from Behind
      if (fy <= 28) {
        // Hair highlights on top crown (fy: 6..10, fx: 20..28)
        if (fy >= 6 && fy <= 10 && fx >= 20 && fx <= 28) {
          return [75, 45, 30, 255]; // Hair highlight
        }
        // Hair strands texture
        if ((fx % 4 === 0) && fy >= 12 && fy <= 26) {
          return [35, 20, 14, 255]; // Dark strand seam
        }
        return [42, 24, 16, 255]; // Dark brown hair
      }
    }

    // ---------------------------------------------------------------
    // 2. TORSO FROM BEHIND (fy in 32..47, fx in 14..34)
    // ---------------------------------------------------------------
    if (fy >= 32 && fy <= 47 && fx >= 14 && fx <= 34) {
      // White Shirt Collar from behind (fy: 32..34, fx: 20..28)
      if (fy <= 34 && fx >= 20 && fx <= 28) {
        return [245, 245, 250, 255];
      }

      // Arm swing in walk cycle
      if (frame === 1) {
        // Left arm forward (fx: 12..16, fy: 35..44)
        if (fx >= 12 && fx <= 16 && fy >= 35 && fy <= 44) {
          if (fy >= 42) return [235, 180, 145, 255];
          return [30, 65, 120, 255];
        }
        // Right arm back (fx: 32..36, fy: 35..44)
        if (fx >= 32 && fx <= 36 && fy >= 35 && fy <= 44) {
          if (fy >= 42) return [235, 180, 145, 255];
          return [20, 45, 85, 255];
        }
      } else if (frame === 3) {
        // Right arm forward (fx: 32..36, fy: 35..44)
        if (fx >= 32 && fx <= 36 && fy >= 35 && fy <= 44) {
          if (fy >= 42) return [235, 180, 145, 255];
          return [30, 65, 120, 255];
        }
        // Left arm back (fx: 12..16, fy: 35..44)
        if (fx >= 12 && fx <= 16 && fy >= 35 && fy <= 44) {
          if (fy >= 42) return [235, 180, 145, 255];
          return [20, 45, 85, 255];
        }
      } else {
        // Standing arms at sides
        if ((fx <= 16 || fx >= 32) && fy >= 35 && fy <= 44) {
          if (fy >= 42) return [235, 180, 145, 255];
          return [25, 55, 100, 255];
        }
      }

      // Back of Jacket (Royal Blue with center back seam)
      if (fx >= 16 && fx <= 32) {
        if (fx === 24) return [22, 48, 90, 255]; // Center back seam
        if (fx <= 17 || fx >= 31) return [20, 45, 85, 255]; // Side shadows
        return [35, 75, 140, 255]; // Royal Blue jacket back
      }
    }

    // ---------------------------------------------------------------
    // 3. BELT (BACK) (fy in 47..49, fx in 17..31)
    // ---------------------------------------------------------------
    if (fy >= 47 && fy <= 49 && fx >= 17 && fx <= 31) {
      return [35, 20, 15, 255]; // Leather belt
    }

    // ---------------------------------------------------------------
    // 4. PANTS / LEGS (BACK) (fy in 50..57)
    // ---------------------------------------------------------------
    if (fy >= 50 && fy <= 57) {
      if (frame === 0 || frame === 2) {
        // Standing legs from behind
        if (fx >= 17 && fx <= 31 && fx !== 24) {
          return (fx === 17 || fx === 31) ? [28, 34, 45, 255] : [45, 55, 72, 255];
        }
      } else if (frame === 1) {
        // Walk 1: Left leg step forward (higher), Right leg push (lower)
        if (fx >= 16 && fx <= 23 && (fy <= 55)) return [48, 58, 76, 255];
        if (fx >= 25 && fx <= 32) return [38, 48, 65, 255];
      } else if (frame === 3) {
        // Walk 2: Right leg step forward, Left leg push
        if (fx >= 25 && fx <= 32 && (fy <= 55)) return [48, 58, 76, 255];
        if (fx >= 16 && fx <= 23) return [38, 48, 65, 255];
      }
    }

    // ---------------------------------------------------------------
    // 5. SHOES / HEELS FROM BEHIND (fy in 58..63)
    // ---------------------------------------------------------------
    if (fy >= 58 && fy <= 63) {
      if (frame === 0 || frame === 2) {
        if (fx >= 17 && fx <= 31 && fx !== 24) {
          if (fy === 63) return [20, 12, 8, 255]; // Shoe heel bottom
          return [70, 42, 24, 255]; // Back of brown leather shoe
        }
      } else if (frame === 1) {
        if (fx >= 16 && fx <= 23 && fy >= 58 && fy <= 61) return [75, 45, 26, 255];
        if (fx >= 25 && fx <= 32) return (fy === 63) ? [20, 12, 8, 255] : [65, 38, 22, 255];
      } else if (frame === 3) {
        if (fx >= 25 && fx <= 32 && fy >= 58 && fy <= 61) return [75, 45, 26, 255];
        if (fx >= 16 && fx <= 23) return (fy === 63) ? [20, 12, 8, 255] : [65, 38, 22, 255];
      }
    }

    return [0, 0, 0, 0];
  }

  // =================================================================
  // ROW 2: FRONT VIEW (Facing Down / Towards Camera)
  // =================================================================
  if (row === 2) {
    // ---------------------------------------------------------------
    // 1. BIG HEAD FROM FRONT (y in 4..32, x in 12..36)
    // ---------------------------------------------------------------
    const isHeadFront = (fy >= 4 && fy <= 31 && fx >= 12 && fx <= 36);
    if (isHeadFront) {
      // Rounded top head and chin shape
      if (fy === 4 && (fx < 18 || fx > 30)) return [0, 0, 0, 0];
      if (fy === 5 && (fx < 16 || fx > 32)) return [0, 0, 0, 0];
      if (fy >= 30 && (fx < 18 || fx > 30)) return [0, 0, 0, 0];

      // Hair volume on top and sides (fy: 4..14)
      if (fy <= 12 && fx >= 14 && fx <= 34) {
        if (fy <= 8 && fx >= 20 && fx <= 28) return [75, 45, 30, 255]; // Hair highlight
        return [42, 24, 16, 255]; // Dark brown hair
      }
      // Side hair covering temples (fx: 13..16 or fx: 32..35, fy: 13..18)
      if ((fx <= 16 || fx >= 32) && fy >= 13 && fy <= 18) {
        return [42, 24, 16, 255];
      }

      // Ears on sides (fx: 12..14 and fx: 34..36, fy: 18..23)
      if ((fx <= 14 || fx >= 34) && fy >= 18 && fy <= 23) {
        return [245, 195, 160, 255];
      }

      // Two Large Expressive Eyes (Left: fx in 18..22, Right: fx in 26..30, fy in 15..18)
      // Eyebrows (fy: 13..14, Left: fx 17..23, Right: fx 25..31)
      if (fy <= 14 && ((fx >= 17 && fx <= 23) || (fx >= 25 && fx <= 31))) {
        return [30, 18, 12, 255]; // Thick eyebrows
      }

      // Left Eye
      if (fy >= 15 && fy <= 18 && fx >= 18 && fx <= 22) {
        if (fx === 20 && fy >= 16 && fy <= 17) return [15, 15, 20, 255]; // Pupil
        if (fx === 20 && fy === 15) return [255, 255, 255, 255]; // Catchlight
        return [245, 245, 248, 255]; // White sclera
      }

      // Right Eye
      if (fy >= 15 && fy <= 18 && fx >= 26 && fx <= 30) {
        if (fx === 28 && fy >= 16 && fy <= 17) return [15, 15, 20, 255]; // Pupil
        if (fx === 28 && fy === 15) return [255, 255, 255, 255]; // Catchlight
        return [245, 245, 248, 255]; // White sclera
      }

      // Nose in center (fx: 23..25, fy: 19..22)
      if (fy >= 19 && fy <= 22 && fx >= 23 && fx <= 25) {
        if (fy === 22) return [220, 150, 120, 255]; // Nose bottom shadow
        return [255, 205, 170, 255]; // Nose bridge
      }

      // Mouth / Big Smile (fx: 20..28, fy: 24..26)
      if (fy >= 24 && fy <= 26 && fx >= 20 && fx <= 28) {
        if (fy === 25 && fx >= 21 && fx <= 27) return [150, 50, 45, 255]; // Smile line
        if (fy === 24 && fx >= 22 && fx <= 26) return [255, 255, 255, 255]; // Teeth
      }

      // Peach Skin tone
      if (fx <= 17 || fx >= 31 || fy >= 29) return [230, 175, 140, 255]; // Shadow
      return [255, 212, 180, 255]; // Main skin
    }

    // ---------------------------------------------------------------
    // 2. TORSO FROM FRONT (fy in 32..47, fx in 14..34)
    // ---------------------------------------------------------------
    if (fy >= 32 && fy <= 47 && fx >= 14 && fx <= 34) {
      // White Collar (fy: 32..34, fx: 21..27)
      if (fy <= 34 && fx >= 21 && fx <= 27) {
        return [250, 250, 255, 255];
      }

      // Red Tie (fy: 34..42, fx: 23..25)
      if (fy >= 34 && fy <= 42 && fx >= 23 && fx <= 25) {
        return (fy % 2 === 0) ? [195, 30, 35, 255] : [155, 20, 25, 255];
      }

      // Arms swinging in walk cycle
      if (frame === 1) {
        // Left arm forward (fx: 12..16, fy: 35..45)
        if (fx >= 12 && fx <= 16 && fy >= 35 && fy <= 45) {
          if (fy >= 43) return [255, 212, 180, 255]; // Hand
          return [30, 65, 120, 255];
        }
        // Right arm back (fx: 32..36, fy: 35..45)
        if (fx >= 32 && fx <= 36 && fy >= 35 && fy <= 45) {
          if (fy >= 43) return [225, 175, 140, 255];
          return [20, 45, 85, 255];
        }
      } else if (frame === 3) {
        // Right arm forward (fx: 32..36, fy: 35..45)
        if (fx >= 32 && fx <= 36 && fy >= 35 && fy <= 45) {
          if (fy >= 43) return [255, 212, 180, 255];
          return [30, 65, 120, 255];
        }
        // Left arm back (fx: 12..16, fy: 35..45)
        if (fx >= 12 && fx <= 16 && fy >= 35 && fy <= 45) {
          if (fy >= 43) return [225, 175, 140, 255];
          return [20, 45, 85, 255];
        }
      } else {
        // Standing arms
        if ((fx <= 16 || fx >= 32) && fy >= 35 && fy <= 45) {
          if (fy >= 43) return [255, 212, 180, 255];
          return [30, 65, 120, 255];
        }
      }

      // Open Jacket over White Shirt
      if (fx >= 17 && fx <= 31) {
        // Center white shirt placket (fx: 21..27)
        if (fx >= 21 && fx <= 27 && fy >= 35 && fy <= 44) {
          return [245, 245, 250, 255];
        }
        // Gold buttons on jacket lapels
        if ((fx === 19 || fx === 29) && (fy === 37 || fy === 41)) {
          return [255, 215, 0, 255];
        }
        if (fx <= 18 || fx >= 30) return [20, 45, 85, 255];
        return [35, 75, 140, 255];
      }
    }

    // ---------------------------------------------------------------
    // 3. BELT & GOLD BUCKLE (fy in 47..49, fx in 17..31)
    // ---------------------------------------------------------------
    if (fy >= 47 && fy <= 49 && fx >= 17 && fx <= 31) {
      if (fx >= 22 && fx <= 26 && fy === 48) return [255, 215, 0, 255]; // Center Gold Buckle
      return [35, 20, 15, 255];
    }

    // ---------------------------------------------------------------
    // 4. PANTS / LEGS (FRONT) (fy in 50..57)
    // ---------------------------------------------------------------
    if (fy >= 50 && fy <= 57) {
      if (frame === 0 || frame === 2) {
        if (fx >= 17 && fx <= 31 && fx !== 24) {
          return (fx === 17 || fx === 31) ? [28, 34, 45, 255] : [45, 55, 72, 255];
        }
      } else if (frame === 1) {
        if (fx >= 16 && fx <= 23) return [48, 58, 76, 255];
        if (fx >= 25 && fx <= 32) return [38, 48, 65, 255];
      } else if (frame === 3) {
        if (fx >= 25 && fx <= 32) return [48, 58, 76, 255];
        if (fx >= 16 && fx <= 23) return [38, 48, 65, 255];
      }
    }

    // ---------------------------------------------------------------
    // 5. SHOES (FRONT) (fy in 58..63)
    // ---------------------------------------------------------------
    if (fy >= 58 && fy <= 63) {
      if (frame === 0 || frame === 2) {
        if (fx >= 16 && fx <= 32 && fx !== 24) {
          if (fy === 63) return [20, 12, 8, 255];
          return [75, 45, 25, 255];
        }
      } else if (frame === 1) {
        if (fx >= 15 && fx <= 23) return (fy === 63) ? [20, 12, 8, 255] : [80, 48, 28, 255];
        if (fx >= 25 && fx <= 32) return (fy === 63) ? [20, 12, 8, 255] : [65, 38, 22, 255];
      } else if (frame === 3) {
        if (fx >= 25 && fx <= 33) return (fy === 63) ? [20, 12, 8, 255] : [80, 48, 28, 255];
        if (fx >= 16 && fx <= 23) return (fy === 63) ? [20, 12, 8, 255] : [65, 38, 22, 255];
      }
    }

    return [0, 0, 0, 0];
  }

  return [0, 0, 0, 0];
});

// 2. Generate Classic Lucasfilm Adventure Pirate Tavern Garden Background (640x360)
// Inspired by Lucasfilm SCUMM aesthetics (Secret of Monkey Island / Scumm Bar Backyard / Melee Island)
const gardenPng = createPng(640, 360, (x, y) => {
  const dist = (x1, y1, x2, y2) => Math.hypot(x1 - x2, y1 - y2);

  // -------------------------------------------------------------
  // NIGHT SKY & MOON (y: 0 .. 150)
  // -------------------------------------------------------------
  if (y < 140 && x > 110) {
    // Crescent Moon (center at x=520, y=50, radius=22)
    const dMoon = dist(x, y, 520, 50);
    const dMoonCut = dist(x, y, 512, 45);
    if (dMoon <= 22 && dMoonCut > 18) {
      if (dMoon <= 20) return [255, 250, 210, 255]; // Bright cream moon
      return [220, 210, 160, 255]; // Moon edge
    }

    // Stars
    if ((x * 37 + y * 91) % 197 === 0 && dMoon > 35) {
      return [240, 245, 255, 255];
    }
    if ((x * 19 + y * 53) % 113 === 0 && dMoon > 35) {
      return [180, 200, 240, 200];
    }

    // Deep night gradient
    const skyGrad = Math.min(1, y / 140);
    const r = Math.floor(8 * (1 - skyGrad) + 18 * skyGrad);
    const g = Math.floor(12 * (1 - skyGrad) + 28 * skyGrad);
    const b = Math.floor(32 * (1 - skyGrad) + 65 * skyGrad);
    return [r, g, b, 255];
  }

  // -------------------------------------------------------------
  // TAVERNE EXTERIOR WALL (x: 0 .. 110, y: 0 .. 240)
  // -------------------------------------------------------------
  if (x <= 110 && y < 240) {
    // Tavern Roof overhang (y: 0..40, slopes down from x=0 to x=115)
    if (y <= 30 + (x * 0.15)) {
      const tile = (Math.floor(x / 12) + Math.floor(y / 8)) % 2;
      return tile === 0 ? [75, 40, 30, 255] : [55, 28, 20, 255];
    }

    // Tavern Hanging Wooden Sign above Doorway (x: 25..95, y: 68..90)
    if (x >= 25 && x <= 95 && y >= 68 && y <= 90) {
      if ((x === 35 || x === 85) && y <= 74) {
        return [140, 110, 40, 255]; // Brass brackets
      }
      if (y >= 74) {
        const isSignBorder = (y === 74 || y === 90 || x === 25 || x === 95);
        if (isSignBorder) return [210, 170, 60, 255]; // Gold frame
        // Carved arrow pointing left towards door (x: 32..44, y: 79..85)
        const isLeftArrow = (y === 82 && x >= 32 && x <= 44) ||
                            (Math.abs(x - 34) === Math.abs(y - 82) && x >= 33 && x <= 37);
        if (isLeftArrow) return [255, 240, 130, 255]; // Gold arrow
        return [85, 45, 22, 255]; // Warm wood sign
      }
    }

    // Tavern Door to Lounge (x: 28..88, y: 92..240)
    if (x >= 28 && x <= 88 && y >= 92) {
      const doorArchY = 125;
      const doorCenterX = 58;
      const isArch = (y >= doorArchY && x >= 32 && x <= 84) || (dist(x, y, doorCenterX, doorArchY) <= 26);
      const isFrame = (Math.abs(x - 30) <= 3 || Math.abs(x - 86) <= 3 || (y <= doorArchY && Math.abs(dist(x, y, doorCenterX, doorArchY) - 27) <= 3));

      // Lantern on right side of tavern door (x: 88..98, y: 105..128)
      if (x >= 88 && x <= 98 && y >= 105 && y <= 128) {
        const isLFrame = x === 88 || x === 98 || y === 105 || y === 128;
        if (isLFrame) return [180, 140, 40, 255];
        return [255, 230, 110, 255]; // Glowing lantern
      }

      if (isFrame) return [50, 32, 22, 255]; // Dark timber arch

      if (isArch) {
        // Glowing warm tavern interior (spilling golden light)
        const dCenter = Math.abs(x - 58);
        const glow = Math.max(0, 1 - dCenter / 26);
        const yGlow = (y - 92) / 140;
        return [
          Math.min(255, Math.floor(205 + 50 * glow + 10 * yGlow)),
          Math.min(255, Math.floor(135 + 40 * glow + 10 * yGlow)),
          Math.min(255, Math.floor(55 + 30 * glow)),
          255
        ];
      }
    }

    // Tavern Timbered Wall
    const beam = (x <= 6 || x >= 104 || y % 45 <= 4 || (x >= 30 && x <= 34) || (x >= 86 && x <= 90));
    if (beam) {
      return [42, 24, 14, 255]; // Dark timber
    }
    // Plaster / dark wood wall
    const wallNoise = (x * 7 + y * 13) % 11;
    return [60 + wallNoise, 45 + wallNoise, 35 + wallNoise, 255];
  }

  // -------------------------------------------------------------
  // DISTANT TREES & GARDEN FENCE (y: 120 .. 240, x > 110)
  // -------------------------------------------------------------
  if (y < 240) {
    // Tree silhouette (y: 100 .. 180)
    const treeTop = 130 + Math.sin(x * 0.03) * 15 + Math.cos(x * 0.08) * 8;
    if (y >= treeTop && y < 195) {
      const dDark = (x % 4 === 0) ? -8 : 0;
      return [12 + dDark, 28 + dDark, 20 + dDark, 255];
    }

    // Wooden Garden Fence (y: 195 .. 238)
    if (y >= 195) {
      // Fence picket posts every 20px
      const picket = (x % 20);
      if (picket <= 6 && y <= 235) {
        // Pointed picket top
        if (y < 202 && Math.abs(picket - 3) > (y - 195)) {
          // air above point
        } else {
          return [70, 50, 32, 255]; // Wood picket
        }
      }
      // Horizontal fence rails (y: 208..212, y: 224..228)
      if ((y >= 208 && y <= 212) || (y >= 224 && y <= 228)) {
        return [55, 38, 24, 255];
      }
      // Grass behind fence
      return [16, 38, 22, 255];
    }

    return [10, 20, 16, 255];
  }

  // -------------------------------------------------------------
  // WALKABLE GARDEN GROUND / PATHWAY (y: 240 .. 360)
  // -------------------------------------------------------------
  // Baseboard / edge: y: 238 .. 242
  if (y <= 242) {
    return [24, 48, 28, 255];
  }

  // Walkable area: y: 242 .. 345, x: 40 .. 600
  // Cobblestone path down the middle (x: 180 .. 460)
  const isPath = (x >= 160 && x <= 480);
  const stoneX = Math.floor(x / 24);
  const stoneY = Math.floor(y / 14);
  const isStoneJoint = ((x % 24 <= 1) && (y % 14 > 1)) || (y % 14 <= 1);

  if (isPath) {
    if (isStoneJoint) {
      return [35, 42, 38, 255]; // Dark mortar
    }
    const stoneTone = ((stoneX * 17 + stoneY * 31) % 19) - 9;
    return [
      Math.max(0, Math.min(255, 75 + stoneTone)),
      Math.max(0, Math.min(255, 82 + stoneTone)),
      Math.max(0, Math.min(255, 78 + stoneTone)),
      255
    ];
  }

  // Grass / Lawn sides (x < 160 or x > 480)
  const grassNoise = (x * 13 + y * 29) % 17;
  const isEdge = (x < 40 || x > 600 || y > 345);
  const shade = isEdge ? -20 : 0;

  return [
    Math.max(0, Math.min(255, 28 + grassNoise + shade)),
    Math.max(0, Math.min(255, 62 + grassNoise + shade)),
    Math.max(0, Math.min(255, 32 + grassNoise + shade)),
    255
  ];
});

const targetDir = path.join('src', 'AdventureChat.Server', 'wwwroot', 'assets', 'images');
fs.mkdirSync(targetDir, { recursive: true });

fs.writeFileSync(path.join(targetDir, 'lounge.png'), loungePng);
fs.writeFileSync(path.join(targetDir, 'garden.png'), gardenPng);
fs.writeFileSync(path.join(targetDir, 'character.png'), charPng);
console.log('Assets created successfully in ' + targetDir);
