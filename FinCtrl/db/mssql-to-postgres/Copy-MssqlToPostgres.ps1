<#
.SYNOPSIS
    Одноразовый перенос данных FinCtrl из MS SQL Server в PostgreSQL (задача 0041).

.DESCRIPTION
    1. Накатывает на целевую базу PostgreSQL baseline-схему db/migrations/0001_initial.sql.
    2. Проверяет, что таблицы целевой базы пусты (иначе останавливается).
    3. Выгружает таблицы источника в CSV (UTF-8) и загружает их через psql \copy
       одной транзакцией, с сохранением id; сдвигает identity-счётчики за максимальный id.
    4. Сверяет источник и приёмник: число строк и суммы по ключевым колонкам.

    Базу SQL Server скрипт только читает. Запускать в Windows PowerShell 5.1
    (в нём есть System.Data.SqlClient):

        powershell -ExecutionPolicy Bypass -File db\mssql-to-postgres\Copy-MssqlToPostgres.ps1 `
            -SourceDb FinCtrl_db_dev2 -TargetDb finctrl_db_dev2

    Пароль PostgreSQL psql берёт из %APPDATA%\postgresql\pgpass.conf.
#>
param(
    [string]$SqlServer = 'Ivan',
    [Parameter(Mandatory = $true)][string]$SourceDb,
    [Parameter(Mandatory = $true)][string]$TargetDb,
    [string]$PgHost = 'localhost',
    [int]$PgPort = 5432,
    [string]$PgUser = 'finctrl',
    [string]$Psql = 'C:\Program Files\PostgreSQL\18\bin\psql.exe',
    # Только сверка уже перенесённых данных, без накатки схемы и загрузки.
    [switch]$VerifyOnly
)

$ErrorActionPreference = 'Stop'
# CSV пишется в UTF-8; без этого psql считает данные в кодировке консоли.
$env:PGCLIENTENCODING = 'UTF8'
# Без NOTICE «уже существует» при повторной накатке идемпотентной схемы.
$env:PGOPTIONS = '-c client_min_messages=warning'
$inv = [System.Globalization.CultureInfo]::InvariantCulture
$utf8 = New-Object System.Text.UTF8Encoding($false)

# Порядок — по внешним ключам.
$tables = @('Categories', 'PaymentSources', 'Accounts', 'Payments', 'Tags', 'PaymentTags', 'Transfers')
$identity = @{
    Categories = 'CategoryId'; PaymentSources = 'PaymentSourceId'; Accounts = 'AccountId'
    Payments = 'PaymentId'; Tags = 'TagId'; Transfers = 'TransferId'
}
# Сверка: SQL-выражения, одинаково считающиеся в обеих СУБД.
$checks = [ordered]@{
    Categories     = 'COUNT(*), SUM(CategoryId), SUM(COALESCE(ParentCategoryCategoryId, 0)), SUM(OrderIndex)'
    PaymentSources = 'COUNT(*), SUM(PaymentSourceId), SUM(COALESCE(CategoryId, 0)), SUM(CASE WHEN IsAiClassified = {T} THEN 1 ELSE 0 END)'
    Accounts       = 'COUNT(*), SUM(AccountId), SUM(CASE WHEN IsSavings = {T} THEN 1 ELSE 0 END), COUNT(AccountNumber)'
    Payments       = 'COUNT(*), SUM(PaymentId), SUM(PaymentSum), SUM(PaymentType), SUM(COALESCE(AccountId, 0)), SUM(COALESCE(PaymentCategoryCategoryId, 0)), SUM(CASE WHEN IsDeleted = {T} THEN 1 ELSE 0 END), SUM(CASE WHEN ExcludeFromStats = {T} THEN 1 ELSE 0 END), COUNT(PaymentDescription)'
    Tags           = 'COUNT(*), SUM(TagId)'
    PaymentTags    = 'COUNT(*), SUM(PaymentId), SUM(TagId)'
    Transfers      = 'COUNT(*), SUM(TransferId), SUM(OutPaymentId), SUM(COALESCE(InPaymentId, 0))'
}

$workDir = Join-Path ([System.IO.Path]::GetTempPath()) ("finctrl-pg-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $workDir | Out-Null

function Invoke-Psql([string[]]$extraArgs) {
    $psqlArgs = @('-h', $PgHost, '-p', $PgPort, '-U', $PgUser, '-d', $TargetDb, '-v', 'ON_ERROR_STOP=1', '-X', '-q') + $extraArgs
    # Ошибку определяет код возврата: stderr при Stop PowerShell 5.1 считает исключением, а там и NOTICE.
    $ErrorActionPreference = 'Continue'
    $out = & $Psql @psqlArgs 2>&1
    if ($LASTEXITCODE -ne 0) { throw "psql завершился с кодом ${LASTEXITCODE}: $out" }
    return $out | Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] }
}

function Get-PgRow([string]$sql) {
    $line = Invoke-Psql @('-A', '-t', '-F', '|', '-c', $sql) | Select-Object -Last 1
    # Запятая: иначе PowerShell развернёт массив из одного элемента в строку.
    return , ([string[]]($line -split '\|'))
}

function Format-Value($value) {
    if ($value -is [System.DBNull]) { return '' }
    if ($value -is [bool]) { if ($value) { return 'true' } else { return 'false' } }
    if ($value -is [datetime]) { return '"' + $value.ToString('yyyy-MM-dd HH:mm:ss.fffffff', $inv) + '"' }
    if ($value -is [decimal] -or $value -is [int] -or $value -is [long]) { return ([System.IFormattable]$value).ToString($null, $inv) }
    return '"' + ([string]$value).Replace('"', '""') + '"'
}

function Format-Number([string]$text) {
    if ([string]::IsNullOrWhiteSpace($text)) { return '0' }
    return ([decimal]::Parse($text.Trim(), $inv)).ToString('0.##########', $inv)
}

$sql = New-Object System.Data.SqlClient.SqlConnection("Server=$SqlServer;Database=$SourceDb;Integrated Security=True;TrustServerCertificate=True")
$sql.Open()
try {
  if (-not $VerifyOnly) {
    $migration = Join-Path $PSScriptRoot '..\migrations\0001_initial.sql'
    Write-Host "Схема: $migration -> $TargetDb"
    # Копия во временной папке: путь к репозиторию с кириллицей psql в PowerShell 5.1 может не принять.
    $migrationCopy = Join-Path $workDir '0001_initial.sql'
    Copy-Item -LiteralPath (Resolve-Path $migration).Path -Destination $migrationCopy
    Invoke-Psql @('-f', $migrationCopy) | Out-Null

    foreach ($t in $tables) {
        $n = [int]((Get-PgRow "SELECT COUNT(*) FROM $t")[0])
        if ($n -ne 0) { throw "Таблица $t в $TargetDb не пуста ($n строк) — перенос только в пустую базу." }
    }

    $load = New-Object System.Text.StringBuilder
    [void]$load.AppendLine('BEGIN;')
    foreach ($t in $tables) {
        $cmd = $sql.CreateCommand()
        $cmd.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @t ORDER BY ORDINAL_POSITION"
        [void]$cmd.Parameters.AddWithValue('@t', $t)
        $columns = @()
        $r = $cmd.ExecuteReader()
        while ($r.Read()) { $columns += $r.GetString(0) }
        $r.Close()

        $file = Join-Path $workDir "$t.csv"
        $writer = New-Object System.IO.StreamWriter($file, $false, $utf8)
        $cmd = $sql.CreateCommand()
        $cmd.CommandText = "SELECT " + (($columns | ForEach-Object { "[$_]" }) -join ', ') + " FROM dbo.[$t]"
        $r = $cmd.ExecuteReader()
        $rows = 0
        $values = New-Object object[] $columns.Count
        while ($r.Read()) {
            [void]$r.GetValues($values)
            $writer.Write((($values | ForEach-Object { Format-Value $_ }) -join ','))
            $writer.Write("`n")
            $rows++
        }
        $r.Close()
        $writer.Close()
        Write-Host ("  {0,-15} {1,7} строк" -f $t, $rows)

        $path = $file.Replace('\', '/')
        [void]$load.AppendLine("\copy $t (" + ($columns -join ', ') + ") FROM '$path' WITH (FORMAT csv)")
    }
    foreach ($t in $identity.Keys) {
        $c = $identity[$t]
        [void]$load.AppendLine("SELECT setval(pg_get_serial_sequence('$($t.ToLower())', '$($c.ToLower())'), COALESCE(MAX($c), 0) + 1, false) FROM $t;")
    }
    [void]$load.AppendLine('COMMIT;')
    $loadFile = Join-Path $workDir 'load.sql'
    [System.IO.File]::WriteAllText($loadFile, $load.ToString(), $utf8)
    Invoke-Psql @('-f', $loadFile) | Out-Null
    Write-Host "Загрузка в $TargetDb завершена."
  }

    $failed = $false
    foreach ($t in $checks.Keys) {
        $cmd = $sql.CreateCommand()
        $cmd.CommandText = "SELECT " + $checks[$t].Replace('{T}', '1') + " FROM dbo.[$t]"
        $r = $cmd.ExecuteReader()
        [void]$r.Read()
        $src = @(); for ($i = 0; $i -lt $r.FieldCount; $i++) { $src += Format-Number ([System.Convert]::ToString($r.GetValue($i), $inv)) }
        $r.Close()
        $dst = (Get-PgRow ("SELECT " + $checks[$t].Replace('{T}', 'true') + " FROM $t")) | ForEach-Object { Format-Number $_ }
        $ok = (($src -join '|') -eq ($dst -join '|'))
        if (-not $ok) { $failed = $true }
        Write-Host ("  {0,-15} {1}  mssql=[{2}] pg=[{3}]" -f $t, $(if ($ok) { 'OK  ' } else { 'DIFF' }), ($src -join ' '), ($dst -join ' '))
    }
    if ($failed) { throw 'Сверка не сошлась.' }
    Write-Host 'Сверка: всё совпадает.'
}
finally {
    $sql.Close()
    Remove-Item -Recurse -Force $workDir -ErrorAction SilentlyContinue
}
