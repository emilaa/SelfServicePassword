using FuzzyString;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SelfServicePassword.DAL;
using SelfServicePassword.Exceptions;
using SelfServicePassword.Models;
using SelfServicePassword.ViewModels;
using System.DirectoryServices.AccountManagement;

namespace SelfServicePassword.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _appDbContext;
        private readonly IConfiguration _configuration;

        public HomeController(AppDbContext appDbContext, IConfiguration configuration)
        {
            _appDbContext = appDbContext;
            _configuration = configuration;
        }

        public IActionResult CheckPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CheckPassword(UserVM existUser)
        {
            string domain = _configuration.GetSection("ADConnection:Domain").Value;
            string administrator = _configuration.GetSection("ADConnection:Admin").Value;
            string administratorPassword = _appDbContext.Admins.FirstOrDefault().Password;

            try
            {
                if (existUser.Username != null && existUser.CurrentPassword != null)
                {
                    using (var context = new PrincipalContext(ContextType.Domain,
                        domain, administrator, administratorPassword))
                    {
                        UserPrincipal user = UserPrincipal.FindByIdentity(context, existUser.Username);

                        if (user != null)
                        {
                            user.RefreshExpiredPassword();

                            bool isValid = context.ValidateCredentials(existUser.Username, existUser.CurrentPassword);

                            if (isValid)
                            {
                                HttpContext.Session.SetString("Displayname", user.Name);
                                HttpContext.Session.SetString("Username", user.SamAccountName);
                                return RedirectToAction(nameof(ChangePassword));
                            }
                            else
                            {
                                ViewBag.UsernameOrPasswordError = "İstifadəçi adı və ya parol yanlışdır!";
                            }
                        }
                        else
                        {
                            ViewBag.UserError = "İstifadəçi adı və ya parol yanlışdır!";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is PrincipalServerDownException)
                {
                    CustomException exception = new("Serverlə əlaqə qurmaq mümkün olmadı.", ex);
                    ViewBag.Error = exception.Message;
                }
            }

            return View();
        }

        public IActionResult ChangePassword()
        {
            ViewBag.Displayname = HttpContext.Session.GetString("Displayname");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(UserVM existUser)
        {
            string domain = _configuration.GetSection("ADConnection:Domain").Value;
            string administrator = _configuration.GetSection("ADConnection:Admin").Value;
            string administratorPassword = _appDbContext.Admins.FirstOrDefault().Password;
            string? username = HttpContext.Session.GetString("Username");
            string? displayname = HttpContext.Session.GetString("Displayname");

            try
            {
                ViewBag.Displayname = displayname;

                if (existUser.NewPassword != null && existUser.ConfirmPassword != null)
                {
                    if (existUser.NewPassword != existUser.ConfirmPassword)
                    {
                        ViewBag.CheckPassword = "Yeni parolunuz ilə uyğun deyil.";
                        ViewBag.Displayname = displayname;
                        return View();
                    }

                    if (username.JaroWinklerDistance(existUser.NewPassword) >= 0.2 ||
                        displayname.JaroWinklerDistance(existUser.NewPassword) >= 0.2)
                    {
                        ViewBag.PasswordError = "Parol istifadəçi adına və ya ad, soyada bənzər olmamalıdır!";
                        ViewBag.Displayname = displayname;
                        return View();
                    }

                    using (var context = new PrincipalContext(ContextType.Domain,
                        domain, administrator, administratorPassword))
                    {
                        UserPrincipal user = UserPrincipal.FindByIdentity(context, username);

                        if (user != null)
                        {
                            user.SetPassword(existUser.NewPassword);
                            user.Save();
                            user.SetPassword(existUser.NewPassword);
                            user.Save();

                            ViewBag.Success = "Parolunuz müvəffəqiyyətlə dəyişdirildi!";
                            ViewBag.Displayname = displayname;

                            if (username == "Administrator")
                            {
                                Admin admin = await _appDbContext.Admins.FirstOrDefaultAsync();
                                admin.Password = existUser.NewPassword;
                                _appDbContext.Admins.Update(admin);
                                await _appDbContext.SaveChangesAsync();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is PasswordException)
                {
                    CustomException exception = new("Parolun minimum uzunluğu 7 olmalıdır. Böyük və kiçik hərf, rəqəm istifadə olunmalıdır.", ex);
                    ViewBag.Error = exception.Message;
                }

                ViewBag.Displayname = displayname;
            }

            return View();
        }
    }
}