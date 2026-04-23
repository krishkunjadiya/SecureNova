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

function Get-KeyloggerActivity {
    Write-Log "Scanning for potential keylogger activity..."
    $findings = @()
    
    # Check for keyboard hooks
    $hooks = Get-Process | Where-Object { $_.Modules.ModuleName -match "hook|keylog" }
    if ($hooks) {
        $findings += @{
            type = "keylogger"
            severity = "high"
            details = "Potential keyboard hooks detected in processes: $($hooks.Name -join ', ')"
        }
    }
    
    # Check for hidden processes
    $hiddenProcs = Get-Process | Where-Object { $_.MainWindowTitle -eq "" -and $_.ProcessName -notmatch "^(svchost|System|Idle)$" }
    foreach ($proc in $hiddenProcs) {
        if (-not (Get-AuthenticodeSignature $proc.Path).Status -eq "Valid") {
            $findings += @{
                type = "hidden_process"
                severity = "medium"
                details = "Unsigned hidden process detected: $($proc.ProcessName)"
                path = $proc.Path
            }
        }
    }
    
    return $findings
}

function Get-SuspiciousFileChanges {
    Write-Log "Scanning for suspicious file modifications..."
    $findings = @()
    
    $sensitiveExtensions = @(".txt", ".log", ".dat", ".vbs", ".exe", ".bat", ".ps1")
    $sensitiveDirectories = @(
        "$env:USERPROFILE\Documents",
        "$env:USERPROFILE\Desktop",
        "$env:APPDATA",
        "$env:LOCALAPPDATA"
    )
    
    foreach ($dir in $sensitiveDirectories) {
        foreach ($ext in $sensitiveExtensions) {
            $files = Get-ChildItem -Path $dir -Filter "*$ext" -Recurse -ErrorAction SilentlyContinue |
                     Where-Object { $_.LastWriteTime -gt (Get-Date).AddMinutes(-$config.scanInterval) }
            
            foreach ($file in $files) {
                $sig = Get-AuthenticodeSignature $file.FullName -ErrorAction SilentlyContinue
                if ($sig.Status -ne "Valid") {
                    $findings += @{
                        type = "suspicious_file"
                        severity = "medium"
                        details = "Recently modified unsigned file: $($file.Name)"
                        path = $file.FullName
                        lastModified = $file.LastWriteTime
                    }
                }
            }
        }
    }
    
    return $findings
}

function Get-SuspiciousExecutions {
    Write-Log "Scanning for suspicious program executions..."
    $findings = @()
    
    $suspiciousPaths = @(
        "$env:TEMP",
        "$env:APPDATA",
        "$env:LOCALAPPDATA\Temp"
    )
    
    $runningProcs = Get-Process | Where-Object { $_.Path -ne $null }
    
    foreach ($proc in $runningProcs) {
        $procPath = $proc.Path
        
        # Check if running from suspicious locations
        if ($suspiciousPaths | Where-Object { $procPath -like "$_*" }) {
            $findings += @{
                type = "suspicious_location"
                severity = "high"
                details = "Process running from suspicious location: $($proc.ProcessName)"
                path = $procPath
                pid = $proc.Id
            }
        }
        
        # Check for unsigned executables
        $sig = Get-AuthenticodeSignature $procPath -ErrorAction SilentlyContinue
        if ($sig.Status -ne "Valid") {
            $findings += @{
                type = "unsigned_executable"
                severity = "medium"
                details = "Unsigned executable running: $($proc.ProcessName)"
                path = $procPath
                pid = $proc.Id
            }
        }
    }
    
    return $findings
}

function Save-Findings {
    param($Findings)
    
    if ($Findings.Count -eq 0) {
        Write-Log "No security issues found in this scan."
        return
    }
    
    $timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
    $jsonFile = Join-Path $jsonLogDir "findings_$timestamp.json"
    
    $report = @{
        timestamp = (Get-Date).ToString("o")
        findings = $Findings
        systemInfo = @{
            computerName = $env:COMPUTERNAME
            userName = $env:USERNAME
            osVersion = [System.Environment]::OSVersion.Version.ToString()
        }
    }
    
    $report | ConvertTo-Json -Depth 10 | Out-File $jsonFile
    Write-Log "Found $($Findings.Count) potential security issues. Details saved to: $jsonFile"
    
    # Alert on high severity findings
    $highSeverity = $Findings | Where-Object { $_.severity -eq "high" }
    if ($highSeverity) {
        Write-Log "⚠️ WARNING: Found $($highSeverity.Count) high severity security issues!"
    }
}

function Start-SecurityScan {
    Write-Log "Starting SecureNova security scan..."
    
    $allFindings = @()
    $allFindings += Get-KeyloggerActivity
    $allFindings += Get-SuspiciousFileChanges
    $allFindings += Get-SuspiciousExecutions
    
    Save-Findings $allFindings
    Write-Log "Security scan completed."
}

# Main execution loop
Write-Log "SecureNova Core Detection Engine started. Scan interval: $($config.scanInterval) minutes"

while ($true) {
    Start-SecurityScan
    Start-Sleep -Seconds ($config.scanInterval * 60)
} 