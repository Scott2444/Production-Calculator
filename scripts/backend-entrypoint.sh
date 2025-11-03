#!/bin/sh
export ConnectionStrings__DefaultConnection="Host=postgres;Port=5432;Database=production_calculator;Username=server;Password=$(cat /run/secrets/server_password);Timeout=10;SSL Mode=Prefer;"
exec dotnet ProductionCalculator.API.dll