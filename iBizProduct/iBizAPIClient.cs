﻿// Copyright (c) iBizVision - 2014
// Author: Dan Siegel

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web;
using System.Web.SessionState;
using iBizProduct.DataContracts;
using Newtonsoft.Json.Linq;

namespace iBizProduct
{
    /// <summary>
    /// iBizProducts should use the iBizAPIClient to communicate with the iBizAPI. This will greatly reduce the time
    /// needed to implement a connection as the functions iBizProduct is meant to connect to are abstracted into easy to use methods. 
    /// This requires that you have configured your product with the External Key in the App Settings for your Product.
    /// </summary>
    public class iBizAPIClient
    {
        private static string ApiKey = iBizProductSettings.ExternalKey();

        #region CommerceManager/ProductManager/ProductOrder

        /// <summary>
        /// Add a new product order to the panel. You should use this when creating a new order such as when a customer
        /// clicks "Add to Cart" or "Checkout" on the PurchaseAdd page of the Panel.
        /// </summary>
        /// <param name="ProductOrderSpec">An associative array of the specifications that Panel will be tracking</param>
        /// <param name="ProductId">[Optional] ProductId. If not provided it must be available in the Environment.</param>
        /// <returns>The ProductOrder ID of the added Product Order.</returns>
        public static int ProductOrderAdd( ProductOrderSpec ProductOrderSpec, int? ProductId = null )
        {
            VerifyExternalKey();

            if( ProductId == null )
            {
                ProductId = iBizProductSettings.ProductId();
            }

            Dictionary<string, object> Params = new Dictionary<string, object>() {
                { "external_key", ApiKey },
                { "product_id", ProductId },
                { "productorder_spec", ProductOrderSpec.OrderSpec() }
            };

            var result = iBizBE.APICall( "JSON/CommerceManager/ProductManager/ProductOrder", "ExternalAdd", Params ).Result;

            if( result[ "error" ] != null ) throw new iBizException( result[ "error" ].ToString() );

            return int.Parse( result[ "productorder_id" ].ToString() );
        }

        /// <summary>
        /// Edit your customer's order. This could be the pricing or the name of a productorder.
        /// </summary>
        /// <param name="ProductOrderId">The ProductOrder Id of the Product Order you wish to edit.</param>
        /// <param name="productOrderSpec">The Specifications that need to change.</param>
        /// <returns>A boolean indicating whether or not the edit was successful.</returns>
        public static bool ProductOrderEdit( int ProductOrderId, ProductOrderSpec productOrderSpec )
        {
            VerifyExternalKey();

            Dictionary<string, object> Params = new Dictionary<string, object>() {
                { "external_key", ApiKey },
                { "productorder_id", ProductOrderId },
                { "productorder_spec", productOrderSpec.OrderSpec() }
            };

            var result = iBizBE.APICall( "JSON/CommerceManager/ProductManager/ProductOrder", "ExternalEdit", Params ).Result;

            if( result[ "error" ] != null ) throw new iBizException( result[ "error" ].ToString() );

            return true;
        }

        /// <summary>
        /// Used for Viewing information about a particular Purchase Order.
        /// </summary>
        /// <param name="ProductOrderId">Product Order ID</param>
        /// <param name="InfoToReturn">Optional ProductOfferInfoToReturn</param>
        /// <returns>Dictionary&lt;string, object&gt; with the requested View Object</returns>
        public static JObject ProductOrderView( int ProductOrderId, ProductOrderInfoToReturn InfoToReturn = null )
        {
            VerifyExternalKey();

            Dictionary<string, object> Params = new Dictionary<string, object>() {
                { "external_key", ApiKey },
                { "productorder_id", ProductOrderId },
            };

            if( InfoToReturn != null ) Params.Add( "info_to_return", InfoToReturn );

            var view = iBizBE.APICall( "JSON/CommerceManager/ProductManager/ProductOrder", "ExternalView", Params ).Result;

            if( view[ "error" ] != null ) throw new iBizException( view[ "error" ].ToString() );

            return view;
        }

        /* 
         * We won't be using this function for the moment.
         * We will be implementing this at a later date.
         * 
        // TODO: Create correct Paramater List
        public static Dictionary<string, object> ProductOrderUpdateInventory( string ProductOrderId ) 
        {
            Dictionary<string, object> Params = new Dictionary<string, object>() {
                { "external_key", ExternalKey },
                { "productorder_id", ProductOrderId }
            };

            return iBizBE.APICall( "JSON/CommerceManager/ProductManager/ProductOrder", "ExternalUpdateInventory", Params ).Result;
        }
        */

        /// <summary>
        /// This will instruct the Panel to make a one time charge to a client. This could be due to a customer modifying the terms of 
        /// their services with your product or an On Demand payment.
        /// </summary>
        /// <param name="CycleBeginData"></param>
        /// <param name="CycleEndDate"></param>
        /// <param name="OneTimeCost"></param>
        /// <param name="ProductOrderId"></param>
        /// <param name="DetailAddon"></param>
        /// <param name="DescriptionAddOn"></param>
        /// <param name="DueNow"></param>
        /// <returns>BillResponse Enum Value</returns>
        public static BillResponse ProductOrderBillOrderAddOneTime( DateTime CycleBeginData, DateTime CycleEndDate, decimal OneTimeCost, int ProductOrderId, string DetailAddon = null, string DescriptionAddOn = null, bool DueNow = true )
        {
            VerifyExternalKey();

            Dictionary<string, object> Params = new Dictionary<string, object>() {
                { "external_key", ApiKey },
                { "productorder_id", ProductOrderId },
                { "cycle_begin_date", UnixTime.ConvertToUnixTime( CycleBeginData ) },
                { "cycle_end_date", UnixTime.ConvertToUnixTime( CycleEndDate ) },
                { "one_time_cost", OneTimeCost }
            };

            if( DetailAddon != null ) Params.Add( "detail_addon", DetailAddon );
            if( DescriptionAddOn != null ) Params.Add( "description_addon", DescriptionAddOn );
            Params.Add( "due_now", ( ( DueNow == true ) ? 1 : 0 ).ToString() );

            var result = iBizBE.APICall( "JSON/CommerceManager/ProductManager/ProductOrder", "ExternalBillOrderAddOneTime", Params ).Result;
            var ResponseCode = int.Parse( result[ "response_code" ].ToString() );

            return ( BillResponse )ResponseCode;
        }

        /// <summary>
        /// Open a case with the owner (end user) of a particular order. (Note: Use $$OFFER_NAME$$ in case fields to substitute the name of the offer at the level of owner.)
        /// </summary>
        /// <param name="ProductOrderId">The id of the productorder to open a case for.</param>
        /// <param name="CaseSpec">The spec for the case added</param>
        /// <returns>The id of the case added</returns>
        public static int ProductOpenCaseWithOwner( int ProductOrderId, CaseSpec CaseSpec )
        {
            VerifyExternalKey();

            Dictionary<string, object> Params = new Dictionary<string, object>() {
                { "external_key", ApiKey },
                { "productorder_id", ProductOrderId },
                { "case_spec", CaseSpec.GetSpec() }
            };

            var result = iBizBE.APICall( "JSON/CommerceManager/ProductManager/ProductOrder", "ExternalOpenCaseWithOwner", Params ).Result;

            if( result[ "error" ] != null ) throw new iBizException( result[ "error" ].ToString() );

            return int.Parse( result[ "new_id" ].ToString() );
        }

        #endregion

        #region CommerceManager/ProductOffer

        /// <summary>
        /// The Product Offer Price provides a way to get a ProductOffer price, including the offer chain.
        /// </summary>
        /// <param name="ProductOfferId">The ID of the Product Offer [REQUIRED]</param>
        /// <param name="AccountHost">Your account host</param>
        /// <param name="AccountId"></param>
        /// <returns></returns>
        public static JObject ProductOfferPrice( int ProductOfferId, string AccountHost, int? AccountId = null ) // Used for getting the offer chain
        {
            VerifyExternalKey();

            Dictionary<string, object> Params = new Dictionary<string, object>() {
                { "external_key", ConfigurationManager.AppSettings[ "ExternalKey" ] },
                { "account_host", AccountHost },
                { "productoffer_id", ProductOfferId }
            };

            if( AccountId != null ) Params.Add( "account_id", AccountId );

            return iBizBE.APICall( "JSON/CommerceManager/ProductOffer", "ExternalProductPrice", Params ).Result;
        }

        #endregion

        /*
 * We will not be using this for the moment. We may revisit this later.
 * 
        #region BillingManager/LedgerEntryManager

        // TODO: Create correct Paramater List
        public static Dictionary<string, object> LedgerEntryManagerProrate( string ProductOfferId, string AccountHost, string AccountId )
        {
            Dictionary<string, object> Params = new Dictionary<string, object>() {
                { "external_key", iBizProductSettings.ExternalKey() },
                { "account_id", AccountId },
                { "account_host", AccountHost },
                { "productoffer_id", ProductOfferId }
            };

            return iBizBE.APICall( "JSON/BillingManager/LedgerEntryManager", "ExternalProrate", Params ).Result;
        }

        #endregion

        #region CommerceManager/ProductOffer/PurchaseOrder

"JSON/CommerceManager/ProductOffer/PurchaseOrder", "ExternalAdd"
"JSON/CommerceManager/ProductOffer/PurchaseOrder", "ExternalEdit"
"JSON/CommerceManager/ProductOffer/PurchaseOrder", "ExternalListOnAccount"
"JSON/CommerceManager/ProductOffer/PurchaseOrder", "ExternalPriceFromPurchase"
"JSON/CommerceManager/ProductOffer/PurchaseOrder", "ExternalGetCycleDelimiters"

        #endregion
*/
        #region CommerceManager/ProductManager/ProductOrder/Event

        /// <summary>
        /// This function allows you to notify iBizAPI of Event Updates on Event requests it has
        /// sent. 
        /// </summary>
        /// <remarks>
        /// This does not affect backoff requests. Backoff requests must be handled by the Product's 
        /// API endpoint Order/{Object}/{Action}
        /// </remarks>
        /// <param name="EventId">The id of the productorder that we're going to update.</param>
        /// <param name="Status">The status for the event, COMPLETE or ERROR.</param>
        /// <param name="Message">A message to include with errors (this is customer visable).</param>
        /// <returns>Indicates whether or not the update was successful</returns>
        public static bool UpdateEvent( int EventId, EventStatus Status, string Message )
        {
            Dictionary<string, object> Params = new Dictionary<string, object>() {
                { "external_key", iBizProductSettings.ExternalKey() },
                { "productorderevent_id", EventId },
                { "message", Message },
                { "status", Status.ToString() }
            };

            var result = iBizBE.APICall( "JSON/CommerceManager/ProductManager/ProductOrder/Event", "ExternalUpdateEvent", Params ).Result;

            if( result[ "error" ] != null ) throw new iBizException( result[ "error" ].ToString() );

            return int.Parse( result[ "success" ].ToString() ) == 1 ? true : false;
        }

        #endregion

        /// <summary>
        /// This method sets the ProductAuthentication variables to the Session for the Product to be able to track
        /// and handle. It also verifies that the session the user has is valid.
        /// </summary>
        /// <param name="authentication">Authentication Paramaters</param>
        /// <returns>Boolean to indicate a valid session</returns>
        public static bool AuthenticateUser( ProductAuthentication authentication )
        {
            //if( HttpContext.Current.Session["Token"] != null )
            {
                // Check the SessionID Passed and validate that you have a valid Panel session
                Dictionary<string, object> Params = new Dictionary<string, object>()
                {
                    { "session_id", authentication.SessionID }
                };

                //var VerifyAccount = iBizBE.APICall( "JSON/AccountManager/AAA", "ViewAccountID", Params );

                // We only load MyAccountID and the Token here. NOTE: We do not want to track the Panel Session ID.
                // This would encourage developers to use non-external calls beyond what we are making here.
                HttpContext.Current.Session[ "MyAccountID" ] = authentication.MyAccountID;
                HttpContext.Current.Session[ "Token" ] = Convert.ToBase64String( Guid.NewGuid().ToByteArray() );
            }

            // We will always load the follow here as they may have changed.
            HttpContext.Current.Session.Add( "AccountID", authentication.AccountID );
            HttpContext.Current.Session.Add( "ProductOrderID", authentication.ProductOrderID );
            HttpContext.Current.Session.Add( "OfferID", authentication.OfferID );
            HttpContext.Current.Session.Add( "Language", authentication.Language );

            return true;
        }

        /// <summary>
        /// Verify that the External Key Sent by the Backend is Valid for iBizProduct
        /// </summary>
        /// <param name="ExternalKey"></param>
        /// <returns></returns>
        public static bool IsValidBackendRequest( string ExternalKey )
        {
            return ExternalKey == iBizProductSettings.ExternalKey();
        }

        /// <summary>
        /// This will verify that you have an External Key set in your AppSettings. If it does not exist 
        /// the Client will not be able to authenticate against the iBizAPI.
        /// </summary>
        /// <returns>True if a value exists for the External Key</returns>
        public static bool ExternalKeyExists()
        {
            return !String.IsNullOrEmpty( iBizProductSettings.ExternalKey() );
        }


        /// <summary>
        /// This method should be called before attempting to connect to the Backend Services. If you do not have 
        /// an External Key none of the functions will work.
        /// </summary>
        private static void VerifyExternalKey()
        {
            if( !ExternalKeyExists() )
                throw new iBizException( "Your Products External Key was not found or is not accessible. Please verify that the key is set in the AppSettings " +
                                            "section of your config file. You can find the Product External Key in the Panel under the External Attributes section " +
                                            "of the ProductEdit page." );
        }
    }
}
