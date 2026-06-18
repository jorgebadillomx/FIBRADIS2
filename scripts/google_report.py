"""
google_report.py - Genera reporte SEO como HTML imprimible (A4).
Intenta usar el script global primero; si falla, usa implementacion local.

Uso:
    python scripts/google_report.py --type full [--input <dir>] [--output <archivo>]
"""
import sys, subprocess, os, re
from pathlib import Path
from datetime import datetime

GLOBAL = r"C:\Users\jorge\.claude\skills\seo\scripts\google_report.py"

def try_global():
    result = subprocess.run([sys.executable, GLOBAL] + sys.argv[1:], env=os.environ)
    return result.returncode == 0

def inline_report():
    args = sys.argv[1:]
    input_dir = Path(".")
    output_file = None
    i = 0
    while i < len(args):
        if args[i] == "--input" and i + 1 < len(args):
            input_dir = Path(args[i + 1]); i += 2
        elif args[i] == "--output" and i + 1 < len(args):
            output_file = args[i + 1]; i += 2
        else:
            i += 1

    report_md = (input_dir / "FULL-AUDIT-REPORT.md").read_text(encoding="utf-8") if (input_dir / "FULL-AUDIT-REPORT.md").exists() else ""
    action_md = (input_dir / "ACTION-PLAN.md").read_text(encoding="utf-8") if (input_dir / "ACTION-PLAN.md").exists() else ""
    date_str = datetime.utcnow().strftime("%Y-%m-%d %H:%M UTC")
    date_slug = datetime.utcnow().strftime("%Y%m%d")

    if not output_file:
        output_file = str(input_dir / f"SEO-REPORT-{date_slug}.html")

    def md_to_html(md):
        lines, out, in_code = md.split("\n"), [], False
        for line in lines:
            if line.startswith("```"):
                out.append("</pre>" if in_code else '<pre class="cb">'); in_code = not in_code; continue
            if in_code:
                out.append(line.replace("<","&lt;").replace(">","&gt;")); continue
            m = re.match(r"^(#{1,6})\s+(.*)", line)
            if m:
                l, t = len(m.group(1)), inline_fmt(m.group(2))
                out.append(f"<h{l}>{t}</h{l}>"); continue
            if re.match(r"^---+$", line.strip()):
                out.append("<hr>"); continue
            m = re.match(r"^\s*[-*+]\s+(.*)", line)
            if m:
                out.append(f"<li>{inline_fmt(m.group(1))}</li>"); continue
            if not line.strip():
                out.append("<br>"); continue
            out.append(f"<p>{inline_fmt(line)}</p>")
        return "\n".join(out)

    def inline_fmt(t):
        t = re.sub(r"\*\*(.+?)\*\*", r"<strong>\1</strong>", t)
        t = re.sub(r"\*(.+?)\*", r"<em>\1</em>", t)
        t = re.sub(r"`(.+?)`", r"<code>\1</code>", t)
        return t

    css = """
*{box-sizing:border-box;margin:0;padding:0}
body{font-family:'Segoe UI',Arial,sans-serif;font-size:13px;color:#1a1a2e;line-height:1.6;background:#fff}
.page{max-width:820px;margin:0 auto;padding:40px}
h1{font-size:26px;color:#0f3460;border-bottom:3px solid #e94560;padding-bottom:8px;margin:28px 0 14px}
h2{font-size:19px;color:#16213e;border-left:4px solid #e94560;padding-left:10px;margin:22px 0 10px}
h3{font-size:15px;color:#0f3460;margin:16px 0 6px}
h4,h5,h6{font-size:13px;color:#333;margin:10px 0 5px}
p{margin:5px 0}li{margin:3px 0 3px 20px}
code{background:#f4f4f4;padding:1px 4px;border-radius:3px;font-family:monospace;font-size:12px}
pre.cb{background:#1a1a2e;color:#e0e0e0;padding:14px;border-radius:6px;font-size:12px;margin:10px 0;overflow-x:auto;white-space:pre-wrap}
hr{border:none;border-top:1px solid #ddd;margin:18px 0}
.cover{text-align:center;padding:60px 0;border-bottom:2px solid #e94560;margin-bottom:36px}
.cover h1{border:none;font-size:32px;margin-bottom:8px}
.score{display:inline-block;padding:18px 28px;border-radius:50%;font-size:32px;font-weight:bold;color:white;background:#d97706;margin:16px}
@media print{.page{padding:20px}a{color:inherit;text-decoration:none}}
"""

    html = f"""<!DOCTYPE html>
<html lang="es">
<head><meta charset="UTF-8"><title>Reporte SEO — fibrasinmobiliarias.com</title>
<style>{css}</style></head>
<body><div class="page">
<div class="cover">
  <h1>Reporte SEO Completo</h1>
  <div style="font-size:16px;color:#666">fibrasinmobiliarias.com</div>
  <div class="score">57</div>
  <div style="font-size:13px;color:#888">Health Score /100 &nbsp;|&nbsp; {date_str}</div>
</div>
<h1>Reporte de Auditoria</h1>
{md_to_html(report_md)}
<div style="page-break-before:always;margin-top:40px"></div>
<h1>Plan de Accion</h1>
{md_to_html(action_md)}
<hr>
<p style="text-align:center;color:#999;font-size:11px;margin-top:24px">
  Fibras Inmobiliarias SEO Audit &middot; {date_str}
</p>
</div></body></html>"""

    Path(output_file).write_text(html, encoding="utf-8")
    print(f"Reporte HTML generado: {output_file} ({len(html):,} chars)")

    try:
        import weasyprint
        pdf = output_file.replace(".html", ".pdf")
        weasyprint.HTML(filename=output_file).write_pdf(pdf)
        print(f"PDF generado: {pdf}")
    except ImportError:
        print("PDF omitido (instala weasyprint para PDF: pip install weasyprint)")

if __name__ == "__main__":
    if not try_global():
        inline_report()
