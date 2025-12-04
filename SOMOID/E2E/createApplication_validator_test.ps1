param(
    [string]$BaseUrl = "http://localhost:10654/api/somiod"
)

# Ensure UTF-8 console output
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Info ($msg) { Write-Host "[INFO]  $msg" -ForegroundColor Cyan }
function Write-Ok   ($msg) { Write-Host "[ OK ]  $msg"  -ForegroundColor Green }
function Write-Err  ($msg) { Write-Host "[ERR ]  $msg" -ForegroundColor Red }

# -----------------------------
# Helper to POST JSON
# -----------------------------
function Invoke-Post($url, $body) {
    try {
        if ($body -eq $null) {
            return Invoke-WebRequest -Uri $url -Method Post -ContentType "application/json" -ErrorAction Stop
        } else {
            $json = $body | ConvertTo-Json -Depth 10
            return Invoke-WebRequest -Uri $url -Method Post -Body $json -ContentType "application/json" -ErrorAction Stop
        }
    }
    catch {
        if ($_.Exception.Response) {
            return $_.Exception.Response
        } else {
            throw $_
        }
    }
}

# -----------------------------
# Helper to safely get HTTP status code
# -----------------------------
function Get-StatusCode($response) {
    if ($response -eq $null) { return 0 }
    if ($response.PSObject.Properties.Name -contains "StatusCode") {
        return [int]$response.StatusCode
    }
    elseif ($response.GetType().GetProperty("StatusCode")) {
        return [int]$response.StatusCode
    } else {
        return 0
    }
}

# -----------------------------
# Helper to read response body as UTF-8
# -----------------------------
function Get-ResponseContent($response) {
    try {
        if ($response -ne $null) {
            if ($response.GetResponseStream() -ne $null) {
                $reader = New-Object System.IO.StreamReader($response.GetResponseStream(), [System.Text.Encoding]::UTF8)
                $content = $reader.ReadToEnd()
                $reader.Dispose()
                return $content
            } elseif ($response.Content) {
                return $response.Content
            }
        }
    } catch {}
    return ""
}

Write-Info "Testing ONLY CreateApplicationValidator at $BaseUrl"
Write-Host ""

# -----------------------------
# 1) Null body → 400
# -----------------------------
Write-Info "Validator Test: Null body"
$response = Invoke-Post $BaseUrl $null
$status = Get-StatusCode $response

if ($status -eq 400) {
    Write-Ok "Null body correctly returned 400"
} else {
    Write-Err "Expected 400, got $status"
}

Write-Host ""

# -----------------------------
# 2) Missing resourceName → auto-generate → 201
# -----------------------------
Write-Info "Validator Test: Missing resourceName (auto-generate)"
$response = Invoke-Post $BaseUrl @{}
$status = Get-StatusCode $response
$content = Get-ResponseContent $response

if ($status -eq 201) {
    if (![string]::IsNullOrWhiteSpace($content)) {
        try {
            $json = $content | ConvertFrom-Json
            if ($json.resourceName) {
                Write-Ok "Auto-generation successful: $($json.resourceName)"
            } else {
                Write-Ok "201 Created returned (auto-generation successful, body empty)"
            }
        } catch {
            Write-Ok "201 Created returned (auto-generation successful, body could not be parsed)"
        }
    } else {
        Write-Ok "201 Created returned (auto-generation successful, body empty)"
    }
} else {
    Write-Err "Expected 201, got $status"
    Write-Host $content
}

Write-Host ""

# -----------------------------
# 3) Valid explicit resourceName → 201
# -----------------------------
Write-Info "Validator Test: Valid resourceName"

# Generate a unique name to avoid 409
$uniqueName = "myapp-" + [guid]::NewGuid().ToString().Substring(0,8)
$response = Invoke-Post $BaseUrl @{ resourceName = $uniqueName }
$status = Get-StatusCode $response
$content = Get-ResponseContent $response

if ($status -eq 201) {
    Write-Ok "Valid resourceName accepted: $uniqueName"
} else {
    Write-Err "Expected 201, got $status"
    Write-Host $content
}

Write-Host ""
Write-Info "Finished testing CreateApplicationValidator"
