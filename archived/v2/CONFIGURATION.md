# 📘 Kernel Memory Configuration Guide

**A complete guide to configuring Kernel Memory**

---

## Table of Contents

- [🎯 Quick Start](#-quick-start)
- [📂 Configuration File](#-configuration-file)
- [🏗️ Nodes](#️-nodes)
- [🔍 Search Configuration](#-search-configuration)
- [📊 Search Indexes](#-search-indexes)
- [🎨 Complete Examples](#-complete-examples)
- [🔧 Troubleshooting](#-troubleshooting)

---

## 🎯 Quick Start

### Default Configuration

When you first run `km`, it creates a default configuration at `~/.km/config.json`:

```json
{
  "nodes": {
    "personal": {
      "id": "personal",
      "access": "Full",
      "contentIndex": {
        "type": "sqlite",
        "path": "~/.km/nodes/personal/content.db"
      },
      "searchIndexes": [
        {
          "type": "sqliteFTS",
          "id": "sqlite-fts",
          "path": "~/.km/nodes/personal/fts.db",
          "enableStemming": true
        }
      ]
    }
  }
}
```

This gives you:
- ✅ One node called "personal"
- ✅ Local SQLite storage
- ✅ Full-text search with stemming
- ✅ Ready to use immediately

### View Configuration

```bash
# View current configuration
km config

# View node details
km config --show-nodes

# Use custom configuration path
km --config /path/to/config.json search "query"
```

---

## 📂 Configuration File

### Location

**Default**: `~/.km/config.json`  
**Custom**: Use `--config <path>` or `-c <path>` flag

### Top-Level Structure

```json
{
  "nodes": { ... },    // Memory nodes (REQUIRED)
  "search": { ... }    // Global search settings (optional)
}
```

### Environment Variables

Use environment variables in configuration:

```json
{
  "nodes": {
    "work": {
      "contentIndex": {
        "type": "sqlite",
        "path": "${HOME}/.km/work/content.db"
      }
    }
  }
}
```

Then set: `export HOME=/custom/path`

---

## 🏗️ Nodes

**Nodes** are independent memory spaces. Think of them as separate notebooks or collections.

### Why Use Multiple Nodes?

- 📁 **Organization**: Separate personal, work, and project content
- 🔒 **Access control**: Different permission levels per node
- ⚖️ **Search weighting**: Prioritize some nodes over others

### Node Structure

```json
{
  "nodes": {
    "personal": {
      "id": "personal",           // Unique identifier
      "access": "Full",           // Full, ReadOnly, or WriteOnly
      "weight": 1.0,              // Search ranking weight (default: 1.0)
      "contentIndex": { ... },    // REQUIRED: Metadata storage
      "searchIndexes": [ ... ]    // Optional: Search indexes
    }
  }
}
```

### Node Properties

#### `id` (string, required)
Unique node identifier (lowercase, no spaces recommended)

```json
"id": "personal"
"id": "work"
"id": "archive"
```

#### `access` (string, default: `"Full"`)
Access level for this node:
- `"Full"` - Read and write allowed
- `"ReadOnly"` - Only searches and reads
- `"WriteOnly"` - Only writes (no search)

```json
"access": "Full"        // Most common
"access": "ReadOnly"    // For archived content
```

#### `weight` (number, default: `1.0`)
Search ranking multiplier. Higher values = more important results.

```json
"weight": 1.0    // Standard weight
"weight": 1.5    // 50% boost
"weight": 0.5    // Half importance (archives)
"weight": 0.2    // Low priority (temp files)
```

**Example**: Same relevance match with weight 1.5 ranks 50% higher than weight 1.0.

### Content Index (Required)

Every node needs a content index - the "source of truth" for metadata.

#### SQLite Content Index

```json
"contentIndex": {
  "type": "sqlite",
  "path": "~/.km/nodes/personal/content.db"
}
```

**When to use**: Local storage, single user, desktop apps

**Features**:
- Fast local access
- No external dependencies
- Automatic schema management
- Transaction support

---

## 🔍 Search Configuration

Global search settings that apply to all search operations.

### Complete Search Configuration

```json
{
  "search": {
    // Result Defaults
    "defaultLimit": 20,
    "defaultMinRelevance": 0.3,
    
    // Performance & Safety
    "searchTimeoutSeconds": 30,
    "maxResultsPerNode": 1000,
    
    // Node Selection
    "defaultNodes": ["*"],
    "excludeNodes": [],
    
    // Security Limits
    "maxQueryDepth": 10,
    "maxBooleanOperators": 50,
    "maxFieldValueLength": 1000,
    "queryParseTimeoutMs": 1000,
    
    // Highlighting
    "highlightPrefix": "<mark>",
    "highlightSuffix": "</mark>",
    
    // Snippets
    "snippetLength": 200,
    "maxSnippetsPerResult": 1,
    "snippetSeparator": "..."
  }
}
```

### Key Properties Explained

#### Result Control

| Property | Default | Description |
|----------|---------|-------------|
| `defaultLimit` | `20` | Max results per search |
| `defaultMinRelevance` | `0.3` | Minimum score (0.0-1.0) |
| `maxResultsPerNode` | `1000` | Memory safety limit per node |

```json
"defaultLimit": 50              // More results
"defaultMinRelevance": 0.5      // Higher quality threshold
```

#### Node Selection

```json
// Search all nodes by default
"defaultNodes": ["*"]

// Search only specific nodes
"defaultNodes": ["personal", "work"]

// Search all except archives
"defaultNodes": ["*"],
"excludeNodes": ["archive", "temp"]
```

#### Performance Tuning

```json
// Fast searches (5s timeout, 100 results max)
"searchTimeoutSeconds": 5,
"maxResultsPerNode": 100

// Comprehensive searches (60s timeout, 2000 results)
"searchTimeoutSeconds": 60,
"maxResultsPerNode": 2000
```

#### Highlighting & Snippets

```json
// HTML highlighting
"highlightPrefix": "<mark>",
"highlightSuffix": "</mark>"

// Markdown highlighting
"highlightPrefix": "**",
"highlightSuffix": "**"

// Custom markers
"highlightPrefix": "[MATCH]",
"highlightSuffix": "[/MATCH]"

// Snippet settings
"snippetLength": 300,           // Longer snippets
"maxSnippetsPerResult": 3       // Show multiple match contexts
```

---

## 📊 Search Indexes

Configure full-text search for your nodes.

### SQLite Full-Text Search (FTS)

**Best for**: Keyword matching, exact phrases, boolean queries

```json
{
  "type": "sqliteFTS",
  "id": "fts-main",
  "path": "~/.km/nodes/personal/fts.db",
  "enableStemming": true,
  "weight": 1.0,
  "required": false
}
```

**Properties**:
- `enableStemming`: Match "running" when searching "run" (recommended: `true`)
- `weight`: Importance in search ranking (default: 1.0)
- `required`: Fail search if unavailable (use `true` for primary indexes)

**Features**:
- Field-specific search (`title:query`, `content:query`, `tags:query`)
- Boolean operators (AND, OR, NOT)
- Phrase search (`"exact phrase"`)
- Wildcard search (`run*` matches running, runner)
- Highlighted matches and snippets

### ⚠️ Impact of Configuration Changes

**Changing settings affects NEW data only**, not existing indexed data.

#### Changing `enableStemming`

```json
// Before: enableStemming = false
// After:  enableStemming = true
```

**Impact**:
- ✅ New content will be indexed with stemming
- ❌ Existing content remains indexed WITHOUT stemming
- **Result**: Inconsistent search behavior (some records match "run" → "running", others don't)

**Solution**: Delete and recreate the FTS database after changing stemming:
```bash
# 1. Backup your content index (source of truth)
cp ~/.km/nodes/personal/content.db ~/.km/nodes/personal/content.db.backup

# 2. Delete FTS index
rm ~/.km/nodes/personal/fts.db

# 3. Restart km - FTS index will rebuild from content index
km list  # Triggers rebuild
```

#### Changing `weight`

```json
// Before: weight = 0.7
// After:  weight = 1.0
```

**Impact**:
- ✅ Takes effect immediately (no rebuild needed)
- Applied during search, not during indexing
- All searches will use new weight

#### Changing `path`

```json
// Before: path = "~/.km/nodes/personal/fts.db"
// After:  path = "~/.km/nodes/personal/fts-new.db"
```

**Impact**:
- Creates a new empty FTS index at the new path
- Old index is NOT deleted automatically
- New index will be built as content is added

**Solution**: If you want to keep existing index data, manually move/rename the file:
```bash
mv ~/.km/nodes/personal/fts.db ~/.km/nodes/personal/fts-new.db
```

#### Adding/Removing Indexes

**Impact**:
- New indexes start empty and build as content is added/updated
- Removed indexes are NOT deleted from disk (manual cleanup needed)
- Search continues with remaining indexes

**Best Practice**: When adding a new search index to an existing node with content, the index starts empty. To populate it, you can either:
1. Wait for natural updates (index builds incrementally)
2. Force reindexing by updating all content (not yet implemented)

### Multiple FTS Indexes

You can configure multiple FTS indexes per node for different purposes:

```json
"searchIndexes": [
  {
    "type": "sqliteFTS",
    "id": "fts-current",
    "path": "~/.km/nodes/work/fts-current.db",
    "enableStemming": true,
    "weight": 0.7,
    "required": true
  },
  {
    "type": "sqliteFTS",
    "id": "fts-archive",
    "path": "~/.km/nodes/work/fts-archive.db",
    "enableStemming": false,
    "weight": 0.3,
    "required": false
  }
]
```

**Use Cases**:
- Separate current vs archived content
- Different stemming configurations
- Incremental indexing (new index while old one rebuilds)

---

## 🎨 Complete Examples

### Example 1: Simple Personal Setup

```json
{
  "nodes": {
    "personal": {
      "id": "personal",
      "access": "Full",
      "contentIndex": {
        "type": "sqlite",
        "path": "~/.km/nodes/personal/content.db"
      },
      "searchIndexes": [
        {
          "type": "sqliteFTS",
          "id": "fts-main",
          "path": "~/.km/nodes/personal/fts.db",
          "enableStemming": true
        }
      ]
    }
  }
}
```

**Use case**: Single user, desktop app, no external dependencies

---

### Example 2: Multi-Node with Weights

```json
{
  "nodes": {
    "personal": {
      "id": "personal",
      "weight": 1.0,
      "contentIndex": {
        "type": "sqlite",
        "path": "~/.km/nodes/personal/content.db"
      },
      "searchIndexes": [
        {
          "type": "sqliteFTS",
          "id": "fts-main",
          "path": "~/.km/nodes/personal/fts.db",
          "enableStemming": true,
          "weight": 1.0
        }
      ]
    },
    "work": {
      "id": "work",
      "weight": 0.9,
      "contentIndex": {
        "type": "sqlite",
        "path": "~/.km/nodes/work/content.db"
      },
      "searchIndexes": [
        {
          "type": "sqliteFTS",
          "id": "fts-main",
          "path": "~/.km/nodes/work/fts.db",
          "enableStemming": true,
          "weight": 1.0
        }
      ]
    },
    "archive": {
      "id": "archive",
      "access": "ReadOnly",
      "weight": 0.3,
      "contentIndex": {
        "type": "sqlite",
        "path": "~/.km/nodes/archive/content.db"
      },
      "searchIndexes": [
        {
          "type": "sqliteFTS",
          "id": "fts-main",
          "path": "~/.km/nodes/archive/fts.db",
          "enableStemming": false,
          "weight": 1.0
        }
      ]
    }
  },
  "search": {
    "defaultNodes": ["*"],
    "excludeNodes": ["archive"]
  }
}
```

**Use case**: Separate personal, work, and archive collections with prioritization

---

### Example 3: Multiple FTS Indexes

```json
{
  "nodes": {
    "personal": {
      "id": "personal",
      "contentIndex": {
        "type": "sqlite",
        "path": "~/.km/nodes/personal/content.db"
      },
      "searchIndexes": [
        {
          "type": "sqliteFTS",
          "id": "fts-current",
          "path": "~/.km/nodes/personal/fts-current.db",
          "enableStemming": true,
          "weight": 0.7,
          "required": true
        },
        {
          "type": "sqliteFTS",
          "id": "fts-archive",
          "path": "~/.km/nodes/personal/fts-archive.db",
          "enableStemming": false,
          "weight": 0.3,
          "required": false
        }
      ]
    }
  }
}
```

**Use case**: Separate current and archived content with different search configurations

---

### Example 4: Performance Optimized

```json
{
  "nodes": {
    "personal": {
      "id": "personal",
      "contentIndex": {
        "type": "sqlite",
        "path": "~/.km/nodes/personal/content.db"
      },
      "searchIndexes": [
        {
          "type": "sqliteFTS",
          "id": "fts-main",
          "path": "~/.km/nodes/personal/fts.db",
          "enableStemming": true,
          "weight": 1.0,
          "required": true
        }
      ]
    }
  },
  "search": {
    "defaultLimit": 10,
    "defaultMinRelevance": 0.5,
    "searchTimeoutSeconds": 5,
    "maxResultsPerNode": 100,
    "snippetLength": 150,
    "maxSnippetsPerResult": 1
  }
}
```

**Use case**: Fast interactive searches, optimized for speed

---

## 🔧 Troubleshooting

### Configuration Not Loading

**Problem**: `km` not using your config file

**Solutions**:
```bash
# Check config location
km config

# Use explicit path
km --config ~/.km/config.json search "query"
```

---

### Search Returns No Results

**Check**:
1. **Indexed content?**
   ```bash
   km list  # Should show your content
   ```

2. **Search in correct nodes?**
   ```bash
   km nodes  # See available nodes
   km search "query" --nodes personal  # Specify explicitly
   ```

3. **Min relevance too high?**
   ```bash
   km search "query" --min-relevance 0.0  # Try minimum threshold
   ```

4. **Index ready?**
   - Check for warnings in search output
   - Indexes build automatically on first upsert

---

### Performance Issues

**Slow searches**:
```json
"search": {
  "searchTimeoutSeconds": 5,
  "maxResultsPerNode": 100,
  "defaultNodes": ["personal"]  // Search fewer nodes
}
```

**High memory usage**:
```json
"search": {
  "maxResultsPerNode": 500  // Reduce from default 1000
}
```

---

### Path Resolution

**Tilde (~) expansion**:
- ✅ Supported: `"path": "~/.km/nodes/personal/content.db"`
- ❌ Not supported: Shell aliases, complex expressions

**Relative paths**:
- Relative to config file location
- Absolute paths recommended for clarity

---

## CLI Overrides

Most configuration settings can be overridden via command-line flags:

```bash
# Override result limits
km search "query" --limit 50 --min-relevance 0.5

# Override node selection
km search "query" --nodes personal,work

# Override timeout
km search "query" --timeout 60

# Override output format
km search "query" --format json

# Multiple overrides
km search "query" \
  --nodes personal \
  --limit 10 \
  --min-relevance 0.4 \
  --snippet \
  --highlight
```

---

## Score Calculation

Final relevance scores are calculated using weighted scoring and diminishing returns.

### Weighted Scoring

Each index result gets weighted:

```
weighted_score = base_relevance × index.weight × node.weight
```

**Example**:
- FTS index returns base_relevance = 0.8 (80% match)
- index.weight = 0.7 (configured for this index)
- node.weight = 1.0 (configured for this node)
- Result: 0.8 × 0.7 × 1.0 = 0.56

### Diminishing Returns (Multiple Indexes)

When the same record appears in multiple indexes:

```
1. Collect all weighted_scores for the record
2. Sort descending (highest first)
3. Apply diminishing multipliers: [1.0, 0.5, 0.25, 0.125]
4. Sum: score₁×1.0 + score₂×0.5 + score₃×0.25 + score₄×0.125
5. Cap at 1.0
```

**Example - Same Record from Two Indexes**:

Record "doc-123" appears in:
- FTS index 1: weighted_score = 0.6
- FTS index 2: weighted_score = 0.4

Aggregation:
- Sort: [0.6, 0.4]
- Apply: 0.6×1.0 + 0.4×0.5 = 0.6 + 0.2 = 0.8
- Final: 0.8

---

## Future Features

The following features are defined in the configuration schema but **not yet implemented**:

- Vector search (semantic similarity)
- Graph search (relationships)
- PostgreSQL backends
- Cloud storage (Azure Blobs)
- File/repository storage
- Embeddings providers (OpenAI, Ollama)
- Caching

Check the project roadmap or GitHub issues for implementation status.

---

## Need More Help?

- **View examples**: `km examples`
- **Command help**: `km search --help`
- **View current config**: `km config`

---

**Last updated**: 2025-12-01  
**Version**: 2.0 (Focused on implemented features)
