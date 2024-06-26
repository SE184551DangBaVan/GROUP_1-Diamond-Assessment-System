﻿using BadmintonBooking.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;

namespace BadmintonBooking.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IWebHostEnvironment environment;

        public AdminController(IWebHostEnvironment environment)
        {
            this.environment = environment;
        }

        public IActionResult Show(int page = 1, string sortOrder = "")
        {
            DemobadmintonContext context = new DemobadmintonContext();
            var courtlist = context.Courts.Where(c => c.UserId == "1").ToList();
            //sort
            ViewBag.SortOrder = sortOrder;

            switch (sortOrder)
            {
                case "name_asc":
                    courtlist = courtlist.OrderBy(c => c.CoName).ToList();
                    break;
                case "name_desc":
                    courtlist = courtlist.OrderByDescending(c => c.CoName).ToList();
                    break;
                case "price_asc":
                    courtlist = courtlist.OrderBy(c => c.CoPrice).ToList();
                    break;
                case "price_desc":
                    courtlist = courtlist.OrderByDescending(c => c.CoPrice).ToList();
                    break;
                default:
                    courtlist = courtlist.OrderBy(c => c.CoId).ToList();
                    break;
            }

            // paging
            int NoOfRecordPerPage = 5;
            int NoOfPages = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(courtlist.Count) / Convert.ToDouble(NoOfRecordPerPage)));
            int NoOfRecordToSkip = (page - 1) * NoOfRecordPerPage;
            ViewBag.Page = page;
            ViewBag.NoOfPages = NoOfPages;
            courtlist = courtlist.Skip(NoOfRecordToSkip).Take(NoOfRecordPerPage).ToList();

            return View(courtlist);
        }
        public IActionResult AddCourt()
        {
            return View();
        }
        [HttpPost]
        public IActionResult AddCourt(Court model)
        {
            try
            {
                ModelState.Remove("UserId");
                ModelState.Remove("User");
                ModelState.Remove("ImagePath");
                if (ModelState.IsValid)
                {


                    DemobadmintonContext context = new DemobadmintonContext();

                    string uniqueFileName = UploadImage(model);
                    var data = new Court()
                    {
                        CoName = model.CoName,
                        CoAddress = model.CoAddress,
                        CoInfo = model.CoInfo,
                        CoPrice = model.CoPrice,
                        UserId = "1",
                        CoStatus = true,
                        CoPath = uniqueFileName,
                    };
                    context.Courts.Add(data);
                    context.SaveChanges();
                    TempData["Success"] = "Record saved successfully";


                    return RedirectToAction("Show");

                }
                ModelState.AddModelError(string.Empty, "Please check all fields again");

            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            return View(model);
        }
        public IActionResult DetailCourt(int id)
        {
            DemobadmintonContext context = new DemobadmintonContext();
            var data = context.Courts.FirstOrDefault(c => c.CoId == id);
            if (data == null)
            {
                return NotFound();
            }

            return View(data);


        }
        public IActionResult EditCourt(int id)
        {
            DemobadmintonContext context = new DemobadmintonContext();
            var data = context.Courts.FirstOrDefault(c => c.CoId == id);
            if (data != null)
            {
                return View(data);
            }
            else
            {
                return RedirectToAction("Show", "Admin");
            }





        }
        [HttpPost]
        public IActionResult EditCourt(Court model)
        {
            DemobadmintonContext context = new DemobadmintonContext();
            var data = context.Courts.FirstOrDefault(c => c.CoId == model.CoId);
            try
            {
                ModelState.Remove("UserId");
                ModelState.Remove("User");
                ModelState.Remove("ImagePath");
                if (ModelState.IsValid)
                {

                    string uniqueFileName = string.Empty;
                    if (model.ImagePath != null)
                    {
                        if (data.CoPath != null)
                        {
                            string filepath = Path.Combine(environment.WebRootPath, "Upload/Image", data.CoPath);
                            if (System.IO.File.Exists(filepath))
                            {
                                System.IO.File.Delete(filepath);

                            }
                        }
                        uniqueFileName = UploadImage(model);
                    }
                    data.CoId = model.CoId;
                    data.CoName = model.CoName;
                    data.CoAddress = model.CoAddress;
                    data.CoInfo = model.CoInfo;
                    data.CoPrice = model.CoPrice;
                    data.CoStatus = true;
                    data.UserId = "1";

                    if (model.ImagePath != null)
                    {
                        data.CoPath = uniqueFileName;
                    }
                    context.Courts.Update(data);
                    context.SaveChanges();
                    TempData["Success"] = "Court Updated Successfully";
                }
                else
                {
                    return View(model);
                }


            }

            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            return RedirectToAction("Show", "Admin");
        }
        public IActionResult DeleteCourt(int id)
        {
            DemobadmintonContext context = new DemobadmintonContext();
            var data = context.Courts.FirstOrDefault(c => c.CoId == id);
            if (data == null || id == 0)
            {
                return NotFound();
            }
            else
            {
                data.CoStatus = false;
                context.Courts.Update(data);
                context.SaveChanges();
                TempData["Success"] = "Court Deleted Successfully";
            }
            return RedirectToAction("Show", "Admin");
        }






        private string UploadImage(Court model)
        {
            string uniqueFileName = string.Empty;
            if (model.ImagePath != null)
            {
                string uploadFolder = Path.Combine(environment.WebRootPath, "Upload/Image/");
                uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImagePath.FileName;
                string filePath = Path.Combine(uploadFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    model.ImagePath.CopyTo(fileStream);
                }



            }
            return uniqueFileName;
        }
        public IActionResult CustomerInfo()
        {
            DemobadmintonContext context = new DemobadmintonContext();
            var data = context.AspNetUsers.ToList();
            return View(data);
        }
       // public IActionResult OverView()
        //{
          //  return View();
        //}

    }
}
