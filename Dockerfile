# ---- build (SDK) ----
FROM registry.access.redhat.com/ubi8/dotnet-80:8.0 AS build
WORKDIR /src

# Copiem sursele
COPY . .

# IMPORTANT: OpenShift ruleaza cu UID arbitrar; facem /src scriibil pentru grupul 0
# (best practice in OCP: chgrp 0 + g=u sau g+rwX pe caile de scriere)
USER 0
RUN chgrp -R 0 /src && chmod -R g+rwX /src
# Optional: mutam cache-urile CLI/NuGet intr-un loc sigur pentru scriere
ENV DOTNET_CLI_HOME=/tmp \
    NUGET_PACKAGES=/tmp/.nuget/packages
USER 1001

# Build & publish
RUN dotnet publish -c Release -o /app

# ---- runtime (ASP.NET Core) ----
FROM registry.access.redhat.com/ubi8/dotnet-80-runtime:8.0
WORKDIR /app
COPY --from=build /app ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "dotnet-scale-poc.dll"]