from flask import Flask, request, send_file
import os
import qrcode
from datetime import datetime

app = Flask(__name__)

UPLOAD_FOLDER = "uploaded"
QR_FOLDER = "qr"
os.makedirs(UPLOAD_FOLDER, exist_ok=True)
os.makedirs(QR_FOLDER, exist_ok=True)

@app.route("/upload", methods=["POST"])
def upload():
    file = request.files["image"]
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    filename = f"{timestamp}.png"
    filepath = os.path.join(UPLOAD_FOLDER, filename)
    file.save(filepath)

    qr_path = os.path.join(QR_FOLDER, f"qr_{filename}")
    qr_data_url = f"http://192.168.0.44:8501/image/{filename}"

    qr = qrcode.make(qr_data_url)
    qr.save(qr_path)

    return f"http://192.168.0.44:8501/qr_image/{filename}"

@app.route("/image/<name>")
def image(name):
    return send_file(os.path.join(UPLOAD_FOLDER, name))

@app.route("/qr_image/<name>")
def qr_image(name):
    return send_file(os.path.join(QR_FOLDER, f"qr_{name}"))

if __name__ == "__main__":
    app.run(host="0.0.0.0", port=8501, debug=True)
