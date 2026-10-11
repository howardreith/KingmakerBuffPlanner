using System;
using System.IO;

namespace KingmakerBuffPlanner.RuntimeTesting
{
    internal enum NativeUiCaptureStatus { Pending, Completed, TimedOut }

    // The engine writes screenshots at frame end. A fresh output path, a
    // later frame and an exclusively readable PNG terminator establish the
    // capture boundary; they do not judge the image's visual quality.
    internal sealed class NativeUiCaptureBoundary
    {
        private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };
        private static readonly byte[] EndChunk = { 0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130 };
        private readonly long _timeoutMilliseconds;
        private NativeUiCaptureStatus _status;

        internal string Path { get; private set; }
        internal int RequestFrame { get; private set; }

        internal NativeUiCaptureBoundary(string path, int requestFrame, long timeoutMilliseconds)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("A capture path is required.", "path");
            if (timeoutMilliseconds <= 0) throw new ArgumentOutOfRangeException("timeoutMilliseconds");
            if (File.Exists(path)) throw new IOException("The capture output already exists: " + path);
            Path = path;
            RequestFrame = requestFrame;
            _timeoutMilliseconds = timeoutMilliseconds;
        }

        internal NativeUiCaptureStatus Observe(int frame, long elapsedMilliseconds)
        {
            if (_status != NativeUiCaptureStatus.Pending) return _status;
            if (elapsedMilliseconds >= _timeoutMilliseconds)
                return _status = NativeUiCaptureStatus.TimedOut;
            if (frame > RequestFrame && CompletePng())
                _status = NativeUiCaptureStatus.Completed;
            return _status;
        }

        private bool CompletePng()
        {
            try
            {
                using (var stream = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    if (stream.Length < 45 || !ReadMatches(stream, Signature)) return false;
                    stream.Seek(-EndChunk.Length, SeekOrigin.End);
                    return ReadMatches(stream, EndChunk);
                }
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        private static bool ReadMatches(Stream stream, byte[] expected)
        {
            foreach (byte value in expected)
                if (stream.ReadByte() != value) return false;
            return true;
        }
    }
}
