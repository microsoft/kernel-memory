# Connection string: "Server=tcp:127.0.0.1,1433;Initial Catalog=master;Persist Security Info=False;User ID=sa;Password=00_CHANGE_ME_00;MultipleActiveResultSets=False;TrustServerCertificate=True;Connection Timeout=30;"

export MSSQL_SA_PASSWORD="00_CHANGE_ME_00"

docker run -p 1433:1433 --hostname mssql --name mssql -e MSSQL_SA_PASSWORD -e "ACCEPT_EULA=Y" \
  -it --rm mcr.microsoft.com/mssql/server:2022-latest
  