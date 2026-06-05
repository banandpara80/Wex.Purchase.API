# Wex.Purchase.API
# Production-Ready Extensible Framework

## Overview

This project is a **production-grade, enterprise-ready, extensible framework** designed to accelerate application development while incorporating the essential cross-cutting concerns required for modern distributed systems.

The framework provides not only the core business functionality but also a comprehensive set of production-ready capabilities out of the box, allowing development teams to focus on business requirements rather than infrastructure and operational concerns.

---

## Key Features

### Core Application Capabilities

* Modular and extensible architecture
* Clean separation of concerns
* Production-ready business workflows
* RESTful API design

### Observability & Monitoring

* Centralized logging
* Structured log management
* Real-time log viewing and monitoring
* Application metrics and observability dashboards
* Distributed tracing support
* Health check endpoints for service monitoring

### API Documentation

* Integrated Swagger/OpenAPI documentation
* Interactive API testing directly from the Swagger UI
* Clear API contract and schema definitions

### Resilience & Reliability

* Circuit breaker implementation
* Rate limiting
* Global exception handling
* Centralized error management
* Fault-tolerant service communication

### Testing

* Comprehensive unit test suite
* Integration tests
* TestContainers-based integration testing for realistic environment validation
* Automated test execution support

### User Interface

* Included UI component
* Visual testing capabilities
* End-to-end validation through both UI and Swagger endpoints

### Containerization

* Fully Dockerized application stack
* Environment consistency across development and deployment
* Simplified local setup and execution

---

## Prerequisites

Before running the application locally, ensure the following software is installed:

* Docker Desktop

Verify installation:

```bash
docker --version
docker compose version
```

---

## Running Locally

Clone the project repository and navigate to the solution root directory.
```
git clone https://github.com/banandpara80/Wex.Purchase.API.git
cd Wex.Purchase.API
```

Start the complete application stack using Docker Compose:

```bash
docker compose up -d
```

This command will start all required services, including:

* Application services
* Supporting infrastructure
* Databases (if configured)
* Observability components
* UI components

---

## Accessing the Application

After startup, the following components will be available:

| Component               | Description                                |URL                                
| ----------------------- | ------------------------------------------ |-------------------------------
| Application API         | Core business APIs                         | http://localhost:8080/
| Swagger UI              | Interactive API documentation and testing  | http://localhost:8080/swagger
| UI Application          | Visual interface for application workflows | http://localhost:8085/
| Health Endpoints        | Service health monitoring                  | http://localhost:8080/health
| Observability Dashboard | Real-time logs and monitoring              |  http://localhost:8081/
| ----------------------- | ------------------------------------------ |--------------------------------

> Refer to the Docker Compose configuration for the exact ports exposed by each service.

---

## Testing

### Run Unit Tests
Run the following command from solution root ```Wex.Purchase.API```. Alternatively, the tests can be run from Visual Studio
```bash
dotnet test .\Wex.Purchase.Unit.Tests\Wex.Purchase.Unit.Tests.csproj --logger "console;verbosity=detailed"

```

### Run Integration Tests
Run the following command from solution root ```Wex.Purchase.API```. The integration tests will run E2E tests in a test container.
Alternatively, the tests can be run from Visual Studio
```bash
dotnet test .\Wex.Purchase.Integration.Tests\Wex.Purchase.Integration.Tests.csproj
```

Integration tests utilize **TestContainers** to provision dependent services dynamically, ensuring test execution closely mirrors production environments.

---

## Production Readiness

This framework includes enterprise-grade operational capabilities such as:

* Centralized logging
* Global error handling
* Health checks
* Swagger/OpenAPI documentation
* Real-time observability
* Circuit breaker patterns
* Rate limiting
* Unit testing
* Integration testing with TestContainers
* Dockerized deployment model

These features provide a strong foundation for building, testing, deploying, and operating modern cloud-native applications in production environments.

---

## Architecture Goals

* Extensible by design
* Production-ready from day one
* Developer-friendly setup
* Observable and maintainable
* Resilient under failure conditions
* Easy local development experience
* Cloud deployment ready

---

## Quick Start

```bash
# Start all services
docker compose up -d

# View logs
docker compose logs -f

# Stop services
docker compose down
```

With Docker Desktop installed, you can get the entire platform running locally with a single command and immediately begin developing, testing, and exploring the application through both the UI and Swagger interfaces.
