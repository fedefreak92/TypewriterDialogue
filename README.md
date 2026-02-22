# TypewriterDialogue

A Unity Mod Manager mod for **Pathfinder: Wrath of the Righteous** that adds a typewriter effect (text appearing character-by-character) to dialogue text.

---

## Table of Contents

1. [Prerequisites](#1-prerequisites)
2. [Install Unity Mod Manager](#2-install-unity-mod-manager)
3. [Open the Project in Visual Studio](#3-open-the-project-in-visual-studio)
4. [Add DLL References](#4-add-dll-references)
5. [Build the Mod](#5-build-the-mod)
6. [Install the Mod](#6-install-the-mod)
7. [Phase A: Discovery](#7-phase-a-discovery)
8. [Phase B: Typewriter Effect](#8-phase-b-typewriter-effect)
9. [Configuration Reference](#9-configuration-reference)
10. [Troubleshooting](#10-troubleshooting)
11. [Checklist](#11-checklist)
12. [Uninstall](#12-uninstall)

---

## 1. Prerequisites

| Software | Version | Notes |
|----------|---------|-------|
| Windows | 10 or 11 | |
| Steam | latest | |
| Pathfinder: Wrath of the Righteous | latest | Default path: `C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Second Adventure\` |
| Visual Studio 2022 | any edition (Community is free) | Workload: **.NET desktop development** |
| .NET Framework 4.8 Targeting Pack | included with VS | If missing: download from [Microsoft](https://dotnet.microsoft.com/download/dotnet-framework/net48) |

> **Tip:** You can also build from the command line with `dotnet build` if you have the .NET SDK installed.

---

## 2. Install Unity Mod Manager

1. Download UMM from [https://www.nexusmods.com/site/mods/21](https://www.nexusmods.com/site/mods/21) (registration required).
2. Extract the ZIP anywhere (e.g. `C:\Tools\UnityModManager\`).
3. Run `UnityModManager.exe` **as Administrator**.
4. In the **Game** dropdown, select **Pathfinder: Wrath of the Righteous**.
   - If the game path is not auto-detected, click the folder icon and browse to `C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Second Adventure\`.
5. Click **Install**.
6. You should see a green "Installed" status.

After installation, these files appear in the game directory:

```
Pathfinder Second Adventure\
  doorstop_config.ini
  winhttp.dll
  Wrath_Data\Managed\UnityModManager\
    UnityModManager.dll                        <-- needed for building the mod
    0Harmony.dll
  Mods\                                        <-- mods go here
```

---

## 3. Open the Project in Visual Studio

**Option A — Open existing project:**

1. Open Visual Studio 2022.
2. File > Open > Project/Solution.
3. Navigate to this repo and open `src\TypewriterDialogue\TypewriterDialogue.csproj`.

**Option B — Clone and open:**

```powershell
git clone <your-repo-url> C:\Users\you\pathfinder_mod_typewriter
cd C:\Users\you\pathfinder_mod_typewriter
start src\TypewriterDialogue\TypewriterDialogue.csproj
```

---

## 4. Add DLL References

The `.csproj` already contains the correct references with HintPaths pointing to the default Steam install location:

```
C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Second Adventure\Wrath_Data\Managed\
```

**If your game is installed elsewhere**, edit the `<WotRManaged>` property at the top of the `.csproj`:

```xml
<PropertyGroup>
  <WotRManaged>D:\Games\Steam\steamapps\common\Pathfinder Second Adventure\Wrath_Data\Managed</WotRManaged>
</PropertyGroup>
```

### Referenced DLLs

| DLL | Purpose |
|-----|---------|
| `UnityModManager\UnityModManager.dll` | UMM API (only exists after UMM install) |
| `UnityEngine.dll` | Base Unity types |
| `UnityEngine.CoreModule.dll` | GameObject, Transform, Object.FindObjectsOfType |
| `UnityEngine.IMGUIModule.dll` | GUILayout for UMM settings panel |
| `UnityEngine.InputLegacyModule.dll` | Input.GetMouseButtonDown, Input.GetKeyDown |

> **Important:** `Unity.TextMeshPro.dll` is intentionally NOT referenced. All TMPro access is via reflection.

If references show yellow warning triangles in Solution Explorer, it means either:
- UMM is not installed yet (for `UnityModManager.dll`), or
- The `<WotRManaged>` path is wrong.

---

## 5. Build the Mod

### From Visual Studio

1. Set configuration to **Release**.
2. Build > Build Solution (or press `Ctrl+Shift+B`).
3. Output: `TypewriterDialogue\TypewriterDialogue.dll` (in the repo root's `TypewriterDialogue\` folder).

### From Command Line

```powershell
cd src\TypewriterDialogue
dotnet build -c Release
```

Verify the output:

```powershell
dir ..\..\TypewriterDialogue\TypewriterDialogue.dll
```

---

## 6. Install the Mod

Copy the entire `TypewriterDialogue\` folder (which contains `Info.json` + `TypewriterDialogue.dll`) into the game's `Mods\` directory:

```powershell
xcopy /E /I /Y TypewriterDialogue "C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Second Adventure\Mods\TypewriterDialogue"
```

Result:

```
Mods\
  TypewriterDialogue\
    Info.json
    TypewriterDialogue.dll
```

---

## 7. Phase A: Discovery

Phase A scans for TextMeshPro text components and logs their hierarchy paths. This helps you find the exact component that displays the main dialogue text.

### Steps

1. Make sure `Config.DebugMode = true` (this is the default).
2. Launch Pathfinder: WotR.
3. Press **Ctrl+F10** to open the UMM GUI.
4. Verify "Typewriter Dialogue Effect" is listed and has a green checkbox.
5. Start a new game or load a save.
6. **Enter a dialogue** with an NPC (main story dialogues are best).
7. Open UMM GUI again (Ctrl+F10) and go to the **Logs** tab.

### Reading the Logs

You'll see lines like:

```
[PhaseA] len=142, name=BodyText, path=Canvas/ServiceWindow/DialogWindow/Body/Content/BodyText
[PhaseA] len=87,  name=SpeakerName, path=Canvas/ServiceWindow/DialogWindow/Header/SpeakerName
```

### Extracting the Target Path

Look for the entry with:
- **Longest text** (`len=` is large, typically 50-300 for dialogue).
- **Name** that suggests dialogue body (e.g. `BodyText`, `DialogText`, `Answer`).
- **Path** that includes dialogue-related parent objects.

**Copy a unique substring** from the `path=` value. Examples:
- `DialogWindow/Body/Content/BodyText`
- `DialogWindow/Body`

This will be used as `TargetPathContains` in Phase B.

---

## 8. Phase B: Typewriter Effect

### Activate Phase B

**Option 1 — Via UMM GUI (no rebuild needed):**

1. Open UMM GUI (Ctrl+F10).
2. Expand the "Typewriter Dialogue Effect" mod settings.
3. Paste the path substring into the **Target path filter** text field.
4. Adjust speed with the slider if desired (default: 45 chars/sec).

**Option 2 — Via code (requires rebuild):**

Edit `src/TypewriterDialogue/Config.cs`:

```csharp
public static string TargetPathContains = "DialogWindow/Body/Content/BodyText";
```

Build and reinstall.

### Test the Effect

1. Enter a dialogue. The text should now appear character-by-character.
2. **Skip test:** While text is revealing, press **Space** or **left-click**. The full text should appear instantly.
3. **Speed test:** Adjust `CharsPerSecond` via UMM GUI slider and try another dialogue line.

### Tuning Tips

| Speed | Effect |
|-------|--------|
| 20 | Slow, dramatic |
| 45 | Default, comfortable reading |
| 80 | Fast but visible |
| 150+ | Nearly instant, barely noticeable |

---

## 9. Configuration Reference

All settings are in `Config.cs` and can also be changed at runtime via UMM GUI.

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `DebugMode` | bool | `true` | Enables Phase A scanning and extra Phase B logging |
| `ScanInterval` | float | `0.333` | Seconds between Phase A scans (0.333 = 3x/sec) |
| `MinTextLen` | int | `20` | Minimum text length for Phase A to log a component |
| `CharsPerSecond` | float | `45` | Typewriter reveal speed |
| `TargetPathContains` | string | `""` | Path filter for Phase B target. Empty = Phase B disabled |

---

## 10. Troubleshooting

### Mod doesn't appear in UMM

- **Folder name must match `Id` in `Info.json`.** Both must be `TypewriterDialogue`.
- Verify `Info.json` is valid JSON (no trailing commas, correct quotes).
- Check that `EntryMethod` is exactly `TypewriterDialogue.Main.Load`.
- Make sure `TypewriterDialogue.dll` is in the same folder as `Info.json`.

### Build fails: "missing reference"

- Install UMM first (see step 2) — `UnityModManager\UnityModManager.dll` only exists after installation.
- Check `<WotRManaged>` path in `.csproj` matches your actual game path.
- Make sure you have the .NET Framework 4.7.2 Targeting Pack installed.

### No log output in Phase A

- Open UMM GUI (Ctrl+F10) > Logs tab. Scroll to the bottom.
- Confirm `DebugMode` is `true` (check the mod settings panel).
- You need to be in a **dialogue**. Menu screens and map views may have no TMPro text longer than 20 chars.
- If you see "Load FAILED" in the log, check the exception message.

### "Assembly 'Unity.TextMeshPro' not loaded"

- This means the TMPro assembly wasn't loaded when the mod initialized. This is unlikely in WotR but could happen if UMM loads the mod very early. Try restarting the game.

### Typewriter not triggering (Phase B)

- `TargetPathContains` is empty — set it to a path substring from Phase A.
- The substring doesn't match any active component — double-check for typos.
- The path may have changed after a game update — re-run Phase A.

### Typewriter applies to wrong text

- Make `TargetPathContains` more specific (use a longer path substring).
- Example: Instead of `"Text"`, use `"DialogWindow/Body/Content/BodyText"`.

### Skip (click/space) not working

- The skip uses `Input.GetMouseButtonDown(0)` and `Input.GetKeyDown(KeyCode.Space)`.
- If the game consumes these inputs before the mod sees them, skip may not trigger. In that case the animation will complete naturally.

### Performance issues

- Disable `DebugMode` in Phase B (stops the Phase A scanner).
- Phase B only calls `FindObjectsOfType` when the cached target is invalid, not every frame.

---

## 11. Checklist

### Phase A Testing

- [ ] UMM is installed and shows "Installed" status.
- [ ] Mod appears in UMM GUI with green checkbox.
- [ ] Log shows `[TypewriterDialogue] Loaded OK` at game start.
- [ ] Entering a dialogue produces `[PhaseA] len=..., name=..., path=...` log entries.
- [ ] Identified the target component path for the main dialogue text body.
- [ ] Copied a unique path substring to use as `TargetPathContains`.

### Phase B Testing

- [ ] Set `TargetPathContains` to the discovered path substring (via GUI or code).
- [ ] Dialogue text now appears character-by-character.
- [ ] Left-click during animation shows full text instantly.
- [ ] Space key during animation shows full text instantly.
- [ ] Speed slider changes the reveal speed in real time.
- [ ] Scene changes / save loading don't cause crashes.
- [ ] Response options are NOT affected by the typewriter.

### Release

- [ ] Set `DebugMode = false` to disable Phase A logging.
- [ ] Build in Release mode.
- [ ] Tested a full dialogue sequence start-to-finish.

---

## 12. Uninstall

1. Close the game.
2. Delete the mod folder:
   ```
   C:\Program Files (x86)\Steam\steamapps\common\Pathfinder Second Adventure\Mods\TypewriterDialogue\
   ```
3. That's it. No game files are modified.

To also remove UMM itself, run `UnityModManager.exe` and click **Uninstall**.
