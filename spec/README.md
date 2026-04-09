# ARC Data Model Specification

This directory contains the normative specification for the ARC Data Model.

## Structure

- [core/](core/) — **ProcessCore** specification. The foundational model that abstracts scientific experiments as a directed graph connecting sources to data via processes.
- [decorations/](decorations/) — Domain-specific extensions that map onto ProcessCore via PropertyValue specializations and type refinements.
- [querying/](querying/) — Query patterns and use cases for traversing the process graph.

## Reading Order

1. Start with [core/README.md](core/README.md) to understand the ProcessCore model.
2. Read [decorations/README.md](decorations/README.md) to understand the extension mechanism.
3. Explore individual decorations ([ISA](decorations/isa/), [Workflow Run](decorations/workflow-run/)) for concrete examples.
