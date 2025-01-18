import sounddevice as sd
import numpy as np
from scipy.io.wavfile import write
from whispercpp import Whisper

# Parameters
fs = 44100  # Sample rate
seconds = 10  # Duration of recording
model_size = 'tiny'
audio_path = "live_recording.wav"

# Function to transcribe audio file
def transcribe_audio(model_size, audio_path):
    try:
        # Initialize the Whisper model
        w = Whisper(model_size)

        # Transcribe the audio file
        result = w.transcribe(audio_path)

        # Extract the text from the transcription result
        text = w.extract_text(result)

        return text[0] if text else ""
    except Exception as e:
        return f"An error occurred: {str(e)}"

print("Recording...")
# Record audio
recording = sd.rec(int(seconds * fs), samplerate=fs, channels=1)  # Changed channels to 1
sd.wait()  # Wait until recording is finished
print("Recording finished")

# Save the recording to a WAV file
write(audio_path, fs, recording)

# Transcribe the audio and print the result
transcribed_text = transcribe_audio(model_size, audio_path)
print(transcribed_text)
