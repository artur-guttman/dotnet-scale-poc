# ---- build stage ----
FROM registry.access.redhat.com/ubi8/dotnet-80-sdk AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

# ---- runtime stage ----
FROM registry.access.redhat.com/ubi8/dotnet-80-runtime
WORKDIR /app
COPY --from=build /app ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "dotnet-scale-poc.dll"]