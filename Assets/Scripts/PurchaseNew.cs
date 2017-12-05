using System;
using UnityEngine;
using System.Collections;
using UnityEngine.Purchasing;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using IAP_SHOP;

public class PurchaseNew : MonoBehaviour, IStoreListener
{
	public static string myServer = "http://112.125.93.9:8888";
	public static int itermCounter = 0;

	public Text counterText;

	private IStoreController controller;
	private static Product m_product;

	void Start()
	{
		Debuger.Log ("Initialize Start.");
		var module = StandardPurchasingModule.Instance();
		ConfigurationBuilder builder = ConfigurationBuilder.Instance(module);
		builder.AddProduct("com.smiletech.jingling.paytest1", ProductType.Consumable);
		builder.AddProduct("com.smiletech.jingling.paytest2", ProductType.Consumable);
		UnityPurchasing.Initialize(this, builder);

		itermCounter = 0;
		setItermNum (itermCounter);
		Debuger.Log ("Initialize complete.");
	}

	/// <summary>
	/// Called when Unity IAP is ready to make purchases.
	/// </summary>
	public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
	{
		Debuger.Log ("OnInitialized Succeed.");
		this.controller = controller;
	}

	/// <summary>
	/// Called when Unity IAP encounters an unrecoverable initialization error.
	///
	/// Note that this will not be called if Internet is unavailable; Unity IAP
	/// will attempt initialization until it becomes available.
	/// </summary>
	public void OnInitializeFailed(InitializationFailureReason error)
	{
		Debuger.Log ("OnInitializeFailed: " + error);
	}

	/// <summary>
	/// Called when a purchase completes.
	///
	/// May be called at any time after OnInitialized().
	/// </summary>
	public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
	{
		Debuger.Log ("PurchaseProcessingResult.");

		//如果有服务器，服务器用这个receipt去苹果验证。
		m_product = args.purchasedProduct;
		var receiptJson = JObject.Parse (args.purchasedProduct.receipt);
		var receipt = receiptJson.SelectToken ("Payload");

		JObject PayloadInfo = new JObject(new JProperty("payload", receipt));
		//Debuger.Log ("receipt: " + m_product.receipt);
		//Debuger.Log ("PayloadInfo: " + PayloadInfo);
		Debuger.Log ("PayloadInfo.ToString(): " + PayloadInfo.ToString());

		IosPurchaseVerify (PayloadInfo.ToString());

		return PurchaseProcessingResult.Pending;//PurchaseProcessingResult.Complete;
	}

	/// <summary>
	/// Called when a purchase fails.
	/// </summary>

	public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
	{
		// A product purchase attempt did not succeed. Check failureReason for more detail. Consider sharing 
		// this reason with the user to guide their troubleshooting actions.
		Debuger.Log(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}", product.definition.storeSpecificId, failureReason));
	}

	public void OnPurchaseClicked(string productId)
	{
		string productName = string.Format ("com.smiletech.jingling.paytest{0}", productId);
		Debuger.Log (productName);
		controller.InitiatePurchase(productName);
	}

	private void IosPurchaseCreate(String productId)
	{
		if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.OSXEditor) {
			BasicConn.instance.ReqPost (string.Format ("{0}/charge/ios/create", myServer),
				productId,
				OnSucceed,
				OnFail);
		}
		else if(Application.platform == RuntimePlatform.IPhonePlayer)
		{
			BasicConn.instance.ReqPost(string.Format("{0}/charge/ios/create", myServer),
				productId,
				OnSucceed,
				OnFail);
		}
	}

	private void IosPurchaseVerify(String PayloadInfo)
	{
		
		if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.OSXEditor) 
		{
			BasicConn.instance.ReqPost(string.Format("{0}/charge/google/verify", myServer),
				PayloadInfo,
				OnVerifySucceed,
				OnFail);
		} 
		else if(Application.platform == RuntimePlatform.IPhonePlayer)
		{
			BasicConn.instance.ReqPost(string.Format("{0}/charge/ios/verify_test", myServer),
				PayloadInfo,
				OnVerifySucceed,
				OnFail);
		}
	}

/*	private void IosPurchaseCreate(String productId)
	{
		BasicConn.instance.ReqPost(string.Format("{0}/charge/ios/create", myServer),
			productId,
			OnSucceed,
			OnFail);
	}

	private void IosPurchaseVerify(String PayloadInfo)
	{
		BasicConn.instance.ReqPost(string.Format("{0}/charge/ios/verify_test", myServer),
			PayloadInfo,
			OnVerifySucceed,
			OnFail);
	}
*/
	public void OnVerifySucceed(string[] returnVal)
	{
		if (m_product != null)
		{
			controller.ConfirmPendingPurchase (m_product);

			getNewIterm ();
			Debuger.Log ("purchase complete.");
		}
	}

	public void OnSucceed(string[] returnVal)
	{
		Debuger.Log (returnVal);
	}

	public void OnFail(string[] failInfo)
	{
		Debuger.Log (failInfo [1]);
		JObject o = JObject.Parse (failInfo [1]);
		int err_code = -1;
		if (o.SelectToken ("code") != null)
		{
			err_code = o.SelectToken ("code").ToObject<int> ();
		}

		Debuger.Log ("err_code: " + err_code);
	}

	private void getNewIterm()
	{
		itermCounter++;
		setItermNum (itermCounter);
	}

	private void setItermNum(int itermNum)
	{
		counterText.text = itermNum.ToString();
	}

	void OnGUI()
	{
		int counter = 0;
		foreach (String log in Debuger.myLogs)
		{
			GUI.Label(new Rect(10, 10 + (counter ++ ) * 20, 1000, 30), log);
		}
	}
}
