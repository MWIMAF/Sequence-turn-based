# Sequence Asset Generator (Figma plugin)

Draws the game's art as editable vectors: move icons, equipment icons, status icons, VFX sprites and
fighter portraits. Pure code, so it costs no Figma AI credits and gives the same result every run.

## Run it

1. Figma desktop app → open (or create) the file for the game's art.
2. Menu → Plugins → Development → **Import plugin from manifest…** → pick `manifest.json` in this folder.
   (Only the first time.)
3. Plugins → Development → **Sequence Asset Generator**.

It builds a page called **Sequence Assets**, one section per group. Running it again replaces that page,
so change art in `code.js` (the `ASSETS` list), not by hand. Rename the page first if you want to keep
hand edits.

## Naming = wiring

Each frame is named after the Unity asset it belongs to (`Move_Slash` → `Move_Slash.asset`,
`Enemy_Grunt` → `Enemy_Grunt.prefab`). Every frame already has a PNG @1x export setting.
To add art: add an entry to `ASSETS` with the matching Unity name.
