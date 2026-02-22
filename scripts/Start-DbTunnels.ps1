# Start-DbTunnels.ps1
# Launch Cloudflare Access TCP tunnels for all Postgres environments

# Function to start a tunnel in a background process
function Start-Tunnel {
    param (
        [string]$Hostname,
        [int]$LocalPort
    )

    Write-Host "Starting tunnel for $Hostname on local port $LocalPort..."
    Start-Process -NoNewWindow -FilePath "cloudflared.exe" `
        -ArgumentList "access", "tcp", "--hostname", $Hostname, "--url", "localhost:$LocalPort"
}

# Paths and hostnames
$tunnels = @(
    @{ Hostname = "dev-db.production-calculator.com";  Port = 5151 },
    @{ Hostname = "staging-db.production-calculator.com"; Port = 5152 },
    @{ Hostname = "db.production-calculator.com"; Port = 5153 }
)

# Start each tunnel
foreach ($t in $tunnels) {
    Start-Tunnel -Hostname $t.Hostname -LocalPort $t.Port
}

Write-Host "`nAll tunnels started! You can now connect via localhost:<port> in DataGrip."
Write-Host "Close these tunnels later by ending the associated 'cloudflared' processes."