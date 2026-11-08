FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/FinanceiroRHC.Api/FinanceiroRHC.Api.csproj backend/FinanceiroRHC.Api/
RUN dotnet restore backend/FinanceiroRHC.Api/FinanceiroRHC.Api.csproj
COPY backend/FinanceiroRHC.Api/ backend/FinanceiroRHC.Api/
RUN dotnet publish backend/FinanceiroRHC.Api/FinanceiroRHC.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FinanceiroRHC.Api.dll"]
