FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SistemaFacturacion.csproj ./
RUN dotnet restore SistemaFacturacion.csproj

COPY . .
RUN dotnet publish SistemaFacturacion.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000

COPY --from=build /app/publish .
COPY --from=build /src/Frontend ./Frontend
RUN mkdir -p /app/Uploads

ENTRYPOINT ["dotnet", "SistemaFacturacion.dll"]
