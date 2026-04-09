# Project Context

<!-- One-liner: what is this project? -->
This repo contains the core data model specifications for the ARC ecosystem. It serves as a reference for the design and implementation of the data structures used in core tooling across nfdi4plants, including ARCtrl and related applications.

## Architecture

<!-- Where things live. The agent will grep/glob to explore, but this saves tokens and wrong turns. -->

<!-- Add key boundaries the agent must respect: -->
<!-- - "All database access goes through src/db/, never import the ORM directly in route handlers" -->
<!-- - "src/legacy/ is frozen — read but never modify" -->

## Tech Stack

<!-- Be specific about versions. Agents default to whatever was common in training data. -->

## Commands

<!-- Exact strings. Agents use these verbatim. -->

## Code Style

<!-- Only rules a linter can't enforce. If ruff/prettier/eslint handles it, don't repeat it here. -->

## Testing

## Git & Commits

- Conventional commits: `feat:`, `fix:`, `refactor:`, `docs:`, `test:`, `chore:`.
- One logical change per commit. Keep diffs reviewable.
- Never force-push to `main`.

## Prohibitions

<!-- Things the agent must never do. Be explicit — agents are eager to help. -->

- Do NOT add new production dependencies without asking first.

## Verification

<!-- What must pass before the agent considers a task complete. -->

Before marking work as done:

## Gotchas

<!-- Add real failure points as you discover them. This section is the highest-signal content. -->
<!-- Examples: -->
<!-- - "The ORM lazy-loads by default. Always use `selectinload()` in queries or you get N+1." -->
<!-- - "The CI runner has no network access. Mock all external API calls." -->
<!-- - "Environment variables are in .env.example, not .env. Copy first." -->