# Flask demo

This Flask app demonstrates Azure Container Apps revisions. Python is required.

From the repository root, run the following commands in Windows PowerShell:

```powershell
py -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install -r demo\requirements.txt
python demo\app.py
```

If you already have a virtual environment, activate it instead of creating a new one.

Open <http://localhost:8080> in a browser. Version information is available as JSON at <http://localhost:8080/version>.
