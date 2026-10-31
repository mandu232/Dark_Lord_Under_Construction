$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$keys = @('ArcaneFocus','AcidVat','MeleeTotem','WarBanner','IronWard','ArcaneWard','GoldMint','BloodAltar','BloodstoneExtractor','MinionRecruitment','MinionQuarters')
$preview = New-Object System.Drawing.Bitmap 768,576
$graphics = [System.Drawing.Graphics]::FromImage($preview)
$graphics.Clear([System.Drawing.Color]::FromArgb(17,21,29))
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
for($i = 0; $i -lt $keys.Count; $i++) {
    $source = [System.Drawing.Image]::FromFile((Join-Path $pwd ('Assets/Sprite/Dungeon/Facilities/' + $keys[$i] + '.png')))
    $graphics.DrawImage($source, ($i % 4) * 192, [math]::Floor($i / 4) * 192, 192, 192)
    $source.Dispose()
}
$palette = @('11151D','202B33','354750','87918B','DDD2AA','4A1E30','8D293D','CF4B55','AD783F','6F9A67','8A77A0')
for($i = 0; $i -lt $palette.Count; $i++) {
    $brush = New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml('#' + $palette[$i]))
    $graphics.FillRectangle($brush, 594 + ($i % 4) * 36, 428 + [math]::Floor($i / 4) * 36, 30, 30)
    $brush.Dispose()
}
$preview.Save((Join-Path $pwd 'Docs/AI/FacilityArtPreview.png'))
$graphics.Dispose()
$preview.Dispose()
