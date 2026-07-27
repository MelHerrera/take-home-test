# Build context is backend/src (see docker-compose.yml) so this Dockerfile can COPY sibling
# class library projects (Domain/Application/Infrastructure) that Fundo.Applications.WebApi references.

# Stage 1: restore + publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only .csproj files first so `dotnet restore` is cached and skipped on rebuilds
# unless a project reference or package actually changed.
COPY Fundo.Domain/Fundo.Domain.csproj Fundo.Domain/
COPY Fundo.Application/Fundo.Application.csproj Fundo.Application/
COPY Fundo.Infrastructure/Fundo.Infrastructure.csproj Fundo.Infrastructure/
COPY Fundo.Applications.WebApi/Fundo.Applications.WebApi.csproj Fundo.Applications.WebApi/
RUN dotnet restore Fundo.Applications.WebApi/Fundo.Applications.WebApi.csproj

# Now copy the rest of the source and publish.
COPY Fundo.Domain/ Fundo.Domain/
COPY Fundo.Application/ Fundo.Application/
COPY Fundo.Infrastructure/ Fundo.Infrastructure/
COPY Fundo.Applications.WebApi/ Fundo.Applications.WebApi/
RUN dotnet publish Fundo.Applications.WebApi/Fundo.Applications.WebApi.csproj \
    -c Release -o /app/publish --no-restore

# Stage 2: runtime only (no SDK, smaller image)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
# "." is the destination here, meaning the current WORKDIR (/app).
COPY --from=build /app/publish .

EXPOSE 8080
# The published output was copied into /app (the WORKDIR above), so the entry point
# just runs the WebApi dll from there with the dotnet runtime.
ENTRYPOINT ["dotnet", "Fundo.Applications.WebApi.dll"]