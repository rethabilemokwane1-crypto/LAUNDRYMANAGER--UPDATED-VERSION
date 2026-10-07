# ---------- Build Stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies first (better caching)
COPY ["LAUNDRYMANAGER.csproj", "./"]
RUN dotnet restore "LAUNDRYMANAGER.csproj"

# Copy the rest of the source code and build
COPY . .
RUN dotnet publish "LAUNDRYMANAGER.csproj" -c Release -o /app/publish

# ---------- Runtime Stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Render provides the PORT environment variable at runtime
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "LAUNDRYMANAGER.dll"]