# BogarKarba

A project for completing checklists with saving progress without registration, aimed at a technical audience.

## Running

### Local Development

```bash
dotnet restore
dotnet build
dotnet run --project src/Web
```

Application will start on `http://localhost:????`. (Port will be printed on the terminal)

### Docker

1. Copy the environment file and configure it:
```bash
cp .env.example .env
# Edit .env with your preferred database credentials
```

2. Start the application with Docker Compose:
```bash
docker-compose up --build
```

This will:
- Start a PostgreSQL database
- Start the web application which will be accessible on `http://localhost:8080` (migrations run automatically on startup)
- Start Adminer on and forward port to `http://localhost:5050` for database management
- Start Seq on `http://localhost:8081` (ingestion endpoint: `http://localhost:5341`)

## Dev

Hot reload:
```bash
dotnet watch run --project src/Web
```
