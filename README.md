# Management System

This solution demonstrates a simple management system built with **.NET 8**. It consists of a web API and a Blazor WebAssembly client that share common libraries.

## Projects

- **Server** – ASP.NET Core Web API providing authentication endpoints and example weather data.
- **Client** – Blazor WebAssembly frontend that interacts with the API and handles user sessions.
- **BaseLibrary** – Shared models, DTOs and response types.
- **ClientLibrary** – Helper classes and services for the client (HTTP client wrapper, authentication state provider, etc.).
- **ServerLibrary** – Data layer, entity framework context and repositories.

All projects are referenced in the solution file `ManagementSystem.sln`.

## Features

- JWT‑based authentication with refresh tokens (`AuthenticationController` and `UserAccountRepository`).
- Example `WeatherForecast` endpoint and page to demonstrate API calls.
- Entity Framework migrations located in `ServerLibrary/Data/Migrations`.

## Getting Started

1. Create a `.env` file for the server to provide the connection string and JWT settings. These are read using `DotNetEnv`.
2. Open `ManagementSystem.sln` in Visual Studio or run `dotnet build`.
3. Start the `Server` project followed by the `Client` project. The client is configured to call the API at `https://localhost:7140`.

## Folder Layout

```
BaseLibrary/      # Shared DTOs and entity definitions
Client/           # Blazor WebAssembly client
ClientLibrary/    # Client-side helpers and services
Server/           # ASP.NET Core Web API
ServerLibrary/    # Data context and repository implementations
