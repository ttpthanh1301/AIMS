FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/AIMS.WebPortal/AIMS.WebPortal.csproj",  "src/AIMS.WebPortal/"]
COPY ["src/AIMS.ViewModels/AIMS.ViewModels.csproj", "src/AIMS.ViewModels/"]
RUN dotnet restore "src/AIMS.WebPortal/AIMS.WebPortal.csproj"

COPY . .
WORKDIR "/src/src/AIMS.WebPortal"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AIMS.WebPortal.dll"]