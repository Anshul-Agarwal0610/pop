import os
from flask import Flask, jsonify

app = Flask(__name__)

VERSION = os.getenv("APP_VERSION", "v1")
MESSAGE = os.getenv("APP_MESSAGE", "ACA revision demo")


@app.route("/")
def home():
    return f"""
    <h1>Azure Container Apps Revision Demo</h1>
    <h2>Version: {VERSION}</h2>
    <p>{MESSAGE}</p>
    """


@app.route("/version")
def version():
    return jsonify({
        "version": VERSION,
        "message": MESSAGE
    })


if __name__ == "__main__":
    app.run(host="0.0.0.0", port=8080)
