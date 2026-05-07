---
title: Workflow Protocol
category: Workflow Run Decoration
categoryindex: 6
index: 4
---

# Workflow Protocol

Workflow Run specialization of [LabProtocol](../../core/LabProtocol.md). Describes the prospective metadata of a computational workflow, combining computational and laboratory workflow descriptions.

**`additionalType`**: `Workflow Protocol`

**Multi-type**: SoftwareSourceCode + ComputationalWorkflow + LabProtocol

Reference: [ARC WR RO-Crate Profile — Workflow Protocol](../../../../references/arc_wr_ro_crate.md)

## Additional Properties (beyond LabProtocol)

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `inputParameters` | [FormalParameter](FormalParameter.md) | SHOULD | Workflow inputs |
| `outputParameters` | [FormalParameter](FormalParameter.md) | SHOULD | Workflow outputs |
| `programmingLanguage` | ComputerLanguage, Text | SHOULD | Runtime environment |
| `creator` | Person, Organization | SHOULD | Creator/author |
| `license` | CreativeWork, URL | SHOULD | License |
| `sdPublisher` | Person, Organization | SHOULD | Host site |
| `hasPart` | CreativeWork | COULD | Tools/scripts used in workflow |
| `computationalTool` | SoftwareApplication, DefinedTerm | COULD | Software used |
