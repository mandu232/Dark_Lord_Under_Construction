param([string]$Manifest = 'Temp/FacilityArtSources/manifest.json')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies @([System.Drawing.Bitmap].Assembly.Location, [System.Drawing.Color].Assembly.Location, [System.Collections.Generic.HashSet[int]].Assembly.Location) -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
public static class FacilityPixelExport {
    public static string Export(string source, string destination) {
        string[] hex={"11151D","202B33","354750","87918B","DDD2AA","4A1E30","8D293D","CF4B55","AD783F","6F9A67","8A77A0"};
        var palette=Array.ConvertAll(hex,h=>Color.FromArgb(255,Convert.ToInt32(h.Substring(0,2),16),Convert.ToInt32(h.Substring(2,2),16),Convert.ToInt32(h.Substring(4,2),16)));
        using(var src=new Bitmap(source)) using(var dst=new Bitmap(32,32,PixelFormat.Format32bppArgb)) {
            int left=src.Width, top=src.Height, right=-1, bottom=-1;
            for(int y=0;y<src.Height;y++) for(int x=0;x<src.Width;x++) if(src.GetPixel(x,y).A>=128) {left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x);bottom=Math.Max(bottom,y);}
            if(right<left) throw new Exception("Empty sprite: "+source);
            int sw=right-left+1,sh=bottom-top+1;
            double scale=Math.Min(28.0/sw,28.0/sh);
            int w=Math.Max(1,(int)Math.Round(sw*scale)),h=Math.Max(1,(int)Math.Round(sh*scale));
            int ox=(32-w)/2,oy=30-h;
            for(int y=0;y<h;y++) for(int x=0;x<w;x++) {
                var c=src.GetPixel(Math.Min(right,left+(int)((x+.5)*sw/w)),Math.Min(bottom,top+(int)((y+.5)*sh/h)));
                if(c.A<128) continue;
                int best=0;double distance=double.MaxValue;
                for(int i=0;i<palette.Length;i++) {var p=palette[i];if(i==9 && !(c.G>c.R*1.1 && c.G>c.B*1.1))continue;double dr=c.R-p.R,dg=c.G-p.G,db=c.B-p.B;double d=dr*dr+dg*dg+db*db;if(d<distance){distance=d;best=i;}}
                dst.SetPixel(ox+x,oy+y,palette[best]);
            }
            var unique=new HashSet<int>();int opaque=0;
            for(int y=0;y<32;y++) for(int x=0;x<32;x++){var c=dst.GetPixel(x,y);unique.Add(c.ToArgb());if(c.A>0)opaque++;if(c.A!=0&&c.A!=255)throw new Exception("Partial alpha");}
            if(unique.Count>12)throw new Exception("Palette limit exceeded");
            dst.Save(destination,ImageFormat.Png);
            return "32x32, "+unique.Count+" RGBA colors including transparency, "+opaque+" opaque pixels";
        }
    }
}
'@
$entries = Get-Content -LiteralPath $Manifest -Raw | ConvertFrom-Json
foreach($entry in $entries) {
    $target = Join-Path $pwd ('Assets/Sprite/Dungeon/Facilities/' + $entry.key + '.png')
    $result = [FacilityPixelExport]::Export($entry.source, $target)
    Write-Output ($entry.key + ': ' + $result)
}


