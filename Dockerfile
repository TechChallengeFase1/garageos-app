FROM mcr.microsoft.com/dotnet/sdk:10.0 AS compilacao
WORKDIR /src

COPY ["Code/GarageOS.Api/GarageOS.Api.csproj", "GarageOS.Api/"]
COPY ["Code/GarageOS.Application/GarageOS.Application.csproj", "GarageOS.Application/"]
COPY ["Code/GarageOS.Domain/GarageOS.Domain.csproj", "GarageOS.Domain/"]
COPY ["Code/GarageOS.Infrastructure/GarageOS.Infrastructure.csproj", "GarageOS.Infrastructure/"]

RUN dotnet restore "GarageOS.Api/GarageOS.Api.csproj"

COPY Code/ .

RUN dotnet publish "GarageOS.Api/GarageOS.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN addgroup --system appuser && adduser --system --ingroup appuser appuser

COPY --from=compilacao /app/publish .
RUN chown -R appuser:appuser /app

USER appuser

EXPOSE 8080
ENTRYPOINT ["dotnet", "GarageOS.Api.dll"]