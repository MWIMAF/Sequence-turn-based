// Sequence asset generator: draws the game's art as editable vectors on a page called "Sequence Assets".
// No AI, no credits. Re-running replaces everything on that page, so edit art here, not by hand in Figma
// (or rename the page first to keep hand edits).
//
// Each frame is named after the Unity asset it belongs to (Move_Slash, Equip_Sword, Enemy_Grunt...),
// so Sequence > Import Art in Unity can match them up.

const PAGE_NAME = "Sequence Assets";

// Colours from Assets/_Project/Scripts/UI/UiPalette.cs.
const C = {
  ink: "#E8EAF0", shadow: "#14161B", dim: "#8C93A3", panel: "#2A2F39",
  attack: "#C2413B", dodge: "#2F6FC9", parry: "#C08A1E", utility: "#2E9E62",
  hp: "#E5484D", stamina: "#3E9BFF", warning: "#F5A524", crit: "#FFD23F", accent: "#6E7BFF",
  skin: "#E8C39E", skinShade: "#C99A72",
};

// ---------------------------------------------------------------- shapes (100 × 100 space)

const SWORD = `<path d="M50 6 L57 17 L57 60 L43 60 L43 17 Z"/>
  <rect x="32" y="60" width="36" height="8" rx="3"/>
  <rect x="45.5" y="68" width="9" height="16" rx="2"/>
  <circle cx="50" cy="89" r="6"/>`;
const SHIELD = `<path d="M50 8 L84 18 L82 50 Q78 78 50 93 Q22 78 18 50 L16 18 Z"/>`;
const BOOT = `<path d="M30 10 H58 V56 L80 63 Q92 67 91 80 V90 H30 Z"/>`;
const CHEVRONS = `<path d="M18 22 H32 L58 50 L32 78 H18 L44 50 Z"/><path d="M46 22 H60 L86 50 L60 78 H46 L72 50 Z"/>`;
const SPARK = (x, y, r) =>
  `<path d="M${x} ${y - r} L${x + r * 0.25} ${y - r * 0.25} L${x + r} ${y} L${x + r * 0.25} ${y + r * 0.25} L${x} ${y + r} L${x - r * 0.25} ${y + r * 0.25} L${x - r} ${y} L${x - r * 0.25} ${y - r * 0.25} Z"/>`;
const STAR = (cx, cy, rOut, rIn, points) => {
  let d = "";
  for (let i = 0; i < points * 2; i++) {
    const r = i % 2 ? rIn : rOut;
    const a = (Math.PI * i) / points - Math.PI / 2;
    d += `${i ? "L" : "M"}${(cx + r * Math.cos(a)).toFixed(1)} ${(cy + r * Math.sin(a)).toFixed(1)} `;
  }
  return `<path d="${d}Z"/>`;
};

// A light glyph with a dark drop shadow, so it reads on any slot-card colour.
const icon = (body, color = C.ink) =>
  `<g transform="translate(0 4)" fill="${C.shadow}" fill-opacity="0.55">${body}</g><g fill="${color}">${body}</g>`;

const svg = (size, body) =>
  `<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}" viewBox="0 0 100 100">${body}</svg>`;

// ---------------------------------------------------------------- fighters (bust portraits)

const bust = ({ body, head = "", front = "" }) => `
  ${body}
  <rect x="43" y="52" width="14" height="14" fill="${C.skinShade}"/>
  <circle cx="50" cy="40" r="19" fill="${C.skin}"/>
  ${head}
  ${front}`;

const PLAYER = bust({
  body: `<path d="M12 100 Q12 68 50 63 Q88 68 88 100 Z" fill="${C.dodge}"/>
         <path d="M34 66 L50 84 L66 66 Q50 62 34 66 Z" fill="${C.stamina}"/>`,
  head: `<path d="M26 44 Q24 14 50 12 Q76 14 74 44 L68 46 Q68 26 50 25 Q32 26 32 46 Z" fill="${C.dodge}"/>
         <rect x="40" y="38" width="5" height="7" rx="2" fill="${C.shadow}"/>
         <rect x="55" y="38" width="5" height="7" rx="2" fill="${C.shadow}"/>`,
});

const GRUNT = bust({
  body: `<path d="M4 100 Q6 64 50 60 Q94 64 96 100 Z" fill="${C.attack}"/>
         <circle cx="16" cy="74" r="11" fill="${C.dim}"/><circle cx="84" cy="74" r="11" fill="${C.dim}"/>`,
  head: `<path d="M28 44 Q28 18 50 17 Q72 18 72 44 L72 56 L28 56 Z" fill="${C.dim}"/>
         <path d="M29 28 Q14 22 12 6 Q24 14 34 20 Z" fill="${C.ink}"/>
         <path d="M71 28 Q86 22 88 6 Q76 14 66 20 Z" fill="${C.ink}"/>
         <rect x="34" y="38" width="32" height="6" rx="2" fill="${C.shadow}"/>
         <rect x="40" y="39.5" width="6" height="3" fill="${C.hp}"/><rect x="54" y="39.5" width="6" height="3" fill="${C.hp}"/>`,
});

const DUELIST = bust({
  body: `<path d="M18 100 Q18 70 50 64 Q82 70 82 100 Z" fill="${C.accent}"/>
         <path d="M50 64 L40 100 H60 Z" fill="${C.ink}"/>
         <path d="M50 66 L44 76 L50 82 L56 76 Z" fill="${C.crit}"/>`,
  head: `<rect x="32" y="35" width="36" height="9" rx="4" fill="${C.shadow}"/>
         <ellipse cx="42" cy="39.5" rx="3" ry="2" fill="${C.ink}"/><ellipse cx="58" cy="39.5" rx="3" ry="2" fill="${C.ink}"/>
         <path d="M38 52 Q50 58 62 52" stroke="${C.skinShade}" stroke-width="2" fill="none"/>
         <ellipse cx="50" cy="26" rx="34" ry="6" fill="${C.shadow}"/>
         <path d="M34 26 Q36 8 50 8 Q64 8 66 26 Z" fill="${C.shadow}"/>
         <path d="M62 14 Q86 0 92 10 Q80 10 66 20 Z" fill="${C.hp}"/>`,
});

// ---------------------------------------------------------------- the asset list

const ASSETS = [
  // Move icons: shown on move buttons and slot cards. Light glyph, transparent background.
  { group: "Moves", name: "Move_Slash", size: 256, svg: icon(`<path d="M10 56 Q36 8 92 14 Q50 22 10 56 Z" opacity="0.6"/><g transform="rotate(45 50 50)">${SWORD}</g>`) },
  { group: "Moves", name: "Move_Bash", size: 256, svg: icon(`<g transform="translate(-8 0)">${SHIELD}</g><rect x="80" y="30" width="16" height="6" rx="3"/><rect x="84" y="47" width="14" height="6" rx="3"/><rect x="80" y="64" width="16" height="6" rx="3"/>`) },
  { group: "Moves", name: "Move_Kick", size: 256, svg: icon(`<g transform="translate(6 0)">${BOOT}</g><rect x="4" y="36" width="22" height="6" rx="3"/><rect x="10" y="52" width="18" height="6" rx="3"/><rect x="4" y="68" width="22" height="6" rx="3"/>`) },
  { group: "Moves", name: "Move_Parry", size: 256, svg: icon(`<g transform="rotate(40 50 55) scale(0.88) translate(7 8)">${SWORD}</g><g transform="rotate(-40 50 55) scale(0.88) translate(7 8)">${SWORD}</g>${SPARK(50, 20, 15)}`) },
  { group: "Moves", name: "Move_Evade", size: 256, svg: icon(CHEVRONS) },
  { group: "Moves", name: "Move_Rest", size: 256, svg: icon(`<path d="M52 16 A34 34 0 1 0 86 64 A27 27 0 1 1 52 16 Z"/><path d="M70 8 H86 V12 L76 24 H86 V28 H69 V24 L79 12 H70 Z"/><path d="M84 32 H94 V35 L88 42 H94 V45 H83 V42 L89 35 H84 Z"/>`) },
  { group: "Moves", name: "Move_Idle", size: 256, svg: icon(`<path d="M28 10 H72 V20 L56 50 L72 80 V90 H28 V80 L44 50 L28 20 Z"/>`, C.dim) },

  // Equipment icons: the loadout's source of each action.
  { group: "Equipment", name: "Equip_Sword", size: 256, svg: icon(SWORD) },
  { group: "Equipment", name: "Equip_Shield", size: 256, svg: icon(`${SHIELD}`) + `<path d="M46 26 H54 V44 H68 V52 H54 V76 H46 V52 H32 V44 H46 Z" fill="${C.parry}"/>` },
  { group: "Equipment", name: "Equip_Armor", size: 256, svg: icon(`<path d="M28 20 L40 15 L50 25 L60 15 L72 20 L75 50 Q73 79 50 91 Q27 79 25 50 Z"/><ellipse cx="20" cy="30" rx="14" ry="11"/><ellipse cx="80" cy="30" rx="14" ry="11"/>`) +
      `<path d="M50 30 V84" stroke="${C.dim}" stroke-width="3"/><rect x="28" y="62" width="44" height="7" fill="${C.utility}"/>` },
  { group: "Equipment", name: "Equip_Boots", size: 256, svg: icon(`<g transform="translate(-4 0)">${BOOT}</g>`) },

  // Status icons: badges and panel chips.
  { group: "Status", name: "Status_Staggered", size: 128, svg: icon(`<path fill-rule="evenodd" d="M10 56 A40 14 0 1 0 90 56 A40 14 0 1 0 10 56 Z M16 56 A34 9 0 1 0 84 56 A34 9 0 1 0 16 56 Z"/>${STAR(18, 52, 14, 6, 5)}${STAR(56, 32, 16, 7, 5)}${STAR(82, 62, 12, 5, 5)}`, C.warning) },

  // VFX sprites: played over the fighters during playback.
  { group: "VFX", name: "Vfx_Slash", size: 512, svg: `<path d="M8 86 Q20 12 94 8 Q40 26 8 86 Z" fill="${C.ink}" opacity="0.35"/><path d="M14 80 Q28 20 90 12 Q42 30 14 80 Z" fill="${C.ink}"/>` },
  { group: "VFX", name: "Vfx_ParryFlash", size: 512, svg: `<g fill="${C.crit}" opacity="0.5">${STAR(50, 50, 48, 16, 8)}</g><g fill="${C.crit}">${STAR(50, 50, 34, 10, 8)}</g><circle cx="50" cy="50" r="10" fill="${C.ink}"/>` },
  { group: "VFX", name: "Vfx_Hit", size: 512, svg: `<g fill="${C.hp}">${STAR(50, 50, 46, 22, 11)}</g><g fill="${C.ink}">${STAR(50, 50, 24, 11, 7)}</g>` },

  // Fighter portraits: the player and each enemy prefab, for the combatant panels.
  { group: "Fighters", name: "Player_Default", size: 512, svg: PLAYER },
  { group: "Fighters", name: "Enemy_Grunt", size: 512, svg: GRUNT },
  { group: "Fighters", name: "Enemy_Duelist", size: 512, svg: DUELIST },
];

// ---------------------------------------------------------------- build the page

async function run() {
  let page = figma.root.children.find((p) => p.name === PAGE_NAME);
  if (!page) {
    page = figma.createPage();
    page.name = PAGE_NAME;
  }
  await figma.setCurrentPageAsync(page);
  for (const node of [...page.children]) node.remove();

  const GAP = 48;
  const PAD = 64;
  let y = 0;
  for (const group of [...new Set(ASSETS.map((a) => a.group))]) {
    const section = figma.createSection();
    section.name = group;
    page.appendChild(section);

    let x = PAD;
    let rowHeight = 0;
    for (const asset of ASSETS.filter((a) => a.group === group)) {
      const frame = figma.createNodeFromSvg(svg(asset.size, asset.svg));
      frame.name = asset.name;
      frame.fills = [];
      frame.exportSettings = [{ format: "PNG", constraint: { type: "SCALE", value: 1 } }];
      section.appendChild(frame);
      frame.x = x;
      frame.y = PAD;
      x += asset.size + GAP;
      rowHeight = Math.max(rowHeight, asset.size);
    }

    section.x = 0;
    section.y = y;
    section.resizeWithoutConstraints(x - GAP + PAD, rowHeight + PAD * 2);
    y += rowHeight + PAD * 2 + GAP;
  }

  figma.viewport.scrollAndZoomIntoView(page.children);
  figma.closePlugin(`Generated ${ASSETS.length} assets on "${PAGE_NAME}".`);
}

if (typeof figma !== "undefined") {
  run().catch((e) => figma.closePlugin(`Asset generator failed: ${e.message}`));
} else {
  module.exports = { ASSETS, svg }; // shortcut: lets preview.js render the same art outside Figma
}
