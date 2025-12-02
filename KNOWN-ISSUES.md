# Known Issues and Limitations

## Search Functionality

### 1. Field Queries with Quoted Values Fail

**Status:** Known bug, not yet fixed

**Issue:** Field-specific queries with quoted values containing special characters fail.

**Example:**
```bash
km put "user:password format"
km search 'content:"user:password"'
# Expected: Find the document
# Actual: SQLite error "unknown special query"
```

**Root Cause:**
- Quoted values after field prefix (`content:"..."`) generate invalid FTS queries
- FTS syntax may not support this pattern
- Need investigation of FTS query generation

**Workaround:** Search without field prefix or without quotes.

---

## Notes

### Query Syntax

The infix query parser requires explicit AND between terms:
- `foo AND NOT bar` (correct) instead of `foo NOT bar` (incorrect - ignores NOT)
- `(foo OR baz) AND NOT bar` (correct) instead of `(foo OR baz) NOT bar` (incorrect)
