function Invoke-FTLDesktopProcess {
    param(
        [Parameter(Mandatory = $true)]
        [string] $FileName,

        [Parameter(Mandatory = $true)]
        [string] $Arguments,

        [Parameter(Mandatory = $true)]
        [string] $WorkingDirectory,

        [Parameter(Mandatory = $true)]
        [string] $PipeName,

        [Parameter(Mandatory = $true)]
        [int] $LauncherPid
    )

    $ErrorActionPreference = 'Stop'
    $pipe = New-Object System.IO.Pipes.NamedPipeClientStream('.', $PipeName, [System.IO.Pipes.PipeDirection]::Out)
    $pipe.Connect(30000)
    $writer = New-Object System.IO.StreamWriter($pipe, (New-Object System.Text.UTF8Encoding($false)))
    $writer.AutoFlush = $true
    $code = -1
    $process = $null

    try {
        $commandArguments = "/d /s /c `"`"$FileName`" $Arguments 2>&1`""
        $startInfo = New-Object System.Diagnostics.ProcessStartInfo
        $startInfo.FileName = Join-Path $env:SystemRoot 'System32\cmd.exe'
        $startInfo.Arguments = $commandArguments
        $startInfo.WorkingDirectory = $WorkingDirectory
        $startInfo.UseShellExecute = $false
        $startInfo.CreateNoWindow = $true
        $startInfo.RedirectStandardOutput = $true
        $process = [System.Diagnostics.Process]::Start($startInfo)
        $writer.WriteLine([char]1 + 'FTL_PID:' + $process.Id)

        $read = $process.StandardOutput.ReadLineAsync()
        while ($true) {
            if ($read.Wait(250)) {
                if ($null -eq $read.Result) {
                    break
                }
                $writer.WriteLine($read.Result)
                $read = $process.StandardOutput.ReadLineAsync()
            }
            elseif (-not (Get-Process -Id $LauncherPid -ErrorAction SilentlyContinue)) {
                throw 'FTToolsLauncher exited; API executor stopped.'
            }
        }

        $process.WaitForExit()
        $code = $process.ExitCode
    }
    catch {
        if ($null -ne $process -and -not $process.HasExited) {
            & taskkill.exe /PID $process.Id /T /F | Out-Null
        }
        try {
            $writer.WriteLine([char]1 + 'FTL_ERR:' + $_.Exception.Message)
        }
        catch {
        }
    }
    finally {
        try {
            $writer.WriteLine([char]1 + 'FTL_EXIT:' + $code)
            $writer.Dispose()
        }
        catch {
        }
    }

    exit $code
}
