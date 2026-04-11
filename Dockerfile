# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1) Copy csproj and restore (cache-friendly)
COPY server/server.csproj server/
RUN dotnet restore server/server.csproj

# 2) Copy the rest of the server project only
# (Avoids missing files due to .dockerignore or small build context)
COPY server/ server/

# 3) Publish directly to /out
RUN dotnet publish server/server.csproj -c Release -o /out /p:UseAppHost=false

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /out .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "server.dll"]