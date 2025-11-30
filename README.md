# Consilium — AI-Powered Legal Communication Platform

**Simplify communication between lawyers and clients while making legal processes easier to understand.**


## Tech Stack

### Frontend
- **Angular 19** - Modern web framework with standalone components
- **TypeScript** - Type-safe JavaScript
- **Nginx** - Production web server (containerized)
- **Vite** - Fast development server with HMR

### Backend
- **ASP.NET Core 9.0** - High-performance API framework
- **.NET SDK 9.0** - Latest .NET runtime
- **C#** - Primary programming language

### Database & Storage
- **PostgreSQL 16** - Relational database
- **Qdrant** - Vector database for AI-powered search and embeddings

### DevOps & Tools
- **Docker & Docker Compose** - Containerization
- **Make** - Build automation
- **Git** - Version control

## How to run

### Prerequisites

Make sure you have installed:
- **Docker** and **Docker Compose**
- **Make** (optional, for convenience commands)

### Quick Start - Production

```bash
# Clone the repository
git clone https://github.com/MESW-LES-2025/C.git
cd C

# Build and start all services
make run-prod

# Check if everything is running
make status
```

**Access the application:**
- Frontend: http://localhost:4200
- Backend API: http://localhost:8080/
- Qdrant: http://localhost:6333

### Quick Start - Development (with hot reload)

```bash
# Start development environment
make run-dev

# Check status
make status-dev

# View logs
make logs-dev
```

**Development servers:**
- Frontend (with HMR): http://localhost:4200
- Backend (with dotnet watch): http://localhost:8080/weatherforecast

### Available Commands

```bash
make help              # Show all available commands
make status            # Check if services are running (prod)
make status-dev        # Check if services are running (dev)
make logs-prod         # View production logs
make logs-dev          # View development logs
make down-prod         # Stop production environment
make down-dev          # Stop development environment
make restart-prod      # Rebuild and restart production
make restart-dev       # Rebuild and restart development
```

### Stopping the Application

```bash
# Stop production
make down-prod

# Stop development
make down-dev
```

## Team 

To add

## Wiki

For any information about the project, check our wiki ![here](https://github.com/MESW-LES-2025/C/wiki)!