from whispercpp import Whisper

w = Whisper('tiny')



result = w.transcribe("/Users/reachsak/whisper.cpp/samples/x.wav")
text = w.extract_text(result)

print(type(text[0]))
print((text[0]))

#pip install piper-tts --no-deps