using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Security.Cryptography;
//using Unity.IO.Compression;

namespace IAP_SHOP
{
	public class BasicConn : MonoBehaviour
	{
		public static BasicConn instance = null;
		const float timeout = 3.0f;
		const int retryTimes = 3;
		void Awake()
		{
			if (instance == null)
			{
				instance = this;
			}
			else if (instance != this)
			{
				Destroy (gameObject);
			}
		}

		public void ReqPost(string reqStr, string body, Action<string[]> onSucceed=null, Action<string[]> onFail=null, int encrypt = 1/*0:no encrypt, 1:normal data 2:login data*/)
		{
			StartCoroutine(DoRequestPost(retryTimes, reqStr, body, onSucceed, onFail,encrypt));
		}

		IEnumerator DoRequestPost(int retry, string reqStr, string body, Action<string[]> onSucceed=null, Action<string[]> onFail=null, int encrypt = 1)
		{
			var www = new UnityWebRequest(reqStr, "POST");
			www.SetRequestHeader ("accept-encoding", "gzip");

			byte[] bodyRaw = Encoding.UTF8.GetBytes (body);
			www.uploadHandler = (UploadHandler)new UploadHandlerRaw (bodyRaw);

			www.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
			www.SetRequestHeader("Content-Type", "application/json");

			yield return www.Send();

			if (!www.isError)
			{
				Debug.Log (reqStr);
				Debug.Log (www.downloadHandler.text);
				JObject o = JObject.Parse (www.downloadHandler.text);
				string rtVal = www.downloadHandler.text;

				if (o.SelectToken ("code") != null && (string)o.SelectToken ("code") != "1000")
				{
					onFail (new string[]{ reqStr, rtVal, body});
				}
				else
				{
					if(onSucceed!=null)
						onSucceed (new string[]{ reqStr,rtVal, body});
				}
			}
			else
			{
				retry--;
				if (retry == 0)
				{
					Debug.Log (string.Format("Post error:{0},{1},{2}",www.url,www.error,www.responseCode.ToString()));
					onFail (new string[]{ reqStr,"{}", body });
				}
				else
				{
					StartCoroutine(DoRequestPost (retry, reqStr, body, onSucceed, onFail,encrypt));
				}
			}
		}

	}
}