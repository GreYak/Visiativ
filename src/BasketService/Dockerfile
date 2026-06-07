# Build context : BasketService via AppHost.AddDockerfile

# Build
FROM mono:6.12 AS build

WORKDIR /src/BasketService

COPY . .

# Restore NuGet
RUN nuget restore packages.config -PackagesDirectory ../../packages

# Limitation Mono v4.7.2 => override de TargetFrameworkVersion
RUN msbuild BasketService.csproj \
        /p:Configuration=Release \
        /p:OutputPath=/output \
        /p:TargetFrameworkVersion=v4.7.2 \
        /p:WarningLevel=0

# Runtime 
FROM mono:6.12

# mono:6.12 est basé sur Debian Buster (EOL) => reedirection vers les archives Debian avant tout apt-get.
RUN sed -i 's|http://deb.debian.org/debian|http://archive.debian.org/debian|g' /etc/apt/sources.list \
    && sed -i 's|http://security.debian.org/debian-security|http://archive.debian.org/debian-security|g' /etc/apt/sources.list \
    && sed -i '/buster-updates/d' /etc/apt/sources.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends mono-xsp4 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY --from=build /output/ bin/
COPY --from=build /src/BasketService/Web.config .
COPY --from=build /src/BasketService/Global.asax .

EXPOSE 8080

# XSP4 = serveur ASP.NET de Mono pour Linux
ENTRYPOINT ["xsp4", "--port", "8080", "--address", "0.0.0.0", "--nonstop"]
