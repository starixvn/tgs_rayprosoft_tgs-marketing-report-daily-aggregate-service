#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BuildConfiguration=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.MarketingReport.DailyAggregateService/Tgs.MarketingReport.DailyAggregateService.csproj", "Tgs.MarketingReport.DailyAggregateService/"]
COPY ["Tgs.MarketingReport.DailyAggregate.Core/Tgs.MarketingReport.DailyAggregate.Core.csproj", "Tgs.MarketingReport.DailyAggregate.Core/"]
COPY ["Tgs.MarketingReport.DailyAggregate.Repositories/Tgs.MarketingReport.DailyAggregate.Repositories.csproj", "Tgs.MarketingReport.DailyAggregate.Repositories/"]
RUN dotnet restore "Tgs.MarketingReport.DailyAggregateService/Tgs.MarketingReport.DailyAggregateService.csproj"
COPY . .
WORKDIR "/src/Tgs.MarketingReport.DailyAggregateService"
RUN dotnet build "Tgs.MarketingReport.DailyAggregateService.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration=Release
RUN dotnet publish "Tgs.MarketingReport.DailyAggregateService.csproj" -c $BuildConfiguration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.MarketingReport.DailyAggregateService.dll"]