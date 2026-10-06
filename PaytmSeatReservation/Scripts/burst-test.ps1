param(
  [string]$BaseUrl = "https://localhost:7074",
    [string]$ShowId = "",
    [int]$Requests = 20,
    [string]$Seat = "A1"
)

if ([string]::IsNullOrWhiteSpace($ShowId)) {
    Write-Host "Usage:"
    Write-Host ".\burst-test.ps1 -ShowId <SHOW_ID>"
    exit 1
}

$jobs = @()

Write-Host "Starting hot-seat test..."
Write-Host "Show: $ShowId"
Write-Host "Seat: $Seat"
Write-Host "Requests: $Requests"
Write-Host ""

for ($i = 1; $i -le $Requests; $i++) {

    $userId = "burst-user-$i"
    $idempotencyKey = "burst-$i"

    $jobs += Start-Job -ScriptBlock {
        param($BaseUrl, $ShowId, $Seat, $UserId, $IdempotencyKey)

        $body = @{
            seats = @($Seat)
         IdempotencyKey = $IdempotencyKey
        } | ConvertTo-Json

        try {
            $response = Invoke-WebRequest `
                -Uri "$BaseUrl/shows/$ShowId/reserve" `
                -Method POST `
                -Headers @{
                    "X-User-Id" = $UserId
                } `
                -ContentType "application/json" `
                -Body $body

            return [PSCustomObject]@{
                User   = $UserId
                Status = [int]$response.StatusCode
                Body   = $response.Content
            }
        }
        catch {
            $status = 500

            if ($_.Exception.Response) {
                $status = [int]$_.Exception.Response.StatusCode
            }

            return [PSCustomObject]@{
                User   = $UserId
                Status = $status
                Body   = $_.ErrorDetails.Message
            }
        }

    } -ArgumentList $BaseUrl, $ShowId, $Seat, $userId, $idempotencyKey
}

$results = $jobs | ForEach-Object {
    Wait-Job $_ | Out-Null
    Receive-Job $_
    Remove-Job $_
}

Write-Host ""
Write-Host "========== RESULT =========="

$results | Group-Object Status | Select-Object Name, Count | Format-Table

Write-Host ""
Write-Host "Successful reservations (201):"
($results | Where-Object { $_.Status -eq 201 }).Count

Write-Host ""
Write-Host "Conflicts (409):"
($results | Where-Object { $_.Status -eq 409 }).Count

Write-Host ""
Write-Host "Other responses:"
($results | Where-Object { $_.Status -ne 201 -and $_.Status -ne 409 }).Count

Write-Host ""
Write-Host "========== DETAILS =========="

$results | Select-Object User, Status | Sort-Object Status, User | Format-Table