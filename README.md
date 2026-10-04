# DRIFT

A third-person puzzle game about switching between a broken world and its restored form.

## Run

Open the project in Unity 6000.3.25f1, open `Assets/Scenes/MainMenu.unity`, and press Play.

## Controls

- WASD: move
- Mouse: look
- Space: jump
- Shift: sprint
- Q: switch reality after collecting the artifact
- E: interact
- Escape: pause

Drift stability holds up to 15 seconds in Normal World and recharges in Broken World, taking 15 seconds from empty to full. Switching preserves the remaining charge; an empty meter needs at least one second of charge before re-entry. The meter is shown in BrokenWorld and hidden in Cave. Falling returns you to your current checkpoint and preserves charge.

The chamber beyond the Silent Overlook is a short exploration loop. Its return path is open; there is no pressure-plate gate.

Behind the final node landing, Normal restores the ridge staircase. Switch to Broken on the resting ledge to jump across the floating rocks. Solving The Missing Weight opens the left-side descent door, which stays open for retries and reality switches.

The Missing Weight room beside the ridge summit guards the third energy node. E picks up the block or places it on a nearby plate. The first placement during a Normal visit records an echo. Move the real block to the other plate, then switch to Broken to activate both weights and open the gate. Enter Normal again to correct the arrangement.

Restore the three anchors with E in any order. Each counts once; restoring all three begins the final escape and tower ascent. The third anchor still requires opening its puzzle gate, but does not require activating the other anchors first.

The barred gate reveals the node before solving. Two gate sockets mirror the plate weights; both light up when the real block and Broken echo complete the circuit. The room entrance saves a silent retry checkpoint.

## Development

MainMenu, Cave and BrokenWorld load one at a time. The player, camera and shared managers persist between gameplay scenes.

Use **DRIFT > Scenes** to edit the environments. Play starts through MainMenu.

Use **DRIFT > Build Windows** to build into `Builds/Windows`. Start a fresh Play session and select **DRIFT > Check Gameplay** to run the gameplay checks.

Edit subtitle lines in `Assets/Data/NarrationSequence.asset`. Subtitles can be disabled in Settings.
