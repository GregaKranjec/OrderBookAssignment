# build frontend
FROM node:24-bookworm-slim AS web-build
WORKDIR /source/src/OrderBook.Web
COPY src/OrderBook.Web/package*.json ./
RUN npm ci
COPY src/OrderBook.Web/ ./
RUN npm run build

# build api
FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS api-build
WORKDIR /source
COPY global.json ./
COPY src/OrderBook.Api/OrderBook.Api.csproj src/OrderBook.Api/
RUN dotnet restore src/OrderBook.Api/OrderBook.Api.csproj
COPY src/OrderBook.Api/ src/OrderBook.Api/
RUN dotnet publish src/OrderBook.Api/OrderBook.Api.csproj \
    --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS app
WORKDIR /app
RUN mkdir /data && chown "$APP_UID" /data
COPY --from=api-build /app/publish/ ./
COPY --from=web-build /source/src/OrderBook.Web/dist/ ./wwwroot/
ENV ASPNETCORE_HTTP_PORTS=8080
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "OrderBook.Api.dll"]
