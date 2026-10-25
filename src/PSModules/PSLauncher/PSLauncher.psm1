<#
.SYNOPSIS
    for PSLauncher
#>
using namespace System.IO;
using namespace System.Management.Automation;

Register-ArgumentCompleter -CommandName Start-Process -ParameterName FilePath -ScriptBlock {
    param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameters)
    $results = [CompletionCompleters]::CompleteCommand($wordToComplete, $null, [CommandTypes]::Application)
    if ($results.Count -gt 0) {
        return $results | Sort-Object -Property ListItemText -Unique
    }
    $results = [CompletionCompleters]::CompleteFilename($wordToComplete);
    if ($results.Count -gt 0) {
        $extList = $env:PATHEXT -split [Path]::PathSeparator
        return $results | Where-Object {
            $_.ResultType -eq 'ProviderContainer' -or [Path]::GetExtension($_.ToolTip) -in $extList
        }
    }
}
