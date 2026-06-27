# Build a partir da raiz do repositório (context: .)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/Consolidado/Consolidado.Worker/Consolidado.Worker.csproj
RUN dotnet publish src/Consolidado/Consolidado.Worker/Consolidado.Worker.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Consolidado.Worker.dll"]
