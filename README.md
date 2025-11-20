# TimeTrackingDemo

A demonstration time tracking application built with .NET Aspire, showcasing event sourcing patterns using Marten and PostgreSQL.

## Overview

TimeTrackingDemo is a modern time tracking system that allows workers to record their work shifts and breaks. The application uses event sourcing to maintain a complete history of time tracking events and provides monthly statistics for workers.

## Architecture

This project is built using .NET 10 and .NET Aspire orchestration, consisting of three main components:

- **TimeTrackingDemo.ApiService**: The main API service that handles time tracking operations
- **TimeTrackingDemo.AppHost**: .NET Aspire orchestration host for managing the application
- **TimeTrackingDemo.ServiceDefaults**: Shared service configuration and defaults

### Key Technologies

- **.NET 10.0**: Latest .NET framework
- **.NET Aspire**: Orchestration and service management
- **Marten**: Event sourcing and document database library
- **PostgreSQL**: Primary data store
- **Scalar**: API documentation UI

## Features

### Time Tracking Operations

- **Start Shift**: Begin a work shift for a specific worker
- **End Shift**: End an active work shift
- **Start Break**: Begin a break during an active shift
- **End Break**: Resume work after a break

### Statistics

- **Monthly Hours**: Query total hours worked by a worker for a specific month

### Event Sourcing

The application uses event sourcing to track all time-related activities:
- `ShiftStarted`: When a worker begins their shift
- `BreakStarted`: When a worker starts a break
- `BreakEnded`: When a worker ends a break
- `ShiftEnded`: When a worker finishes their shift

### German Shift Calendar

The application uses a German timezone-aware shift calendar with a 3-hour shift cutoff, meaning shifts starting before 3 AM are counted as part of the previous day.

## API Endpoints

All endpoints are prefixed with `/time-tracking`:

### Shift Management
- `POST /time-tracking/shifts/start` - Start a new shift
- `POST /time-tracking/shifts/end` - End an active shift

### Break Management
- `POST /time-tracking/breaks/start` - Start a break
- `POST /time-tracking/breaks/end` - End a break

### Statistics
- `GET /time-tracking/stats/{workerId}/{year}/{month}` - Get monthly statistics

### Request Format

Most endpoints accept a request body with the following format:
```json
{
  "workerId": 1,
  "timestamp": "2024-11-20T14:30:00Z"
}
```

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Docker (for PostgreSQL container)

## Getting Started

### Build the Project

```bash
dotnet build
```

### Run the Application

```bash
cd TimeTrackingDemo.AppHost
dotnet run
```

This will start:
- The API service with Scalar documentation
- PostgreSQL database with persistent data volume
- pgAdmin at http://localhost:8888 for database management

### Access the API Documentation

Once running, access the Scalar API documentation at the URL shown in the Aspire dashboard (typically available when you run the AppHost).

## Project Structure

```
TimeTrackingDemo/
├── TimeTrackingDemo.ApiService/
│   ├── Domain/              # Domain models and events
│   │   └── WorkShift.cs     # Work shift aggregate
│   ├── Features/            # API endpoints and features
│   │   └── ShiftFeatures.cs # Shift management endpoints
│   ├── Infrastructure/      # Infrastructure concerns
│   │   ├── MonthlyHoursProjection.cs  # Event projection for statistics
│   │   └── WorkerMonthlyStats.cs      # Monthly statistics model
│   └── Shared/              # Shared utilities
│       ├── GermanShiftCalendar.cs     # German timezone logic
│       └── ShiftKeyGenerator.cs       # Stream key generation
├── TimeTrackingDemo.AppHost/
│   └── AppHost.cs           # .NET Aspire orchestration configuration
└── TimeTrackingDemo.ServiceDefaults/
    └── Extensions.cs        # Service configuration extensions
```

## Domain Model

### WorkShift Aggregate

The `WorkShift` aggregate manages the lifecycle of a work shift with state transitions:
- `NotStarted` → `Working` (via StartShift)
- `Working` → `OnBreak` (via StartBreak)
- `OnBreak` → `Working` (via StopBreak)
- `Working` → `Finished` (via EndShift)

### Projections

The `MonthlyHoursProjection` maintains real-time statistics by processing work shift events and calculating total hours worked per worker per month.

## Development

### Health Checks

The API service includes health check endpoints at `/health`.

### Database

The application uses PostgreSQL with Marten for event sourcing. The database schema is automatically created on startup.

### Configuration

- **Database Connection**: Configured via connection string named "marten"
- **PostgreSQL**: Runs in a Docker container with persistent volume
- **pgAdmin**: Available on port 8888 for database inspection

## License

This is a demonstration project.
