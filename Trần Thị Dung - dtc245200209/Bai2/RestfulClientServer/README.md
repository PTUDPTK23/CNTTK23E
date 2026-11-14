# RestfulClientServer

A tiny .NET 10.0 solution that demonstrates a **RESTful API** (`Submission.Api`) and a **console client** (`Submission.Client`).

## What it does
- Serves a list of assignments (`/api/assignments`).
- Allows creating a submission for an assignment (`POST /api/assignments/{id}/submissions`).
- Supports reading, deleting submissions and proper error handling (404, 400, 409, 415). 
- Includes a ready‑to‑run **VS Code launch configuration** and **tasks**.

## Prerequisites
- Windows 11 (or any OS supporting .NET 10) with the **.NET SDK 10.0** installed.
- Git for Windows (or any Git client).

## How to run locally
```powershell
# 1️⃣ Start the API (listening on http://localhost:5000)
cd D:\B2PT\RestfulClientServer
dotnet run --project src\Submission.Api --no-build --urls http://localhost:5000

# 2️⃣ In another terminal, start the client
cd D:\B2PT\RestfulClientServer
dotnet run --project src\Submission.Client --no-build -- http://localhost:5000
```
Or simply double‑click the provided `run-api.bat` and `run-client.bat`.

## Testing the API
The project ships with a **REST Client** file (`requests.http`). Open it in VS Code (install the *humao.rest-client* extension) and click **Send Request** on each block to exercise every endpoint.

## CI/CD with GitHub Actions
A basic workflow is already added (`.github/workflows/dotnet.yml`). It builds the solution on **Ubuntu**, **Windows**, and **macOS**, runs the unit‑test suite (if any), and publishes the build artefacts.

## License
This project is released under the **MIT License**. See the `LICENSE` file for details.

