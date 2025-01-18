from flask import Flask, request, send_file
import subprocess
import os

app = Flask(__name__)

@app.route('/generate_wav', methods=['POST'])
def generate_wav():
    # Get the text from the request
    text = request.form.get('text')
    
    if not text:
        return 'No text provided', 400
    
    # Define the output WAV file path
    output_file = 'welcome.wav'

    # Build the command as a list of arguments
    command = [
        'piper',
        '--model', 'en_US-lessac-medium',
        '--output_file', output_file
    ]
    
    # Run the command using subprocess
    process = subprocess.Popen(command, stdin=subprocess.PIPE, text=True)
    process.communicate(input=text)
    
    # Check if the file was created
    if not os.path.exists(output_file):
        return 'Failed to generate WAV file', 500
    
    # Send the WAV file back to the client
    return send_file(output_file, mimetype='audio/wav')

if __name__ == '__main__':
    app.run(port=1234)

## use py11 conda, to run this script, run the pyproject pagekite