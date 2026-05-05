# Decorations

Decorations are domain-specific extensions of the ProcessCore model. They map specialized data models onto the generic process graph by:

1. **Specializing core types** — e.g., Dataset becomes Investigation, Study, or Assay in the ISA decoration.
2. **Defining PropertyValue subtypes** — e.g., Parameter, Characteristic, Factor in ISA; Workflow Input, Prefix, Position in Workflow Run.
3. **Attaching cross-cutting metadata** — typically by reusing `additionalProperty` on the host core type.
4. **Adding domain-specific properties** — properties beyond what the core type defines.

## Extension Mechanism

A decoration MUST NOT modify core types. Instead, it:
- Uses `additionalType` to discriminate specializations of a core type.
- Prefers `additionalProperty` when metadata should remain extensible and composable across core types.
- Adds domain-specific properties as recommendations (SHOULD) or options (COULD), never altering core requirements.
- Groups its PropertyValue subtypes via the `additionalType` discriminator.

## Available Decorations

| Decoration | Domain | Reference Profile |
|------------|--------|-------------------|
| [Datamap](datamap/README.md) | Data fragment context and content annotations | [ARC Datamap RO-Crate Profile](../../references/arc_datamap_ro_crate.md) |
| [ISA](isa/README.md) | Experimental metadata (Investigation/Study/Assay) | [ISA RO-Crate Profile](../../references/isa_ro_crate.md) |
| [Workflow Run](workflow-run/README.md) | Computational workflow provenance | [ARC WR RO-Crate Profile](../../references/arc_wr_ro_crate.md) |
