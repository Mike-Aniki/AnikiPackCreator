# Aniki Visual Pack Creator

Aniki Visual Pack Creator is a small Windows tool for making custom Visual Packs for Aniki ReMake.
It prepares the images at the sizes expected by the theme and exports a ZIP that can be imported directly from Aniki Helper.

## Features

- Handles the 14 images used by an Aniki ReMake Visual Pack.
- Opens an existing Visual Pack folder when the files already use the expected names.
- Lets you reuse one image for empty views.
- Drag the image to reposition it and use the mouse wheel to zoom.
- Includes saturation, brightness, contrast and dark overlay controls.
- Applies the default Aniki image treatment automatically, except on `MainBackground.jpg`.
- Can show interface previews for some views while editing. These overlays are not included in the exported images.
- Saves editable `.avpc` project files.
- Exports the finished pack as a ZIP ready for Aniki Helper.

## How to use it

1. Enter a name for the pack and the author name.
2. Select a view from the list on the left.
3. Select or drop an image.
4. Move and zoom the image until the preview looks right.
5. Repeat for the other views. **Fill empty views** can reuse the current image where nothing has been selected yet.
6. Click **Export Visual Pack ZIP**.
7. Import the ZIP from the Visual Packs page in Aniki Helper.

`MainBackground.jpg` keeps its original colors. The other images receive the default Aniki treatment before export.

**Show UI preview** adds an interface reference over the editor preview when one is available. It is only there to help with image placement and is never exported into the pack.

Project files keep source-image paths relative to the `.avpc` file when possible. If you move a project, keeping its source images with it will make reopening it easier.

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
publish\win-x64\AnikiVisualPackCreator.exe
```
