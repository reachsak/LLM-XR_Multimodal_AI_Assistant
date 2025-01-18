from flask import Flask, request, jsonify, send_file
from whispercpp import Whisper
import os
import subprocess
import io
from pydub import AudioSegment

app = Flask(__name__)

w = Whisper('tiny')

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

@app.route('/text-to-speech', methods=['POST'])
def text_to_speech():
    data = request.get_json()
    text = data.get('text')
    model = 'en_US-lessac-medium'  # You can adjust this as needed

    try:
        command = f"echo '{text}' | piper --model {model} --output_file -"
        result = subprocess.run(command, shell=True, check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

        if result.returncode == 0:
            audio_data = io.BytesIO(result.stdout)
            audio_segment = AudioSegment.from_file(audio_data, format="wav")
            audio_data.seek(0)
            return send_file(audio_data, mimetype="audio/wav")
        else:
            return jsonify({'error': f"Failed to synthesize speech: {result.stderr.decode()}"}), 500
    except subprocess.CalledProcessError as e:
        return jsonify({'error': f"Error occurred: {e.stderr.decode()}"}), 500

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=8080)
