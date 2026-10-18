$ErrorActionPreference = 'Stop'
$sourceDoc = Join-Path (Get-Location) 'output/character/마왕님은_공사중_캐릭터_외형설정집_기술과수치_보완본.docx'
$qaFolder = Join-Path (Get-Location) 'tmp/pdfs/character_skills_stats_qa'
New-Item -ItemType Directory -Path $qaFolder -Force | Out-Null
$wordInstance = $null
$openedDocument = $null
try {
 $wordInstance = New-Object -ComObject Word.Application
 $wordInstance.Visible = $false
 $wordInstance.DisplayAlerts = 0
 $openedDocument = $wordInstance.Documents.Open($sourceDoc, $false, $true)
 $openedDocument.Repaginate()
 $openedDocument.ExportAsFixedFormat((Join-Path $qaFolder 'word-preview.pdf'),17)
 Write-Output ('Rendered pages: ' + $openedDocument.ComputeStatistics(2))
} finally {
 if ($null -ne $openedDocument) { $openedDocument.Close(0) }
 if ($null -ne $wordInstance) { $wordInstance.Quit() }
}






