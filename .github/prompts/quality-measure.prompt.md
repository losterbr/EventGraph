# Quality Measure Prompt

When working on this repository, follow these quality expectations before considering a change complete:

- Keep the codebase buildable and testable.
- Run the existing quality checks and relevant tests after changes.
- Prefer small, focused changes with clear intent.
- Avoid introducing new warnings or regressions.
- Maintain or improve test coverage.
- Target at least 90% line coverage for the main production code paths.

## Required workflow

1. Inspect the relevant code and tests.
2. Add or update tests for behavior changes.
3. Run the quality script or equivalent checks.
4. If coverage is below 90%, add tests until the threshold is met or explain why it cannot be achieved.
5. Report the verification results clearly.

## Commands to use

```bash
./scripts/quality.sh
```

If coverage reporting is available, ensure the measured coverage meets the 90% target.
