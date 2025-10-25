# Database Initialization

This directory contains SQL scripts that are automatically executed when the PostgreSQL database is first initialized.

## How it works

The `init/` directory is mounted to `/docker-entrypoint-initdb.d/` in the PostgreSQL Docker container. Any `.sql` files in this directory are executed in alphabetical order when the database is created for the first time.

## Files

- `01-schema.sql` - Main database schema with all tables, indexes, and constraints

## Adding new initialization scripts

To add new initialization scripts:

1. Create a new `.sql` file in the `init/` directory
2. Prefix the filename with a number to control execution order (e.g., `02-seed-data.sql`)
3. Scripts are executed in alphabetical order

Example:
```
db/init/
  01-schema.sql      # Creates tables
  02-seed-data.sql   # Inserts initial data
  03-functions.sql   # Creates stored procedures
```

## Reinitializing the database

The initialization scripts only run when the database is created for the first time. To reinitialize:

```bash
# Stop and remove volumes
docker compose -f docker-compose.yml down -v

# Start again (will recreate database with init scripts)
docker compose -f docker-compose.yml up -d
```

Or using Make commands:
```bash
make clean-prod
make run-prod
```

## Verifying the schema

To verify the database schema was created correctly:

```bash
# List all tables
docker compose -f docker-compose.yml exec db psql -U postgres -d mydb -c "\dt"

# List all indexes
docker compose -f docker-compose.yml exec db psql -U postgres -d mydb -c "\di"

# Describe a specific table
docker compose -f docker-compose.yml exec db psql -U postgres -d mydb -c "\d users"
```

## Database Schema Overview

The database schema includes:

- **Users** - Base table for all system users (clients, lawyers, admins)
- **Clients** - Client-specific information
- **Lawyers** - Lawyer-specific information  
- **Admins** - Admin-specific information
- **Processes** - Legal cases/processes
- **Documents** - Documents attached to processes
- **Appointments** - Scheduled meetings
- **Notifications** - System notifications
- **Chats** - Chat sessions between clients and lawyers
- **Messages** - Individual chat messages

All tables use UUIDs as primary keys for better scalability and security.
