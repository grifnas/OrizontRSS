using System.Runtime.InteropServices;
using System.Text;

var samples = 0;
Native.SynthCallback callback = (wave, count, events) => { samples += Math.Max(0, count); return 0; };
var engineDirectory = Path.Combine(AppContext.BaseDirectory, "SpeechEngines", "eSpeakNG");
var sampleRate = Native.espeak_Initialize(1, 0, engineDirectory, 0);
if (sampleRate <= 0) return Fail("Inițializarea eSpeak NG a eșuat.");
Native.espeak_SetSynthCallback(callback);
var voices = Native.espeak_ListVoices(IntPtr.Zero);
var romanianListed = false;
var voiceCount = 0;
for (var index = 0; ; index++)
{
    var pointer = Marshal.ReadIntPtr(voices, index * IntPtr.Size);
    if (pointer == IntPtr.Zero) break;
    var voice = Marshal.PtrToStructure<Native.Voice>(pointer);
    var identifier = Marshal.PtrToStringUTF8(voice.Identifier) ?? string.Empty;
    if (identifier.StartsWith("mb/", StringComparison.OrdinalIgnoreCase) || identifier.StartsWith("mb\\", StringComparison.OrdinalIgnoreCase)) continue;
    var language = voice.Languages == IntPtr.Zero || Marshal.ReadByte(voice.Languages) == 0
        ? string.Empty
        : Marshal.PtrToStringUTF8(IntPtr.Add(voice.Languages, 1)) ?? string.Empty;
    voiceCount++;
    if (string.Equals(language, "ro", StringComparison.OrdinalIgnoreCase)) romanianListed = true;
}
if (!romanianListed) return Fail("Vocea română nu apare în lista vocilor eSpeak NG.");
if (Native.espeak_SetVoiceByName("ro") != 0) return Fail("Vocea română eSpeak NG nu a fost găsită.");
var text = Encoding.UTF8.GetBytes("Orizont RSS citește corect în limba română.\0");
var result = Native.espeak_Synth(text, (nuint)text.Length, 0, 1, 0, 1u | 0x1000u, IntPtr.Zero, IntPtr.Zero);
if (result != 0) return Fail($"Sinteza eSpeak NG a returnat eroarea {result}.");
Native.espeak_Synchronize();
GC.KeepAlive(callback);
if (samples < sampleRate / 4) return Fail($"Au fost obținute prea puține eșantioane audio: {samples}.");
Console.WriteLine($"OK: eSpeak NG, {voiceCount} voci, voce ro, {sampleRate} Hz, {samples} eșantioane.");
return 0;

static int Fail(string message) { Console.Error.WriteLine(message); return 1; }

static class Native
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SynthCallback(IntPtr wave, int sampleCount, IntPtr events);

    [StructLayout(LayoutKind.Sequential)]
    internal struct Voice
    {
        internal IntPtr Name;
        internal IntPtr Languages;
        internal IntPtr Identifier;
        internal byte Gender;
        internal byte Age;
        internal byte Variant;
        internal byte Reserved;
        internal int Score;
        internal IntPtr Spare;
    }

    [DllImport("libespeak-ng.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    internal static extern int espeak_Initialize(int output, int bufferLength, string path, int options);
    [DllImport("libespeak-ng.dll", CallingConvention = CallingConvention.Cdecl)]
    internal static extern void espeak_SetSynthCallback(SynthCallback callback);
    [DllImport("libespeak-ng.dll", CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr espeak_ListVoices(IntPtr voiceSpec);
    [DllImport("libespeak-ng.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    internal static extern int espeak_SetVoiceByName(string name);
    [DllImport("libespeak-ng.dll", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int espeak_Synth(byte[] text, nuint size, uint position, int positionType, uint endPosition, uint flags, IntPtr uniqueIdentifier, IntPtr userData);
    [DllImport("libespeak-ng.dll", CallingConvention = CallingConvention.Cdecl)]
    internal static extern int espeak_Synchronize();
}
