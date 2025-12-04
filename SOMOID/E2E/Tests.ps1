param(
    [string]$BaseUrl = "http://localhost:10654",
    [switch]$SkipCleanup
)

# Test script for SOMOID Web API endpoints
# Usage:
#   ./scripts/test_endpoints.ps1 -BaseUrl "http://localhost:10654"
# Requires PowerShell 5+ and network access to the running API.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Info($msg) { Write-Host "[INFO]  $msg" -ForegroundColor Cyan }
function Write-Ok($msg)   { Write-Host "[ OK ]  $msg" -ForegroundColor Green }
function Write-Err($msg)  { Write-Host "[ERR ]  $msg" -ForegroundColor Red }

function Invoke-Api {
    param(
        [Parameter(Mandatory=$true)][ValidateSet('GET','POST','PUT','DELETE')] [string]$Method,
        [Parameter(Mandatory=$true)][string]$Url,
        [int]$ExpectStatus = 200,
        [string]$Description = '',
        $Body = $null
    )

    if ($Description) { Write-Info $Description }

    $params = @{
        Method      = $Method
        Uri         = $Url
        ErrorAction = 'Stop'
    }
    if ($Body -ne $null) {
        $json = ($Body | ConvertTo-Json -Depth 10)
        $params['Body'] = $json
        $params['ContentType'] = 'application/json'
    }

    try {
        $resp = Invoke-WebRequest @params
        if ($ExpectStatus -and ($resp.StatusCode -ne $ExpectStatus)) {
            throw "Unexpected status code $($resp.StatusCode) (expected $ExpectStatus). Body: $($resp.Content)"
        }
        Write-Ok "$Method $Url => $($resp.StatusCode)"
        return $resp
    }
    catch {
        $status = 0
        $content = ''
        # Try to extract more details from the error
        try {
            if ($_.Exception.Response) {
                $status = [int]$_.Exception.Response.StatusCode
                $stream = $_.Exception.Response.GetResponseStream()
                if ($stream) {
                    $reader = New-Object System.IO.StreamReader($stream)
                    $content = $reader.ReadToEnd()
                    $reader.Dispose()
                }
            } elseif ($_.ErrorDetails.Message) {
                $content = $_.ErrorDetails.Message
            }
        } catch { }

        Write-Err "$Method $Url failed. Status: $status. Details: $content"
        throw
    }
}

# Normalize base URL (no trailing slash)
if ($BaseUrl.EndsWith('/')) { $BaseUrl = $BaseUrl.TrimEnd('/') }

# Generate unique resource names
$timestamp = Get-Date -Format 'yyyyMMddHHmmss'
$rand = -join ((48..57 + 97..122) | Get-Random -Count 6 | ForEach-Object {[char]$_})

$appName       = "app-$timestamp-$rand"
$containerName = "cont-$timestamp"
$ciName        = "ci-$timestamp"
$subName       = "sub-$timestamp"

Write-Info "BaseUrl      : $BaseUrl"
Write-Info "App          : $appName"
Write-Info "Container    : $containerName"
Write-Info "ContentInst  : $ciName"
Write-Info "Subscription : $subName"

$created = [ordered]@{
    App = $false
    Container = $false
    CI = $false
    Sub = $false
}

function ApiUrl([string]$path) { return "$BaseUrl$path" }

try {
    # 1) Create Application
    $resp = Invoke-Api -Method POST -Url (ApiUrl "/api/somiod") -ExpectStatus 201 -Description "Create application" -Body @{ resourceName = $appName }
    try { $appName = ($resp.Content | ConvertFrom-Json).resourceName } catch { }
    $created['App'] = $true

    # 2) Get Application
    Invoke-Api -Method GET -Url (ApiUrl "/api/somiod/$appName") -ExpectStatus 200 -Description "Get application by name"

    # 3) Create Container
    Invoke-Api -Method POST -Url (ApiUrl "/api/somiod/$appName") -ExpectStatus 201 -Description "Create container under app" -Body @{ resourceName = $containerName }
    $created['Container'] = $true

    # 4) Get Container
    Invoke-Api -Method GET -Url (ApiUrl "/api/somiod/$appName/$containerName") -ExpectStatus 200 -Description "Get container by name"

    # 5) Create Content Instance
    Invoke-Api -Method POST -Url (ApiUrl "/api/somiod/$appName/$containerName") -ExpectStatus 201 -Description "Create content-instance" -Body @{ resourceName = $ciName; contentType = "text/plain"; content = "hello world" }
    $created['CI'] = $true

    # 6) Get Content Instance
    Invoke-Api -Method GET -Url (ApiUrl "/api/somiod/$appName/$containerName/$ciName") -ExpectStatus 200 -Description "Get content-instance by name"

    # 7) Create Subscription
    Invoke-Api -Method POST -Url (ApiUrl "/api/somiod/$appName/$containerName/subs") -ExpectStatus 201 -Description "Create subscription" -Body @{ resourceName = $subName; evt = 1; endpoint = "http://localhost:9999/notify" }
    $created['Sub'] = $true

    # 8) Get Subscription
    Invoke-Api -Method GET -Url (ApiUrl "/api/somiod/$appName/$containerName/subs/$subName") -ExpectStatus 200 -Description "Get subscription by name"

    Write-Ok "All create/get operations succeeded. Proceeding to cleanup..."
}
finally {
    if (-not $SkipCleanup) {
        Write-Info "Cleanup (best-effort) starting..."
        # Delete Subscription
        if ($created['Sub']) {
            try { Invoke-Api -Method DELETE -Url (ApiUrl "/api/somiod/$appName/$containerName/subs/$subName") -ExpectStatus 200 -Description "Delete subscription" } catch { Write-Err "Cleanup: delete subscription failed." }
        }
        # Delete Content Instance
        if ($created['CI']) {
            try { Invoke-Api -Method DELETE -Url (ApiUrl "/api/somiod/$appName/$containerName/$ciName") -ExpectStatus 200 -Description "Delete content-instance" } catch { Write-Err "Cleanup: delete content-instance failed." }
        }
        # Delete Container
        if ($created['Container']) {
            try { Invoke-Api -Method DELETE -Url (ApiUrl "/api/somiod/$appName/$containerName") -ExpectStatus 200 -Description "Delete container" } catch { Write-Err "Cleanup: delete container failed." }
        }
        # Delete Application
        if ($created['App']) {
            try { Invoke-Api -Method DELETE -Url (ApiUrl "/api/somiod/$appName") -ExpectStatus 200 -Description "Delete application" } catch { Write-Err "Cleanup: delete application failed." }
        }
        Write-Ok "Cleanup finished."
    } else {
        Write-Info "SkipCleanup enabled. Resources were NOT deleted."
    }
}

Write-Ok "End-to-end endpoint tests completed successfully."
