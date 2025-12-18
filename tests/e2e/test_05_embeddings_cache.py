#!/usr/bin/env python3
"""
E2E Test: Embeddings Cache

Verifies that embeddings are cached and reused.
Tests cache hit/miss scenarios and verifies timestamp and token_count are stored.
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
    print("TEST: Embeddings Cache")
    print("="*70)

    tmp_dir = Path(tempfile.mkdtemp(prefix="km-e2e-test05-"))
    log_path = prepare_log_path(Path(__file__).with_suffix(".log"))

    try:
        # Setup
        print("\n[SETUP] Creating config with vector search and cache...")
        config = {
            "nodes": {
                "test": {
                    "id": "test",
                    "access": "Full",
                    "contentIndex": {"type": "sqlite", "path": str(tmp_dir / "content.db")},
                    "searchIndexes": [
                        {
                            "type": "sqliteVector",
                            "id": "vector",
                            "path": str(tmp_dir / "vector.db"),
                            "dimensions": 1024,
                            "embeddings": {
                                "type": "ollama",
                                "model": "qwen3-embedding:0.6b"
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
        print("  Cache enabled: ReadWrite mode")

        # Check Ollama
        print("\n[PREREQ] Checking Ollama availability...")
        import urllib.request
        try:
            urllib.request.urlopen("http://localhost:11434/api/tags", timeout=2)
            print("  ✓ Ollama reachable")
        except Exception as e:
            print(f"  ❌ TEST SKIPPED: Ollama not available")
            config_result = run_km("config", "--format", "json", config_path=config_path, log_path=log_path)
            assert config_result.returncode == 0, f"Config command failed while skipping: {config_result.stderr}"
            assert_log_has_entries(log_path, markers=["km CLI starting", "Command=config"])
            return 0

        # Step 1: Put first document (cache miss)
        print("\n[STEP 1] Adding first document (cache miss)...")
        result = run_km("put", "artificial intelligence", "--id", "doc1", "--format", "json", config_path=config_path, log_path=log_path)
        assert result.returncode == 0
        data = json.loads(result.stdout)
        assert data["completed"] == True, "Should complete"
        print("  ✓ Document indexed")

        # Verify cache has 1 entry
        cache_db = tmp_dir / "cache.db"
        assert cache_db.exists(), "Cache database should exist"

        conn = sqlite3.connect(str(cache_db))
        cursor = conn.execute("SELECT COUNT(*) FROM embeddings_cache")
        count = cursor.fetchone()[0]
        print(f"  Cache entries: {count}")
        assert count == 1, "Cache should have 1 entry"
        print("  ✓ PASS: Embedding cached (cache miss → cached)")

        # Check cache entry has timestamp and token_count
        cursor = conn.execute("SELECT provider, model, dimensions, timestamp, token_count FROM embeddings_cache LIMIT 1")
        row = cursor.fetchone()
        provider, model, dims, timestamp, token_count = row
        print(f"  Cached: provider={provider}, model={model}, dims={dims}")
        print(f"  Timestamp: {timestamp}")
        print(f"  Token count: {token_count}")

        assert timestamp is not None, "Timestamp should be stored"
        assert len(timestamp) > 0, "Timestamp should not be empty"
        print("  ✓ PASS: Timestamp stored")

        # Token count may be None (Ollama doesn't provide it)
        if token_count is None:
            print("  ℹ Token count: None (Ollama doesn't provide token count)")
        else:
            print(f"  ✓ Token count: {token_count}")

        conn.close()

        # Step 2: Put same text again (cache hit)
        print("\n[STEP 2] Adding document with same text (cache hit)...")
        result = run_km("put", "artificial intelligence", "--id", "doc2", "--format", "json", config_path=config_path, log_path=log_path)
        assert result.returncode == 0
        data = json.loads(result.stdout)
        assert data["completed"] == True
        print("  ✓ Document indexed")

        # Cache should still have 1 entry (cache hit, no new embedding generated)
        conn = sqlite3.connect(str(cache_db))
        cursor = conn.execute("SELECT COUNT(*) FROM embeddings_cache")
        count = cursor.fetchone()[0]
        print(f"  Cache entries: {count}")
        assert count == 1, "Cache should still have 1 entry (cache hit)"
        print("  ✓ PASS: Cache reused (no new entry)")
        conn.close()

        # Step 3: Put different text (cache miss)
        print("\n[STEP 3] Adding document with different text (cache miss)...")
        result = run_km("put", "deep learning neural networks", "--id", "doc3", "--format", "json", config_path=config_path, log_path=log_path)
        assert result.returncode == 0
        data = json.loads(result.stdout)
        assert data["completed"] == True
        print("  ✓ Document indexed")

        # Cache should now have 2 entries
        conn = sqlite3.connect(str(cache_db))
        cursor = conn.execute("SELECT COUNT(*) FROM embeddings_cache")
        count = cursor.fetchone()[0]
        print(f"  Cache entries: {count}")
        assert count == 2, f"Cache should have 2 entries, got {count}"
        print("  ✓ PASS: New embedding cached")
        conn.close()

        # Step 4: Verify vector database has 3 entries
        print("\n[STEP 4] Verifying vector database...")
        vector_db = tmp_dir / "vector.db"
        conn = sqlite3.connect(str(vector_db))
        cursor = conn.execute("SELECT COUNT(*) FROM km_vectors")
        count = cursor.fetchone()[0]
        print(f"  Vector DB entries: {count}")
        assert count == 3, f"Should have 3 vectors, got {count}"
        print("  ✓ PASS: All 3 documents have vectors")

        # Verify each vector is correct size (1024 dims * 4 bytes = 4096)
        cursor = conn.execute("SELECT content_id, LENGTH(vector) FROM km_vectors ORDER BY content_id")
        for row in cursor.fetchall():
            cid, size = row
            print(f"  {cid}: {size} bytes")
            assert size == 4096, f"Vector should be 4096 bytes, got {size}"

        print("  ✓ PASS: All vectors are correct size (1024 dimensions)")
        conn.close()

        print("\n[VERIFY] Checking C# log file...")
        assert_log_has_entries(log_path, markers=["km CLI starting", "Command=put"])
        print(f"  ✓ PASS: C# log captured at {log_path}")

        print("\n" + "="*70)
        print("✅ TEST PASSED: Embeddings cache works correctly")
        print("="*70)
        print("\nVerified:")
        print("  ✓ Cache miss → embedding generated and cached")
        print("  ✓ Cache hit → embedding reused")
        print("  ✓ Timestamp stored in cache")
        print("  ✓ Multiple documents share cached embeddings when text matches")
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
