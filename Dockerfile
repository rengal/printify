FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore ./Printify.sln
RUN dotnet publish ./src/Printify.Web/Printify.Web.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
COPY --from=build /app/publish .
COPY --from=build /src/src/Printify.Web/html ./html
EXPOSE 8080 9100-15000
ENTRYPOINT ["dotnet", "Printify.Web.dll"]
