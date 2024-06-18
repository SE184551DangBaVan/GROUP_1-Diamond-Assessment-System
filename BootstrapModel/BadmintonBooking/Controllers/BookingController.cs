﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BadmintonBooking.Models;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Globalization;

namespace BadmintonBooking.Controllers
{
    public class BookingController : Controller
    {
        private readonly DemobadmintonContext _demobadmintonContext;
        private static List<TimeSlot> _slots = new List<TimeSlot>();
        public BookingController(DemobadmintonContext demobadmintonContext)
        {
            _demobadmintonContext = demobadmintonContext;
        }
        [HttpGet]
        public async Task<IActionResult> GetBookSlots()
        {
            var booked = await _demobadmintonContext.TimeSlots.ToListAsync();
            return Ok(booked);
        }
        [HttpPost]
        public IActionResult UpdateBooking([FromBody] BookingData bookingData)
        {
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
                    UserId = TempData["UserID"].ToString(),
                    BBookingType = "Casual",
                    CoId = int.Parse(TempData["CoId"].ToString()),
                };
                TimeSlot slot = new TimeSlot()
                {
                    CoId = int.Parse(TempData["CoId"].ToString()),
                    TsCheckedIn = false,
                    TsDate = date,
                    TsStart = time,
                    TsEnd = time.AddHours(1),
                };
                booking.TimeSlots.Add(slot);
                /*
                 * booking.Payments.add(payment)
                    _demobadmintonContext.Booking.add(booking);
                _demobadmintonContext.SaveChanges();
                    _demobadmintonContext.TimeSlots.add(slot);
                 */
                _demobadmintonContext.Add(booking);
                _demobadmintonContext.SaveChanges();
                //_slots.Add(slot);
                Console.WriteLine(_slots);
                // Here, you can implement the logic to update the database with the booking data.
                // For now, we just print the received data to the console for debugging purposes.
                //SaveBookingsToDatabase().Wait();
                return Ok(new { message = "Booking data received successfully." });
            }
            catch (Exception ex)
            {
                // Log the exception for debugging purposes
                Console.WriteLine($"Error processing booking: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpPost]
        public async Task<IActionResult> SaveBookingsToDatabase()
        {
            try
            {
                foreach (var slot in _slots)
                {
                    _demobadmintonContext.TimeSlots.Add(slot);
                }

                await _demobadmintonContext.SaveChangesAsync();
                _slots.Clear(); // Clear the list after saving to avoid duplicate entries

                return Ok(new { message = "All bookings saved to database successfully." });
            }
            catch (Exception ex)
            {
                // Log the exception for debugging purposes
                Console.WriteLine($"Error saving bookings to database: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
