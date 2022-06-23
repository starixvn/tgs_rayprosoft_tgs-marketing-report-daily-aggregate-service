#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BuildConfiguration=Release
RUN --mount=type=secret,id=nuget,dst=/pat cp /pat ~/.nuget/NuGet/NuGet.Config
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.MarketingReport.DailyAggregateService/Tgs.Marketing.ReportDailyAggregateService.csproj", "Tgs.MarketingReport.DailyAggregateService/"]
COPY ["Tgs.MarketingReport.DailyAggregate.Repositories/Tgs.MarketingReport.DailyAggregate.Repositories.csproj", "Tgs.MarketingReport.DailyAggregate.Repositories/"]
COPY ["Tgs.MarketingReport.DailyAggregate.Core/Tgs.MarketingReport.DailyAggregate.Core.csproj", "Tgs.MarketingReport.DailyAggregate.Core/"]
RUN dotnet restore "Tgs.MarketingReport.DailyAggregateService/Tgs.Marketing.ReportDailyAggregateService.csproj"
COPY . .
WORKDIR "/src/Tgs.MarketingReport.DailyAggregateService"
RUN dotnet build "Tgs.Marketing.ReportDailyAggregateService.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration
RUN dotnet publish "Tgs.Marketing.ReportDailyAggregateService.csproj" -c $BuildConfiguration -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.Marketing.ReportDailyAggregateService.dll"]