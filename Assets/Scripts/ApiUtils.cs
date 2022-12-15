using System;
using UnityEngine;

public static class ApiUtils
{
    public static string Tob64String(this Texture2D texture)
    {
        if (texture == null) return null;
        return Convert.ToBase64String(texture.EncodeToPNG());
    }
}

public struct ProgressData
{
    public float percent;
    public string image;
    public int steps;
    public bool skipped;
    public bool interrupted;
    public string job;
    public int job_count;
    public int job_no;
    public int sampling_step;
    public int sampling_steps;

    public string Info
    {
        get => $"{percent * 100:F2}%\t\t\tjob {job_no + 1}/{job_count}\t\t\tstep {sampling_step}/{sampling_steps}";
    }
}