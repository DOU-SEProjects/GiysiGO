param(
    [string]$SqlPackagePath,
    [string]$MSBuildPath,
    [string]$VisualStudioVersion
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path $PSScriptRoot
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$installation = (& $vswhere -latest -products Microsoft.VisualStudio.Product.Community Microsoft.VisualStudio.Product.Professional Microsoft.VisualStudio.Product.Enterprise Microsoft.VisualStudio.Product.BuildTools -format json | ConvertFrom-Json) | Select-Object -First 1
if (-not $MSBuildPath) { $MSBuildPath = Join-Path $installation.installationPath 'MSBuild\Current\Bin\MSBuild.exe' }
if (-not $VisualStudioVersion) { $VisualStudioVersion = ([version]$installation.installationVersion).Major.ToString() + '.0' }
if (-not $SqlPackagePath) { $SqlPackagePath = Join-Path $installation.installationPath 'Common7\IDE\Extensions\Microsoft\SQLDB\DAC\SqlPackage.exe' }
$localDbTool = (Get-Command SqlLocalDB -ErrorAction SilentlyContinue).Source
if (-not $localDbTool) {
    $localDbTool = Get-ChildItem (Join-Path $env:ProgramFiles 'Microsoft SQL Server') -Filter SqlLocalDB.exe -Recurse | Select-Object -First 1 -ExpandProperty FullName
}
$sqlcmdTool = (Get-Command sqlcmd -ErrorAction SilentlyContinue).Source
if (-not $sqlcmdTool) {
    $sqlcmdTool = Get-ChildItem (Join-Path $env:ProgramFiles 'Microsoft SQL Server') -Filter SQLCMD.EXE -Recurse | Select-Object -First 1 -ExpandProperty FullName
}
foreach ($tool in @($MSBuildPath, $SqlPackagePath, $localDbTool, $sqlcmdTool)) {
    if (-not $tool -or -not (Test-Path -LiteralPath $tool)) { throw 'MSBuild/SSDT, SqlPackage, SqlLocalDB and sqlcmd are required.' }
}

& $MSBuildPath (Join-Path $repositoryRoot 'database/GiysiGo.Database/GiysiGo.Database.sqlproj') /p:Configuration=Release "/p:VisualStudioVersion=$VisualStudioVersion" /p:Platform=AnyCPU /m /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw 'DACPAC build failed.' }

$suffix = [Guid]::NewGuid().ToString('N')
$instanceName = 'GiysiGoIdentityTest_' + $suffix
$databaseName = 'GiysiGo_IdentityTest_' + $suffix
if ($databaseName -notmatch '^GiysiGo_IdentityTest_[a-f0-9]{32}$') { throw 'Invalid isolated database name.' }
$serverName = '(localdb)\' + $instanceName
$connectionString = "Server=$serverName;Database=$databaseName;Integrated Security=true;Encrypt=false;Pooling=false"
$previousConnection = $env:GIYSIGO_IDENTITY_TEST_CONNECTION
$instanceCreated = $false
$instanceStarted = $false
try {
    & $localDbTool create $instanceName
    if ($LASTEXITCODE -ne 0) { throw 'LocalDB instance creation failed.' }
    $instanceCreated = $true
    & $localDbTool start $instanceName
    if ($LASTEXITCODE -ne 0) { throw 'LocalDB startup failed.' }
    $instanceStarted = $true
    & $SqlPackagePath /Action:Publish "/SourceFile:$(Join-Path $repositoryRoot 'database/GiysiGo.Database/bin/Release/GiysiGo.Database.dacpac')" "/TargetConnectionString:$connectionString" /p:BlockOnPossibleDataLoss=true
    if ($LASTEXITCODE -ne 0) { throw 'Test database deployment failed.' }
    $env:GIYSIGO_IDENTITY_TEST_CONNECTION = $connectionString
    & dotnet test (Join-Path $repositoryRoot 'tests/Modules/Identity/GiysiGo.Modules.Identity.Tests.csproj') --configuration Release --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw 'Identity data tests failed.' }
}
finally {
    $env:GIYSIGO_IDENTITY_TEST_CONNECTION = $previousConnection
    if ($instanceStarted) {
        & $sqlcmdTool -S $serverName -d master -E -b -i (Join-Path $PSScriptRoot 'Drop-IdentityTestDatabase.sql') -v "DatabaseName=$databaseName"
        if ($LASTEXITCODE -ne 0) { throw "Could not clean test database $databaseName. Instance is retained for recovery." }
        else {
            & $localDbTool stop $instanceName
            if ($LASTEXITCODE -ne 0) { throw 'Could not stop the isolated LocalDB instance.' }
            & $localDbTool delete $instanceName
            if ($LASTEXITCODE -ne 0) { throw 'Could not delete the isolated LocalDB instance.' }
        }
    }
    elseif ($instanceCreated) {
        & $localDbTool delete $instanceName
    }
}
