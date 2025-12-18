"""Helpers for per-test C# log files."""
from __future__ import annotations

from pathlib import Path


def prepare_log_path(log_path: Path) -> Path:
    """
    Prepare a dedicated log file path for a test (in the same folder as the test).

    - Ensures the directory exists.
    - Removes any previous log files for this test (including rolled files).
    - Returns the full path to the log file to place in the C# config/CLI options.
    """
    log_path = log_path.resolve()
    log_path.parent.mkdir(parents=True, exist_ok=True)

    pattern = f"{log_path.stem}*.log"
    for file in log_path.parent.glob(pattern):
        file.unlink(missing_ok=True)

    return log_path


def assert_log_has_entries(log_path: Path, markers: list[str] | None = None) -> None:
    """
    Verify the C# log file exists, is non-empty, and contains expected markers.

    markers are short substrings expected to come from the C# logging output,
    e.g., "km CLI starting" or "Command=put".
    """
    assert log_path.exists(), f"Expected log file at {log_path}"
    assert log_path.stat().st_size > 0, f"Log file {log_path} should not be empty"

    if markers:
        content = log_path.read_text(encoding="utf-8", errors="ignore")
        for marker in markers:
            assert marker in content, f"Expected log marker '{marker}' in {log_path}"
