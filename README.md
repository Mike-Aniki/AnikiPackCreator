# Aniki Pack Creator

Aniki Pack Creator is a Windows tool for creating packs for Aniki ReMake. It supports **Visual Packs**, **Sound Packs**, **Color Packs**, **Login Packs**, and **Complete Packs**.
Use the permanent tabs at the top to move between pack creators, build the active pack with guided slots, then export a validated ZIP.

## Features

- Switch instantly between **Visual Pack**, **Sound Pack**, **Color Pack**, **Login Pack**, **Complete Pack**, and **Home** from the permanent tabs at the top of the app.
- Color Pack projects use the separate `.acpc` format and expose 15 master colors, grouped into Accents, Interface and Text for a simple but flexible workflow.
- Existing `.avpc` Visual Pack projects remain compatible.
- Sound Pack projects use the separate `.aspc` format.
- Sound Packs can be partial: only assigned audio files are exported and missing items can fall back to Aniki defaults when applied.
- Preview assigned WAV sounds and MP3 ambient music directly in the Creator.
- Validates WAV/MP3 sources before export and generates a `soundpack.json` manifest.
- Handles the 14 images used by an Aniki ReMake Visual Pack.
- Opens an existing Visual Pack folder when the files already use the expected names.
- Lets you reuse one image for empty views.
- Drag the image to reposition it and use the mouse wheel to zoom.
- Includes saturation, brightness, contrast and dark overlay controls.
- Applies the default Aniki image treatment automatically, except on `MainBackground.jpg`.
- Can show interface previews for some views while editing. These overlays are not included in the exported images.
- Saves editable `.avpc` project files.
- Stores a permanent Community Pack ID in each project so future versions of the same pack can be detected as updates.
- Stores the pack version and optional description.
- Exports the finished pack as a ZIP ready for Aniki Helper.
- Generates a `visualpack.json` manifest containing the Community Pack metadata.
- Includes a **Share Community Pack** button in every pack view that opens the matching submission form in `AnikiCommunityPacks`.

## How to use it

1. Enter a name for the pack and the author name.
2. Keep the default version (`1.0.0`) or enter a newer semantic version when updating a pack.
3. Optionally add a short description.
4. Select a view from the list on the left.
5. Select or drop an image.
6. Move and zoom the image until the preview looks right.
7. Repeat for the other views. **Fill empty views** can reuse the current image where nothing has been selected yet.
8. Click **Export Visual Pack ZIP**.
9. Import the ZIP from the Visual Packs page in Aniki Helper, or use **Share Community Pack** to submit it to the community catalog.

`MainBackground.jpg` keeps its original colors. The other images receive the default Aniki treatment before export.

**Show UI preview** adds an interface reference over the editor preview when one is available. It is only there to help with image placement and is never exported into the pack.

Project files keep source-image paths relative to the `.avpc` file when possible. If you move a project, keeping its source images with it will make reopening it easier.

Older `.avpc` projects remain supported. When an older project without Community Pack metadata is opened, the Creator assigns it a permanent ID and marks the project as modified so the new metadata can be saved.


## Sound Packs

Sound Pack mode exposes 20 optional audio slots from the current Aniki ReMake audio system:

- 17 theme and event sounds in WAV format, such as navigation, activation, notifications, Playnite shutdown and game events.
- 3 ambient music slots in MP3 format: login music (`LoginOST.mp3`), Hub music (`HubOST.mp3`) and secondary-view music (`SecondaryViewsOST.mp3`).

The format guidance changes with the selected slot: sound slots accept WAV files, while ambient-music slots accept MP3 files. Each selected source is validated, previewable and exported to the exact theme path expected by Aniki ReMake. Special Lucky/Konami audio remains outside the standard Sound Pack.

Every exported Sound Pack contains a root `soundpack.json` manifest with its permanent pack ID, semantic version and the list of included audio slots.


## Color Packs

Color Pack mode lets the user edit 15 master colors: primary accent, secondary accent, focus/selection, action buttons, progress/indicators, background/secondary views, top+bottom bars, menus/panels, menu headers, cards/surfaces, borders, notifications, primary text, secondary text, and highlighted text.

The Creator then adapts the complete Aniki ReMake palette while preserving the theme structure, brushes, gradients and styles.

Every exported Color Pack contains:

- `colorpack.json` with the permanent pack ID, semantic version and the 15 master colors.
- `colors.xaml`, a complete Theme Color resource dictionary ready for Aniki ReMake.

Color Pack project files use the `.acpc` extension.

The Color Pack preview loads the brushes and gradients directly from the generated Theme Color XAML and provides four compact Aniki scenes (**Main**, **Hub**, **Menus**, and **Details**). Selecting or editing any of the 15 master colors now automatically opens the most relevant preview scene and displays a short explanation of which real theme areas that color family affects. This keeps the preview useful for users who do not know the internal XAML resource names.

## Login Packs

Login Pack mode creates a single MP4 login-background pack. The selected video must be under 50 MB and use H.264/AVC or H.265/HEVC. Audio tracks are allowed, but Aniki ReMake plays login videos muted. Login Pack projects use `.alpc`, and exported ZIPs contain `loginpack.json` plus `Login.mp4`.

## Complete Packs

Complete Pack mode is an installation bundle, not a merged theme. A Complete Pack requires a Visual Pack and a Login Pack, with Sound and Color Packs optional. Each selected ZIP is validated by its own manifest and copied unchanged into the bundle:

```text
completepack.json
packs/visual.zip
packs/login.zip
packs/sound.zip   (optional)
packs/color.zip   (optional)
```

After import, Aniki Helper can install the embedded packs into their individual libraries so users can later change only the Login Pack, Sound Pack, Color Pack, or Visual Pack without affecting the others. Complete Pack project files use `.acmp`.

## Home

The Home tab is shown when the app starts. It introduces the available pack types, shows the running Creator version, and provides direct links to Discord, Ko-fi, the Aniki Pack Creator repository, and the Aniki ReMake repository.

## Community metadata

Every exported ZIP contains `visualpack.json` at its root. Example:

```json
{
  "formatVersion": 1,
  "id": "tekken-8-a72f194c",
  "name": "Tekken 8",
  "author": "Galva",
  "version": "1.0.0",
  "description": "Visual Pack inspired by Tekken 8.",
  "builtInSeed": false,
  "createdWith": "Aniki Pack Creator",
  "creatorVersion": "1.0.0"
}
```

The `id` is created once and then stored in the `.avpc` project. Renaming the pack or increasing its version does not change that ID. This allows Aniki Helper to identify installed packs and detect future updates from the Community Visual Pack catalog.

Versions use semantic versioning such as `1.0.0`, `1.1.0` or `2.0.0`.

Creator repository:

`https://github.com/Mike-Aniki/AnikiPackCreator`

Community repository:

`https://github.com/Mike-Aniki/AnikiCommunityPacks`

## Images used by a Visual Pack

| File | Dimensions |
| --- | ---: |
| `MainBackground.jpg` | 1920 × 1080 |
| `Welcome.jpg` | 1920 × 1080 |
| `StatView.jpg` | 1920 × 1080 |
| `FriendsView.jpg` | 1920 × 1080 |
| `AchievementsView.jpg` | 1920 × 1080 |
| `MediaView.jpg` | 1920 × 1080 |
| `StoreView.jpg` | 1920 × 1080 |
| `MainMenu.jpg` | 531 × 986 |
| `SettingsBackground.jpg` | 487 × 1080 |
| `FrameSettingsBackground.jpg` | 1247 × 900 |
| `MessageBox.jpg` | 830 × 429 |
| `GameMenu.jpg` | 470 × 655 |
| `ItemMenu.jpg` | 503 × 818 |
| `Login.jpg` | 857 × 238 |

## Build

The project uses .NET 8 and WPF.

Run:

```text
publish-win-x64.cmd
```

The portable executable will be created here:

```text
publish\win-x64\AnikiPackCreator.exe
```
