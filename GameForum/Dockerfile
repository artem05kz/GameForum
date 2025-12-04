FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgdiplus libc6-dev fonts-dejavu \
    && ln -s /usr/lib/libgdiplus.so /usr/lib/libgdiplus.so.0 || true \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["GameForum.csproj", "."]
RUN dotnet restore "GameForum.csproj"
COPY . .
RUN dotnet build "GameForum.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GameForum.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

COPY --from=build /src/Migrations ./Migrations
ENTRYPOINT ["dotnet", "GameForum.dll"]
