# Stop-DbTunnels.ps1
# Stops all running cloudflared processes (used for DB tunnels)

Write-Host "Stopping cloudflared tunnels on ports 5151, 5152, and 5153..."

# Define the target ports
$targetPorts = @(5151, 5152, 5153)

# Get all cloudflared processes
$cloudflaredProcs = Get-Process cloudflared -ErrorAction SilentlyContinue

foreach ($proc in $cloudflaredProcs) {
    # Get the command line for each process
    $cmdLine = (Get-CimInstance Win32_Process -Filter "ProcessId = $($proc.Id)").CommandLine
    if ($cmdLine) {
        foreach ($port in $targetPorts) {
            if ($cmdLine -match ":$port(\s|$)") {
                Write-Host "Stopping process Id $($proc.Id) (port $port)..."
                Stop-Process -Id $proc.Id -Force
                break
            }
        }
    }
}

Write-Host "Selected cloudflared tunnels stopped."
