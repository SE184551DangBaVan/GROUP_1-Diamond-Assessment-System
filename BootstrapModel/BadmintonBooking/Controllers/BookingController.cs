﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BadmintonBooking.Models;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Globalization;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Net.NetworkInformation;
using Microsoft.AspNetCore.Identity;


namespace BadmintonBooking.Controllers
{
    public class BookingController : Controller
    {
        private readonly DemobadmintonContext _demobadmintonContext;
        private static List<TimeSlot> _slots = new List<TimeSlot>();
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<IdentityUser> _UserManager;
        public BookingController(DemobadmintonContext demobadmintonContext, IHttpContextAccessor httpContextAccessor, UserManager<IdentityUser> userManager)
        {
            _demobadmintonContext = demobadmintonContext;
            _httpContextAccessor = httpContextAccessor;
            _UserManager = userManager;
        }
        [HttpGet]
        public async Task<IActionResult> GetBookSlots()
        {
            var booked = await _demobadmintonContext.TimeSlots.ToListAsync();
            return Ok(booked);
        }
        
        [HttpPost]
        public async Task<IActionResult> Cancel()
        {
            _httpContextAccessor.HttpContext.Session.Remove("Booking");
            return Ok(new { message = "Cancel successfully" });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBooking([FromBody] BookingData bookingData)
        {
            string userId = _UserManager.GetUserId(User);

            // Check if user ID is found
            if (userId == null)
            {
                return Unauthorized(new { message = "User is not authenticated. Redirecting to login." });
            }
            try
            {
                if (bookingData == null)
                {
                    return BadRequest("Invalid booking data.");
                }

                TimeOnly time = TimeOnly.ParseExact(bookingData.Time, "h:mm tt", CultureInfo.InvariantCulture);
                DateOnly date = DateOnly.ParseExact(bookingData.Date, "MMM d", CultureInfo.InvariantCulture);
                bool booked = bookingData.Booked;

                Console.WriteLine($"Parsed Booking Data - Time: {time}, Date: {date}, Booked: {booked}");
                Booking booking = new Booking()
                {
                    UserId = userId,
                    BBookingType = _httpContextAccessor.HttpContext.Session.GetString("Types"),
                    CoId = int.Parse(_httpContextAccessor.HttpContext.Session.GetString("CoId")),
                    BGuestName = _UserManager.GetUserName(User)
                };
                TimeSlot slot = new TimeSlot()
                {
                    CoId = int.Parse(_httpContextAccessor.HttpContext.Session.GetString("CoId")),
                    TsCheckedIn = false,
                    TsDate = date,
                    TsStart = time,
                    TsEnd = time.AddHours(1),
                };

                booking.TimeSlots.Add(slot);
                Payment payment = new Payment()
                {
                    PDateTime = DateTime.Now,
                    PAmount = 50000000
                };
                booking.Payments.Add(payment);
                int quantity = booking.TimeSlots.Count;
                _httpContextAccessor.HttpContext.Session.SetString("quantity", quantity.ToString());
                var jsonString = JsonConvert.SerializeObject(booking);
                _httpContextAccessor.HttpContext.Session.SetString("Booking", jsonString);
                Console.WriteLine(_slots);
                return Ok(new { message = "Booking data received successfully." });
            }

            catch (Exception ex)
            {
                // Log the exception for debugging purposes
                Console.WriteLine($"Error processing booking: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpGet]
        public async Task<IActionResult> SaveBookingToDb()
        {
            try
            {
                //var booking = TempData["Booking"] as Booking;
                var jsonString = _httpContextAccessor.HttpContext.Session.GetString("Booking");
                if (string.IsNullOrEmpty(jsonString))
                {
                    return null;
                }

                var booking = JsonConvert.DeserializeObject<Booking>(jsonString);
                if (booking == null)
                {
                    return BadRequest("Booking data is missing.");
                }

                var saveResult = await SaveBookingsToDatabase(booking);
                if (!saveResult)
                {
                    return StatusCode(500, "Failed to save booking to database.");
                }

                return RedirectToAction("PaymentSuccess", "Paypal");
            }
            catch (Exception ex)
            {
                // Log the exception for debugging purposes
                Console.WriteLine($"Error saving booking to database: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpPost]
        public async Task<bool> SaveBookingsToDatabase(Booking booking)
        {
            try
            {
                await _demobadmintonContext.AddAsync(booking);
                await _demobadmintonContext.SaveChangesAsync();
                _httpContextAccessor.HttpContext.Session.Remove("Booking");

                return true;
            }
            catch (Exception ex)
            {
                // Log the exception for debugging purposes
                Console.WriteLine($"Error saving bookings to database: {ex.Message}");
                return false;
            }
        }
    }
}
