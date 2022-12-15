using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public enum ResizeMode
{
    Smoosh = 0, Crop = 1, FitAndFill = 2,
}

[Serializable]
public class RequestData
{
    static RequestData() => fieldInfo = typeof(RequestData).GetFields(BindingFlags.Instance | BindingFlags.Public);
    private static FieldInfo[] fieldInfo;

    [Range(0, 1)] public float denoising_strength = .5f;
    public ResizeMode resize_mode = 0;
    [Range(0, 20)] public int mask_blur = 5;

    public List<Texture2D> init_images = new();
    public Texture2D mask = null;

    [Range(0, 5)] public int inpainting_fill = 1;
    public bool inpaint_full_res = false;
    public int inpaint_full_res_padding = 0;
    public int inpainting_mask_invert = 0;
    [Range(512, 1024)] public int width = 512;
    [Range(512, 1024)] public int height = 512;
    [Range(512, 1024)] public int firstphase_width = 0;
    [Range(512, 1024)] public int firstphase_height = 0;
    [Range(1, 10)] public int batch_size = 1;
    [Range(1, 10)] public int n_iter = 1;
    [Range(1, 100)] public int steps = 50;
    [Range(1, 30)] public float cfg_scale = 7.5f;
    [Multiline(2)] public string prompt = "";
    [Multiline(2)] public string negative_prompt = "";
    public List<string> style = new();
    public int seed = -1;
    public int subseed = -1;
    public float subseed_strength = 0;
    public int seed_resize_from_h = -1;
    public int seed_resize_from_w = -1;
    public bool restore_faces = false;
    public bool tiling = false;
    public bool enable_hr = false;
    public float eta = 0;
    public float s_noise = 1;
    public string sampler_index = "Euler a";

    public virtual string SerializeFields()
    {
        Dictionary<string, object> postData = new();
        foreach (var field in fieldInfo)
        {
            var value = field.GetValue(this);
            switch (value)
            {
                case Texture2D texture:
                    var s = texture != null ? $"data:image/png;base64,{texture.Tob64String()}" : null;
                    postData.Add(field.Name, s);
                    break;
                case List<Texture2D> textureList:
                    List<string> textureStrings = new();
                    foreach (var t in textureList) textureStrings.Add($"data:image/png;base64,{t.Tob64String()}");
                    postData.Add(field.Name, textureStrings);
                    break;
                default:
                    postData.Add(field.Name, value);
                    break;
            }
        }

        return JsonConvert.SerializeObject(postData, Formatting.Indented);
    }

    public virtual byte[] GetFieldDataBytes()
    {
        return Encoding.UTF8.GetBytes(SerializeFields());
    }
}