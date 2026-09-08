FROM mcr.microsoft.com/dotnet/sdk:10.0 AS development
WORKDIR /workspace
COPY global.json Directory.Build.props ./
COPY src/backend/Carpool.Api/Carpool.Api.csproj src/backend/Carpool.Api/packages.lock.json src/backend/Carpool.Api/
RUN dotnet restore src/backend/Carpool.Api --locked-mode
COPY src/backend/ src/backend/
WORKDIR /workspace/src/backend/Carpool.Api
EXPOSE 8080
CMD ["dotnet", "watch", "run", "--no-launch-profile"]

FROM development AS publish
RUN dotnet publish -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS production
WORKDIR /app
COPY --from=publish /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Carpool.Api.dll"]
