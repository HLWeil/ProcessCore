# ProcessCore Documentation Plan

## Summary

Create a hybrid fsdocs documentation set for F# users of `ProcessCore`.
The docs live under `docs/project/ProcessCore/`.

Use literate `.fsx` pages for executable walkthroughs with compiled code and evaluated output.
Use Markdown pages for conceptual orientation, invariants, and extension-point explanations.

## Pages

| Page | Kind | Purpose |
|------|------|---------|
| `docs/project/ProcessCore/index.md` | Markdown | Entry point and mental model |
| `docs/project/ProcessCore/creating-datasets.fsx` | Literate F# | Construct datasets from F# objects |
| `docs/project/ProcessCore/yaml-parsing.fsx` | Literate F# | Read, write, and round-trip YAML |
| `docs/project/ProcessCore/querying.fsx` | Literate F# | Traverse and query process graphs |
| `docs/project/ProcessCore/fragment-selector-providers.fsx` | Literate F# | Use and implement fragment selector providers |
| `docs/project/ProcessCore/tables.fsx` | Literate F# | Work with live tabular views over process graphs |
| `docs/project/ProcessCore/property-values.md` | Markdown | Explain annotation slots and `AdditionalType` values |
| `docs/project/ProcessCore/graph-invariants.md` | Markdown | Explain identity, back-edges, scope, and nested datasets |

## Didactic Shape

- Start with the smallest mental model: datasets contain processes; processes connect sample and data nodes; property values annotate nodes, processes, protocols, and datasets.
- Use one running example per literate page.
- Keep setup code hidden and visible snippets short.
- Use collapsible `<details>` blocks for long YAML or generated output.
- End each page with a "What To Use When" section.
- Link conceptual pages from use-case pages instead of repeating normative specification prose.

## Verification

- Run `.\build.cmd BuildDocs`.
- Confirm fsdocs evaluates all `.fsx` pages with `--eval`.
- Confirm generated docs include the `project/ProcessCore/` pages.
- Confirm links from `docs/index.md` and `docs/project/implementation.md` resolve.

## Assumptions

- Primary audience is F# library users.
- No new production dependencies are added.
- Examples use current core vocabulary: `Process`, `Plan`, `inputs`, `outputs`, and `executesProtocol`.
