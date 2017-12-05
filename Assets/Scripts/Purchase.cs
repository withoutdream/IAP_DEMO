using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Placing the Purchaser class in the CompleteProject namespace allows it to interact with ScoreManager, 
// one of the existing Survival Shooter scripts.
namespace IAP_SHOP
{
	// Deriving the Purchaser class from IStoreListener enables it to receive messages from Unity Purchasing.
	public class Purchase : MonoBehaviour, IStoreListener
	{
		public static string myServer = "http://112.125.93.9:8888";
		public static int itermCounter = 0;

		public Text counterText;

		private static IStoreController m_StoreController;          // The Unity Purchasing system.
		private static IExtensionProvider m_StoreExtensionProvider; // The store-specific Purchasing subsystems.
		private static Product m_product;

		public static string kProductIDConsumable = "";//"iterm";
		// Product identifiers for all products capable of being purchased: 
		// "convenience" general identifiers for use with Purchasing, and their store-specific identifier 
		// counterparts for use with and outside of Unity Purchasing. Define store-specific identifiers 
		// also on each platform's publisher dashboard (iTunes Connect, Google Play Developer Console, etc.)

		// General product identifiers for the consumable, non-consumable, and subscription products.
		// Use these handles in the code to reference which product to purchase. Also use these values 
		// when defining the Product Identifiers on the store. Except, for illustration purposes, the 
		// kProductIDSubscription - it has custom Apple and Google identifiers. We declare their store-
		// specific mapping to Unity Purchasing's AddProduct, below.
		//public static string kProductIDConsumable =    "iterm";   
		//public static string kProductIDNonConsumable = "iterm4";
		//public static string kProductIDSubscription =  "subscription"; 

		// Apple App Store-specific product identifier for the subscription product.
		//private static string kProductNameAppleSubscription =  "com.unity3d.subscription.new";

		// Google Play Store-specific product identifier subscription product.
		//private static string kProductNameGooglePlaySubscription =  "com.unity3d.subscription.original"; 

		void Start()
		{
			// If we haven't set up the Unity Purchasing reference
			if (m_StoreController == null)
			{
				// Begin to configure our connection to Purchasing
				InitializePurchasing();
			}
			itermCounter = 0;
			setItermNum (itermCounter);
		}

		public void InitializePurchasing() 
		{
			// If we have already connected to Purchasing ...
			if (IsInitialized())
			{
				// ... we are done here.
				return;
			}

			Debuger.Log ("Init start.");
			// Create a builder, first passing in a suite of Unity provided stores.
			var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

			// Add a product to sell / restore by way of its identifier, associating the general identifier
			// with its store-specific identifiers.

			builder.AddProduct("com.smiletech.jingling.paytest1", ProductType.Consumable);
			builder.AddProduct("com.smiletech.jingling.paytest2", ProductType.Consumable);
		//	builder.AddProduct("1", ProductType.Consumable);
		//	builder.AddProduct("2", ProductType.Consumable);

			// Continue adding the non-consumable product.
			//builder.AddProduct("Iterm4", ProductType.NonConsumable);
			// And finish adding the subscription product. Notice this uses store-specific IDs, illustrating
			// if the Product ID was configured differently between Apple and Google stores. Also note that
			// one uses the general kProductIDSubscription handle inside the game - the store-specific IDs 
			// must only be referenced here. 
			//builder.AddProduct(kProductIDSubscription, ProductType.Subscription, new IDs(){
			//	{ kProductNameAppleSubscription, AppleAppStore.Name },
			//	{ kProductNameGooglePlaySubscription, GooglePlay.Name },
			//});

			// Kick off the remainder of the set-up with an asynchrounous call, passing the configuration 
			// and this class' instance. Expect a response either in OnInitialized or OnInitializeFailed.
			UnityPurchasing.Initialize(this, builder);
			Debuger.Log ("Init end.");
		}


		private bool IsInitialized()
		{
			// Only say we are initialized if both the Purchasing references are set.
			return m_StoreController != null && m_StoreExtensionProvider != null;
		}

	/*	public void BuyConsumable()
		{
			// Buy the consumable product using its general identifier. Expect a response either 
			// through ProcessPurchase or OnPurchaseFailed asynchronously.
			BuyProductID(kProductIDConsumable);
		}


		public void BuyNonConsumable()
		{
			// Buy the non-consumable product using its general identifier. Expect a response either 
			// through ProcessPurchase or OnPurchaseFailed asynchronously.
			BuyProductID(kProductIDNonConsumable);
		}


		public void BuySubscription()
		{
			// Buy the subscription product using its the general identifier. Expect a response either 
			// through ProcessPurchase or OnPurchaseFailed asynchronously.
			// Notice how we use the general product identifier in spite of this ID being mapped to
			// custom store-specific identifiers above.
			BuyProductID(kProductIDSubscription);
		}*/


		public void BuyProductID(String buttonId)
		{
			Debuger.Log ("BuyProductID.");
			string productId = string.Format ("com.smiletech.jingling.paytest{0}", buttonId);
			Debuger.Log (productId);
			// If Purchasing has been initialized ...
			if (IsInitialized())
			{
				// ... look up the Product reference with the general product identifier and the Purchasing 
				// system's products collection.
				Product product = m_StoreController.products.WithID(productId);

				// If the look up found a product for this device's store and that product is ready to be sold ... 
				if (product != null && product.availableToPurchase)
				{
					Debuger.Log(string.Format("Purchasing product asychronously: '{0}'", product.definition.id));
					// ... buy the product. Expect a response either through ProcessPurchase or OnPurchaseFailed 
					// asynchronously.
					m_StoreController.InitiatePurchase(product);
				}
				// Otherwise ...
				else
				{
					// ... report the product look-up failure situation  
					Debuger.Log("BuyProductID: FAIL. Not purchasing product, either is not found or is not available for purchase");
				}
			}
			// Otherwise ...
			else
			{
				// ... report the fact Purchasing has not succeeded initializing yet. Consider waiting longer or 
				// retrying initiailization.
				Debuger.Log("BuyProductID FAIL. Not initialized.");
			}
		}


		// Restore purchases previously made by this customer. Some platforms automatically restore purchases, like Google. 
		// Apple currently requires explicit purchase restoration for IAP, conditionally displaying a password prompt.
		public void RestorePurchases()
		{
			// If Purchasing has not yet been set up ...
			if (!IsInitialized())
			{
				// ... report the situation and stop restoring. Consider either waiting longer, or retrying initialization.
				Debuger.Log("RestorePurchases FAIL. Not initialized.");
				return;
			}

			// If we are running on an Apple device ... 
			if (Application.platform == RuntimePlatform.IPhonePlayer || 
				Application.platform == RuntimePlatform.OSXPlayer)
			{
				// ... begin restoring purchases
				Debuger.Log("RestorePurchases started ...");

				// Fetch the Apple store-specific subsystem.
				var apple = m_StoreExtensionProvider.GetExtension<IAppleExtensions>();
				// Begin the asynchronous process of restoring purchases. Expect a confirmation response in 
				// the Action<bool> below, and ProcessPurchase if there are previously purchased products to restore.
				apple.RestoreTransactions((result) => {
					// The first phase of restoration. If no more responses are received on ProcessPurchase then 
					// no purchases are available to be restored.
					Debuger.Log("RestorePurchases continuing: " + result + ". If no further messages, no purchases available to restore.");
				});
			}
			// Otherwise ...
			else
			{
				// We are not running on an Apple device. No work is necessary to restore purchases.
				Debuger.Log("RestorePurchases FAIL. Not supported on this platform. Current = " + Application.platform);
			}
		}


		//  
		// --- IStoreListener
		//

		public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
		{
			// Purchasing has succeeded initializing. Collect our Purchasing references.
			Debuger.Log("OnInitialized: PASS");

			// Overall Purchasing system, configured with products for this application.
			m_StoreController = controller;
			// Store specific subsystem, for accessing device-specific store features.
			m_StoreExtensionProvider = extensions;
		}

		public void OnInitializeFailed(InitializationFailureReason error)
		{
			// Purchasing set-up has not succeeded. Check error for reason. Consider sharing this reason with the user.
			Debuger.Log("OnInitializeFailed InitializationFailureReason:" + error);
		}

		public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args) 
		{
			Debuger.Log ("PurchaseProcessingResult called.");

			//如果有服务器，服务器用这个receipt去苹果验证。
			m_product = args.purchasedProduct;
			var receiptJson = JObject.Parse (args.purchasedProduct.receipt);
			var receipt = receiptJson.SelectToken ("Payload");

			JObject PayloadInfo = new JObject(new JProperty("payload", receipt));
			//Debuger.Log ("receipt: " + m_product.receipt);
			//Debuger.Log ("PayloadInfo: " + PayloadInfo);
			Debuger.Log ("PayloadInfo.ToString(): " + PayloadInfo.ToString());

			IosPurchaseVerify (PayloadInfo.ToString());

			// Or ... a non-consumable product has been purchased by this user.
			/*if (String.Equals(args.purchasedProduct.definition.id, kProductIDNonConsumable, StringComparison.Ordinal))
			{
				Debuger.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));

				IosPurchaseVerify (args.purchasedProduct.definition.id);
				// TODO: The non-consumable item has been successfully purchased, grant this item to the player.
			}
			// A consumable product has been purchased by this user.
			else if (args.purchasedProduct.definition.id.StartsWith(kProductIDConsumable))
			{
				Debuger.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));
				IosPurchaseVerify (args.purchasedProduct.definition.id);
				// The consumable item has been successfully purchased, add 100 coins to the player's in-game score.
			//	ScoreManager.score += 100;
			}
			// Or ... a subscription product has been purchased by this user.
			else if (String.Equals(args.purchasedProduct.definition.id, kProductIDSubscription, StringComparison.Ordinal))
			{
				Debuger.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));
				// TODO: The subscription item has been successfully purchased, grant this to the player.
			}
			// Or ... an unknown product has been purchased by this user. Fill in additional products here....
			else 
			{
				Debuger.Log(string.Format("ProcessPurchase: FAIL. Unrecognized product: '{0}'", args.purchasedProduct.definition.id));
			}*/

			// Return a flag indicating whether this product has completely been received, or if the application needs 
			// to be reminded of this purchase at next app launch. Use PurchaseProcessingResult.Pending when still 
			// saving purchased products to the cloud, and when that save is delayed. 
			return PurchaseProcessingResult.Pending;//Complete;
		}


		public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
		{
			// A product purchase attempt did not succeed. Check failureReason for more detail. Consider sharing 
			// this reason with the user to guide their troubleshooting actions.
			Debuger.Log(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}", product.definition.storeSpecificId, failureReason));
		}

		private void IosPurchaseCreate(String productId)
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

		public void OnVerifySucceed(string[] returnVal)
		{
			if (m_product != null)
			{
				m_StoreController.ConfirmPendingPurchase (m_product);

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
			counterText.text = itermNum.ToString();//String.Format( "Counter: {0}", itermNum);
		//	counterStr.text = "Counter: " + itermNum;
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
}