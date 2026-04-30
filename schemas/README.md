# Schema Representations

This directory contains derived schema representations of the ARC Data Model specification.

Schemas are generated from or aligned with the normative spec in [spec/](../spec/). They are not the spec itself — the markdown specifications are authoritative.

## Representations

- [sql/](sql/) — Relational (SQL) schema representation of the process graph.
- [document-db/](document-db/) — Document database schema representation.
- [yml/](yml/) — YAML schema representation (JSON Schema draft-2020-12 expressed in YAML).

## YAML Schemas

Each core entity has a dedicated schema file in [yml/](yml/):

| File | Entity |
|------|--------|
| [yml/DefinedTerm.yml](yml/DefinedTerm.yml) | Ontology annotation / controlled vocabulary term |
| [yml/FormalParameter.yml](yml/FormalParameter.yml) | Prospective parameter descriptor for a protocol |
| [yml/PropertyValue.yml](yml/PropertyValue.yml) | Extensible key-value-unit triple |
| [yml/Material.yml](yml/Material.yml) | Input/output biological or chemical material |
| [yml/Data.yml](yml/Data.yml) | Data file or fragment |
| [yml/LabProtocol.yml](yml/LabProtocol.yml) | Planned procedure description |
| [yml/LabProcess.yml](yml/LabProcess.yml) | Core transformation node (inputs → outputs) |
| [yml/Dataset.yml](yml/Dataset.yml) | Container grouping processes and metadata |

### Cross-referencing

Relational properties (e.g. `inputs`, `executesProtocol`) accept either:
- A **string** `id` — a reference to an entity defined elsewhere in the document or registry.
- An **inline object** — a fully embedded entity conforming to its schema.

This keeps the default representation flat and diff-friendly while still allowing nested documents where convenient.

## Current Drafts

- [sql/core_logical_erd.md](sql/core_logical_erd.md) — SQLite-oriented logical ERD for the currently specified core types, with explicit join tables for multi-valued properties.
