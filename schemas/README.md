# Schema Representations

This directory contains derived schema representations of the ARC Data Model specification.

Schemas are generated from or aligned with the normative spec in [spec/](../spec/). They are not the spec itself — the markdown specifications are authoritative.

## Representations

- [sql/](sql/) — Relational (SQL) schema representation of the process graph.
- [document-db/](document-db/) — Document database schema representation.

## Current Drafts

- [sql/core_logical_erd.md](sql/core_logical_erd.md) — SQLite-oriented logical ERD for the currently specified core types, with explicit join tables for multi-valued properties.
