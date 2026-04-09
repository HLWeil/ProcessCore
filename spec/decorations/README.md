# Decorations

Decorations are domain-specific extensions of the ProcessCore model. They map specialized data models onto the generic process graph by:

1. **Specializing core types** — e.g., Dataset becomes Investigation, Study, or Assay in the ISA decoration.
2. **Defining PropertyValue subtypes** — e.g., Parameter, Characteristic, Factor in ISA; Workflow Input, Prefix, Position in Workflow Run.
3. **Adding domain-specific properties** — properties beyond what the core type defines.

## Extension Mechanism

A decoration MUST NOT modify core types. Instead, it:
- Uses `additionalType` to discriminate specializations of a core type.
- Adds domain-specific properties as recommendations (SHOULD) or options (COULD), never altering core requirements.
- Groups its PropertyValue subtypes via the `additionalType` discriminator.

## Available Decorations

| Decoration | Domain | Reference Profile |
|------------|--------|-------------------|
| [ISA](isa/) | Experimental metadata (Investigation/Study/Assay) | [ISA RO-Crate Profile](../../references/isa_ro_crate.md) |
| [Workflow Run](workflow-run/) | Computational workflow provenance | [ARC WR RO-Crate Profile](../../references/arc_wr_ro_crate.md) |
