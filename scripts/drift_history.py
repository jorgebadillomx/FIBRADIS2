"""Wrapper — delega al script global de SEO skills."""
import sys, subprocess, os
GLOBAL = r"C:\Users\jorge\.claude\skills\seo\scripts\drift_history.py"
result = subprocess.run([sys.executable, GLOBAL] + sys.argv[1:], env=os.environ)
sys.exit(result.returncode)
