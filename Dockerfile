FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore trinova-erp-backend.csproj
# Performance: /p:PublishReadyToRun=true generates pre-compiled native code
# during publish so the JIT does less work on first requests after a cold start.
# Does NOT enable Native AOT — the runtime image is unchanged and Railway
# compatibility is fully preserved.
RUN dotnet publish trinova-erp-backend.csproj \
    -c Release \
    -o /app/publish \
    /p:PublishReadyToRun=true

FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet","trinova-erp-backend.dll"]