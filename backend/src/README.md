## Running the Backend

To build the backend, navigate to the `src` folder and run:  
```sh
dotnet build
```

To run all tests:  
```sh
dotnet test
```

To start the main API:  
```sh
cd Fundo.Applications.WebApi  
dotnet run
```

Migrations and seed data are applied automatically on startup. The following endpoint should return **200 OK**:  
```http
GET -> http://localhost:60501/loans
```
(check the console output for the actual port — it's declared in `Fundo.Applications.WebApi/Properties/launchSettings.json`)

See the [root README](../../README.md) for the full setup guide, including running via Docker Compose.

## Notes  

Feel free to modify the code as needed, but try to **respect and extend the current architecture**, as this is intended to be a replica of the Fundo codebase.
