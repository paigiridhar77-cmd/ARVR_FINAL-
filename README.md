# Virtual Teach Pendant Trainer for Industrial Robot Programming in Virtual Reality

A Unity 6 trainer that reproduces the teach pendant workflow used to program an
industrial robot on the shop floor, and measures what the trainee actually does.

Mini project for **MTE 4442 Augmented Reality and Virtual Reality**, Department of
Mechatronics Engineering, Manipal Institute of Technology, MAHE, Manipal.

| Team member | Roll number |
|---|---|
| Sriprada Bhat | 230929312 |
| Giridhar Pai | 230929002 |
| Nakhuda Mohammed Yusuf | 230929320 |
| Mohammed Zayn | 230929186 |

---

## Overview

Industrial robots are programmed with a handheld teach pendant, one joint at a time.
A six axis cell with fixtures and safety fencing costs several lakhs of rupees, and a
beginner who jogs the arm the wrong way can damage the arm or the fixture, so
engineering students rarely get to practise on real hardware.

This project puts the same workflow in a virtual cell. The trainee selects a joint,
jogs it while holding a deadman switch, adjusts the speed override, teaches program
points with gripper actions, and plays the program back with coordinated joint motion
to carry a part from a machine fixture to a conveyor.

What separates it from existing virtual pendants is the measurement layer. The
software records every collision between a robot link and the cell hardware, every
joint limit violation, the program cycle time, the number of points taught, the total
joint travel and whether the task completed, and writes them to CSV with no observer
present.

## Features

- Six axis articulated robot with the joint axes, travel limits and maximum speeds of
  a FANUC LR Mate 200iD
- Teach pendant logic: joint selection, deadman jogging, speed override, point
  teaching with gripper open and close, program editing and playback
- Joint interpolated playback, where the joint with the furthest to travel sets the
  duration and all six joints start and finish together, as a real controller runs a
  J move
- A fenced work cell with a machine table and fixture, a belt conveyor, a control
  cabinet and a safety beacon, all generated procedurally in code
- Automatic per session logging to `summary.csv` plus a timestamped event file
- An on screen tool position readout in millimetres, because a perspective view
  cannot tell you whether the gripper is lined up over the part
- Desktop keyboard control, so the whole trainer runs without a headset
- A Python analysis script that turns the logged sessions into group comparison,
  learning curve and clean pass figures

## Requirements

| Item | Version or detail |
|---|---|
| Unity | 6000.x LTS, Universal 3D template, Universal Render Pipeline |
| Platform | Windows or macOS |
| RAM | 8 GB minimum, 16 GB recommended |
| Disk | About 30 GB free for Unity and the project |
| Python | 3.9 or later, for the analysis only |
| Python packages | `pandas`, `matplotlib`, `scipy` |
| Headset | Optional. Meta Quest 2 or 3 with OpenXR. The trainer runs fully on a desktop without one |

## Setup

1. **Install Unity.** Install Unity Hub, then a Unity 6 editor version beginning
   `6000.` and marked LTS.

2. **Open the project.** Clone this repository and add the project folder in Unity
   Hub, then open it. The first launch takes several minutes while Unity rebuilds its
   `Library` folder, which is not tracked here by design.

3. **Set the input handling.** Open `Edit > Project Settings > Player > Other
   Settings` and change **Active Input Handling** to **Both**. Unity restarts.
   Without this the keyboard controls do not respond and there is no error message to
   explain why.

4. **Import TextMeshPro.** `Window > TextMeshPro > Import TMP Essential Resources`.

5. **Build the scene.** Open the scene in `Assets/Scenes`, then run
   **`Tools > Robot Trainer > 0. Build Everything`**. This generates the textures and
   materials, builds the robot and the work cell, sets the lighting, frames the camera
   and wires the trainer system together.

   You should see a yellow six axis arm on a dark pedestal, a belt conveyor to one
   side, a machine table with a blue part held in a fixture, wire mesh fencing around
   the cell, a control cabinet and an amber beacon.

## Running it

Press **Play**, then **click inside the Game view** so it has keyboard focus.

| Key | Action |
|---|---|
| `1` to `6` | Select joint J1 to J6 |
| `Space` | Deadman switch. Hold it or nothing moves |
| `Up` / `Down` | Jog the selected joint |
| `[` / `]` | Speed override down and up |
| `T` | Teach a point |
| `C` | Teach a point that closes the gripper |
| `O` | Teach a point that opens the gripper |
| `Backspace` | Delete the last taught point |
| `P` | Play the program |
| `X` | Stop |
| `K` | Save the session to CSV |
| `G` | Hide or show the tool position readout |

`K` is used rather than a function key because macOS assigns the function keys to
system controls, so `F5` never reaches the application. The session can also be saved
by right clicking the **Session Logger (Script)** header in the Inspector during play
and choosing **Save session now**.

### Teaching the pick and place

Work the numbers in the top left of the Game view, not the picture. Get **across**
green, under 25 mm, by jogging J1 and J2, then bring **height** down until the status
line reads `IN THE GRIP, PRESS C`.

1. Jog until the gripper is about 100 mm above the part. Press `T`.
2. Jog down until the readout says the part is in the grip. Press `C`.
3. Jog straight up, clear of the fixture and its clamps. Press `T`.
4. Jog across until the gripper is above the conveyor belt. Press `T`.
5. Jog down towards the belt. Press `O`.
6. Jog back up. Press `T`.
7. Press `P` to play the program back.

### Where the session files go

| Platform | Path |
|---|---|
| Windows | `C:\Users\<you>\AppData\LocalLow\DefaultCompany\<ProjectName>\` |
| macOS | `~/Library/Application Support/DefaultCompany/<ProjectName>/` |

Both folders are hidden by default. The exact path is printed to the Console when the
session is saved.

## Editor menu

| Item | What it does |
|---|---|
| `0. Build Everything` | Everything below, in order. Use this |
| `1. Build Detailed Robot` | Rebuilds the arm only |
| `2. Build Detailed Cell` | Rebuilds the cell furniture only |
| `3. Set Up Lighting` | Key light, cool fill and ambient |
| `4. Frame Camera (work view)` | Robot and fixture, clear of the fence posts. The default |
| `4b. Frame Camera (whole cell)` | The whole cell, for figures |
| `4c. Frame Camera (over the fixture)` | Tight on the fixture, for lining the gripper up |
| `5. Wire Trainer System` | Reconnects the pendant, logger and link reporters |

Each builder deletes its own old objects first, so running them again is safe.

## Analysis

```bash
cd Analysis
pip install pandas matplotlib scipy
cp "<save folder>/summary.csv" .
python analyse.py
```

This prints run counts, means, standard deviations and Welch two sample t tests, and
writes three figures: a group comparison, a learning curve across attempts, and a
clean pass rate.

`make_simulated_data.py` generates a dataset in the logger's exact schema so the
analysis chain can be verified before any participant is run. Every row it writes
carries `data_source=simulated`, a column the logger never writes. `analyse.py`
detects that column and watermarks every figure it produces, so simulated output can
never be mistaken for a measured result. The watermark disappears by itself once a
real `summary.csv` replaces it.

## Repository structure

```
Assets/Scripts/            15 runtime scripts
Assets/Scripts/Editor/     4 editor scripts that build the robot, cell and scene
Assets/Scenes/             the work cell scene
Packages/                  Unity package manifest
ProjectSettings/           project configuration
Analysis/                  analyse.py, make_simulated_data.py, sample data
Media/                     demonstration video and audio
Docs/                      report and presentation
```

**`Assets/Scripts/Editor` must keep that exact name.** Unity treats a folder called
`Editor` specially; scripts that use the `UnityEditor` namespace only compile inside
one. Moving them elsewhere breaks the build with a confusing error.

### Scripts

| Script | Responsibility |
|---|---|
| `RobotJoint.cs` | One revolute joint: axis, travel limits, maximum speed, current angle |
| `RobotArm.cs` | The ordered joint chain. Reads and writes poses, computes move duration |
| `TeachPendant.cs` | Jogging, deadman, speed override, teaching, program storage, playback |
| `Gripper.cs` | Picks the part up by parenting it to the tool, and releases it |
| `SessionLogger.cs` | Counters for collisions, limits, cycle time and completion. Writes the CSV files |
| `CellSafety.cs` | Goes on every link. Reports contact with anything tagged as cell hardware |
| `TargetZone.cs` | The trigger above the conveyor that decides the task is finished |
| `DesktopPendantInput.cs` | Maps the keyboard onto the pendant interface |
| `PickAssist.cs` | On screen tool position readout in millimetres |
| `PendantScreen.cs` | Text on the VR pendant display |
| `PendantButton.cs` | One physical button on the VR pendant |
| `ConveyorAnimator.cs` | Scrolls the belt and spins the rollers |
| `BeaconLight.cs` | Pulses the safety beacon |
| `MeshKit.cs` | The three mesh primitives the cell is built from: lathe, bevelled box, textured quad |
| `TextureKit.cs` | Generates every texture from deterministic noise |
| `Editor/AssetForge.cs` | Saves generated meshes, textures and materials as project assets |
| `Editor/RobotBuilderPro.cs` | Builds the arm, the pedestal and the collision volumes |
| `Editor/CellBuilderPro.cs` | Builds the floor, table, fixture, conveyor, fencing, cabinet and beacon |
| `Editor/SceneBuilderPro.cs` | The editor menu. Builds everything, lights it, frames the camera and wires it up |

## Design notes

**Joint interpolated motion, not linear motion.** Linear motion needs an inverse
kinematics solver. Joint motion is what a trainee learns first on a real pendant and
keeps the robot model simple and correct. The consequence, also true on a real
machine, is that the tool sweeps a curve rather than travelling in a straight line.

**The gripper parents the part rather than simulating finger friction.** Friction
based grasping is unstable in a game physics engine. Parenting the part to the tool on
close is how commercial robot simulation packages handle it.

**Trigger colliders on kinematic rigidbodies for every link.** The arm is moved by
setting transform values, and Unity raises no ordinary collision events for objects
moved that way. Trigger events do fire between a kinematic rigidbody and a static
collider, which is why every link carries both.

**Visual meshes carry no colliders.** Collision uses six simple box volumes matched to
each link's swept volume, which is standard practice in commercial robot simulation.
It keeps the physics stable and meant the collision behaviour was unchanged when the
geometry was rebuilt.

**The robot stands on a 0.40 m pedestal for kinematic reasons, not visual ones.** The
wrist centre reaches only 0.590 m from the shoulder. Bolted flat to the floor the
shoulder sits at 0.330 m, the part sits at 0.760 m, and the wrist would have had to
reach 0.680 m, which is 90 mm more than the arm has. Raising the shoulder to 0.730 m
and lowering the table to 0.60 m puts every taught point at 81 to 87 percent of the
wrist's reach.

**All geometry is generated in code.** Nothing is downloaded, so there is no asset
licence to account for, and the whole cell regenerates from one menu item on any
machine.

## Acknowledgements

Built with [Unity 6](https://unity.com) and the Universal Render Pipeline, Unity's
[XR Interaction Toolkit](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@latest)
and [OpenXR](https://www.khronos.org/openxr/), and
[TextMeshPro](https://docs.unity3d.com/Packages/com.unity.textmeshpro@latest).
Analysis uses [pandas](https://pandas.pydata.org),
[matplotlib](https://matplotlib.org) and [SciPy](https://scipy.org).

Robot joint travel limits and maximum speeds are based on the published specification
of the FANUC LR Mate 200iD. No FANUC code, model or asset is used or redistributed
here; the geometry is original and generated procedurally.

The design of the feedback follows two findings from the literature: that plain
virtual reality adds little over conventional training unless it gives the trainee
information the real cell does not (Nathanael, Mosialos and Vosniakos, 2016), and that
over assistance makes later performance worse (Wolfartsberger et al., 2023). The
trainer therefore reports consequences, never answers.

## Status

The trainer is complete and operational as a desktop application. Nine of ten system
test cases pass; cycle time repeatability across repeated playbacks was not measured.
The VR layer is prepared and exercised through the XR Device Simulator, but the
handheld pendant object and headset validation were not completed.

The comparative user study the instrument was built for was designed in full and the
measurement and analysis chain was verified end to end, but no participants were run
before the submission date, so no claim is made about whether practice in the trainer
improves performance.
