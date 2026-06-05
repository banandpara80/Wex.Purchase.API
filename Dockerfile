FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Install .NET 8 SDK alongside .NET 10 SDK
RUN apt-get update && apt-get install -y --no-install-recommends wget ca-certificates && \
    wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh && \
    bash /tmp/dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet && \
    rm /tmp/dotnet-install.sh && \
    apt-get remove -y wget ca-certificates && \
    apt-get autoremove -y && apt-get clean && rm -rf /var/lib/apt/lists/*

# Copy all project files
COPY ["Wex.Purchase.API/Wex.Purchase.API.csproj", "Wex.Purchase.API/"]
COPY ["Wex.Purchase.API.ServiceDefaults/Wex.Purchase.API.ServiceDefaults.csproj", "Wex.Purchase.API.ServiceDefaults/"]
COPY ["Wex.Purchase.BusinessModels/Wex.Purchase.BusinessModels.csproj", "Wex.Purchase.BusinessModels/"]
COPY ["Wex.Purchase.Common/Wex.Purchase.Common.csproj", "Wex.Purchase.Common/"]
COPY ["Wex.Purchase.Service/Wex.Purchase.Service.csproj", "Wex.Purchase.Service/"]
COPY ["Wex.Purchase.Manager/Wex.Purchase.Manager.csproj", "Wex.Purchase.Manager/"]
COPY ["Wex.Purchase.Repository/Wex.Purchase.Repository.csproj", "Wex.Purchase.Repository/"]
COPY ["Wex.Purchase.UI/Wex.Purchase.UI.csproj", "Wex.Purchase.UI/"]
COPY ["Wex.Purchase.Unit.Tests/Wex.Purchase.Unit.Tests.csproj", "Wex.Purchase.Unit.Tests/"]
COPY ["Wex.Purchase.Integration.Tests/Wex.Purchase.Integration.Tests.csproj", "Wex.Purchase.Integration.Tests/"]

RUN dotnet restore "./Wex.Purchase.API/Wex.Purchase.API.csproj"
COPY . .

# Build API
WORKDIR "/src/Wex.Purchase.API"
RUN dotnet build "./Wex.Purchase.API.csproj" -c Release -o /app/build

# Build UI and copy static files to wwwroot
WORKDIR "/src/Wex.Purchase.UI"
RUN dotnet build "./Wex.Purchase.UI.csproj" -c Release -o /app/ui-build

# Run tests before publishing
FROM build AS test
WORKDIR /src
RUN echo "Running Unit Tests..." && dotnet test "./Wex.Purchase.Unit.Tests/Wex.Purchase.Unit.Tests.csproj" -c Release --no-build --logger "console;verbosity=normal"

# Publish depends on test, ensuring tests run first
FROM test AS publish
WORKDIR /src/Wex.Purchase.API
RUN dotnet publish "./Wex.Purchase.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Copy UI static files to API wwwroot if needed
FROM publish AS publish-with-ui
WORKDIR /src/Wex.Purchase.UI
RUN dotnet publish "./Wex.Purchase.UI.csproj" -c Release -o /app/ui-publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Install .NET 8 ASP.NET runtime on top of .NET 10
RUN apt-get update && apt-get install -y --no-install-recommends wget ca-certificates && \
    wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh && \
    bash /tmp/dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet && \
    rm /tmp/dotnet-install.sh && \
    apt-get remove -y wget ca-certificates && \
    apt-get autoremove -y && apt-get clean && rm -rf /var/lib/apt/lists/*

ENV DOTNET_ROOT=/usr/share/dotnet
ENV PATH="$PATH:/usr/share/dotnet"

EXPOSE 8080
# Copy API published files
COPY --from=publish-with-ui /app/publish .
# Copy UI published files to wwwroot for static file serving (if using Blazor Server)
# Uncomment the next line if UI should be served as static content from API
# COPY --from=publish-with-ui /app/ui-publish/wwwroot ./wwwroot/ui

ENTRYPOINT ["dotnet", "Wex.Purchase.API.dll"]
