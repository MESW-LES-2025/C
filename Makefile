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
	@echo "  build-dev           Build dev Docker images"
	@echo "  build-prod          Build prod Docker images"
	@echo "  up-dev              Start dev environment (with hot reload)"
	@echo "  up-prod             Start prod environment"
	@echo "  run-dev             Build & run dev environment (with hot reload)"
	@echo "  run-prod            Build & run prod environment"
	@echo "  logs-dev            Show logs for dev environment"
	@echo "  logs-prod           Show logs for prod environment"
	@echo "  down-dev            Stop dev environment"
	@echo "  down-prod           Stop prod environment"
	@echo "  restart-dev         Rebuild & restart dev"
	@echo "  restart-prod        Rebuild & restart prod"
	@echo "  backend-watch       Backend hot reload (dev)"
	@echo "  frontend-watch      Frontend hot reload (dev)"
	@echo "  status              Check status of all services"
	@echo "  status-dev          Check status of dev services"

# ------------------------
# Build
# ------------------------
build-dev:
	$(COMPOSE) -f $(DEV_FILE) build

build-prod:
	$(COMPOSE) -f $(PROD_FILE) build

# ------------------------
# Up
# ------------------------
up-dev:
	$(COMPOSE) -f $(DEV_FILE) up -d

run-dev: build-dev up-dev
	@echo "Dev environment running with hot reload."
	@echo "Backend: http://localhost:8080"
	@echo "Frontend: http://localhost:4200"
	@echo "Use 'make logs-dev' to see logs"

up-prod:
	$(COMPOSE) -f $(PROD_FILE) up -d

run-prod: build-prod up-prod
	@echo "Prod environment running."
	@echo "Frontend: http://localhost:4200"
	@echo "Backend: http://localhost:8080"


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

clean-dev:
	$(COMPOSE) -f $(DEV_FILE) down --rmi all
	@echo "Pruned Docker objects"

clean-prod:
	$(COMPOSE) -f $(PROD_FILE) down --rmi all
	@echo "Pruned Docker objects"

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
	@echo "=== Testing localhost:6333 (Qdrant) ==="
	@curl -s http://localhost:6333/ | jq '.title' 2>/dev/null || echo "❌ Qdrant not responding"
	@echo ""
	@echo "✅ Status check complete"

status-dev:
	@echo "=== Testing localhost:4200 (Angular Dev Server) ==="
	@curl -s http://localhost:4200/ | grep -o '<title>.*</title>' || echo "❌ Frontend not responding"
	@echo ""
	@echo "=== Testing localhost:8080/weatherforecast (Backend API) ==="
	@curl -s http://localhost:8080/weatherforecast | jq '.[0]' 2>/dev/null || echo "❌ Backend not responding"
	@echo ""
	@echo "=== Testing localhost:6333 (Qdrant) ==="
	@curl -s http://localhost:6333/ | jq '.title' 2>/dev/null || echo "❌ Qdrant not responding"
	@echo ""
	@echo "✅ Dev status check complete"