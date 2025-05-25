# StadiaUI

A simple Steam game launcher inspired by the Stadia UI, built with Blazor and .NET.

## Features

- Browse and launch your Steam games with a modern UI
- Game images and metadata fetched from Steam and SteamGridDB
- Local caching of your game library and images
- Responsive design with Fluent UI components

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- A Steam API key (for fetching your game library)
- A SteamGridDB API key (for fetching high-quality game images)

### Setup

1. **Restore dependencies:**
   ```sh
   dotnet restore
   ```

2. **Configure your API keys:**
   - Copy `appsettings.example.json` to `appsettings.Development.json`:
     ```sh
     cp appsettings.example.json appsettings.Development.json
     ```
   - Edit `appsettings.Development.json` and fill in your Steam and SteamGridDB API keys and your SteamID.

3. **Run the application:**
   ```sh
   dotnet run
   ```
   - The app will automatically create a local SQLite database (`game-library.db`) and all required tables on first run.

4. **Access the app:**
   - Open your browser and go to `http://localhost:5231` (or the port shown in your terminal).

### Notes

- **Database:** The SQLite database file (`game-library.db`) is created automatically and is ignored by git.
- **No manual setup required:** All required tables are created based on the models when the app starts.
- **Dependencies:** All required DLLs (including SQLite) are managed by NuGet and restored automatically.

### Development

- To watch for changes and auto-reload:
  ```sh
  dotnet watch run
  ```

- To update the database schema (if you change the models), you can use EF Core migrations (optional, not required for basic use):
  ```sh
  dotnet tool install --global dotnet-ef
  dotnet ef migrations add YourMigrationName
  dotnet ef database update
  ```

### Contributing

Pull requests are welcome! For major changes, please open an issue first to discuss what you would like to change.
