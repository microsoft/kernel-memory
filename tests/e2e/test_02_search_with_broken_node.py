#!/usr/bin/env python3
"""
E2E Test: Search with broken node

Regression test for bug found 2025-12-04:
km search was crashing when one node had missing database.
Should skip broken node and search working nodes.
"""
import subprocess
import json
import tempfile
import shutil
from pathlib import Path
from framework.cli import locate_km_binary
from framework.logging import assert_log_has_entries, prepare_log_path


def run_km(*args, config_path, log_path):
    """Execute km command and return result."""
    km_binary = locate_km_binary()
    cmd = ["dotnet", str(km_binary)] + list(args) + [
        "--config",
        config_path,
        "--log-file",
        str(log_path),
        "--verbosity",
        "verbose",
        "--format",
        "json",
    ]
    return subprocess.run(cmd, capture_output=True, text=True, timeout=30)


def main():
    print("="*70)
    print("TEST: Search with broken node (regression test)")
    print("="*70)

    tmp_dir = Path(tempfile.mkdtemp(prefix="km-e2e-test02-"))
    log_path = prepare_log_path(Path(__file__).with_suffix(".log"))

    try:
        # Setup: Config with 2 nodes
        print("\n[SETUP] Creating config with 2 nodes...")
        config = {
            "nodes": {
                "working": {
                    "id": "working",
                    "access": "Full",
                    "contentIndex": {"type": "sqlite", "path": str(tmp_dir / "working/content.db")},
                    "searchIndexes": [
                        {"type": "sqliteFTS", "id": "fts1", "path": str(tmp_dir / "working/fts.db"), "required": True}
                    ]
                },
                "broken": {
                    "id": "broken",
                    "access": "Full",
                    "contentIndex": {"type": "sqlite", "path": str(tmp_dir / "broken/content.db")},
                    "searchIndexes": [
                        {"type": "sqliteFTS", "id": "fts2", "path": str(tmp_dir / "broken/fts.db"), "required": True}
                    ]
                }
            }
        }
        config_path = str(tmp_dir / "config.json")
        with open(config_path, 'w') as f:
            json.dump(config, f)
        print(f"  Nodes: working, broken")

        # Step 1: Put content only to working node
        print("\n[STEP 1] Adding content to 'working' node only...")
        result = run_km("put", "searchable content", "--node", "working", config_path=config_path, log_path=log_path)
        assert result.returncode == 0, f"Put failed: {result.stderr}"
        print("  ✓ Content added to working node")
        print("  Note: 'broken' node has no database")

        # Step 2: Search across all nodes
        print("\n[STEP 2] Running: km search 'searchable' (searches all nodes)")
        result = run_km("search", "searchable", config_path=config_path, log_path=log_path)

        print(f"  Exit code: {result.returncode}")
        print(f"  Stderr: {result.stderr[:200] if result.stderr else '(empty)'}...")

        # Check: Should NOT crash
        assert result.returncode == 0, \
            f"Search should succeed even with broken node. Exit code: {result.returncode}, stderr: {result.stderr}"
        print("  ✓ PASS: Search did not crash")

        # Check: Should find content from working node
        assert "searchable content" in result.stdout, \
            f"Should find content from working node. Output: {result.stdout}"
        print("  ✓ PASS: Found content from working node")

        # Check: Should log warning about broken node
        has_warning = "broken" in result.stderr.lower() or "skipping" in result.stderr.lower()
        assert has_warning, f"Should warn about skipping broken node. stderr: {result.stderr}"
        print("  ✓ PASS: Warning logged for broken node")

        print("\n[VERIFY] Checking C# log file...")
        assert_log_has_entries(log_path, markers=["km CLI starting", "Command=put", "Command=search"])
        print(f"  ✓ PASS: C# log captured at {log_path}")

        print("\n" + "="*70)
        print("✅ TEST PASSED: Search handles broken nodes gracefully")
        print("="*70)
        return 0

    except AssertionError as e:
        print(f"\n❌ TEST FAILED: {e}")
        return 1
    except Exception as e:
        print(f"\n❌ TEST ERROR: {e}")
        import traceback
        traceback.print_exc()
        return 1
    finally:
        shutil.rmtree(tmp_dir)


if __name__ == "__main__":
    exit(main())
