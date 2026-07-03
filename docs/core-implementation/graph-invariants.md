---
title: Graph Identity, Back-Edges, And Scope
category: Core Implementation
categoryindex: 3
index: 10
---

# Graph Identity, Back-Edges, And Scope

ProcessCore is an object graph with convenience indexes maintained by the model. Understanding a few invariants makes traversal behavior much easier to predict.

## Shared Node Identity

Samples are equal by `Name`. Data nodes are equal by `Path` and `Selector`.

When a process is added to a dataset, its input and output nodes are canonicalized against the root dataset registry. If another process already uses the same logical node, both processes point to the same object instance after canonicalization.

This is why a sample produced by one process can be consumed by another process without manually wiring both processes to the same object instance.

## Back-Edges

ProcessCore maintains back-edges when you use the public add/remove methods:

| Relationship | Back-edge |
|--------------|-----------|
| Process has input node | Node `InputOf` contains the process |
| Process has output node | Node `OutputOf` contains the process |
| Dataset has process | Process `ProcessOf` points to the dataset |
| Dataset has child dataset | Child `PartOf` points to the parent |

Use `AddInputSample`, `AddOutputData`, `AddProcess`, and `AddPart` instead of editing backing collections directly when you want these links to stay correct.

## Scope

Traversal methods on `Sample`, `Data`, and `IONode` can accept a process scope. Dataset-scoped helpers pass `dataset.AllProcesses()` for you.

Use a scope when:

- A node is shared across nested datasets.
- You want a query limited to one dataset.
- You want fragment selector providers resolved through the dataset that owns the processes.

Without a scope, node-centered traversal follows the node's back-edges directly.

## Nested Datasets

`Dataset.AllProcesses()` walks the dataset and all nested `HasPart` children. Adding a child dataset re-canonicalizes the child graph against the parent root, so shared sample/data nodes can connect processes across the hierarchy.

Removing a child detaches it and rebuilds the child's own registry so it can act as a root dataset again.

## Fragment Selectors

Data identity includes the selector. Two data nodes with the same path but different selectors are distinct nodes. Traversal can still connect them if their selectors are semantically related and the dataset has a matching fragment selector provider registered.

The [fragment selector provider walkthrough](fragment-selector-providers.fsx) demonstrates this with CSV row, column, and cell fragments.

## What To Use When

| Task | API |
|------|-----|
| Add processes safely | `dataset.AddProcess` |
| Add nested datasets safely | `dataset.AddPart` |
| Connect process I/O safely | `AddInputSample`, `AddInputData`, `AddOutputSample`, `AddOutputData` |
| Stay inside a dataset | Dataset-scoped helpers such as `NodesUpstreamOf` |
| Query from a node with explicit scope | `node.UpstreamNodes(scope = dataset.AllProcesses())` |
| Traverse related fragments | Register an `IFragmentSelectorProvider` on the dataset |
