# ---- build ----
FROM docker.io/library/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

# ---- runtime ----
FROM docker.io/library/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "dotnet-scale-poc.dll"]