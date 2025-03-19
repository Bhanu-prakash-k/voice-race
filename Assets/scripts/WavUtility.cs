using System;
using System.IO;
using UnityEngine;

public static class WavUtility
{
    public static byte[] FromAudioClip(AudioClip clip)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            int sampleCount = clip.samples * clip.channels;
            int frequency = clip.frequency;
            int channelCount = clip.channels;

            // WAV header
            stream.Write(new byte[44], 0, 44);
            float[] samples = new float[sampleCount];
            clip.GetData(samples, 0);
            short[] intData = new short[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                intData[i] = (short)(samples[i] * short.MaxValue);
            }

            byte[] byteData = new byte[sampleCount * 2];
            Buffer.BlockCopy(intData, 0, byteData, 0, byteData.Length);
            stream.Write(byteData, 0, byteData.Length);

            // Update WAV header
            stream.Seek(0, SeekOrigin.Begin);
            WriteWavHeader(stream, sampleCount, frequency, channelCount);

            return stream.ToArray();
        }
    }

    private static void WriteWavHeader(Stream stream, int sampleCount, int frequency, int channels)
    {
        BinaryWriter writer = new BinaryWriter(stream);
        writer.Write(new char[] { 'R', 'I', 'F', 'F' });
        writer.Write(36 + sampleCount * 2);
        writer.Write(new char[] { 'W', 'A', 'V', 'E' });
        writer.Write(new char[] { 'f', 'm', 't', ' ' });
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(frequency);
        writer.Write(frequency * channels * 2);
        writer.Write((short)(channels * 2));
        writer.Write((short)16);
        writer.Write(new char[] { 'd', 'a', 't', 'a' });
        writer.Write(sampleCount * 2);
    }
}
