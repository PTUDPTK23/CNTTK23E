# Catalog API - Bai 2

RESTful API cho ung dung phan tan.

## Technologies
- ASP.NET Core Web API
- .NET 10
- C#

## Projects
- Catalog.Api: RESTful API
- Catalog.Client: HTTP Client

## Run
dotnet run --project src/Catalog.Api/Catalog.Api.csproj

## API Testing

### Start the API
dotnet run --project src/Catalog.Api/Catalog.Api.csproj

### Test endpoints
- GET / : Check API status
- GET /api/products : Get all products
- GET /api/products/1 : Get product by ID
- GET /api/products/999 : Test not found response
- POST /api/products : Create a new product

### HTTP requests
Use the requests.http file to send HTTP requests and verify API responses.

## Project Structure

- src/Catalog.Api: ASP.NET Core RESTful API.
- src/Catalog.Client: Console application for API requests.
- requests.http: HTTP requests for API testing.
- CatalogApi.slnx: Solution file.

## Development Environment

- Framework: ASP.NET Core Web API
- Runtime: .NET 10
- Programming language: C#
- IDE: Visual Studio / Visual Studio Code
- Version control: Git and GitHub

## Configuration

The API project uses:
- appsettings.json for application configuration.
- appsettings.Development.json for development settings.
- Properties/launchSettings.json for local launch profiles.

Do not commit passwords, API keys, or other private credentials.

## Catalog Client

Catalog.Client is a console application used to communicate with the Catalog API.

Run the client with:

dotnet run --project src/Catalog.Client/Catalog.Client.csproj

Make sure the API server is running before starting the client.

## API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| GET | / | API information |
| GET | /api/products | Get all products |
| GET | /api/products/{id} | Get product by ID |
| POST | /api/products | Create a product |

## Error Handling

The API uses appropriate HTTP status codes to indicate request results.

- 200 OK: Request completed successfully.
- 201 Created: Resource created successfully.
- 400 Bad Request: Invalid request data.
- 404 Not Found: Requested resource does not exist.
- 415 Unsupported Media Type: Unsupported content type.

## Project Requirements

- ASP.NET Core Web API
- RESTful API endpoints
- JSON request and response format
- HTTP status code handling
- Console client for API interaction
