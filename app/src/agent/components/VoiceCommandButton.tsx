import React, { useState, useRef } from 'react';
import { Mic, MicOff, Loader2 } from 'lucide-react';
import { agentApi } from '../services/agentApi';

interface VoiceCommandButtonProps {
  onTranscript: (text: string) => void;
  disabled?: boolean;
}

export const VoiceCommandButton: React.FC<VoiceCommandButtonProps> = ({
  onTranscript,
  disabled = false,
}) => {
  const [isRecording, setIsRecording] = useState(false);
  const [isProcessing, setIsProcessing] = useState(false);
  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const chunksRef = useRef<Blob[]>([]);

  const startRecording = async () => {
    // 1. Try Browser Web Speech API first for zero-latency instant recognition
    const windowWithSpeech = window as unknown as {
      webkitSpeechRecognition?: any;
      SpeechRecognition?: any;
    };
    const SpeechRecognition = windowWithSpeech.SpeechRecognition || windowWithSpeech.webkitSpeechRecognition;

    if (SpeechRecognition) {
      try {
        const recognition = new SpeechRecognition();
        recognition.lang = 'en-US';
        recognition.continuous = false;
        recognition.interimResults = false;

        recognition.onstart = () => setIsRecording(true);
        recognition.onresult = (event: any) => {
          const transcript = event.results[0][0].transcript;
          if (transcript) onTranscript(transcript);
        };
        recognition.onerror = () => setIsRecording(false);
        recognition.onend = () => setIsRecording(false);

        recognition.start();
        return;
      } catch {
        // Fallback to MediaRecorder audio stream
      }
    }

    // 2. MediaRecorder stream fallback to /api/agent/transcribe
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      const recorder = new MediaRecorder(stream);
      mediaRecorderRef.current = recorder;
      chunksRef.current = [];

      recorder.ondataavailable = (e) => {
        if (e.data.size > 0) chunksRef.current.push(e.data);
      };

      recorder.onstop = async () => {
        setIsProcessing(true);
        try {
          const blob = new Blob(chunksRef.current, { type: 'audio/webm' });
          const res = await agentApi.transcribeAudio(blob);
          if (res?.transcribedText) {
            onTranscript(res.transcribedText);
          }
        } catch (err) {
          console.warn('Voice transcription fallback failed:', err);
        } finally {
          setIsProcessing(false);
          setIsRecording(false);
          stream.getTracks().forEach((track) => track.stop());
        }
      };

      recorder.start();
      setIsRecording(true);
    } catch (err) {
      console.warn('Microphone access denied:', err);
      setIsRecording(false);
    }
  };

  const stopRecording = () => {
    if (mediaRecorderRef.current && isRecording) {
      mediaRecorderRef.current.stop();
    } else {
      setIsRecording(false);
    }
  };

  return (
    <button
      type="button"
      onClick={isRecording ? stopRecording : startRecording}
      disabled={disabled || isProcessing}
      title={isRecording ? 'Click to stop listening' : 'Hold or click to speak voice command'}
      className={`relative p-2.5 rounded-lg border transition-all duration-200 flex items-center justify-center ${
        isRecording
          ? 'bg-red-500/20 text-red-500 border-red-500/40 animate-pulse'
          : 'bg-bg-tertiary hover:bg-bg-secondary text-text-secondary hover:text-text-primary border-border-primary'
      }`}
    >
      {isProcessing ? (
        <Loader2 className="w-5 h-5 animate-spin text-accent-primary" />
      ) : isRecording ? (
        <MicOff className="w-5 h-5 text-red-500" />
      ) : (
        <Mic className="w-5 h-5" />
      )}
      {isRecording && (
        <span className="absolute -top-1 -right-1 flex h-3 w-3">
          <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-red-400 opacity-75"></span>
          <span className="relative inline-flex rounded-full h-3 w-3 bg-red-500"></span>
        </span>
      )}
    </button>
  );
};
