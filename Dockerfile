FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

COPY ["DataAccessLayer/DataAccessLayer.csproj", "DataAccessLayer/"]
COPY ["BusinessLayer/BusinessLayer.csproj", "BusinessLayer/"]
COPY ["PresentationLayer/PresentationLayer.csproj", "PresentationLayer/"]
RUN dotnet restore "PresentationLayer/PresentationLayer.csproj"

COPY . .

RUN dotnet publish "PresentationLayer/PresentationLayer.csproj" -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build-env /app/out .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PresentationLayer.dll"]