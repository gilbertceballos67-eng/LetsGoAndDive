# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Copy and restore dependencies
COPY ["LetdsGoAndDive/LetdsGoAndDive.csproj", "LetdsGoAndDive/"]
RUN dotnet restore "LetdsGoAndDive/LetdsGoAndDive.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/LetdsGoAndDive"
RUN dotnet publish "LetdsGoAndDive.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render requires you to expose the port your app listens on.
# By default, ASP.NET uses port 8080 in containers.

EXPOSE 10000
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}

ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Start the app
ENTRYPOINT ["dotnet", "LetdsGoAndDive.dll"]
