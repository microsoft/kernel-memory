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

## Resolved Issues

### NOT Operator Issues (Resolved)

**Status:** Fixed

**Issue:** The NOT operator had two problems:
1. **Standalone NOT crashed:** `km search "NOT foo"` threw FTS5 syntax error
2. **NOT didn't exclude:** `km search "foo AND NOT bar"` returned documents containing both instead of excluding "bar"

**Resolution:**
- Implemented `FtsQueryResult` record to separate FTS query string from NOT terms
- Modified `FtsQueryExtractor` to collect NOT terms separately instead of passing them to FTS5
- Added LINQ post-filtering in `NodeSearchService.SearchAsync()` to exclude NOT terms
- Added `GetAllDocumentsAsync()` in `SqliteFtsIndex` to handle standalone NOT queries
- Case-insensitive filtering checks title, description, and content fields
- E2E tests added in `SearchEndToEndTests.cs` (tests: `KnownIssue1_*`)

**Important Note:** The infix query parser requires explicit AND between terms. Use:
- `foo AND NOT bar` (correct) instead of `foo NOT bar` (incorrect - ignores NOT)
- `(foo OR baz) AND NOT bar` (correct) instead of `(foo OR baz) NOT bar` (incorrect)

**Files Changed:**
- `src/Core/Search/NodeSearchService.cs` - Added `FtsQueryResult`, `NotTerm` records and LINQ filtering
- `src/Core/Search/SqliteFtsIndex.cs` - Added `GetAllDocumentsAsync()` for standalone NOT support

---

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

