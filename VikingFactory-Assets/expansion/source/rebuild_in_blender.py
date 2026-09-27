"""Rebuild expansion GLBs using Blender's bundled NumPy, then validate."""
import runpy
import sys
from pathlib import Path

SOURCE = Path(__file__).resolve().parent
sys.path.insert(0, str(SOURCE))
import build_expansion as expansion

expansion.build()
expansion.export()
runpy.run_path(str(SOURCE / "validate_expansion.py"), run_name="__main__")
