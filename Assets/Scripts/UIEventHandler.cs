using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class UIEventHandler : MonoBehaviour
{
    public string serverUri = "";

    public Transform m_SphereTransform;
    public Renderer m_SphereRenderer;
    public Renderer m_RPlaneRenderer;
    public Renderer m_RRPlaneRenderer;
    public Renderer m_LPlaneRenderer;
    public Renderer m_LLPlaneRenderer;

    private bool isRunning;
    [SerializeField, Range(0f, 1f)] private float progress;
    private List<SdImage> outputImages = new();
    [SerializeField] public RequestData requestData;

    [SerializeField]
    public UIDocument m_UIDocument;
    private VisualElement rootElement;
    private TextField m_ServerTextField;
    private TextField m_AuthTextField;
    private DropdownField m_ModelDropdown;
    private Button m_RefreshModelButton;
    private DropdownField m_SamplerDropdown;
    private Button m_RefreshSamplerButton;
    private Slider m_SamplingStepsSlider;
    private Slider m_WidthSlider;
    private Slider m_HeightSlider;
    private Slider m_CFGScaleSlider;
    private TextField m_SeedTextField;
    private TextField m_PromptTextField;
    private TextField m_NegativePromptTextField;
    private ProgressBar m_GenerateProgressBar;
    private Button m_GenerateButton;
    private Button m_SaveButton;
    private Label m_TestLabel;
    private VisualElement m_ImageVisualElement;
    private Slider m_TileWidthSlider;

    Texture2D image;

    public void Start()
    {
        rootElement = m_UIDocument.rootVisualElement;

        m_ServerTextField = rootElement.Q<TextField>("ServerTextField");
        m_AuthTextField = rootElement.Q<TextField>("AuthTextField");
        m_ModelDropdown = rootElement.Q<DropdownField>("ModelDropdown");
        m_RefreshModelButton = rootElement.Q<Button>("RefreshModelButton");
        m_RefreshModelButton.clickable.clicked += OnRefreshModelButtonClicked;
        m_RefreshSamplerButton = rootElement.Q<Button>("RefreshSamplerButton");
        m_RefreshSamplerButton.clickable.clicked += OnRefreshSamplerButtonClicked;
        m_SamplerDropdown = rootElement.Q<DropdownField>("SamplerDropdown");
        m_SamplingStepsSlider = rootElement.Q<Slider>("SamplingStepsSlider");
        m_WidthSlider = rootElement.Q<Slider>("WidthSlider");
        m_HeightSlider = rootElement.Q<Slider>("HeightSlider");
        m_CFGScaleSlider = rootElement.Q<Slider>("CFGScaleSlider");
        m_SeedTextField = rootElement.Q<TextField>("SeedTextField");
        m_PromptTextField = rootElement.Q<TextField>("PromptTextField");
        m_NegativePromptTextField = rootElement.Q<TextField>("NegativePromptTextField");
        m_GenerateProgressBar = rootElement.Q<ProgressBar>("GenerateProgressBar");
        m_GenerateButton = rootElement.Q<Button>("GenerateButton");
        m_GenerateButton.clickable.clicked += OnGenerateButtonClicked;
        m_SaveButton = rootElement.Q<Button>("SaveButton");
        m_SaveButton.clickable.clicked += OnSaveButtonClicked;
        m_TestLabel = rootElement.Q<Label>("TestLabel");
        m_ImageVisualElement = rootElement.Q<VisualElement>("ImageVisualElement");
        m_TileWidthSlider = rootElement.Q<Slider>("TileWidthSlider");

        if (string.IsNullOrEmpty(m_ServerTextField.value))
        {
            m_TestLabel.text = "Please Set Your WebUI API Link";
        }
        else
        {
            serverUri = m_ServerTextField.value;
        }

        StartCoroutine(GetSamplers());
        StartCoroutine(GetModels());

        m_ModelDropdown.RegisterValueChangedCallback(evt =>
        {
            SetModel(evt.newValue);
        });

    }

    private void OnGenerateButtonClicked()
    {
        if (string.IsNullOrEmpty(m_ServerTextField.value))
        {
            m_TestLabel.text = "Please Set Your WebUI API Link";
        }
        else
        {
            serverUri = m_ServerTextField.value;
        }

        requestData.sampler_index = m_ModelDropdown.value;
        requestData.sampler_index = m_SamplerDropdown.value;
        requestData.prompt = m_PromptTextField.text;
        requestData.negative_prompt= m_NegativePromptTextField.text;
        requestData.steps = (int) m_SamplingStepsSlider.value;
        requestData.width = (int) m_WidthSlider.value;
        requestData.height = (int) m_HeightSlider.value;
        requestData.cfg_scale = m_CFGScaleSlider.value;
        requestData.seed = Convert.ToInt32(m_SeedTextField.value);
        requestData.tiling = true;
        StartCoroutine(Generate());
    }

    private void OnSaveButtonClicked()
    {
        string dateFormat = "yyyy-MM-dd-HH-mm-ss";
        string filename = System.DateTime.Now.ToString(dateFormat);
        byte[] texture = image.EncodeToPNG();
        WebGLFileSaver.SaveFile(texture, filename + ".png", "image/png");
    }

    private void OnRefreshSamplerButtonClicked()
    {
        StartCoroutine(GetSamplers());
    }

    private void OnRefreshModelButtonClicked()
    {
        StartCoroutine(GetModels());
    }

    public IEnumerator GetModels()
    {
        UnityWebRequest request = UnityWebRequest.Get(serverUri + "/sdapi/v1/sd-models");
        string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(m_AuthTextField.value));
        request.SetRequestHeader("Authorization", "Basic " + svcCredentials);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(request.error);
        }
        else
        {
            var jarr = JArray.Parse(request.downloadHandler.text);
            List<string> models = new();
            foreach (var token in jarr)
            {
                models.Add(token["title"]?.ToString());
            }
            m_ModelDropdown.choices = models;
            m_ModelDropdown.value = models[0];
        }
        request.Dispose();
    }

    public IEnumerator SetModel(string model)
    {
        Dictionary<string, object> parameters = new() { { "data", new List<string>() { model } } };
        var reqBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(parameters, Formatting.Indented));
        UnityWebRequest request = new UnityWebRequest(serverUri + "/sdapi/v1/291", "POST");
        string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(m_AuthTextField.value));
        request.SetRequestHeader("Authorization", "Basic " + svcCredentials);

        request.uploadHandler = new UploadHandlerRaw(reqBytes);
        request.useHttpContinue = false;
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            m_TestLabel.text = request.error;
        }
        else
        {
            m_TestLabel.text = request.downloadHandler.text;
        }
        request.Dispose();
    }

    public IEnumerator GetSamplers()
    {
        UnityWebRequest request = UnityWebRequest.Get(serverUri + "/sdapi/v1/samplers");
        string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(m_AuthTextField.value));
        request.SetRequestHeader("Authorization", "Basic " + svcCredentials);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            m_TestLabel.text = request.error;
        }
        else
        {
            var jarr = JArray.Parse(request.downloadHandler.text);
            List<string> samplers = new();
            foreach (var token in jarr)
            {
                samplers.Add(token["name"]?.ToString());
            }
            m_SamplerDropdown.choices = samplers;
            m_SamplerDropdown.value = samplers[0];
            m_TestLabel.text = request.downloadHandler.text;
        }

        request.Dispose();
    }

    public IEnumerator Generate()
    {
        isRunning = true;
        progress = 0;
        StartCoroutine(ProgressCheck());

        yield return Generate(requestData, generatedImages =>
        {
            foreach (var generatedImage in generatedImages)
            {
                outputImages.Add(generatedImage);
                m_SphereRenderer.material.SetTexture("_MainTex", generatedImage.image);
                m_RPlaneRenderer.material.SetTexture("_MainTex", generatedImage.image);
                m_RRPlaneRenderer.material.SetTexture("_MainTex", generatedImage.image);
                m_LPlaneRenderer.material.SetTexture("_MainTex", generatedImage.image);
                m_LLPlaneRenderer.material.SetTexture("_MainTex", generatedImage.image);
                m_TestLabel.text = generatedImage.data;
                image = generatedImage.image;
            }
        });

        isRunning = false;
        progress = 1;
    }

    public IEnumerator Generate(RequestData requestData, Action<List<SdImage>> callback)
    {
        var images = new List<SdImage>();
        var hasInitImage = requestData.init_images.Count > 0;
        var request = new UnityWebRequest($"{serverUri + "/sdapi/v1/"}{(hasInitImage ? "txt2img" : "txt2img")}", UnityWebRequest.kHttpVerbPOST);
        string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(m_AuthTextField.value));
        request.SetRequestHeader("Authorization", "Basic " + svcCredentials);

        UploadHandlerRaw uH = new UploadHandlerRaw(requestData.GetFieldDataBytes());
        DownloadHandler dH = new DownloadHandlerBuffer();

        request.uploadHandler = uH;
        request.downloadHandler = dH;

        request.useHttpContinue = false;
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var resJson = JObject.Parse(dH.text);
            var imagesToken = resJson["images"];
            var infoToken = resJson["info"];

            var info = infoToken.ToString();
            var seedmatch = Regex.Match(info, @"\sseed: (\d+)").Groups[1];

            var c = imagesToken!.Count();
            for (int i = 0; i < c; i++)
            {
                var t2d = new Texture2D(requestData.width, requestData.height);
                t2d.LoadImage(Convert.FromBase64String(imagesToken[i]!.ToString()));
                t2d.Apply();

                images.Add(new(t2d, info));
            }

            uH.Dispose();
            request.Dispose();

            callback?.Invoke(images);
        }
    }

    private IEnumerator ProgressCheck()
    {
        while (isRunning)
        {
            yield return new WaitForSeconds(.5f);

            UnityWebRequest request = UnityWebRequest.Get(serverUri + "/sdapi/v1/progress");
            string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(m_AuthTextField.value));
            request.SetRequestHeader("Authorization", "Basic " + svcCredentials);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(request.error);
            }
            else
            {
                JObject jo = JObject.Parse(request.downloadHandler.text);
                var pd = new ProgressData();

                pd.image = jo["current_image"]!.ToString();
                pd.percent = jo["progress"]!.Value<float>();

                var state = jo["state"];
                if (state != null)
                {
                    pd.skipped = state["skipped"]?.Value<bool>() ?? false;
                    pd.interrupted = state["interrupted"]?.Value<bool>() ?? false;
                    pd.job = state["job"]?.Value<string>() ?? "";
                    pd.job_count = state["job_count"]?.Value<int>() ?? 0;
                    pd.job_no = state["job_no"]?.Value<int>() ?? 0;
                    pd.sampling_step = state["sampling_step"]?.Value<int>() ?? 0;
                    pd.sampling_steps = state["sampling_steps"]?.Value<int>() ?? 0;
                }

                progress = pd.percent;

                if (m_GenerateProgressBar != null)
                {
                    m_GenerateProgressBar.title = pd.Info;
                    m_GenerateProgressBar.value = progress;
                }
            }

            request.Dispose();
        }

        yield return null;
    }

    void Update()
    {
        m_SphereTransform.position = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width - (Screen.width / 4), Screen.height / 2, 2.5f));

        int SamplingStepSize = 1;
        float steppedSamplingValue = Mathf.Round(m_SamplingStepsSlider.value / SamplingStepSize) * SamplingStepSize;
        if (steppedSamplingValue != m_SamplingStepsSlider.value)
        {
            m_SamplingStepsSlider.value = steppedSamplingValue;
        }

        int WidthStepSize = 64;
        float steppedWidthValue = Mathf.Round(m_WidthSlider.value / WidthStepSize) * WidthStepSize;
        if (steppedWidthValue != m_WidthSlider.value)
        {
            m_WidthSlider.value = steppedWidthValue;
        }

        int HeightStepSize = 64;
        float steppedHeightValue = Mathf.Round(m_HeightSlider.value / HeightStepSize) * HeightStepSize;
        if (steppedHeightValue != m_HeightSlider.value)
        {
            m_HeightSlider.value = steppedHeightValue;
        }

        float CFGScaleStepSize = 0.5f;
        float steppedCFGScaleValue = Mathf.Round(m_CFGScaleSlider.value / CFGScaleStepSize) * CFGScaleStepSize;
        if (steppedCFGScaleValue != m_CFGScaleSlider.value)
        {
            m_CFGScaleSlider.value = steppedCFGScaleValue;
        }

        int TileWidthStepSize = 1;
        float steppedTileWidthValue = Mathf.Round(m_TileWidthSlider.value / TileWidthStepSize) * TileWidthStepSize;
        if (steppedTileWidthValue != m_TileWidthSlider.value)
        {
            m_TileWidthSlider.value = steppedTileWidthValue;
        }

        m_SphereRenderer.material.SetTextureScale("_MainTex", new Vector2(m_TileWidthSlider.value, m_TileWidthSlider.value));
        m_RPlaneRenderer.material.SetTextureScale("_MainTex", new Vector2(m_TileWidthSlider.value, m_TileWidthSlider.value));
        m_RRPlaneRenderer.material.SetTextureScale("_MainTex", new Vector2(m_TileWidthSlider.value, m_TileWidthSlider.value));
        m_LPlaneRenderer.material.SetTextureScale("_MainTex", new Vector2(m_TileWidthSlider.value, m_TileWidthSlider.value));
        m_LLPlaneRenderer.material.SetTextureScale("_MainTex", new Vector2(m_TileWidthSlider.value, m_TileWidthSlider.value));
    }

    private void OnDestroy()
    {
        m_GenerateButton.clickable.clicked -= OnGenerateButtonClicked;
        m_RefreshModelButton.clickable.clicked -= OnRefreshModelButtonClicked;
    }

}
