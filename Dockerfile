# ---- build (SDK) ----
FROM registry.access.redhat.com/ubi8/dotnet-80:8.0 AS build
WORKDIR /src
COPY . .

USER 0
RUN chgrp -R 0 /src && chmod -R g+rwX /src
USER 1001

RUN dotnet publish -c Release -o /src/publish

# ---- runtime (ASP.NET) ----
FROM registry.access.redhat.com/ubi8/dotnet-80-runtime:8.0
WORKDIR /app
COPY --from=build /src/publish ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "dotnet-scale-poc.dll"]