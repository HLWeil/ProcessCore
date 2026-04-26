# Core Datamodel Table Decoration

The core datamodel defines a set of fundamental types and properties for representing processes, materials, data, and protocols. These processes can also be represented in a tabular format, as defined in [The Conversion Specification](../references/TabularAndProcessConversion.md). To support this, the datamodel includes a set of "table decoration" types that allow for representing the same underlying entities in a way that is more suitable for tabular representation. The API should follow the existing patterns of the previous tabular datamodel, see [Tabular Datamodel](../references/Tabular/ArcTableAPI.md), but with the necessary adjustments to fit the core datamodel structure and semantics.

## Requirements

- There should be a `Table` type that represents a tabular view of the processes in a dataset. The API should follow the existing patterns of the previous tabular datamodel, see [Tabular Datamodel](../references/Tabular/ArcTableAPI.md). 

- From a dataset, instead of calling the `processes` property to get the list of processes, there should be a `Tables` property that returns a collection of `Table` objects, each representing a tabular view of the processes in the dataset.

- Making changes in the `Table` (e.g. adding a row, modifying a cell) should update the underlying processes in the dataset accordingly, and vice versa. This means that the `Table` should maintain a reference to the underlying processes it represents, and any changes made to the `Table` should be reflected in those processes.