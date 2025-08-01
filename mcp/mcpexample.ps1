# Set variables
$projectName = "SampleMcpServer"
$packageId = "KevinRansom.SampleMcpServer"
$weatherEnv = "sunny,humid,freezing"

# Step 1: Install the official MCP server template (requires .NET SDK 10 Preview 6+)
dotnet new install Microsoft.Extensions.AI.Templates

# Step 2: Create a new MCP server project
dotnet new mcpserver -n $projectName

# Step 3: Navigate into the project
Set-Location $projectName

# Step 4: Build the project
dotnet build

# Step 5: Update .csproj PackageId to ensure NuGet uniqueness
(Get-Content "$projectName.csproj") `
  -replace '<PackageId>.*?</PackageId>', "<PackageId>$packageId</PackageId>" |
  Set-Content "$projectName.csproj"

# Step 6: Create VS Code configuration for Copilot chat tool linkage
New-Item -ItemType Directory -Path ".vscode" -Force | Out-Null
@"
{
  "servers": {
    "$projectName": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "."],
      "env": {
        "WEATHER_CHOICES": "$weatherEnv"
      }
    }
  }
}
"@ | Set-Content ".vscode/mcp.json"

# Step 7: Create MCP registry metadata for NuGet publishing
New-Item -ItemType Directory -Path ".mcp" -Force | Out-Null
@"
{
  "\$schema": "https://modelcontextprotocol.io/schemas/draft/2025-07-09/server.json",
  "description": "A minimal MCP server exposing random tools.",
  "name": "io.github.kevinransom/$projectName",
  "packages": [
    {
      "registry_name": "nuget",
      "name": "$packageId",
      "version": "0.0.1",
      "package_arguments": [],
      "environment_variables": [
        {
          "name": "WEATHER_CHOICES",
          "value": "{weather_choices}",
          "variables": {
            "weather_choices": {
              "description": "Comma separated list of weather descriptions to randomly select.",
              "is_required": true,
              "is_secret": false
            }
          }
        }
      ]
    }
  ],
  "repository": {
    "url": "https://github.com/kevinransom/$projectName",
    "source": "github"
  },
  "version_detail": {
    "version": "0.0.1"
  }
}
"@ | Set-Content ".mcp/server.json"

# Final step: Echo instructions to user
Write-Host "`n?? Project setup complete! You can run it locally with:" -ForegroundColor Cyan
Write-Host "   dotnet run --project ." -ForegroundColor Green
Write-Host "`n?? Test your tools via Copilot chat in VS Code." -ForegroundColor Cyan
Write-Host "?? When ready, pack and publish with:" -ForegroundColor Cyan
Write-Host "   dotnet pack -c Release" -ForegroundColor Yellow
Write-Host "   dotnet nuget push bin/Release/*.nupkg --api-key <your-key> --source https://api.nuget.org/v3/index.json" -ForegroundColor Yellow
