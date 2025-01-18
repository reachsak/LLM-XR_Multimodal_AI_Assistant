from flask import Flask, request, jsonify
from whispercpp import Whisper
import os

app = Flask(__name__)

w = Whisper('medium')

@app.route('/transcribe', methods=['POST'])
def transcribe():
    if 'file' not in request.files:
        return jsonify({'error': 'No file part in the request'}), 400

    file = request.files['file']
    
    if file.filename == '':
        return jsonify({'error': 'No selected file'}), 400

    file_path = os.path.join('/tmp', file.filename)
    file.save(file_path)

    result = w.transcribe(file_path)
    text = w.extract_text(result)

    os.remove(file_path)

    return jsonify({'text': text[0]})

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=8080)
## use crewAI conda, to run this script, run the py11 pagekite