"""SQLite database inspection utilities."""
import sqlite3
from pathlib import Path
from typing import Optional, List, Dict, Any


class SqliteDb:
    """Wrapper for inspecting SQLite databases."""

    def __init__(self, db_path: Path):
        self.db_path = db_path
        self.conn: Optional[sqlite3.Connection] = None

    def __enter__(self):
        self.conn = sqlite3.connect(str(self.db_path))
        self.conn.row_factory = sqlite3.Row
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        if self.conn:
            self.conn.close()

    def count_rows(self, table: str) -> int:
        """Count rows in a table."""
        cursor = self.conn.execute(f"SELECT COUNT(*) FROM {table}")
        return cursor.fetchone()[0]

    def get_row(self, table: str, id_value: str) -> Optional[Dict[str, Any]]:
        """Get a single row by ID."""
        cursor = self.conn.execute(f"SELECT * FROM {table} WHERE Id = ?", (id_value,))
        row = cursor.fetchone()
        return dict(row) if row else None

    def has_table(self, table_name: str) -> bool:
        """Check if table exists."""
        cursor = self.conn.execute(
            "SELECT name FROM sqlite_master WHERE type='table' AND name=?",
            (table_name,)
        )
        return cursor.fetchone() is not None

    def get_columns(self, table_name: str) -> List[str]:
        """Get list of column names in a table."""
        cursor = self.conn.execute(f"PRAGMA table_info({table_name})")
        return [row[1] for row in cursor.fetchall()]

    def has_column(self, table: str, column: str) -> bool:
        """Check if table has a specific column."""
        return column in self.get_columns(table)
