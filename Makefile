# ================================
# Makefile for Dev & Prod Docker
# ================================

COMPOSE=docker compose
DEV_FILE=docker-compose.dev.yml
PROD_FILE=docker-compose.yml

WEB=web

# ------------------------
# Help
# ------------------------
help:
	@echo "Usage: make [target]"
	@echo ""
	@echo "Targets:"
	@echo "  up-dev              Start dev environment (with hot reload)"
	@echo "  up-prod             Start prod environment"
	@echo "  run-dev             Build & run dev environment (with hot reload)"
	@echo "  run-prod            Build & run prod environment"
	@echo "  logs-dev            Show logs for dev environment"
	@echo "  logs-prod           Show logs for prod environment"
	@echo "  restart-dev         Rebuild & restart dev"
	@echo "  restart-prod        Rebuild & restart prod"
	@echo "  backend-watch       Backend hot reload (dev)"
	@echo "  frontend-watch      Frontend hot reload (dev)"
	@echo "  status              Check status of all services"
	@echo "  status-dev          Check status of dev services"
	@echo "  test                Run all tests (backend + frontend)"
	@echo "  test-backend        Run backend tests only"
	@echo "  test-frontend       Run frontend tests only"
	@echo "  clean               Remove all containers"

# ------------------------
# Build
# ------------------------
build-dev:
	$(COMPOSE) -f $(DEV_FILE) build --no-cache

build-prod:
	$(COMPOSE) -f $(PROD_FILE) build --no-cache
	$(COMPOSE) -f $(PROD_FILE) build

# ------------------------
# Up
# ------------------------
up-dev:
	$(COMPOSE) -f $(DEV_FILE) up -d

run-dev: build-dev up-dev
	@echo "✓ Dev environment running with hot reload & DEBUG logs"
	@echo "  Backend (with Swagger):  http://localhost:8080/healthz"
	@echo "  Backend Swagger Docs:    http://localhost:8080/swagger/index.html"
	@echo "  Frontend (hot reload):   http://localhost:4200"
	@echo "  Use 'make logs-dev' to see DEBUG logs"

up-prod:
	$(COMPOSE) -f $(PROD_FILE) up -d

run-prod: 
	FRONTEND_CONFIG=docker $(COMPOSE) -f $(PROD_FILE) build --no-cache
	FRONTEND_CONFIG=docker $(COMPOSE) -f $(PROD_FILE) up -d
	@echo "✓ Prod environment running (Local Backend)"
	@echo "  Frontend: http://localhost:4200"
	@echo "  Backend:  http://localhost:8080"

# ------------------------
# Logs
# ------------------------
logs-dev:
	$(COMPOSE) -f $(DEV_FILE) logs -f

logs-prod:
	$(COMPOSE) -f $(PROD_FILE) logs -f

# ------------------------
# Down
# ------------------------
down-dev:
	$(COMPOSE) -f $(DEV_FILE) down

down-prod:
	$(COMPOSE) -f $(PROD_FILE) down

# ------------------------
# Rebuild & restart
# ------------------------
restart-dev: down-dev build-dev up-dev

restart-prod: down-prod build-prod up-prod

# ------------------------
# Hot reload individually
# ------------------------
backend-watch:
	$(COMPOSE) -f $(DEV_FILE) logs -f backend

frontend-watch:
	$(COMPOSE) -f $(DEV_FILE) logs -f frontend

clean:
	$(COMPOSE) -f $(DEV_FILE) down
	$(COMPOSE) -f $(PROD_FILE) down
	@echo "✓ All containers removed"

# ------------------------
# Status check
# ------------------------
status:
	@echo "=== Testing localhost:4200 (Angular App) ==="
	@curl -s http://localhost:4200/ | grep -o '<title>.*</title>' || echo "❌ Frontend not responding"
	@echo ""
	@echo "=== Testing localhost:4200/weatherforecast (API via proxy) ==="
	@curl -s http://localhost:4200/weatherforecast | jq '.[0]' 2>/dev/null || echo "❌ API not responding"
	@echo ""
	@echo "=== Testing localhost:8080/weatherforecast (Backend API direct) ==="
	@curl -s http://localhost:8080/weatherforecast | jq '.[0]' 2>/dev/null || echo "❌ Backend not responding"
	@echo ""
	@echo "✅ Status check complete"

status-dev:
	@echo "=== Testing localhost:4200 (Angular Dev Server) ==="
	@curl -s http://localhost:4200/ | grep -o '<title>.*</title>' || echo "❌ Frontend not responding"
	@echo ""
	@echo "=== Testing localhost:8080/weatherforecast (Backend API) ==="
	@curl -s http://localhost:8080/weatherforecast | jq '.[0]' 2>/dev/null || echo "❌ Backend not responding"
	@echo ""
	@echo "✅ Dev status check complete"

# ------------------------
# Tests
# ------------------------
test-backend:
	@echo "=== Running Backend Tests (.NET) ==="
	@cd src && dotnet test Consilium.Tests/Consilium.Tests.csproj --verbosity minimal
	@echo ""

test-frontend:
	@echo "=== Running Frontend Tests (Angular) ==="
	@cd src/frontend && npm test -- --watch=false --browsers=ChromeHeadless
	@echo ""

test: test-backend test-frontend
	@echo "✅ All tests completed successfully!"

# ------------------------
# Coverage-enabled tests
# ------------------------
test-backend-coverage:
	@echo "=== Running Backend Tests with Coverage (XPlat) ==="
	@rm -rf src/TestResults/Coverage
	@cd src && \
		dotnet test Consilium.Tests/Consilium.Tests.csproj --verbosity minimal --collect:"XPlat Code Coverage" --settings coverlet.runsettings --results-directory TestResults/Coverage || exit 1
	@COV_FILE="src/TestResults/Coverage/coverage.cobertura.xml"; \
	if [ ! -f "$$COV_FILE" ]; then \
		COV_FILE=$$(find src/TestResults/Coverage -type f -name 'coverage.cobertura.xml' -print -quit); \
	fi; \
	if [ -z "$$COV_FILE" ]; then echo "Coverage file not found"; exit 1; fi; \
	COV=$$(grep -m1 '<coverage ' "$$COV_FILE" | sed -n 's/.*line-rate="\([0-9.]*\)".*/\1/p'); \
	PCT=$$(awk -v cov="$$COV" 'BEGIN {printf "%0.2f", cov * 100}'); \
	echo "Backend coverage (lines): $$PCT%"; \
	if [ $$(echo "$$COV >= 0.75" | bc -l) -ne 1 ]; then echo "Backend coverage is below 75%"; exit 1; fi

test-frontend-coverage:
	@echo "=== Running Frontend Tests with Coverage (Karma) ==="
	@cd src/frontend && npm ci && npm test -- --watch=false --browsers=ChromeHeadless --code-coverage 2>&1 | tee /tmp/frontend-tests.log || exit 1
	@PCT=$$(grep -Eo "Lines\s*:\s*[0-9]+\.?[0-9]*%" /tmp/frontend-tests.log | head -n 1 | sed 's/[^0-9.]//g'); \
	if [ -z "$$PCT" ]; then echo "Failed to extract frontend coverage from test output"; exit 1; fi; \
	echo "Frontend coverage (lines): $$PCT%"; \
	if [ $$(echo "$$PCT >= 75" | bc -l) -ne 1 ]; then echo "Frontend coverage is below 75%"; exit 1; fi