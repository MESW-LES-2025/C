# GitHub Actions Badges

Add these to your README.md:

## Test Coverage Status

```markdown
![Tests](https://github.com/MESW-LES-2025/C/actions/workflows/test-coverage.yml/badge.svg)
```

## What the workflow does:

✅ **Runs on:**
- Push to `main`, `develop`, or any `task/**` branch
- Pull requests to `main` or `develop`

✅ **Actions performed:**
1. Restores dependencies
2. Builds the solution in Release mode
3. Runs all unit tests with code coverage collection
4. Generates coverage report (HTML + Text summary)
5. Checks if coverage is ≥ 75% (excluding API and Domain projects)
6. Uploads coverage report as artifact (kept for 30 days)
7. Comments coverage summary on pull requests

✅ **Coverage requirements:**
- **Minimum:** 75% line coverage
- **Excluded from coverage:** `Consilium.API`, `Consilium.Domain`
- **Included in coverage:** `Consilium.Infrastructure`, `Consilium.Application`

## Current Coverage (as of latest run):
- **Line coverage:** 76.2% ✅
- **Branch coverage:** 64.2%
- **Method coverage:** 86.6%

## Viewing Coverage Reports

After each workflow run:
1. Go to Actions tab in GitHub
2. Click on the workflow run
3. Download the `coverage-report` artifact
4. Extract and open `index.html` in your browser for detailed coverage
