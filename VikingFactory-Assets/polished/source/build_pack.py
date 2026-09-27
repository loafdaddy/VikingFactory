"""Build the polished machines other than the water wheel.
The water wheel stays in build_water_wheel.py. Prototype GLBs are not overwritten.
"""
import sys
from math import cos, sin, pi
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from meshkit import Scene, beam, box, cylinder, oriented

def shaft():
    scene = Scene("vf_shaft_2m")
    scene.group("Static", (0, 0, 0))
    scene.group("Rotor", (0, 0.5, 0), axis="z")
    scene.marker("Kinetic_In", (0, 0.5, -1))
    scene.marker("Kinetic_Out", (0, 0.5, 1))
    for z in (-0.62, 0.62):
        scene.add("Static", box("Cradle", (0, 0.22, z), (0.28, 0.12, 0.22), (1, 0, 0), scene.oak))
        scene.add("Static", box("Foot", (0, 0.05, z), (0.36, 0.1, 0.28), (1, 0, 0), scene.stone))
        scene.add("Static", box("Strap", (0, 0.34, z), (0.3, 0.04, 0.08), (1, 0, 0), scene.iron))
        scene.add("Static", cylinder("Bearing", (0, 0.5, z), 0.11, 0.08, "z", scene.bronze, 12))
    scene.add("Rotor", cylinder("Log", (0, 0, 0), 0.075, 1.92, "z", scene.oak, 10))
    scene.add("Rotor", cylinder("BandA", (0, 0, -0.82), 0.09, 0.04, "z", scene.iron, 10))
    scene.add("Rotor", cylinder("BandB", (0, 0, 0.82), 0.09, 0.04, "z", scene.iron, 10))
    scene.finish()

def crank():
    scene = Scene("vf_hand_crank")
    scene.group("Static", (0, 0, 0))
    scene.group("Crank", (0, 1.05, 0), axis="x")
    scene.marker("Kinetic_Out", (-0.375, 1.05, 0))
    scene.marker("Interact", (0.72, 1.43, 0))
    scene.add("Static", box("Foot", (0, 0.06, 0), (0.46, 0.12, 0.4), (1, 0, 0), scene.stone))
    scene.add("Static", box("Post", (0, 0.58, 0), (0.16, 0.92, 0.16), (0, 1, 0), scene.oak))
    scene.add("Static", box("Cap", (0, 1.05, 0), (0.28, 0.16, 0.28), (1, 0, 0), scene.oak))
    scene.add("Static", box("Strap", (0, 0.7, 0), (0.2, 0.05, 0.2), (1, 0, 0), scene.iron))
    scene.add("Static", cylinder("Bearing", (-0.16, 1.05, 0), 0.1, 0.1, "x", scene.bronze, 12))
    scene.add("Crank", cylinder("Axle", (-0.12, 0, 0), 0.04, 0.42, "x", scene.iron, 8))
    scene.add("Crank", beam("Arm", (0.16, 0, 0), (0.16, 0.36, 0), 0.06, 0.05, scene.oak))
    scene.add("Crank", cylinder("Grip", (0.42, 0.36, 0), 0.035, 0.42, "x", scene.oak_light, 8))
    scene.finish()

def clutch():
    # Pack has no clutch mesh. Ports sit on the shaft axle, not on the old cube centre.
    scene = Scene("vf_clutch")
    scene.group("Static", (0, 0, 0))
    scene.marker("Kinetic_In", (0, 0.5, -0.35))
    scene.marker("Kinetic_Out", (0, 0.5, 0.35))
    scene.add("Static", box("Foot", (0, 0.06, 0), (0.4, 0.12, 0.5), (0, 0, 1), scene.stone))
    scene.add("Static", box("Post", (0, 0.32, 0), (0.14, 0.4, 0.14), (0, 1, 0), scene.oak))
    scene.add("Static", box("Housing", (0, 0.5, 0), (0.28, 0.22, 0.46), (0, 0, 1), scene.oak))
    scene.add("Static", cylinder("Sleeve", (0, 0.5, 0), 0.09, 0.7, "z", scene.bronze, 12))
    scene.add("Static", box("Lever", (0.16, 0.72, 0), (0.06, 0.28, 0.06), (0, 1, 0), scene.iron))
    scene.finish()

def cog():
    scene = Scene("vf_cog")
    scene.group("Static", (0, 0, 0))
    scene.group("Rotor", (0, 0.68, 0), axis="x")
    scene.marker("Kinetic_In", (-0.35, 0.68, 0))
    scene.marker("Kinetic_Out", (0.35, 0.68, 0))
    for x in (-0.28, 0.28):
        scene.add("Static", box("Post", (x, 0.4, 0), (0.12, 0.7, 0.12), (0, 1, 0), scene.oak))
        scene.add("Static", box("Foot", (x, 0.05, 0), (0.22, 0.1, 0.28), (0, 0, 1), scene.stone))
        scene.add("Static", cylinder("Bearing", (x, 0.68, 0), 0.09, 0.08, "x", scene.bronze, 12))
    scene.add("Static", beam("Tie", (-0.28, 0.16, 0), (0.28, 0.16, 0), 0.08, 0.08, scene.oak))
    scene.add("Rotor", cylinder("Hub", (0, 0, 0), 0.12, 0.18, "x", scene.bronze, 12))
    scene.add("Rotor", cylinder("Disc", (0, 0, 0), 0.36, 0.06, "x", scene.oak, 16))
    for i in range(8):
        a = 2 * pi * i / 8
        scene.add("Rotor", oriented(
            "Tooth",
            (0, 0.46 * cos(a), 0.46 * sin(a)),
            (0.05, 0.16, 0.1),
            (1, 0, 0),
            (0, cos(a), sin(a)),
            scene.oak_light))
    scene.finish()

def conveyor():
    scene = Scene("vf_conveyor_2m")
    scene.group("Static", (0, 0, 0))
    scene.group("Belt_Surface", (0, 0.77, 0), scroll="u")
    scene.group("Roller_-0.86", (0, 0.7, -0.86), axis="x")
    scene.group("Roller_0.86", (0, 0.7, 0.86), axis="x")
    scene.marker("Item_In", (0, 0.82, -1))
    scene.marker("Item_Out", (0, 0.82, 1))
    # Power ports match the old scaled belt: one metre from the centre, raised to the shaft axle.
    scene.marker("Kinetic_In", (0, 0.5, -1))
    scene.marker("Kinetic_Out", (0, 0.5, 1))
    scene.marker("Roller_Drive", (0.58, 0.7, -0.86))
    for x in (-0.5, 0.5):
        for z in (-0.7, 0.7):
            scene.add("Static", box("Leg", (x, 0.34, z), (0.08, 0.68, 0.08), (0, 1, 0), scene.oak))
            scene.add("Static", box("Foot", (x, 0.04, z), (0.16, 0.08, 0.16), (1, 0, 0), scene.stone))
        scene.add("Static", beam("Rail", (x, 0.84, -0.95), (x, 0.84, 0.95), 0.06, 0.08, scene.oak))
        scene.add("Static", box("Strap", (x, 0.7, 0), (0.08, 0.04, 0.16), (0, 0, 1), scene.iron))
    scene.add("Static", beam("TieA", (-0.5, 0.2, -0.7), (0.5, 0.2, -0.7), 0.06, 0.06, scene.oak))
    scene.add("Static", beam("TieB", (-0.5, 0.2, 0.7), (0.5, 0.2, 0.7), 0.06, 0.06, scene.oak))
    scene.add("Belt_Surface", box("Hide", (0, 0, 0), (0.86, 0.02, 1.9), (0, 0, 1), scene.hide))
    for name in ("Roller_-0.86", "Roller_0.86"):
        scene.add(name, cylinder("Roll", (0, 0, 0), 0.05, 0.82, "x", scene.oak, 10))
        scene.add(name, cylinder("Pin", (0, 0, 0), 0.02, 1.05, "x", scene.iron, 8))
    scene.finish()

def corner():
    scene = Scene("vf_conveyor_corner")
    scene.group("Static", (0, 0, 0))
    scene.marker("Item_In", (0.21, 0.82, -0.6))
    scene.marker("Item_Out", (-0.6, 0.82, 0.21))
    scene.marker("Kinetic", (0.65, 0.65, 0))
    for x, z in ((0.35, -0.35), (0.35, 0.35), (-0.35, 0.35)):
        scene.add("Static", box("Leg", (x, 0.36, z), (0.08, 0.72, 0.08), (0, 1, 0), scene.oak))
        scene.add("Static", box("Foot", (x, 0.04, z), (0.16, 0.08, 0.16), (1, 0, 0), scene.stone))
    scene.add("Static", box("DeckZ", (0.21, 0.74, -0.15), (0.7, 0.06, 0.7), (0, 0, 1), scene.oak))
    scene.add("Static", box("DeckX", (-0.15, 0.74, 0.21), (0.7, 0.06, 0.7), (1, 0, 0), scene.oak))
    scene.add("Static", box("HideZ", (0.21, 0.78, -0.18), (0.58, 0.02, 0.55), (0, 0, 1), scene.hide))
    scene.add("Static", box("HideX", (-0.18, 0.78, 0.21), (0.55, 0.02, 0.58), (1, 0, 0), scene.hide))
    scene.add("Static", cylinder("Hub", (0.05, 0.8, 0.05), 0.08, 0.06, "y", scene.bronze, 10))
    scene.finish()

def feeder():
    scene = Scene("vf_feeder")
    scene.group("Static", (0, 0, 0))
    scene.group("Arm_Yaw", (0, 0.8, 0), axis="y", motion="swing")
    scene.marker("Pickup", (0, 0.68, 1.08))
    scene.marker("Dropoff", (0, 0.68, -1.08))
    scene.marker("Kinetic", (0.4, 0.4, 0))
    scene.add("Static", box("Foot", (0, 0.06, 0), (0.5, 0.12, 0.5), (1, 0, 0), scene.stone))
    scene.add("Static", box("Post", (0, 0.42, 0), (0.14, 0.6, 0.14), (0, 1, 0), scene.oak))
    scene.add("Static", box("Head", (0, 0.78, 0), (0.28, 0.16, 0.28), (1, 0, 0), scene.oak))
    scene.add("Static", cylinder("Bearing", (0, 0.8, 0), 0.1, 0.08, "y", scene.bronze, 12))
    scene.add("Static", box("Strap", (0, 0.45, 0), (0.18, 0.04, 0.18), (1, 0, 0), scene.iron))
    scene.add("Arm_Yaw", beam("Arm", (0, 0, 0.12), (0, -0.08, 0.78), 0.06, 0.05, scene.oak))
    scene.add("Arm_Yaw", box("Scoop", (0, -0.12, 0.86), (0.16, 0.08, 0.14), (0, 0, 1), scene.bronze))
    scene.finish()

def basket():
    scene = Scene("vf_catch_basket")
    scene.group("Static", (0, 0, 0))
    scene.marker("Item_In", (0, 0.82, 0))
    scene.marker("Item_Out", (0, 0.35, 0.5))
    scene.add("Static", box("Floor", (0, 0.05, 0), (0.9, 0.08, 0.8), (1, 0, 0), scene.oak))
    scene.add("Static", box("Liner", (0, 0.1, 0), (0.78, 0.02, 0.68), (1, 0, 0), scene.hide))
    scene.add("Static", box("SideL", (-0.46, 0.38, 0), (0.06, 0.58, 0.86), (0, 1, 0), scene.oak))
    scene.add("Static", box("SideR", (0.46, 0.38, 0), (0.06, 0.58, 0.86), (0, 1, 0), scene.oak))
    scene.add("Static", box("Back", (0, 0.38, -0.4), (0.86, 0.58, 0.06), (1, 0, 0), scene.oak))
    scene.add("Static", box("Lip", (0, 0.16, 0.4), (0.86, 0.12, 0.06), (1, 0, 0), scene.oak))
    for x in (-0.46, 0.46):
        scene.add("Static", box("Strap", (x, 0.38, 0), (0.08, 0.04, 0.2), (0, 0, 1), scene.iron))
    scene.finish()

def splitter():
    scene = Scene("vf_splitter")
    scene.group("Static", (0, 0, 0))
    scene.group("Selector", (0, 0.87, 0), axis="y", motion="swing")
    scene.marker("Item_In", (0, 0.85, -0.7))
    scene.marker("Item_Out_Left", (-0.7, 0.85, 0))
    scene.marker("Item_Out_Right", (0.7, 0.85, 0))
    scene.marker("Item_Out_Forward", (0, 0.85, 0.7))
    scene.marker("Kinetic", (0, 0.52, 0.7))
    for x, z in ((0.4, 0.4), (0.4, -0.4), (-0.4, 0.4), (-0.4, -0.4)):
        scene.add("Static", box("Leg", (x, 0.32, z), (0.08, 0.64, 0.08), (0, 1, 0), scene.oak))
    scene.add("Static", box("Deck", (0, 0.68, 0), (1.15, 0.08, 1.15), (1, 0, 0), scene.oak))
    scene.add("Static", box("LaneZ", (0, 0.76, 0), (0.28, 0.06, 1.05), (0, 0, 1), scene.oak_light))
    scene.add("Static", box("LaneX", (0, 0.76, 0), (1.05, 0.06, 0.28), (1, 0, 0), scene.oak_light))
    scene.add("Static", cylinder("Hub", (0, 0.84, 0), 0.08, 0.06, "y", scene.bronze, 10))
    scene.add("Selector", beam("Gate", (0, 0.04, 0.08), (0, 0.04, 0.42), 0.08, 0.03, scene.bronze))
    scene.finish()

def trough():
    scene = Scene("vf_gravity_trough")
    scene.group("Static", (0, 0, 0))
    scene.marker("Item_In", (0, 0.94, 1))
    scene.marker("Item_Out", (0, 0.53, -1))
    scene.add("Static", oriented("Floor", (0, 0.66, 0), (0.62, 1.85, 0.04), (1, 0, 0), (0, -0.48, -1.7), scene.oak))
    scene.add("Static", oriented("WallL", (-0.28, 0.74, 0), (0.04, 1.85, 0.16), (1, 0, 0), (0, -0.48, -1.7), scene.oak))
    scene.add("Static", oriented("WallR", (0.28, 0.74, 0), (0.04, 1.85, 0.16), (1, 0, 0), (0, -0.48, -1.7), scene.oak))
    scene.add("Static", box("FootHigh", (0, 0.28, 0.75), (0.5, 0.56, 0.16), (1, 0, 0), scene.oak))
    scene.add("Static", box("FootLow", (0, 0.12, -0.75), (0.5, 0.24, 0.16), (1, 0, 0), scene.oak))
    scene.add("Static", box("StoneHigh", (0, 0.05, 0.75), (0.58, 0.1, 0.24), (1, 0, 0), scene.stone))
    scene.add("Static", box("StoneLow", (0, 0.04, -0.75), (0.58, 0.08, 0.24), (1, 0, 0), scene.stone))
    scene.add("Static", box("Strap", (0, 0.5, -0.2), (0.66, 0.04, 0.06), (1, 0, 0), scene.iron))
    scene.finish()

def mill():
    scene = Scene("vf_recipe_mill")
    scene.group("Static", (0, 0, 0))
    scene.group("Millstone", (0, 1.43, 0), axis="y")
    scene.marker("Item_In_0", (-0.84, 1.13, 0))
    scene.marker("Item_In_1", (0.84, 1.13, 0))
    scene.marker("Item_In_2", (0, 1.13, -0.74))
    scene.marker("Item_In_3", (0, 1.7, -0.87))
    scene.marker("Item_Out", (0, 1.06, 1.18))
    scene.marker("Kinetic", (0.86, 0.65, 0))
    for x, z in ((0.55, 0.4), (0.55, -0.4), (-0.55, 0.4), (-0.55, -0.4)):
        scene.add("Static", box("Leg", (x, 0.5, z), (0.1, 1.0, 0.1), (0, 1, 0), scene.oak))
    scene.add("Static", box("Top", (0, 1.05, 0), (1.4, 0.1, 1.1), (1, 0, 0), scene.oak))
    scene.add("Static", box("Hopper", (0, 1.85, -0.35), (0.36, 0.4, 0.36), (0, 1, 0), scene.oak))
    scene.add("Static", cylinder("Spindle", (0.7, 0.7, 0), 0.05, 0.5, "x", scene.iron, 8))
    scene.add("Millstone", cylinder("Stone", (0, 0, 0), 0.42, 0.14, "y", scene.stone, 16))
    scene.add("Millstone", cylinder("Collar", (0, 0, 0), 0.1, 0.18, "y", scene.iron, 10))
    scene.finish()

def quarry():
    scene = Scene("vf_quarry")
    scene.group("Static", (0, 0, 0))
    scene.group("Drill", (0, 1.25, 0), axis="y")
    scene.marker("Item_Out", (0, 0.49, 1.35))
    scene.marker("Kinetic", (1.05, 2.35, 0))
    scene.marker("Ground_Probe", (0, 0, 0))
    scene.add("Static", box("Base", (0, 0.08, 0), (1.8, 0.16, 1.5), (1, 0, 0), scene.stone))
    for x in (-0.8, 0.8):
        scene.add("Static", box("Post", (x, 1.25, 0), (0.16, 2.3, 0.16), (0, 1, 0), scene.oak))
        scene.add("Static", box("Strap", (x, 1.6, 0), (0.2, 0.05, 0.2), (1, 0, 0), scene.iron))
    scene.add("Static", beam("Beam", (-0.8, 2.35, 0), (0.8, 2.35, 0), 0.14, 0.12, scene.oak))
    scene.add("Static", box("Chute", (0, 0.4, 0.85), (0.4, 0.2, 0.5), (0, 0, 1), scene.oak))
    scene.add("Drill", cylinder("Bit", (0, -0.15, 0), 0.06, 1.5, "y", scene.iron, 8))
    scene.add("Drill", cylinder("Head", (0, 0.55, 0), 0.14, 0.12, "y", scene.bronze, 10))
    scene.finish()

if __name__ == "__main__":
    shaft()
    crank()
    clutch()
    cog()
    conveyor()
    corner()
    feeder()
    basket()
    splitter()
    trough()
    mill()
    quarry()
