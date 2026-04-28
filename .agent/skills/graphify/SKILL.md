---
name: graphify
description: Builds a multi-modal knowledge graph from the codebase to help understand structural relationships, call graphs, and "god nodes". Outputs interactive HTML, JSON, and a markdown report.
---

# Graphify

Graphify is an open-source knowledge graph builder that uses Tree-sitter and LLMs to extract relationships and semantic communities from code and documentation.

## How to Call

You can invoke Graphify by running it directly via the terminal using your `run_command` tool. 
It operates on a target directory (usually the root of the project or a specific script folder).

### 1. Installation (If not installed)
First, verify if it's installed. If not, install it using Python's package manager:
```bash
pip install graphifyy
```

### 2. Building the Knowledge Graph
To build a knowledge graph for a specific directory (e.g., Unity Scripts):
```bash
graphify ./Assets/Script
```

*(Note: It may use your configured LLM API key to perform semantic extraction on documentation/diagrams.)*

### 3. Understanding the Outputs
Once the command finishes, it will create a `graphify-out/` directory containing:

- **`GRAPH_REPORT.md`**: An audit report detailing core nodes ("god nodes"), surprises, and suggested questions. **You should use `view_file` to read this report to understand the codebase architecture.**
- **`graph.json`**: A persistent, queryable knowledge graph.
- **`graph.html`**: An interactive visualization of the codebase structure.

## Typical Use Case for AI
When you encounter a complex, unfamiliar codebase or need to refactor a highly-coupled architecture, use Graphify to map out the dependencies before making architectural decisions.
1. Run `graphify ./Assets/Script`.
2. Read `graphify-out/GRAPH_REPORT.md` to identify the most heavily depended-upon classes.
3. Use the insights to avoid breaking changes in core nodes.
