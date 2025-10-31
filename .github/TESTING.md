# GitHub Actions Setup Summary

## Created Workflows

### 1. **test-coverage.yml** - Full Test Suite with Coverage
**Triggers:** Push to `main`, `develop`, `task/**` branches, or PRs to `main`/`develop`

**What it does:**
- ✅ Runs all unit tests
- ✅ Collects code coverage
- ✅ Generates HTML coverage reports
- ✅ **Fails if coverage < 75%**
- ✅ Excludes `Consilium.API` and `Consilium.Domain` from coverage calculation
- ✅ Uploads coverage report artifact (30-day retention)
- ✅ Comments coverage summary on pull requests

**Coverage Status:** Currently at **76.2%** ✅

### 2. **quick-tests.yml** - Fast Test Run
**Triggers:** Every push and PR on any branch

**What it does:**
- ✅ Runs all unit tests
- ✅ No coverage collection (faster feedback)
- ✅ Perfect for quick validation during development

## Test Coverage Breakdown

### Current Coverage (Infrastructure only):
```
Line coverage:     76.2% ✅
Branch coverage:   64.2%
Method coverage:   86.6%

Consilium.Infrastructure:                    76.2%
  ├─ AppDbContext                            100%
  ├─ UserRepository                          100%
  ├─ PasswordHasher                          89.2%
  └─ ClientRepository                        58.3%
```

### Coverage Requirements:
- **Minimum threshold:** 75%
- **Excluded projects:**
  - Consilium.API (endpoints, DTOs)
  - Consilium.Domain (models, enums)
- **Included projects:**
  - Consilium.Infrastructure (repositories, services, data)
  - Consilium.Application (interfaces, business logic)

## Adding Badges to README

Add these to your `README.md`:

```markdown
[![Tests](https://github.com/MESW-LES-2025/C/actions/workflows/test-coverage.yml/badge.svg)](https://github.com/MESW-LES-2025/C/actions/workflows/test-coverage.yml)
[![Quick Tests](https://github.com/MESW-LES-2025/C/actions/workflows/quick-tests.yml/badge.svg)](https://github.com/MESW-LES-2025/C/actions/workflows/quick-tests.yml)
```

## Viewing Coverage Reports

### On GitHub:
1. Go to **Actions** tab
2. Click on a **"Unit Tests and Coverage"** workflow run
3. Scroll to **Artifacts** section
4. Download `coverage-report.zip`
5. Extract and open `index.html` for detailed line-by-line coverage

### Locally:
```bash
# Run tests with coverage
cd src/Consilium.Tests
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Generate report
reportgenerator \
  -reports:"./coverage/**/coverage.cobertura.xml" \
  -targetdir:"../../coverage-report" \
  -reporttypes:"Html;TextSummary" \
  -assemblyfilters:"-Consilium.API;-Consilium.Domain"

# View report
open ../../coverage-report/index.html  # macOS
```

## How Coverage Failures Work

### Scenario: Coverage drops below 75%
1. ❌ Workflow fails with error message
2. 📧 GitHub sends failure notification
3. 🚫 PR checks show as failed
4. 💬 PR comment shows current coverage percentage

### What to do:
- Add more tests to increase coverage
- Focus on `ClientRepository` (currently 58.3%)
- Or adjust threshold in workflow if appropriate

## Test Structure

```
Consilium.Tests/
├── Consilium.Infrastructure/
│   ├── Services/
│   │   └── PasswordHasherTests.cs        (3 tests)
│   └── Repositories/
│       ├── UserRepositoryTests.cs        (3 tests)
│       └── ClientRepositoryTests.cs      (3 tests)
└── Consilium.API/
    └── Services/
        └── JwtTokenServiceTests.cs       (1 test)

Total: 10 tests, all passing ✅
```

## Next Steps

### To improve coverage:
1. **Add more ClientRepository tests** (currently 58.3%)
   - Test Update method
   - Test Delete method
   - Test error scenarios

2. **Add Application layer tests**
   - Test business logic when you add it
   - Test validation logic

3. **Add integration tests**
   - Use the test database in `appsettings.Test.json`
   - Test endpoints end-to-end

### To customize workflows:
- **Change coverage threshold:** Edit `threshold=75.0` in `test-coverage.yml`
- **Add more excluded projects:** Edit `-assemblyfilters:"-Consilium.API;-Consilium.Domain"`
- **Run on different branches:** Edit `branches:` in workflow triggers
