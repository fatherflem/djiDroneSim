#!/usr/bin/env python3
"""Run the default Normal benchmark protocol headlessly, archive it, and gate it.

Steps:
  1. Find the Unity editor matching ProjectSettings/ProjectVersion.txt.
  2. Launch it in batch mode with HeadlessBenchmarkRunner.RunFromCommandLine.
  3. Zip BenchmarkRuns/<session>/ into BenchmarkRuns/<session>.zip (same layout as F9 uploads).
  4. Run Tools/check_acceptance.py against the baseline session.

Close the project in the Unity Editor first: batch mode can't open a project that's already open.

Usage:
  python Tools/run_headless_benchmark.py
  python Tools/run_headless_benchmark.py --unity "C:/Program Files/Unity/Hub/Editor/6000.3.17f1/Editor/Unity.exe"
  python Tools/run_headless_benchmark.py --fail-on-drift          # parity check vs the freeze baseline
  python Tools/run_headless_benchmark.py --analyze                # also regenerate the analyzer report

Exit code: the Unity runner's code if it failed (1 run, 2 timeout, 3 setup), otherwise
check_acceptance's code (0 ok, 1 regression, 2 drift), or 4 if Unity couldn't be launched.
"""

import argparse
import json
import os
import platform
import shutil
import subprocess
import sys
import threading
import time
import zipfile
from pathlib import Path
from typing import Optional

REPO_ROOT = Path(__file__).resolve().parent.parent
RUNS_ROOT = REPO_ROOT / "BenchmarkRuns"
RESULT_FILE = RUNS_ROOT / "headless_benchmark_result.json"
EXECUTE_METHOD = "DroneSim.EditorTools.Benchmark.HeadlessBenchmarkRunner.RunFromCommandLine"
REAL_CSV = "Apr-8th-2026-08-15AM-Flight-Airdata.csv"
LOG_HIGHLIGHTS = ("[HeadlessBenchmark]", "BenchmarkRunner", "error CS", "Exception", "Scripts have compiler errors")


def project_unity_version() -> str:
    text = (REPO_ROOT / "ProjectSettings" / "ProjectVersion.txt").read_text(encoding="utf-8")
    for line in text.splitlines():
        if line.startswith("m_EditorVersion:"):
            return line.split(":", 1)[1].strip()
    raise RuntimeError("Could not read m_EditorVersion from ProjectVersion.txt")


def find_unity(explicit: Optional[str], version: str) -> Path:
    candidates = []
    if explicit:
        candidates.append(Path(explicit))
    if os.environ.get("UNITY_EDITOR"):
        candidates.append(Path(os.environ["UNITY_EDITOR"]))

    system = platform.system()
    if system == "Windows":
        for base in (os.environ.get("ProgramFiles", r"C:\Program Files"), r"C:\Program Files"):
            candidates.append(Path(base) / "Unity" / "Hub" / "Editor" / version / "Editor" / "Unity.exe")
    elif system == "Darwin":
        candidates.append(Path(f"/Applications/Unity/Hub/Editor/{version}/Unity.app/Contents/MacOS/Unity"))
    else:
        candidates.append(Path.home() / "Unity" / "Hub" / "Editor" / version / "Editor" / "Unity")

    for candidate in candidates:
        if candidate.is_file():
            return candidate
    tried = "\n  ".join(str(c) for c in candidates)
    raise FileNotFoundError(f"Unity {version} not found. Tried:\n  {tried}\nPass --unity or set UNITY_EDITOR.")


def tail_log(path: Path, stop: threading.Event, verbose: bool) -> None:
    """Print highlighted lines from the Unity log while it's being written."""
    while not path.exists() and not stop.is_set():
        time.sleep(0.2)
    if not path.exists():
        return
    with path.open("r", encoding="utf-8", errors="replace") as fp:
        while True:
            line = fp.readline()
            if line:
                if verbose or any(h in line for h in LOG_HIGHLIGHTS):
                    print(f"  unity> {line.rstrip()}", flush=True)
            elif stop.is_set():
                break
            else:
                time.sleep(0.2)


def zip_session(session_dir: Path) -> Path:
    zip_path = session_dir.parent / f"{session_dir.name}.zip"
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as zf:
        zf.write(session_dir, f"{session_dir.name}/")
        for file in sorted(session_dir.rglob("*")):
            if file.is_file():
                zf.write(file, f"{session_dir.name}/{file.relative_to(session_dir).as_posix()}")
    return zip_path


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--unity", help="Path to the Unity editor executable")
    ap.add_argument("--scene", default="Assets/Scenes/DroneTrainingVerticalSlice.unity")
    ap.add_argument("--timeout", type=float, default=900, help="Seconds before the Unity runner gives up")
    ap.add_argument("--graphics", action="store_true", help="Don't pass -nographics (try this if rendering setup fails)")
    ap.add_argument("--fail-on-error", action="store_true", help="Fail if Unity logs any error during the run")
    ap.add_argument("--baseline", default="session_20260413_142657", help="Baseline for check_acceptance ('none' to skip)")
    ap.add_argument("--fail-on-drift", action="store_true", help="Treat metric drift vs baseline as failure")
    ap.add_argument("--no-check", action="store_true", help="Skip the acceptance gate")
    ap.add_argument("--analyze", action="store_true", help="Also run analyze_airdata.py on the new session")
    ap.add_argument("--keep-folder", action="store_true", help="Keep the unzipped session folder")
    ap.add_argument("--verbose", action="store_true", help="Stream the full Unity log")
    args = ap.parse_args()

    try:
        version = project_unity_version()
        unity = find_unity(args.unity, version)
    except (OSError, RuntimeError) as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 4

    lock = REPO_ROOT / "Temp" / "UnityLockfile"
    if lock.exists():
        print(f"warning: {lock} exists. If the project is open in the Unity Editor, close it first.", file=sys.stderr)

    RUNS_ROOT.mkdir(exist_ok=True)
    if RESULT_FILE.exists():
        RESULT_FILE.unlink()

    log_path = REPO_ROOT / "Logs" / "headless_benchmark.log"
    log_path.parent.mkdir(exist_ok=True)
    if log_path.exists():
        log_path.unlink()

    cmd = [str(unity), "-batchmode"]
    if not args.graphics:
        cmd.append("-nographics")
    cmd += [
        "-projectPath", str(REPO_ROOT),
        "-executeMethod", EXECUTE_METHOD,
        "-benchmarkScene", args.scene,
        "-benchmarkOutput", str(RUNS_ROOT),
        "-benchmarkTimeout", str(args.timeout),
        "-logFile", str(log_path),
    ]
    if args.fail_on_error:
        cmd.append("-benchmarkFailOnError")

    print(f"Unity {version}: {unity}")
    print(f"Log: {log_path}")
    started = time.time()
    stop = threading.Event()
    tailer = threading.Thread(target=tail_log, args=(log_path, stop, args.verbose), daemon=True)
    tailer.start()
    try:
        # Unity enforces its own timeout; this one only catches a hung editor.
        proc = subprocess.run(cmd, timeout=args.timeout + 600)
        unity_code = proc.returncode
    except subprocess.TimeoutExpired:
        unity_code = 2
        print("error: Unity did not exit; killed after timeout.", file=sys.stderr)
    except OSError as exc:
        print(f"error: could not launch Unity: {exc}", file=sys.stderr)
        return 4
    finally:
        stop.set()
        tailer.join(timeout=5)
    print(f"Unity exited with {unity_code} after {time.time() - started:.0f}s")

    result = json.loads(RESULT_FILE.read_text(encoding="utf-8")) if RESULT_FILE.exists() else None
    if result is None:
        print("error: Unity wrote no result file. It probably failed before the runner started "
              "(compile errors, project already open, or license). See the log above.", file=sys.stderr)
        return unity_code or 3
    if unity_code != 0 or result.get("status") != "success":
        print(f"error: headless benchmark failed: {result.get('failure_reason')}", file=sys.stderr)
        for err in result.get("errors") or []:
            print(f"  {err}", file=sys.stderr)
        return unity_code or result.get("exit_code") or 1

    session_dir = Path(result["output_session_directory"])
    zip_path = zip_session(session_dir)
    if not args.keep_folder:
        shutil.rmtree(session_dir)
    print(f"Archived {result['csv_count']} runs to {zip_path.relative_to(REPO_ROOT)}")
    session_id = result["session_id"]

    if args.analyze:
        subprocess.run([sys.executable, str(REPO_ROOT / "Tools" / "analyze_airdata.py"), REAL_CSV,
                        "--session", session_id], cwd=REPO_ROOT, check=False)

    if args.no_check:
        return 0
    check = [sys.executable, str(REPO_ROOT / "Tools" / "check_acceptance.py"), str(zip_path), "--baseline", args.baseline]
    if args.fail_on_drift:
        check.append("--fail-on-drift")
    print()
    return subprocess.run(check, cwd=REPO_ROOT).returncode


if __name__ == "__main__":
    sys.exit(main())
