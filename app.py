from flask import Flask, request, send_file, render_template, jsonify
import os
import qrcode
from datetime import datetime
import sqlite3
import socket

app = Flask(__name__)

# 설정
UPLOAD_FOLDER = "uploaded"
QR_FOLDER = "qr"
DB_FILE = "gallery.db"
address = "http://192.168.0.44:8501"
#HOST_IP = "192.168.0.44" 
#PORT = 8501

os.makedirs(UPLOAD_FOLDER, exist_ok=True)
os.makedirs(QR_FOLDER, exist_ok=True)

# 데이터베이스 초기화
def init_db():
    conn = sqlite3.connect(DB_FILE)
    c = conn.cursor()
    # 이미지가 존재하면 좋아요 수를 저장하는 테이블 생성
    c.execute('''CREATE TABLE IF NOT EXISTS images 
                 (filename TEXT PRIMARY KEY, timestamp TEXT, likes INTEGER DEFAULT 0)''')
    conn.commit()
    conn.close()

init_db()

def get_db_connection():
    conn = sqlite3.connect(DB_FILE)
    conn.row_factory = sqlite3.Row
    return conn

def sync_existing_images():
    print("Checking for existing images...")
    conn = get_db_connection()
    
    # DB에 있는 파일 목록 가져오기
    db_files = set(row['filename'] for row in conn.execute('SELECT filename FROM images').fetchall())
    
    # 실제 폴더에 있는 파일 목록 가져오기
    if os.path.exists(UPLOAD_FOLDER):
        physical_files = [f for f in os.listdir(UPLOAD_FOLDER) if f.lower().endswith(('.png', '.jpg', '.jpeg', '.gif'))]
        
        for filename in physical_files:
            # 1. DB에 없는 경우 추가
            if filename not in db_files:
                print(f" -> Syncing new found image: {filename}")
                # 파일명에서 날짜 추출 시도 (형식이 안 맞으면 파일 수정 시간 사용)
                try:
                    # 파일명이 YYYYMMDD_HHMMSS.png 형식이라고 가정
                    timestamp_str = filename.rsplit('.', 1)[0]
                except:
                    timestamp_str = datetime.now().strftime("%Y%m%d_%H%M%S")
                
                conn.execute('INSERT INTO images (filename, timestamp, likes) VALUES (?, ?, ?)', 
                             (filename, timestamp_str, 0))
            
            # 2. QR 코드가 없는 경우 생성 (DB에 있더라도 파일이 없으면 재생성)
            qr_filename = f"qr_{filename}"
            qr_path = os.path.join(QR_FOLDER, qr_filename)
            
            if not os.path.exists(qr_path):
                print(f" -> Generating missing QR for: {filename}")
                qr_data_url = f"http://{HOST_IP}:{PORT}/image/{filename}"
                qr = qrcode.make(qr_data_url)
                qr.save(qr_path)

    conn.commit()
    conn.close()
    print("Sync complete.")

# 초기화 및 동기화 실행
init_db()
sync_existing_images()


@app.route("/")
def gallery():
    conn = get_db_connection()
    # 최신순으로 이미지 가져오기
    images = conn.execute('SELECT * FROM images ORDER BY timestamp DESC').fetchall()
    conn.close()
    return render_template("index.html", images=images, address = address)

@app.route("/upload", methods=["POST"])
def upload():
    if "image" not in request.files:
        return "No image part", 400
        
    file = request.files["image"]
    if file.filename == '':
        return "No selected file", 400

    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    filename = f"{timestamp}.png"
    filepath = os.path.join(UPLOAD_FOLDER, filename)
    file.save(filepath)

    # QR 생성
    qr_path = os.path.join(QR_FOLDER, f"qr_{filename}")
    qr_data_url = f"{address}/image/{filename}"
    
    qr = qrcode.make(qr_data_url)
    qr.save(qr_path)

    # DB에 정보 저장
    conn = get_db_connection()
    conn.execute('INSERT INTO images (filename, timestamp, likes) VALUES (?, ?, ?)', 
                 (filename, timestamp, 0))
    conn.commit()
    conn.close()

    return f"{address}/qr_image/{filename}"

@app.route("/like/<filename>", methods=["POST"])
def like_image(filename):
    conn = get_db_connection()
    conn.execute('UPDATE images SET likes = likes + 1 WHERE filename = ?', (filename,))
    conn.commit()
    
    # 업데이트된 좋아요 수 반환
    updated_row = conn.execute('SELECT likes FROM images WHERE filename = ?', (filename,)).fetchone()
    conn.close()
    
    if updated_row:
        return jsonify({"likes": updated_row["likes"]})
    return jsonify({"error": "Image not found"}), 404

@app.route("/image/<name>")
def image(name):
    return send_file(os.path.join(UPLOAD_FOLDER, name))

@app.route("/qr_image/<name>")
def qr_image(name):
    return send_file(os.path.join(QR_FOLDER, f"qr_{name}"))

if __name__ == "__main__":
    
    print(f"Server starting at https://192.168.0.44:8501")
    app.run(host="0.0.0.0", port=8501, debug=True)