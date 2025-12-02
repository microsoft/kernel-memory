# Known Issues and Limitations

## Search Functionality

### 1. NOT Operator Doesn't Exclude Matches

**Status:** Known bug, not yet fixed

**Issue:** Queries like `"foo NOT bar"` should find documents containing "foo" but not "bar". Currently, it returns documents containing both.

**Example:**
```bash
km put "foo and bar together"
km put "only foo here"
km search "foo NOT bar"
# Expected: 1 result (only foo here)
# Actual: 2 results (both documents)
```

**Root Cause:**
- FTS query extraction passes `"NOT (bar)"` to SQLite FTS5
- SQLite FTS5's NOT operator support is limited/broken
- No LINQ post-filtering is applied to exclude NOT terms
- The architecture assumes FTS handles all logic, but NOT needs LINQ filtering

**Workaround:** None currently. Avoid using NOT operator.

**Fix Required:**
1. Split query: extract positive terms for FTS, negative terms for filtering
2. Apply LINQ filter to FTS results using QueryLinqBuilder
3. Filter out documents matching NOT terms

**Files Affected:**
- `src/Core/Search/NodeSearchService.cs:190` - ExtractLogical NOT handling
- Need to add LINQ filtering after line 89

---

### 2. Field Queries with Quoted Values Fail

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

## Resolved Issues

### Quoted Phrases Don't Escape Operators (Resolved)

**Status:** Fixed

**Issue:** Cannot search for literal phrases containing reserved words like "AND", "OR", "NOT".

**Example:**
```bash
km put "Meeting with Alice AND Bob"
km search '"Alice AND Bob"'
# Now works correctly and finds the document
```

**Resolution:**
- The tokenizer correctly handles quoted strings and preserves them as literal text
- The FTS query extractor properly quotes phrases containing reserved words
- E2E tests added in `SearchEndToEndTests.cs` to prevent regression (tests: `KnownIssue2_*`)

---

## Testing Gaps

These bugs were discovered through comprehensive E2E testing. Previous tests only verified:
- AST structure correctness
- LINQ expression building
- Direct FTS calls

But did NOT test:
- Full pipeline: Parse -> Extract FTS -> Search -> Filter -> Rank
- Default settings (MinRelevance=0.3)
- Actual result verification

**Lesson:** Exit code testing and structure testing are insufficient. Must test actual behavior with real data.

---

