<#
.SYNOPSIS
    Запускает FinCtrl для разработки: backend, frontend и AiAssistant — каждый в своей вкладке Windows Terminal.

.DESCRIPTION
    - backend      — dotnet watch на http://localhost:5000 (Swagger — /swagger), перезапуск при правке кода;
    - frontend     — Vite dev-сервер на http://localhost:5173 (HMR), при отсутствии node_modules сначала npm install;
    - ai           — FinCtrl.AiAssistant (uvicorn --reload) на http://localhost:8000 (Swagger — /docs).
    SQL Server (служба MSSQLSERVER) скрипт не запускает — только проверяет, что он работает.
    Сервис, чей порт уже занят, пропускается (считается, что он уже запущен).
    Без Windows Terminal каждый сервис открывается в отдельном окне PowerShell.

.EXAMPLE
    .\dev.ps1
.EXAMPLE
    .\dev.ps1 -Only backend,frontend
.EXAMPLE
    .\dev.ps1 -NoWatch -OpenBrowser
#>
param(
    # Какие сервисы запускать (по умолчанию все).
    [ValidateSet('backend', 'frontend', 'ai')]
    [string[]] $Only = @('backend', 'frontend', 'ai'),

    # Backend через dotnet run, без dotnet watch.
    [switch] $NoWatch,

    # Открыть frontend в браузере, когда Vite поднимется.
    [switch] $OpenBrowser
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$frontendDir = Join-Path $root 'FinCtrl.Frontend\FinCtrl.Frontend'
$aiDir = Join-Path $root 'FinCtrl.AiAssistant'

# Явные пути к инструментам: не зависим от PATH и от того, какой dotnet/npm найдётся первым
# (в PATH есть и x86-версия dotnet). npm — через npm.cmd: npm.ps1 может блокироваться политикой выполнения.
$dotnet = 'C:\Program Files\dotnet\dotnet.exe'
$npm = 'C:\Program Files\nodejs\npm.cmd'
$node = 'C:\Program Files\nodejs\node.exe'

# Новый Windows Terminal (и окна PowerShell без wt) наследуют окружение этого процесса. Если PATH урезан (например, в терминале VS Code),
# инструменты, которые вызывают dotnet/npm, во вкладках не найдутся, поэтому дополняем PATH системным и пользовательским из реестра.
# Уже открытый Windows Terminal окружение не наследует — для frontend PATH дополнительно выставляется в самой вкладке.
# Папка $node — первой: npm-скрипты (vite) запускают «node» по PATH, и должен найтись именно этот.
$env:Path = (@((Split-Path $node), $env:Path) + [Environment]::GetEnvironmentVariable('Path', 'Machine') + [Environment]::GetEnvironmentVariable('Path', 'User') |
    ForEach-Object { $_ -split ';' } |
    Where-Object { $_ } |
    Select-Object -Unique) -join ';'

function Test-PortListening([int] $Port) {
    [bool](Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)
}

function ConvertTo-EncodedCommand([string] $Command) {
    [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($Command))
}

# --- SQL Server ---
$sql = Get-Service -Name 'MSSQLSERVER' -ErrorAction SilentlyContinue
if (-not $sql) {
    Write-Warning 'Служба SQL Server (MSSQLSERVER) не найдена — backend не сможет подключиться к БД.'
}
elseif ($sql.Status -ne 'Running') {
    Write-Warning "SQL Server не запущен (статус: $($sql.Status)). Запустите от администратора: Start-Service MSSQLSERVER"
}

# --- Описание сервисов ---
if ($NoWatch) {
    $backendCommand = "& '$dotnet' run --project FinCtrl.Website/FinCtrl.Website.csproj --launch-profile FinCtrl.Website --urls http://localhost:5000"
}
else {
    $backendCommand = '$env:DOTNET_WATCH_RESTART_ON_RUDE_EDIT = ''1''; $env:DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER = ''1''; ' +
        "& '$dotnet' watch run --project FinCtrl.Website/FinCtrl.Website.csproj --launch-profile FinCtrl.Website --urls http://localhost:5000"
}

$services = @(
    [pscustomobject]@{
        Key     = 'backend'
        Title   = 'FinCtrl backend :5000'
        Port    = 5000
        Dir     = Join-Path $root 'FinCtrl'
        Command = $backendCommand
    },
    [pscustomobject]@{
        Key     = 'frontend'
        Title   = 'FinCtrl frontend :5173'
        Port    = 5173
        Dir     = $frontendDir
        # PATH — внутри вкладки: уже открытый Windows Terminal не передаёт ей окружение этого процесса,
        # а шим node_modules\.bin\vite.cmd ищет «node» по PATH.
        Command = "`$env:Path = '$(Split-Path $node);' + `$env:Path; if (-not (Test-Path node_modules)) { & '$npm' install }; & '$npm' run dev"
    },
    [pscustomobject]@{
        Key     = 'ai'
        Title   = 'FinCtrl AiAssistant :8000'
        Port    = 8000
        Dir     = $aiDir
        Command = '.\.venv\Scripts\python.exe -m uvicorn main:app --port 8000 --reload'
    }
) | Where-Object { $Only -contains $_.Key }

# --- Проверка окружения ---
$toStart = @()
foreach ($service in $services) {
    if (Test-PortListening $service.Port) {
        Write-Host "[$($service.Key)] порт $($service.Port) уже занят — пропускаю (уже запущен?)." -ForegroundColor Yellow
        continue
    }

    # continue внутри switch относится к switch, а не к foreach, поэтому пропуск — через флаг
    $ready = $true
    if ($service.Key -eq 'backend' -and -not (Test-Path -LiteralPath $dotnet)) {
        Write-Warning "[backend] dotnet не найден: $dotnet — пропускаю."
        $ready = $false
    }
    if ($service.Key -eq 'frontend' -and -not (Test-Path -LiteralPath $npm)) {
        Write-Warning "[frontend] npm не найден: $npm — пропускаю."
        $ready = $false
    }
    if ($service.Key -eq 'frontend' -and -not (Test-Path -LiteralPath $node)) {
        Write-Warning "[frontend] node не найден: $node — пропускаю."
        $ready = $false
    }
    if ($service.Key -eq 'ai') {
        if (-not (Test-Path (Join-Path $aiDir '.venv\Scripts\python.exe'))) {
            Write-Warning '[ai] нет FinCtrl.AiAssistant\.venv — пропускаю. Создать: см. FinCtrl.AiAssistant\README.md, раздел «Запуск».'
            $ready = $false
        }
        elseif (-not (Test-Path (Join-Path $aiDir '.env'))) {
            Write-Warning '[ai] нет FinCtrl.AiAssistant\.env (образец — .env.example): без LLM_API_KEY сервис не стартует.'
        }
    }

    if ($ready) {
        $toStart += $service
    }
}

if ($toStart.Count -eq 0) {
    Write-Host 'Запускать нечего.'
    return
}

# --- Запуск ---
# Полный путь обязателен: уже открытый Windows Terminal не запускает вкладку с голым «pwsh»
# (вкладка сразу закрывается), а псевдоним из WindowsApps пропускаем.
$shell = Get-Command pwsh -CommandType Application -All -ErrorAction SilentlyContinue |
    Where-Object Source -notlike '*\WindowsApps\*' |
    Select-Object -First 1 -ExpandProperty Source
if (-not $shell) {
    $shell = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
}

$tabs = foreach ($service in $toStart) {
    $command = "`$Host.UI.RawUI.WindowTitle = '$($service.Title)'; Set-Location -LiteralPath '$($service.Dir)'; $($service.Command)"
    [pscustomobject]@{
        Title   = $service.Title
        Encoded = ConvertTo-EncodedCommand $command
    }
}

if (Get-Command wt -ErrorAction SilentlyContinue) {
    $wtArgs = ($tabs | ForEach-Object {
        "new-tab --title `"$($_.Title)`" --suppressApplicationTitle `"$shell`" -NoExit -EncodedCommand $($_.Encoded)"
    }) -join ' ; '
    Start-Process wt -ArgumentList $wtArgs
}
else {
    foreach ($tab in $tabs) {
        Start-Process $shell -ArgumentList '-NoExit', '-EncodedCommand', $tab.Encoded
    }
}

Write-Host "Запущено: $(($toStart | ForEach-Object Key) -join ', ')." -ForegroundColor Green
foreach ($service in $toStart) {
    Write-Host "  $($service.Key.PadRight(8)) http://localhost:$($service.Port)"
}

if ($OpenBrowser -and ($toStart.Key -contains 'frontend')) {
    $deadline = (Get-Date).AddSeconds(60)
    while (-not (Test-PortListening 5173) -and (Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 500
    }
    Start-Process 'http://localhost:5173'
}
