# MemoSphere kornyezeti valtozok beallitasa .env fajlbol
# Hasznalat: .\setup-env-from-file.ps1

param(
    [string]$EnvFile = ".env",
    [switch]$Machine
)

Write-Host "=== MemoSphere Kornyezeti Valtozok Beallitasa ===" -ForegroundColor Cyan
Write-Host ""

# Ellenorzes, hogy letezik-e a .env fajl
if (-not (Test-Path $EnvFile)) {
    Write-Host "HIBA: Nem talalhato a '$EnvFile' fajl!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Kerlek, masold le a .env.template fajlt:" -ForegroundColor Yellow
    Write-Host "  cp .env.template .env" -ForegroundColor Gray
    Write-Host "Majd toltsd ki az ertekeket a .env fajlban." -ForegroundColor Yellow
    Write-Host ""
    Read-Host "Nyomj Enter-t a kilepeshez"
    exit 1
}

# Admin jogosultsag ellenorzese Machine scope eseten
if ($Machine) {
    $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    if (-not $isAdmin) {
        Write-Host "HIBA: Machine scope-hoz admin jogosultsag szukseges!" -ForegroundColor Red
        Write-Host "Futtasd ujra PowerShell-t adminisztratorent, vagy hagyd el a -Machine kapcsolot." -ForegroundColor Yellow
        exit 1
    }
}

$targetScope = if ($Machine) { "Machine" } else { "User" }

Write-Host "Scope: $targetScope" -ForegroundColor Gray
Write-Host "Fajl: $EnvFile" -ForegroundColor Gray
Write-Host ""

# .env fajl beolvasasa
$envVars = @{}
$lineNumber = 0

Get-Content $EnvFile | ForEach-Object {
    $lineNumber++
    $line = $_.Trim()
    
    # Ures sorok es kommentek kihagyasa
    if ($line -eq "" -or $line.StartsWith("#")) {
        return
    }
    
    # Kulcs=Ertek feldolgozasa
    if ($line -match "^([^=]+)=(.*)$") {
        $key = $matches[1].Trim()
        $value = $matches[2].Trim()
        
        # Idezőjelek eltavolitasa, ha vannak
        $value = $value.Trim('"', "'")
        
        if ($value -ne "" -and -not $value.StartsWith("your-") -and -not $value.Contains("...")) {
            $envVars[$key] = $value
        } else {
            Write-Host "! Sor ${lineNumber}: '$key' erteke placeholder, kihagyva" -ForegroundColor Yellow
        }
    }
}

if ($envVars.Count -eq 0) {
    Write-Host "HIBA: Nem talalhato ervенyes kornyezeti valtozo a '$EnvFile' fajlban!" -ForegroundColor Red
    Write-Host "Ellenorizd, hogy kitoltotted-e az ertekeket." -ForegroundColor Yellow
    exit 1
}

Write-Host "Talalt valtozok:" -ForegroundColor Green
foreach ($key in $envVars.Keys) {
    $value = $envVars[$key]
    # Erzekeny adatok maszkolasa
    if ($value.Length -gt 30) {
        $masked = $value.Substring(0, 20) + "..." + $value.Substring($value.Length - 5)
    } elseif ($value.Length -gt 10) {
        $masked = $value.Substring(0, 10) + "..."
    } else {
        $masked = "***"
    }
    Write-Host "  $key = $masked" -ForegroundColor Gray
}
Write-Host ""

$confirm = Read-Host "Folytatod a beallitast? (I/N)"
if ($confirm -ne "I" -and $confirm -ne "i") {
    Write-Host "Megszakitva." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Kornyezeti valtozok beallitasa..." -ForegroundColor Cyan

$successCount = 0
$errorCount = 0

foreach ($key in $envVars.Keys) {
    $value = $envVars[$key]
    
    try {
        [System.Environment]::SetEnvironmentVariable($key, $value, $targetScope)
        Write-Host "OK $key beallitva" -ForegroundColor Green
        $successCount++
    }
    catch {
        Write-Host "X Hiba a $key beallitasakor: $_" -ForegroundColor Red
        $errorCount++
    }
}

Write-Host ""
Write-Host "=== Beallitas kesz! ===" -ForegroundColor Cyan
Write-Host "Sikeres: $successCount | Hibas: $errorCount" -ForegroundColor Gray
Write-Host ""

if ($successCount -gt 0) {
    Write-Host "FONTOS:" -ForegroundColor Yellow
    Write-Host "  1. Inditsd ujra a Visual Studio-t / IDE-t" -ForegroundColor Yellow
    Write-Host "  2. Vagy inditsd ujra a gepet, hogy minden alkalmazas lassa az uj ertekeket" -ForegroundColor Yellow
}

Write-Host ""

# Ellenorzes
Write-Host "Ellenorzes..." -ForegroundColor Cyan
$verifySuccess = 0
foreach ($key in $envVars.Keys) {
    $currentValue = [System.Environment]::GetEnvironmentVariable($key, $targetScope)
    if ($currentValue -eq $envVars[$key]) {
        Write-Host "OK $key helyesen beallitva" -ForegroundColor Green
        $verifySuccess++
    } else {
        Write-Host "X $key HIBA a beallitasban!" -ForegroundColor Red
    }
}

Write-Host ""
if ($verifySuccess -eq $envVars.Count) {
    Write-Host "Minden valtozo sikeresen beallitva!" -ForegroundColor Green
} else {
    Write-Host "Nehany valtozo beallitasa nem sikerult. Ellenorizd a jogosultsagokat." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Script vegrehajtva. Inditsd ujra a Visual Studio-t!" -ForegroundColor Cyan
Read-Host "Nyomj Enter-t a kilepeshez"