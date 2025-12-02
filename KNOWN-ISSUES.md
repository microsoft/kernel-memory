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

### 2. Quoted Phrases Don't Escape Operators

**Status:** Known bug, not yet fixed

**Issue:** Cannot search for literal phrases containing reserved words like "AND", "OR", "NOT".

**Example:**
```bash
km put "Meeting with Alice AND Bob"
km search '"Alice AND Bob"'
# Expected: Find the document
# Actual: Parser error or incorrect results
```

**Root Cause:**
- Quoted strings should treat content literally
- Current parser/tokenizer doesn't properly handle operator escaping within quotes
- May be FTS query generation issue

**Workaround:** Rephrase searches to avoid reserved words.

**Fix Required:** Investigate tokenizer and FTS query extraction for quoted phrases.

---

### 3. Field Queries with Quoted Values Fail

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

### 4. Reserved Words Cannot Be Searched

**Status:** Known limitation

**Issue:** Cannot search for the literal words "AND", "OR", "NOT" even with quotes.

**Example:**
```bash
km put "this is NOT important"
km search "NOT"
# Expected: Find the document
# Actual: Parser error "Unexpected end of query"
```

**Root Cause:**
- Tokenizer treats AND/OR/NOT as reserved keywords (case-insensitive)
- Even quoted, they're tokenized as operators
- Parser expects operands after NOT

**Workaround:** None. These words cannot be searched.

**Fix Required:**
- Tokenizer must recognize quotes and treat content literally
- Major parser refactoring needed

---

## Testing Gaps

These bugs were discovered through comprehensive E2E testing. Previous tests only verified:
- ✅ AST structure correctness
- ✅ LINQ expression building
- ✅ Direct FTS calls

But did NOT test:
- ❌ Full pipeline: Parse → Extract FTS → Search → Filter → Rank
- ❌ Default settings (MinRelevance=0.3)
- ❌ Actual result verification

**Lesson:** Exit code testing and structure testing are insufficient. Must test actual behavior with real data.

---

## Resolved Issues

### BM25 Score Normalization (FIXED)
- **Issue:** All searches returned 0 results despite FTS finding matches
- **Cause:** BM25 scores (~0.000001) filtered by MinRelevance=0.3
- **Fix:** Exponential normalization maps [-10, 0] → [0.37, 1.0]
- **Commit:** 4cb283e

### Field-Specific Equal Operator (FIXED)
- **Issue:** `content:summaries` failed with SQLite error
- **Cause:** Equal operator didn't extract FTS queries
- **Fix:** ExtractComparison now handles both Contains and Equal
- **Commit:** 59bf3f2
