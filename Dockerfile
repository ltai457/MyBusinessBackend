# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY RadiatorStockAPI.csproj ./
RUN dotnet restore
COPY . .
# publish to a clear path
RUN dotnet publish -c Release -o /out

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
# copy the published output from build stage
COPY --from=build /out .

# Elastic Beanstalk’s Nginx proxies to 127.0.0.1:8080 by default
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "RadiatorStockAPI.dll"]
