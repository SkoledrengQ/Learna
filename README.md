# Learna - School Management System

A comprehensive school management system inspired by Lectio, designed to help schools manage students, teachers, classes, rooms, and subjects efficiently.

## Tech Stack

- **Backend**: ASP.NET Core 8 Web API
- **Frontend**: Angular 17+ with Standalone Components
- **Database**: PostgreSQL with Entity Framework Core
- **UI Library**: Angular Material

## Features

- Student management (CRUD operations)
- Teacher management (planned)
- Class scheduling (planned)
- Room allocation (planned)
- Subject management (planned)
- Attendance tracking (planned)

## Prerequisites

Before you begin, ensure you have the following installed:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) and npm
- [Angular CLI](https://angular.io/cli): `npm install -g @angular/cli`
- [PostgreSQL 16+](https://www.postgresql.org/download/) or Docker

## Project Structure

```
Learna/
├── src/
│   ├── backend/
│   │   ├── Learna.Api/              # ASP.NET Core Web API
│   │   ├── Learna.Core/             # Domain entities and interfaces
│   │   ├── Learna.Infrastructure/   # Data access and repositories
│   │   └── Learna.Tests/            # Unit and integration tests
│   └── frontend/
│       └── learna-app/              # Angular application
├── docs/                             # Documentation
├── .gitignore
├── package.json                      # Root scripts (concurrently)
└── README.md
```

## Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd Learna
```

### 2. Database Setup

#### Option A: PostgreSQL (Native Installation)

1. Install PostgreSQL from [postgresql.org](https://www.postgresql.org/download/)
2. Create a new database:

```bash
psql -U postgres
CREATE DATABASE learna_dev;
\q
```

#### Option B: PostgreSQL (Docker)

```bash
docker run --name learna-postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=learna_dev \
  -p 5432:5432 \
  -d postgres:16
```

### 3. Backend Setup

Navigate to the API project:

```bash
cd src/backend/Learna.Api
```

Update the connection string in [appsettings.Development.json](src/backend/Learna.Api/appsettings.Development.json) if needed:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=learna_dev;Username=postgres;Password=postgres"
  }
}
```

Restore dependencies:

```bash
dotnet restore
```

Apply database migrations:

```bash
dotnet ef database update
```

Run the API:

```bash
dotnet run
```

The API will be available at:
- HTTP: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger

### 4. Frontend Setup

Navigate to the Angular app:

```bash
cd src/frontend/learna-app
```

Install dependencies:

```bash
npm install
```

Run the development server:

```bash
ng serve
```

The application will be available at: http://localhost:4200

### 5. Run Both Projects Simultaneously

From the **root directory**, run:

```bash
npm install
npm start
```

This will start both the backend and frontend concurrently.

## Development Workflow

### Daily Development

```bash
# From root directory
npm start
```

- Backend runs on: http://localhost:5000
- Frontend runs on: http://localhost:4200
- Swagger UI: http://localhost:5000/swagger

### Running Tests

```bash
# Backend tests
dotnet test

# Frontend tests
cd src/frontend/learna-app
ng test

# Or run both from root
npm test
```

### Building for Production

```bash
# From root directory
npm run build
```

## API Endpoints

### Students

- `GET /api/students` - Get all students
- `GET /api/students/{id}` - Get student by ID
- `POST /api/students` - Create a new student
- `PUT /api/students/{id}` - Update a student
- `DELETE /api/students/{id}` - Delete a student

See Swagger UI for complete API documentation: http://localhost:5000/swagger

## Database Migrations

### Create a new migration

```bash
cd src/backend/Learna.Api
dotnet ef migrations add MigrationName -p ../Learna.Infrastructure
```

### Apply migrations

```bash
dotnet ef database update
```

### Rollback migration

```bash
dotnet ef database update PreviousMigrationName
```

## Adding New Features

### Backend

1. Create entity in `Learna.Core/Entities/`
2. Create interface in `Learna.Core/Interfaces/`
3. Add `DbSet<T>` to `ApplicationDbContext`
4. Create migration: `dotnet ef migrations add AddEntity -p Learna.Infrastructure -s Learna.Api`
5. Update database: `dotnet ef database update -s Learna.Api`
6. Create repository in `Learna.Infrastructure/Repositories/`
7. Create DTOs in `Learna.Api/DTOs/`
8. Create controller in `Learna.Api/Controllers/`
9. Register repository in `Program.cs` DI
10. Test with Swagger UI

### Frontend

1. Create model in `src/app/shared/models/`
2. Generate service: `ng generate service features/<feature>/services/<name>`
3. Generate component: `ng generate component features/<feature>/components/<name>`
4. Add routing in feature module
5. Test in browser

## Troubleshooting

### Backend Issues

**Port already in use:**
- Change ports in [Properties/launchSettings.json](src/backend/Learna.Api/Properties/launchSettings.json)

**Database connection failed:**
- Ensure PostgreSQL is running
- Verify connection string in [appsettings.Development.json](src/backend/Learna.Api/appsettings.Development.json)
- Check username and password

**Migration failed:**
```bash
# Drop and recreate database
dotnet ef database drop
dotnet ef database update
```

### Frontend Issues

**Port 4200 already in use:**
```bash
ng serve --port 4300
```

**CORS errors:**
- Verify backend is running on http://localhost:5000
- Check CORS configuration in [Program.cs](src/backend/Learna.Api/Program.cs)

**Module not found:**
```bash
cd src/frontend/learna-app
rm -rf node_modules package-lock.json
npm install
```

## Future Enhancements

- Authentication & Authorization (ASP.NET Core Identity + JWT)
- Teacher management
- Class scheduling with timetable generation
- Room allocation
- Subject management
- Attendance tracking
- Grading system
- Parent portal
- Email notifications
- Real-time updates (SignalR)
- Reporting (PDF generation)
- File upload (student photos, documents)

## Contributing

(Add contribution guidelines here)

## License

(Add license information here)

## Contact

(Add contact information here)
