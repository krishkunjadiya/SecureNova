# SecureNova Core Detection Engine
# Author: SecureNova Team
# Version: 1.0.0

# Ensure we're running with admin privileges
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Error "SecureNova needs to run with administrator privileges!"
    exit 1
}

# Import configuration
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$configPath = Join-Path $scriptPath "config.json"
$config = Get-Content $configPath | ConvertFrom-Json

# Initialize logging
$logDir = Join-Path $scriptPath "logs"
$jsonLogDir = Join-Path $scriptPath "findings"
if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir }
if (-not (Test-Path $jsonLogDir)) { New-Item -ItemType Directory -Path $jsonLogDir }

function Write-Log {
    param($Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] $Message"
    $logFile = Join-Path $logDir "securenova-$(Get-Date -Format 'yyyy-MM-dd').log"
    Add-Content -Path $logFile -Value $logMessage
    Write-Host $logMessage
}

param(
    [switch]$SingleScan = $false
)

# Helper function to convert findings to JSON
function ConvertTo-JsonResult {
    param(
        [string]$Type,
        [object]$Data
    )
    @{
        Type = $Type
        Data = $Data
    } | ConvertTo-Json -Compress
}

# Function to check for keylogger-like behavior
function Get-KeyloggerActivity {
    Write-Information "Scanning for potential keylogger activity..."
    $suspiciousProcesses = Get-Process | Where-Object {
        $_.MainWindowTitle -eq "" -and 
        ($_.ProcessName -like "*key*" -or $_.ProcessName -like "*log*") -and
        $_.Path -notlike "*Windows*"
    }

    foreach ($process in $suspiciousProcesses) {
        $finding = @{
            Type = "Potential Keylogger"
            Severity = "high"
            Details = "Suspicious process detected: $($process.ProcessName) (PID: $($process.Id))"
        }
        Write-Output (ConvertTo-JsonResult -Type "Finding" -Data $finding)
    }
}

# Function to monitor sensitive directories
function Get-SensitiveDirectoryChanges {
    Write-Information "Checking sensitive directories for recent changes..."
    $sensitiveDirectories = @(
        "$env:USERPROFILE\Documents",
        "$env:USERPROFILE\Desktop",
        "$env:APPDATA"
    )

    foreach ($dir in $sensitiveDirectories) {
        Write-Information "Scanning directory: $dir"
        if (Test-Path $dir) {
            # Scan all files in all subdirectories
            $recentChanges = Get-ChildItem -Path $dir -File -Recurse -ErrorAction SilentlyContinue |
                            Where-Object { $_.LastWriteTime -gt (Get-Date).AddMinutes(-5) }

            foreach ($change in $recentChanges) {
                $fileChange = @{
                    Path = $change.FullName
                    ChangeType = [System.IO.WatcherChangeTypes]::Changed
                }
                Write-Output (ConvertTo-JsonResult -Type "FileChange" -Data $fileChange)
            }
        }
    }
}

# Function to check for unsigned executables
function Get-UnsignedExecutables {
    Write-Information "Scanning for unsigned executables in high-risk locations..."
    $locations = @(
        "$env:USERPROFILE\Downloads",
        "$env:TEMP"
    )

    foreach ($location in $locations) {
        Write-Information "Scanning location: $location"
        if (Test-Path $location) {
            # Scan all subdirectories for executables
            $exeFiles = Get-ChildItem -Path $location -Include *.exe -File -Recurse -ErrorAction SilentlyContinue
            foreach ($file in $exeFiles) {
                $signature = Get-AuthenticodeSignature $file.FullName
                if ($signature.Status -ne "Valid") {
                    $finding = @{
                        Type = "Unsigned Executable"
                        Severity = "medium"
                        Details = "Unsigned executable found: $($file.Name) in $($file.Directory)"
                    }
                    Write-Output (ConvertTo-JsonResult -Type "Finding" -Data $finding)
                }
            }
        }
    }
}

# Function to monitor running processes
function Get-ProcessInformation {
    Write-Information "Analyzing running processes..."
    $processes = Get-Process | Where-Object { $_.Path -ne $null }
    $processCount = $processes.Count
    $current = 0

    foreach ($process in $processes) {
        $current++
        if ($current % 10 -eq 0) {
            Write-Information "Processed $current of $processCount processes..."
        }

        try {
            $signature = Get-AuthenticodeSignature $process.Path -ErrorAction SilentlyContinue
            $signatureStatus = if ($signature.Status -eq "Valid") { "Signed" } else { "Unsigned" }
            
            $riskLevel = "low"
            if ($signatureStatus -eq "Unsigned") { $riskLevel = "medium" }
            if ($process.WorkingSet64 -gt 500MB -and $signatureStatus -eq "Unsigned") { $riskLevel = "high" }

            $processInfo = @{
                Name = $process.ProcessName
                Id = $process.Id
                Path = $process.Path
                RiskLevel = $riskLevel
                SignatureStatus = $signatureStatus
            }
            Write-Output (ConvertTo-JsonResult -Type "ProcessChange" -Data $processInfo)
        }
        catch {
            # Skip processes that can't be analyzed
            continue
        }
    }
}

# Main scanning loop
Write-Information "Starting security scan..."

Write-Information "Phase 1/4: Checking for keyloggers..."
Write-Output (Get-KeyloggerActivity)

Write-Information "Phase 2/4: Monitoring sensitive directories..."
Write-Output (Get-SensitiveDirectoryChanges)

Write-Information "Phase 3/4: Scanning for unsigned executables..."
Write-Output (Get-UnsignedExecutables)

Write-Information "Phase 4/4: Analyzing running processes..."
Write-Output (Get-ProcessInformation)

Write-Information "Security scan completed."

if (-not $SingleScan) {
    Write-Information "Entering continuous monitoring mode..."
    while ($true) {
        Start-Sleep -Seconds 300  # Wait 5 minutes before next scan
        Write-Information "Starting periodic scan..."
        Write-Output (Get-KeyloggerActivity)
        Write-Output (Get-SensitiveDirectoryChanges)
        Write-Output (Get-UnsignedExecutables)
        Write-Output (Get-ProcessInformation)
    }
} 