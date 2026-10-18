$ErrorActionPreference = 'Stop'
$sourceDoc = Join-Path (Get-Location) 'output/character/캐릭터의_외형_설정_이형원_5946158_서식통일.docx'
$qaFolder = Join-Path (Get-Location) 'output/pdf'
New-Item -ItemType Directory -Path $qaFolder -Force | Out-Null
$wordInstance = $null
$openedDocument = $null
try {
 $wordInstance = New-Object -ComObject Word.Application
 $wordInstance.Visible = $false
 $wordInstance.DisplayAlerts = 0
 $openedDocument = $wordInstance.Documents.Open($sourceDoc, $false, $true)
 $openedDocument.Repaginate()
 $openedDocument.ExportAsFixedFormat((Join-Path $qaFolder '캐릭터의_외형_설정_이형원_5946158_서식통일.pdf'),17)
 Write-Output ('Rendered pages: ' + $openedDocument.ComputeStatistics(2))
} finally {
 if ($null -ne $openedDocument) { $openedDocument.Close(0) }
 if ($null -ne $wordInstance) { $wordInstance.Quit() }
}






