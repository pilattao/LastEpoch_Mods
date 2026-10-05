# Sentinel Model Swapper — Last Epoch 1.5

Standalone restoration of the disabled character-model feature in
`LastEpoch_Hud/Scripts/Mods/Character/Character_Visuals.cs` by RCInet.
This project compiles separately; it does not load the rest of LastEpoch_Hud.

## Use

1. Install MelonLoader 0.7.3 and launch the game once to generate its IL2CPP assemblies.
2. With the game closed, place **only** `LastEpoch.ModelSwapper.dll` in the game's `Mods` folder.
3. Enter the game with a Sentinel (any mastery). The initial selection is **Rogue**.
4. Press **F8** to open the menu and select **Rogue**, **Acolyte**, or **Original**.
5. Enter another area or return to character selection and reload the character to apply it.

The menu saves `[SentinelModelSwapper] Model` in `UserData/MelonPreferences.cfg`.
Invalid settings use the original model. To uninstall, close the game and remove
`Mods/LastEpoch.ModelSwapper.dll`; no save conversion is needed.

## Behavior

- Only the local Sentinel's visual actor is eligible. Other classes and remote players are untouched.
- `ActorVisuals.CreateClassVisuals(int)` receives the selected model ID.
- `ActorVisuals.GetCharacterInfoProvider()` supplies a separate native component for
  the swapped visual actor. It copies level, mastery, and faction information while
  supplying the selected **visual** class. The gameplay provider is not modified.
- Class-specific equipment incompatible with the body uses a compatible base appearance.
  Target-class bases are preferred, then shared bases. An incompatible unique uses
  an ordinary base appearance; its inventory item and stats are not changed.
- Equipment has the same body choice throughout that actor's lifetime. Menu changes
  take effect at the next model creation, avoiding mixed rigs during asynchronous equipping.
- Startup installs all five patches together; a patching failure removes them all.
- The mod never assigns `CharacterData.CharacterClass`, writes saves, or changes skills.

Cosmetic MTX and every Sentinel skill animation have not been exhaustively tested.
Full online/offline support should not be inferred from a successful build.

## Build and tests

Use .NET SDK 8 with the generated assemblies from the game being targeted:

```sh
dotnet build ModelSwapper/LastEpoch.ModelSwapper.csproj -c Release -p:LastEpochPath="/path/to/Last Epoch"
dotnet test ModelSwapper.Tests/ModelSwapper.Tests.csproj
```

Output: `ModelSwapper/bin/Release/net6.0/LastEpoch.ModelSwapper.dll`.
The game, Unity, Harmony, and MelonLoader DLLs are references only and are not distributed.

The tests exercise selection, local-player isolation, invalid preferences, per-actor
state, cleanup, and compatible armor selection. They cannot exercise Unity's native
asset loading or animations.

## Verification boundary

Built against Steam build **25711439**, Unity **6000.4.8f1**, and generated assemblies
from MelonLoader **0.7.3**. Compilation and the 26 policy/state tests pass.
In-game startup loaded all five patches together with the three existing Terrible
mods. The local Sentinel requested class ID 4; Unity created
`Rogue Visuals Prefab_V4 (Clone)` and its equipment manager reported initialized.
Other player visual actors retained their original class IDs. These observations
confirm native model creation, but do not establish correct armor or animations.

Human gameplay verification is pending: enter a Sentinel, confirm the body and armor, cast
Smite, move, change equipment, transition between areas, and check that class/mastery
and skills remain Sentinel/Paladin. Then test Acolyte and Original in the same way.

`MelonLoader/Latest.log` reports the patch count, requested class/body IDs, and visual
readiness. A `CreateClassVisuals` message confirms a requested substitution; it does
not by itself prove that Unity loaded the correct model and animations.
