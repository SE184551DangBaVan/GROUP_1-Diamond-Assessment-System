﻿using BadmintonBooking.Models;
using BadmintonBooking.Repository.Service;
using BadmintonBooking.ViewModels;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayPal;
using PayPal.Api;

namespace BadmintonBooking.Controllers
{
    public class PayPalController : Controller
    {
        private IHttpContextAccessor httpContextAccessor;
        IConfiguration _configuration;
        private readonly UserManager<IdentityUser> _userManager;
        private PayPal.Api.Payment payment;

        public PayPalController(IHttpContextAccessor httpContextAccessor, IConfiguration configuration, UserManager<IdentityUser> userManager)
        {
            this.httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _userManager = userManager;
        }

        public ActionResult PaymentWithPaypal(string Cancel = null, string blogId = "", string PayerID = "", string guid = "")
        {
            var ClientID = _configuration.GetValue<string>("PayPal:Key");
            var ClientSecret = _configuration.GetValue<string>("PayPal:Secret");
            var mode = _configuration.GetValue<string>("PayPal:mode");

            APIContext apiContext = PaypalConfiguration.GetAPIContext(ClientID, ClientSecret, mode);

            try
            {
                string payerId = PayerID;
                if (string.IsNullOrEmpty(payerId))
                {
                    string baseURI = this.Request.Scheme + "://" + this.Request.Host + "/PayPal/PaymentWithPaypal?";
                    string guidd = Convert.ToString(new Random().Next(10000));
                    guid = guidd;

                    var createdPayment = this.CreatePayment(apiContext, baseURI + "guid=" + guid, blogId);

                    var links = createdPayment.links.GetEnumerator();
                    string paypalRedirectUrl = null;
                    while (links.MoveNext())
                    {
                        Links Ink = links.Current;
                        if (Ink.rel.ToLower().Trim().Equals("approval_url"))
                        {
                            //saving the payapalredirect URL to which user will be redirected for payment
                            paypalRedirectUrl = Ink.href;
                        }
                    }
                    // saving the paymentID in the key guid
                    httpContextAccessor.HttpContext.Session.SetString("payment", createdPayment.id);
                    return Redirect(paypalRedirectUrl);
                }
                else
                {
                    var paymentId = httpContextAccessor.HttpContext.Session.GetString("payment");
                    var executedPayment = ExecutePayment(apiContext, payerId, paymentId as string);
                    //If executed payment failed then we will show payment failure message to user
                    if (executedPayment.state.ToLower() != "approved")
                    {
                        return View("PaymentFailed");
                    }
                    //var blogIds = executedPayment.transactions[0].item_list.items[0].sku;
                    return RedirectToAction("SaveBookingToDb", "Booking");
                }
            }
            catch (Exception ex)
            {
                return View("PaymentFailed");
            }
            return View("SuccessView");
        }

        private PayPal.Api.Payment ExecutePayment(APIContext apiContext, string payerId, string paymentId)
        {
            var paymentExecution = new PaymentExecution()
            {
                payer_id = payerId
            };
            this.payment = new PayPal.Api.Payment()
            {
                id = paymentId
            };
            return this.payment.Execute(apiContext, paymentExecution);
        }


        private PayPal.Api.Payment CreatePayment(APIContext apiContext, string redirectUrl, string blogId)
        {
            var quantity = int.Parse(httpContextAccessor.HttpContext.Session.GetString("quantity"));
            var itemList = new ItemList()
            {
                items = new List<Item>()
            };
            {
                itemList.items.Add(new Item()
                {
                    name = "Book Time Slot Fixed",
                    currency = "USD",
                    price = "10.00",
                    quantity = quantity.ToString(),
                    sku = "BOOK-TS-101"
                });
                var payer = new Payer()
                {
                    payment_method = "paypal"
                };

                var redirUrls = new RedirectUrls()
                {
                    cancel_url = redirectUrl + "&Cancel=true",
                    return_url = redirectUrl
                };

                var details = new Details()
                {
                    tax = "1",
                    shipping = "1",
                    subtotal = (quantity * 10.00D).ToString()
                };

                var amount = new Amount()
                {
                    currency = "USD",
                    total = ((quantity * 10.00D)+2.00D).ToString(), // Total must be equal to sum of tax, shipping and subto //
                    details = details
                };

                var transactionList = new List<Transaction>();
                // Adding description about the transaction

                transactionList.Add(new Transaction()
                {
                    description = "Transaction description",
                    invoice_number = Guid.NewGuid().ToString(), //Generate an Invoice No
                    amount = amount,
                    item_list = itemList

                });

                this.payment = new PayPal.Api.Payment()
                {
                    intent = "sale",
                    payer = payer,
                    transactions = transactionList,
                    redirect_urls = redirUrls
                };
                return this.payment.Create(apiContext);
            }
        }

        public IActionResult PaymentSuccess()
        {
            return View();
        }
        public IActionResult Invoice()
        {
            InvoiceViewModel invoiceViewModel = null;
            try
            {
                using var context = new DemobadmintonContext();

                // Retrieve BId from session
                var bId = httpContextAccessor.HttpContext.Session.GetInt32("BId");
                if (bId == null)
                {
                    throw new Exception("BId not found in session.");
                }

                // Fetch payment using BId
                string formattedDate = null;
                string formattedTime = null;
                BadmintonBooking.Models.Payment? payment = context.Payments.FirstOrDefault(p => p.BId == bId.Value);
                if (payment != null)
                {
                    formattedDate = payment.PDateTime.ToString("MMMM dd, yyyy");
                    formattedTime = payment.PDateTime.ToString("hh:mm tt"); // Format time as HH:MM AM/PM
                    ViewData["formattedDate"] = formattedDate;
                    ViewData["formattedTime"] = formattedTime;
                }
                var booking = context.Bookings.FirstOrDefault(b => b.BId == bId.Value);
                string typeOfBooking = booking.BBookingType;
                string courtName = context.Courts.FirstOrDefault(c => c.CoId == booking.CoId).CoName;
                invoiceViewModel = new InvoiceViewModel()
                {
                    PId = payment.PId,
                    formattedDate = formattedDate,
                    formattedTime = formattedTime,
                    toUser = _userManager.GetUserName(User),
                    typeOfBooking = typeOfBooking,
                    courtName = courtName,
                    amount = payment.PAmount
                };

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return View(invoiceViewModel);
        }
    }
}
