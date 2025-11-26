using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

public class SendTextureToFlask : MonoBehaviour
{
    public GameObject sketchObject;     // 텍스처를 보낼 대상
    public GameObject qrCodeObject;     // QR코드를 적용할 대상 (RawImage 또는 MeshRenderer)
    public string flaskUrl = "https://ccd6904565f6.ngrok-free.app/upload";  // Flask 서버 URL

    public void OnButtonClick()
    {
        StartCoroutine(SendImage());
    }

    IEnumerator SendImage()
    {
        Texture2D texture = (Texture2D)sketchObject.GetComponent<Renderer>().material.mainTexture;
        byte[] bytes = texture.EncodeToPNG();

        WWWForm form = new WWWForm();
        form.AddBinaryData("image", bytes, "upload.png", "image/png");

        UnityWebRequest request = UnityWebRequest.Post(flaskUrl, form);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Error: " + request.error);
        }
        else
        {
            // Flask가 반환한 QR코드 이미지 URL
            string qrImageUrl = request.downloadHandler.text;
            StartCoroutine(DownloadQRCode(qrImageUrl));
        }
    }

    IEnumerator DownloadQRCode(string url)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D qrTexture = DownloadHandlerTexture.GetContent(request);
            qrCodeObject.GetComponent<Renderer>().material.mainTexture = qrTexture;
        }
        else
        {
            Debug.Log("QR 다운로드 실패: " + request.error);
        }
    }
}
