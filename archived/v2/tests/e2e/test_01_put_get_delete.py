#!/usr/bin/env python3
"""
E2E Test: Basic CRUD workflow (put → get → delete)

Tests the most fundamental user workflow.
Verifies database state at each step.
"""
import subprocess
import json
import sqlite3
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
    ]
    result = subprocess.run(cmd, capture_output=True, text=True, timeout=30)
    return result


def main():
    print("="*70)
    print("TEST: Basic CRUD workflow (put → get → delete)")
    print("="*70)

    tmp_dir = Path(tempfile.mkdtemp(prefix="km-e2e-test01-"))
    log_path = prepare_log_path(Path(__file__).with_suffix(".log"))

    try:
        # Setup: Create config
        print("\n[SETUP] Creating test config...")
        config = {
            "nodes": {
                "test": {
                    "id": "test",
                    "access": "Full",
                    "contentIndex": {"type": "sqlite", "path": str(tmp_dir / "content.db")},
                    "searchIndexes": [
                        {"type": "sqliteFTS", "id": "fts", "path": str(tmp_dir / "fts.db"), "required": True}
                    ],
                }
            }
        }
        config_path = str(tmp_dir / "config.json")
        with open(config_path, 'w') as f:
            json.dump(config, f)
        print(f"  Config: {config_path}")

        # Step 1: Put content
        print("\n[STEP 1] Running: km put 'Hello world' --id test-1")
        result = run_km("put", "Hello world", "--id", "test-1", "--format", "json", config_path=config_path, log_path=log_path)

        print(f"  Exit code: {result.returncode}")
        assert result.returncode == 0, f"Put failed: {result.stderr}"

        data = json.loads(result.stdout)
        print(f"  Response: id={data['id']}, completed={data['completed']}")
        assert data["id"] == "test-1", "ID should be test-1"
        assert data["completed"] == True, "Should complete immediately"
        print("  ✓ PASS: Content created")

        # Step 2: Verify database
        print("\n[STEP 2] Checking database state...")
        db_path = tmp_dir / "content.db"
        print(f"  Database: {db_path}")
        assert db_path.exists(), "Database file should exist"
        print("  ✓ Database file exists")

        conn = sqlite3.connect(str(db_path))
        cursor = conn.execute("SELECT COUNT(*) FROM km_content")
        count = cursor.fetchone()[0]
        print(f"  Row count in km_content: {count}")
        assert count == 1, f"Expected 1 row, got {count}"
        print("  ✓ PASS: Database has 1 row")

        cursor = conn.execute("SELECT Id, Content FROM km_content WHERE Id = ?", ("test-1",))
        row = cursor.fetchone()
        assert row, "Content row should exist"
        print(f"  Content: '{row[1]}'")
        assert "Hello world" in row[1], "Content should contain 'Hello world'"
        print("  ✓ PASS: Content matches")
        conn.close()

        # Step 3: Get content
        print("\n[STEP 3] Running: km get test-1")
        result = run_km("get", "test-1", "--format", "json", config_path=config_path, log_path=log_path)

        print(f"  Exit code: {result.returncode}")
        assert result.returncode == 0, f"Get failed: {result.stderr}"

        data = json.loads(result.stdout)
        print(f"  Retrieved content: '{data['content'][:50]}'")
        assert "Hello world" in data["content"], "Should retrieve correct content"
        print("  ✓ PASS: Get succeeded")

        # Step 4: Delete content
        print("\n[STEP 4] Running: km delete test-1")
        result = run_km("delete", "test-1", "--format", "json", config_path=config_path, log_path=log_path)

        print(f"  Exit code: {result.returncode}")
        assert result.returncode == 0, f"Delete failed: {result.stderr}"
        print("  ✓ PASS: Delete succeeded")

        # Step 5: Verify deletion
        print("\n[STEP 5] Verifying content deleted from database...")
        conn = sqlite3.connect(str(db_path))
        cursor = conn.execute("SELECT COUNT(*) FROM km_content")
        count = cursor.fetchone()[0]
        print(f"  Row count after delete: {count}")
        assert count == 0, "Content should be deleted"
        print("  ✓ PASS: Content deleted from database")
        conn.close()

        print("\n[VERIFY] Checking C# log file...")
        assert_log_has_entries(log_path, markers=["km CLI starting", "Command=put", "Command=get", "Command=delete"])
        print(f"  ✓ PASS: C# log captured at {log_path}")

        print("\n" + "="*70)
        print("✅ TEST PASSED: All steps completed successfully")
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
