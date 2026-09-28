# -------------------------------
# Stage 1 - Build the application
# -------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy the project file and restore dependencies
COPY ["AI200FoundryChat.csproj", "./"]

RUN dotnet restore "AI200FoundryChat.csproj"

# Copy the remaining source code
COPY . .

# Build and publish the application
RUN dotnet publish "AI200FoundryChat.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


# -------------------------------
# Stage 2 - Runtime image
# -------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

# ASP.NET Core container port
ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

# Copy published files from build stage
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "AI200FoundryChat.dll"]