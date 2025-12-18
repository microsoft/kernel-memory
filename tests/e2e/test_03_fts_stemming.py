#!/usr/bin/env python3
"""
E2E Test: FTS Stemming

Verifies that FTS stemming is working correctly.
With stemming enabled, searching for "running" should find "run".
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
    print("TEST: FTS Stemming")
    print("="*70)

    tmp_dir = Path(tempfile.mkdtemp(prefix="km-e2e-test03-"))
    log_path = prepare_log_path(Path(__file__).with_suffix(".log"))

    try:
        # Setup: Create config with stemming enabled
        print("\n[SETUP] Creating config with FTS stemming enabled...")
        config = {
            "nodes": {
                "test": {
                    "id": "test",
                    "access": "Full",
                    "contentIndex": {"type": "sqlite", "path": str(tmp_dir / "content.db")},
                    "searchIndexes": [
                        {
                            "type": "sqliteFTS",
                            "id": "fts-stemmed",
                            "path": str(tmp_dir / "fts.db"),
                            "enableStemming": True,
                            "required": True
                        }
                    ]
                }
            }
        }
        config_path = str(tmp_dir / "config.json")
        with open(config_path, 'w') as f:
            json.dump(config, f)
        print("  FTS stemming: enabled")

        # Step 1: Put content with base word "test"
        print("\n[STEP 1] Adding content with word 'test'...")
        result = run_km("put", "We test the software thoroughly", "--id", "doc1", "--format", "json", config_path=config_path, log_path=log_path)
        assert result.returncode == 0, f"Put failed: {result.stderr}"
        print("  ✓ Content added: 'We test the software thoroughly'")

        # Step 2: Put content with variant word "testing"
        print("\n[STEP 2] Adding content with word 'testing'...")
        result = run_km("put", "Testing is important for quality", "--id", "doc2", "--format", "json", config_path=config_path, log_path=log_path)
        assert result.returncode == 0, f"Put failed: {result.stderr}"
        print("  ✓ Content added: 'Testing is important for quality'")

        # Step 3: Put content with variant word "tests"
        print("\n[STEP 3] Adding content with word 'tests'...")
        result = run_km("put", "All tests passed successfully", "--id", "doc3", "--format", "json", config_path=config_path, log_path=log_path)
        assert result.returncode == 0, f"Put failed: {result.stderr}"
        print("  ✓ Content added: 'All tests passed successfully'")

        # Step 4: Search for "testing" - should find all 3 due to stemming
        print("\n[STEP 4] Searching for 'testing' (should find all variants due to stemming)...")
        result = run_km("search", "testing", "--format", "json", config_path=config_path, log_path=log_path)
        assert result.returncode == 0, f"Search failed: {result.stderr}"

        data = json.loads(result.stdout)
        total_results = data.get("totalResults", 0)
        print(f"  Total results found: {total_results}")

        # With stemming: "testing" should match "test", "testing", "tests"
        assert total_results == 3, f"Stemming should find all 3 variants. Found: {total_results}"
        print("  ✓ PASS: Stemming found all 3 variants (test, testing, tests)")

        # Verify all 3 documents are in results
        result_ids = {r["id"] for r in data["results"]}
        print(f"  Result IDs: {result_ids}")
        assert "doc1" in result_ids, "Should find 'test'"
        assert "doc2" in result_ids, "Should find 'testing'"
        assert "doc3" in result_ids, "Should find 'tests'"
        print("  ✓ PASS: All expected documents found")

        # Step 5: Search for variant not in documents - verify stemming finds the stem
        print("\n[STEP 5] Searching for 'tested' (not in any document, stems to 'test')...")
        result = run_km("search", "tested", "--format", "json", config_path=config_path, log_path=log_path)
        assert result.returncode == 0

        data = json.loads(result.stdout)
        total_results = data.get("totalResults", 0)
        print(f"  Total results: {total_results}")

        # "tested" stems to "test", should find all 3 documents
        assert total_results == 3, f"Stemming 'tested' should find all 'test' variants. Found: {total_results}"
        print("  ✓ PASS: Stemming works for 'tested' → finds all 'test' variants")

        print("\n[VERIFY] Checking C# log file...")
        assert_log_has_entries(log_path, markers=["km CLI starting", "Command=put", "Command=search"])
        print(f"  ✓ PASS: C# log captured at {log_path}")

        print("\n" + "="*70)
        print("✅ TEST PASSED: FTS stemming works correctly")
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
