# ---- build (SDK) ----
FROM registry.access.redhat.com/ubi8/dotnet-80:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

# ---- runtime (ASP.NET Core) ----
FROM registry.access.redhat.com/ubi8/dotnet-80-runtime:8.0
WORKDIR /app
COPY --from=build /app ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "dotnet-scale-poc.dll"]