dotnet ef migrations add init
dotnet ef database update

dotnet ef migrations script --idempotent --output "script.sql"