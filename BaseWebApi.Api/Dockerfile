# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY BaseWebApi.sln ./
COPY BaseWebApi.Shared/BaseWebApi.Shared.csproj BaseWebApi.Shared/
COPY BaseWebApi.Domain/BaseWebApi.Domain.csproj BaseWebApi.Domain/
COPY BaseWebApi.Application/BaseWebApi.Application.csproj BaseWebApi.Application/
COPY BaseWebApi.Infrastructure/BaseWebApi.Infrastructure.csproj BaseWebApi.Infrastructure/
COPY BaseWebApi.Api/BaseWebApi.Api.csproj BaseWebApi.Api/

RUN dotnet restore BaseWebApi.Api/BaseWebApi.Api.csproj

COPY BaseWebApi.Shared/ BaseWebApi.Shared/
COPY BaseWebApi.Domain/ BaseWebApi.Domain/
COPY BaseWebApi.Application/ BaseWebApi.Application/
COPY BaseWebApi.Infrastructure/ BaseWebApi.Infrastructure/
COPY BaseWebApi.Api/ BaseWebApi.Api/
RUN dotnet publish BaseWebApi.Api/BaseWebApi.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:6644
ENV ASPNETCORE_ENVIRONMENT=Production
ENV GENERATE_PROTOS=false

# External service endpoints (override via compose / orchestrator)
ENV SqlServer__ConnectionString=""
ENV Redis__Configuration=""
ENV Minio__Endpoint=""
ENV Minio__AccessKey=""
ENV Minio__SecretKey=""
ENV Minio__BucketName=""
ENV Kafka__BootstrapServers=""
ENV Elasticsearch__Url=""
ENV Jwt__SigningKey=""
ENV Jwt__Issuer=""
ENV Jwt__Audience=""
ENV Api__BaseUrl=""

EXPOSE 6644
EXPOSE 5001

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BaseWebApi.Api.dll"]
