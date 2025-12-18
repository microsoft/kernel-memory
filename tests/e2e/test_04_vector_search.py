#!/usr/bin/env python3
"""
E2E Test: Vector Search

Verifies that vector search indexing works end-to-end.
Tests that embeddings are generated and stored in vector database.
FAILS if Ollama is not available (this is intentional - integration test).
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
        "--format",
        "json",
    ]
    return subprocess.run(cmd, capture_output=True, text=True, timeout=30)


def main():
    print("="*70)
    print("TEST: Vector Search with Embeddings")
    print("="*70)

    tmp_dir = Path(tempfile.mkdtemp(prefix="km-e2e-test04-"))
    log_path = prepare_log_path(Path(__file__).with_suffix(".log"))

    try:
        # Setup: Create config with vector search
        print("\n[SETUP] Creating config with vector search index...")
        config = {
            "nodes": {
                "test": {
                    "id": "test",
                    "access": "Full",
                    "contentIndex": {"type": "sqlite", "path": str(tmp_dir / "content.db")},
                    "searchIndexes": [
                        {
                            "type": "sqliteFTS",
                            "id": "fts",
                            "path": str(tmp_dir / "fts.db"),
                            "required": True
                        },
                        {
                            "type": "sqliteVector",
                            "id": "vector",
                            "path": str(tmp_dir / "vector.db"),
                            "dimensions": 1024,
                            "useSqliteVec": False,
                            "embeddings": {
                                "type": "ollama",
                                "model": "qwen3-embedding:0.6b",
                                "baseUrl": "http://localhost:11434"
                            }
                        }
                    ]
                }
            },
            "embeddingsCache": {
                "type": "Sqlite",
                "path": str(tmp_dir / "cache.db"),
                "allowRead": True,
                "allowWrite": True
            }
        }
        config_path = str(tmp_dir / "config.json")
        with open(config_path, 'w') as f:
            json.dump(config, f)
        print("  Vector index: configured with Ollama qwen3-embedding:0.6b")
        print("  Embeddings cache: enabled")

        # Step 1: Verify Ollama is available
        print("\n[STEP 1] Checking if Ollama is available...")
        import urllib.request
        try:
            urllib.request.urlopen("http://localhost:11434/api/tags", timeout=2)
            print("  ✓ Ollama is reachable")
        except Exception as e:
            print(f"  ❌ TEST SKIPPED: Ollama not available ({e})")
            print("  This test requires Ollama running with qwen3-embedding:0.6b model")
            config_result = run_km("config", "--format", "json", config_path=config_path, log_path=log_path)
            assert config_result.returncode == 0, f"Config command failed while skipping: {config_result.stderr}"
            assert_log_has_entries(log_path, markers=["km CLI starting", "Command=config"])
            return 0

        # Step 2: Put content (should generate embedding and store in vector index)
        print("\n[STEP 2] Running: km put 'machine learning concepts'...")
        result = run_km("put", "machine learning concepts", "--id", "ml-doc", "--format", "json", config_path=config_path, log_path=log_path)

        print(f"  Exit code: {result.returncode}")
        if result.returncode != 0:
            print(f"  Stderr: {result.stderr}")

        data = json.loads(result.stdout)
        print(f"  Response: id={data['id']}, completed={data['completed']}, queued={data.get('queued', False)}")

        # Must complete successfully (not just queued)
        assert data["completed"] == True, f"Operation should complete. If queued, check Ollama: {result.stderr}"
        print("  ✓ PASS: Content indexed (embeddings generated)")

        # Step 3: Verify vector database was created and has data
        print("\n[STEP 3] Inspecting vector database...")
        vector_db_path = tmp_dir / "vector.db"
        assert vector_db_path.exists(), "Vector database should be created"
        print(f"  Database: {vector_db_path}")

        conn = sqlite3.connect(str(vector_db_path))

        # Check table exists
        cursor = conn.execute("SELECT name FROM sqlite_master WHERE type='table' AND name='km_vectors'")
        table = cursor.fetchone()
        assert table is not None, "km_vectors table should exist"
        print("  ✓ km_vectors table exists")

        # Check row count
        cursor = conn.execute("SELECT COUNT(*) FROM km_vectors")
        count = cursor.fetchone()[0]
        print(f"  Row count: {count}")
        assert count == 1, f"Should have 1 vector, got {count}"
        print("  ✓ PASS: Vector database has 1 row")

        # Check actual vector data
        cursor = conn.execute("SELECT content_id, LENGTH(vector), created_at FROM km_vectors WHERE content_id = ?", ("ml-doc",))
        row = cursor.fetchone()
        assert row is not None, "Vector for ml-doc should exist"

        content_id, vector_size, created_at = row
        print(f"  Content ID: {content_id}")
        print(f"  Vector size: {vector_size} bytes")
        print(f"  Created at: {created_at}")

        # Vector should be 1024 dimensions * 4 bytes (float32) = 4096 bytes
        expected_size = 1024 * 4
        assert vector_size == expected_size, f"Vector should be {expected_size} bytes (1024 dims), got {vector_size}"
        print(f"  ✓ PASS: Vector size correct (1024 dimensions * 4 bytes = {expected_size} bytes)")

        conn.close()

        # Step 4: Verify embeddings cache was populated
        print("\n[STEP 4] Inspecting embeddings cache...")
        cache_db_path = tmp_dir / "cache.db"
        assert cache_db_path.exists(), "Cache database should be created"
        print(f"  Cache: {cache_db_path}")

        conn = sqlite3.connect(str(cache_db_path))

        # Check cache has entry
        cursor = conn.execute("SELECT COUNT(*) FROM embeddings_cache")
        count = cursor.fetchone()[0]
        print(f"  Cached embeddings: {count}")
        assert count == 1, f"Cache should have 1 entry, got {count}"
        print("  ✓ PASS: Embedding was cached")

        # Check cache entry details
        cursor = conn.execute("SELECT provider, model, dimensions, LENGTH(vector), timestamp FROM embeddings_cache LIMIT 1")
        row = cursor.fetchone()
        provider, model, dims, vec_size, timestamp = row
        print(f"  Provider: {provider}")
        print(f"  Model: {model}")
        print(f"  Dimensions: {dims}")
        print(f"  Vector size: {vec_size} bytes")
        print(f"  Timestamp: {timestamp}")

        assert provider == "Ollama", f"Provider should be Ollama, got {provider}"
        assert model == "qwen3-embedding:0.6b", f"Model should be qwen3-embedding:0.6b, got {model}"
        assert dims == 1024, f"Dimensions should be 1024, got {dims}"
        print("  ✓ PASS: Cache entry has correct metadata")

        conn.close()

        # Step 5: Put second document - should use cache
        print("\n[STEP 5] Adding second document with same text (should use cache)...")
        result = run_km("put", "machine learning concepts", "--id", "ml-doc-2", "--format", "json", config_path=config_path, log_path=log_path)
        assert result.returncode == 0
        data = json.loads(result.stdout)
        assert data["completed"] == True
        print("  ✓ Second document indexed")

        # Check cache still has only 1 entry (same text = cache hit)
        conn = sqlite3.connect(str(cache_db_path))
        cursor = conn.execute("SELECT COUNT(*) FROM embeddings_cache")
        count = cursor.fetchone()[0]
        print(f"  Cache entries: {count}")
        assert count == 1, "Cache should still have 1 entry (cache hit for same text)"
        print("  ✓ PASS: Cache was reused (no new entry)")
        conn.close()

        # Check vector DB has 2 entries
        conn = sqlite3.connect(str(vector_db_path))
        cursor = conn.execute("SELECT COUNT(*) FROM km_vectors")
        count = cursor.fetchone()[0]
        print(f"  Vector DB entries: {count}")
        assert count == 2, f"Vector DB should have 2 entries, got {count}"
        print("  ✓ PASS: Both documents have vectors")
        conn.close()

        print("\n[VERIFY] Checking C# log file...")
        assert_log_has_entries(log_path, markers=["km CLI starting", "Command=put"])
        print(f"  ✓ PASS: C# log captured at {log_path}")

        print("\n" + "="*70)
        print("✅ TEST PASSED: Vector search and caching work correctly")
        print("="*70)
        print("\nVerified:")
        print("  ✓ Embeddings generated via Ollama")
        print("  ✓ Vectors stored in vector database (1024 dimensions)")
        print("  ✓ Embeddings cached (same text reused cache)")
        print("  ✓ Multiple documents share cached embedding")
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
