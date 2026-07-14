# Learna - School Management System

A comprehensive school management system inspired by Lectio, designed to help schools manage students, teachers, classes, rooms, and subjects efficiently.

## Tech Stack

- **Backend**: ASP.NET Core 8 Web API
- **Frontend**: Angular (standalone components, zoneless change detection)
- **Database**: MySQL 8 with Entity Framework Core (Pomelo provider)
- **UI Library**: Angular Material

## Features

- Authentication & Authorization (JWT + refresh tokens, Admin/Teacher/Student/Parent roles)
- Student management (CRUD operations), with Thai naming support
- Guardian management, linked to one or more students
- Teacher management (CRUD operations)
- Subject management (CRUD, English + Thai names)
- School year and term management (CRUD)
- Class scheduling (planned)
- Room allocation (planned)
- Attendance tracking (planned)

## Prerequisites

Before you begin, ensure you have the following installed:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) and npm
- [Angular CLI](https://angular.io/cli): `npm install -g @angular/cli`
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for the local MySQL database)

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

A `docker-compose.yml` is provided at the repo root, running MySQL 8 with the credentials
`appsettings.Development.json` already expects:

```bash
docker compose up -d
```

This starts a `learna-mysql` container on port 3306 (root/root, database `learna_dev`),
with a named volume so data persists across restarts. No native MySQL install needed.

### 3. Backend Setup

Navigate to the API project:

```bash
cd src/backend/Learna.Api
```

The connection string in [appsettings.Development.json](src/backend/Learna.Api/appsettings.Development.json)
already matches the docker-compose database:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=learna_dev;User=root;Password=root;"
  }
}
```

Restore dependencies:

```bash
dotnet restore
```

Apply database migrations:

```bash
cd ../Learna.Infrastructure
dotnet ef database update --startup-project ../Learna.Api --project .
```

Run the API:

```bash
cd ../Learna.Api
dotnet run
```

The API will be available at:
- HTTP: http://localhost:5157
- Swagger UI: http://localhost:5157/swagger

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

- Backend runs on: http://localhost:5157
- Frontend runs on: http://localhost:4200
- Swagger UI: http://localhost:5157/swagger

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

All endpoints below require a valid JWT (`Authorize: Bearer <token>`), obtained via
`POST /api/auth/login`. Write endpoints (POST/PUT/DELETE) on Teachers, Subjects, and
School Years/Terms additionally require the Admin role.

### Students

- `GET /api/students` / `GET /api/students/{id}` - List / get a student
- `POST /api/students` / `PUT /api/students/{id}` / `DELETE /api/students/{id}` - Create / update / delete
- `GET|POST /api/students/{id}/guardians` - List / add guardians for a student
- `PUT|DELETE /api/students/{id}/guardians/{guardianId}` - Update / unlink a guardian
- `POST /api/students/{id}/guardians/{guardianId}/link` - Link an existing guardian to another student

### Teachers

- `GET /api/teachers` / `GET /api/teachers/{id}` - List / get a teacher
- `POST /api/teachers` / `PUT /api/teachers/{id}` / `DELETE /api/teachers/{id}` - Create / update / delete (Admin)

### Subjects

- `GET /api/subjects` / `GET /api/subjects/{id}` - List / get a subject
- `POST /api/subjects` / `PUT /api/subjects/{id}` / `DELETE /api/subjects/{id}` - Create / update / delete (Admin)

### School Years & Terms

- `GET /api/school-years` / `GET /api/school-years/{id}` - List / get a school year
- `POST /api/school-years` / `PUT /api/school-years/{id}` / `DELETE /api/school-years/{id}` - Create / update / delete (Admin)
- `GET|POST /api/school-years/{id}/terms` - List / add terms for a school year (Admin for POST)
- `PUT|DELETE /api/school-years/{id}/terms/{termId}` - Update / delete a term (Admin)

See Swagger UI for complete API documentation: http://localhost:5157/swagger

## Database Migrations

Run these from `src/backend/Learna.Infrastructure`:

### Create a new migration

```bash
cd src/backend/Learna.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../Learna.Api --project .
```

### Apply migrations

```bash
dotnet ef database update --startup-project ../Learna.Api --project .
```

### Rollback migration

```bash
dotnet ef database update PreviousMigrationName --startup-project ../Learna.Api --project .
```

## Adding New Features

### Backend

1. Create entity in `Learna.Core/Entities/`
2. Create interface in `Learna.Core/Interfaces/`
3. Add `DbSet<T>` to `ApplicationDbContext`
4. Create migration (from `Learna.Infrastructure`): `dotnet ef migrations add AddEntity --startup-project ../Learna.Api --project .`
5. Update database: `dotnet ef database update --startup-project ../Learna.Api --project .`
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
- Ensure the MySQL container is running: `docker ps` should show `learna-mysql` as healthy; if not, `docker compose up -d`
- Verify connection string in [appsettings.Development.json](src/backend/Learna.Api/appsettings.Development.json)

**Migration failed:**
```bash
# Drop and recreate database (from src/backend/Learna.Infrastructure)
dotnet ef database drop --startup-project ../Learna.Api --project .
dotnet ef database update --startup-project ../Learna.Api --project .
```

### Frontend Issues

**Port 4200 already in use:**
```bash
ng serve --port 4300
```

**CORS errors:**
- Verify backend is running on http://localhost:5157
- Check CORS configuration in [Program.cs](src/backend/Learna.Api/Program.cs)

**Module not found:**
```bash
cd src/frontend/learna-app
rm -rf node_modules package-lock.json
npm install
```

## Future Enhancements

- Class/homeroom, subject group, and student enrollment management
- Class scheduling with timetable generation
- Room allocation
- Attendance tracking
- Grading system
- Parent and teacher login/portal
- Localization (English/Thai UI)
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
