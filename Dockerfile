FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["NuGet.Config", "./"]
COPY ["Innovation4Albania.API/Innovation4Albania.API.csproj", "Innovation4Albania.API/"]
COPY ["Innovation4Albania.Application/Innovation4Albania.Application.csproj", "Innovation4Albania.Application/"]
COPY ["Innovation4Albania.Domain/Innovation4Albania.Domain.csproj", "Innovation4Albania.Domain/"]
COPY ["Innovation4Albania.Infrastructure/Innovation4Albania.Infrastructure.csproj", "Innovation4Albania.Infrastructure/"]

RUN dotnet restore "Innovation4Albania.API/Innovation4Albania.API.csproj"

COPY . .
RUN dotnet publish "Innovation4Albania.API/Innovation4Albania.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 10000

ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-10000} dotnet Innovation4Albania.API.dll"]
