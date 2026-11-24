FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
WORKDIR /src

# Restore dependencies first for better layer caching
COPY ["GymCrudApp.csproj", "./"]
RUN dotnet restore "GymCrudApp.csproj"

# Copy remaining source files and publish
COPY . .
RUN dotnet publish "GymCrudApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS final
WORKDIR /app

# Create directory for uploaded receipt images / data protection keys
RUN mkdir -p /app/wwwroot/uploads /app/keys

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GymCrudApp.dll"]