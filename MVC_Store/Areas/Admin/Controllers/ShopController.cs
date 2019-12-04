﻿using MVC_Store.Models.Data;
using MVC_Store.Models.ViewModels;
using MVC_Store.Models.ViewModels.Shop;
using PagedList;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace MVC_Store.Areas.Admin.Controllers
{
    public class ShopController : Controller
    {
        public ActionResult Categories()
        {
            List<CategoryViewModel> categoryVMList;
            using (Db db = new Db())
            {
                categoryVMList = db.Categories.ToArray().OrderBy(x => x.Sorting).Select(x => new CategoryViewModel(x)).ToList();
            }
            return View(categoryVMList);
        }

        [HttpPost]
        public string AddNewCategory(string catName)
        {
            string id;
            using (Db db = new Db())
            {
                if (db.Categories.Any(x => x.Name == catName))
                {
                    return "titletaken";
                }

                CategoryDTO categoryDTO = new CategoryDTO();
                categoryDTO.Name = catName;
                categoryDTO.Slug = catName.Replace(" ", "-").ToLower();
                categoryDTO.Sorting = 100;

                db.Categories.Add(categoryDTO);
                db.SaveChanges();

                id = categoryDTO.Id.ToString();
            }
            return id;
        }

        public ActionResult DeleteCategory(int id)
        {
            using (Db db = new Db())
            {
                CategoryDTO categoryDTO = db.Categories.Find(id);
                if (categoryDTO == null)
                {
                    return Content("This category does not exist");
                }
                db.Categories.Remove(categoryDTO);
                db.SaveChanges();
            }
            TempData["DM"] = "You have deleted the category";
            return RedirectToAction("Categories");
        }

        [HttpPost]
        public string RenameCategory(string newCatName, int id)
        {
            using (Db db = new Db())
            {
                if (db.Categories.Any(x => x.Name == newCatName))
                {
                    return "titletaken";
                }

                CategoryDTO categoryDTO = db.Categories.Find(id);
                categoryDTO.Name = newCatName;
                categoryDTO.Slug = newCatName.Replace(" ", "-").ToLower();
                db.SaveChanges();
            }
            return "Fine";
        }

        [HttpGet]
        public ActionResult AddProduct()
        {
            ProductViewModel productViewModel = new ProductViewModel();
            using (Db db = new Db())
            {
                productViewModel.Categories = new SelectList(db.Categories.ToList(), dataValueField: "Id", dataTextField: "Name");
            }
            return View(productViewModel);
        }

        [HttpPost]
        public async Task<ActionResult> AddProduct(ProductViewModel productViewModel, HttpPostedFileBase file)
        {
            if (!ModelState.IsValid)
            {
                using (Db db = new Db())
                {
                    productViewModel.Categories = new SelectList(await db.Categories.ToListAsync(), "Id", "Name");
                    return View(productViewModel);
                }
            }

            using (Db db = new Db())
            {
                if (db.Products.Any(x => x.Name == productViewModel.Name))
                {
                    productViewModel.Categories = new SelectList(await db.Categories.ToListAsync(), "Id", "Name");
                    ModelState.AddModelError("", "That product name is taken");
                    return View(productViewModel);
                }
            }
            int id;

            using (Db db = new Db())
            {
                ProductDTO productDTO = new ProductDTO();
                productDTO.Name = productViewModel.Name;
                productDTO.Slug = productViewModel.Name.Replace(" ", "-").ToLower();
                productDTO.Description = productViewModel.Description;
                productDTO.Price = productViewModel.Price;
                productDTO.CategoryId = productViewModel.CategoryId;

                CategoryDTO categoryDTO = await db.Categories.FirstOrDefaultAsync(x => x.Id == productViewModel.CategoryId);
                productDTO.CategoryName = categoryDTO.Name;

                db.Products.Add(productDTO);

                await db.SaveChangesAsync();

                id = productDTO.Id;
            }

            //Работа с изображениями
            DirectoryInfo originalDirectory = new DirectoryInfo(string.Format($"{Server.MapPath(@"\")}Images\\Uploads"));
            string path1 = Path.Combine(originalDirectory.FullName, "Products");
            string path2 = Path.Combine(originalDirectory.FullName, "Products\\" + id.ToString());
            string path3 = Path.Combine(originalDirectory.FullName, "Products\\" + id.ToString() + "\\Thumbs");
            string path4 = Path.Combine(originalDirectory.FullName, "Products\\" + id.ToString() + "\\Gallery");
            string path5 = Path.Combine(originalDirectory.FullName, "Products\\" + id.ToString() + "\\Gallery\\Thumbs");

            if (!Directory.Exists(path1))
                Directory.CreateDirectory(path1);

            if (!Directory.Exists(path2))
                Directory.CreateDirectory(path2);

            if (!Directory.Exists(path3))
                Directory.CreateDirectory(path3);

            if (!Directory.Exists(path4))
                Directory.CreateDirectory(path4);

            if (!Directory.Exists(path5))
                Directory.CreateDirectory(path5);

            if (file != null && file.ContentLength > 0)
            {
                string contentType = file.ContentType.ToLower();

                if (contentType != "image/jpg" &&
                    contentType != "image/jpeg" &&
                    contentType != "image/pjpeg" &&
                    contentType != "image/gif" &&
                    contentType != "image/x-png" &&
                    contentType != "image/png")
                {
                    using (Db db = new Db())
                    {
                        productViewModel.Categories = new SelectList(await db.Categories.ToListAsync(), "Id", "Name");
                        ModelState.AddModelError("", "The image was not uploaded - wrong image extension");
                        return View(productViewModel);
                    }
                }

                string imageName = file.FileName;

                using (Db db = new Db())
                {
                    ProductDTO productDTO = await db.Products.FindAsync(id);
                    productDTO.ImageName = imageName;

                    await db.SaveChangesAsync();
                }
                string imageOrigPath = string.Format($"{path2}\\{imageName}");
                string imageThumbsPath = string.Format($"{path3}\\{imageName}");

                file.SaveAs(imageOrigPath);

                WebImage img = new WebImage(file.InputStream);
                img.Resize(200, 200);
                img.Save(imageThumbsPath);
            }
            TempData["SM"] = "You have successfully added a product.";
            return RedirectToAction("AddProduct");
        }

        public ActionResult Products(int? page, int? categoryId)
        {
            List<ProductViewModel> productVMList;
            int pageNumber = page ?? 1;

            using (Db db = new Db())
            {
                productVMList = db.Products.ToArray()
                    .Where(x => categoryId == null || categoryId == 0 || x.CategoryId == categoryId)
                    .Select(x => new ProductViewModel(x)).ToList();

                ViewBag.Categories = new SelectList(db.Categories.ToList(), "Id", "Name");
                ViewBag.SelectedCategory = (categoryId.ToString());
            }
            var onePageOfProducts = productVMList.ToPagedList(pageNumber, 3);
            ViewBag.OnePageOfProducts = onePageOfProducts;
            return View(productVMList);
        }
    }
}