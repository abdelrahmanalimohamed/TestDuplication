#!/usr/bin/env bash
set -e
dotnet new sln -n ItemDeduplication --force
dotnet sln add src/*/*.csproj tests/*/*.csproj
dotnet restore
echo "Done. Next: docker compose up -d, then dotnet run --project src/ItemDeduplication.Api"
