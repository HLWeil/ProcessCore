# ProcessCore.SQL.JavaScript

JavaScript runtime adapter for `ProcessCore.SQL`.

This project is intended for Fable JavaScript output and targets Node with `better-sqlite3`. The .NET build compiles a stub so the solution can stay green before Node/Fable packaging is wired.

Planned npm packaging work:

- Add repo-level Node tooling (`package.json`, lockfile, scripts).
- Add `better-sqlite3` as the runtime dependency for the generated JS package.
- Add Fable transpilation output folders and ignore generated artifacts where appropriate.
- Run the shared Pyxpecto tests under Node.
